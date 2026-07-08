using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Godot;
using GodotArray = Godot.Collections.Array;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

public static class VDFParser
{
    private static readonly Regex PropRegex = new(
        @"^""?(.*?)?""?\s+""?(.*?)?""?(?:$|(\s\[.+\]))$", RegexOptions.Compiled);
    private static readonly Regex VectorRegex = new(
        @"^([-\d\.e]+)\s([-\d\.e]+)\s([-\d\.e]+)$", RegexOptions.Compiled);
    private static readonly Regex ColorRegex = new(
        @"^([-\d\.e]+)\s([-\d\.e]+)\s([-\d\.e]+)\s([-\d\.e]+)$", RegexOptions.Compiled);
    private static readonly Regex UvRegex = new(
        @"\[([-\d\.e]+)\s([-\d\.e]+)\s([-\d\.e]+)\s([-\d\.e]+)\]\s([-\d\.e]+)", RegexOptions.Compiled);
    private static readonly Regex PlaneRegex = new(
        @"\(([\d\-\.e]+\s[\d\-\.e]+\s[\d\-\.e]+)\)\s?\(([\d\-\.e]+\s[\d\-\.e]+\s[\d\-\.e]+)\)\s?\(([\d\-\.e]+\s[\d\-\.e]+\s[\d\-\.e]+)\)",
        RegexOptions.Compiled);

    public static GodotDict? Parse(string filePath, bool keysToLower = false)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            GD.PushError("ValveFormatParser: No file path provided");
            return null;
        }
        if (!FileAccess.FileExists(filePath))
        {
            GD.PushError("ValveFormatParser: File does not exist: " + filePath);
            return null;
        }
        using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        return ParseFromString(file.GetAsText(), keysToLower);
    }

    public static Variant ParseValue(string line)
    {
        if (line.Length == 0) return line;

        // Short-circuit: no spaces means it can only be a number or plain string.
        // Avoids running any regex for the most common value types.
        if (line.IndexOf(' ') < 0)
        {
            if (long.TryParse(line, out long intVal)) return intVal;
            if (float.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatVal)) return floatVal;
            return line;
        }

        // Dispatch to the one regex that can possibly match based on delimiter characters.
        if (line.IndexOf('(') >= 0)
        {
            var m = PlaneRegex.Match(line);
            if (m.Success)
            {
                var points = new GodotArray();
                for (int i = 1; i <= 3; i++)
                {
                    var parts = m.Groups[i].Value.Split(' ');
                    points.Add(new Vector3(
                        float.Parse(parts[0], CultureInfo.InvariantCulture),
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture)));
                }
                var p0 = points[0].AsVector3();
                var p1 = points[1].AsVector3();
                var p2 = points[2].AsVector3();
                return new GodotDict
                {
                    ["value"] = new Plane(p0, p1, p2),
                    ["points"] = points,
                    ["vecsum"] = p0 + p1 + p2,
                };
            }
        }
        else if (line.IndexOf('[') >= 0)
        {
            var m = UvRegex.Match(line);
            if (m.Success)
                return new GodotDict
                {
                    ["x"] = F(m, 1),
                    ["y"] = F(m, 2),
                    ["z"] = F(m, 3),
                    ["shift"] = F(m, 4),
                    ["scale"] = F(m, 5),
                };
        }
        else
        {
            var m = VectorRegex.Match(line);
            if (m.Success) return new Vector3(F(m, 1), F(m, 2), F(m, 3));
            m = ColorRegex.Match(line);
            if (m.Success) return new Color(F(m, 1) / 255f, F(m, 2) / 255f, F(m, 3) / 255f, F(m, 4) / 255f);
        }

        return line;
    }

    private static float F(Match m, int g) =>
        float.Parse(m.Groups[g].Value, CultureInfo.InvariantCulture);

    private static void DefineStructure(List<GodotDict> hierarchy, string line, bool keysToLower)
    {
        // Strip surrounding quotes, braces, and whitespace without allocating intermediate strings.
        int start = 0, end = line.Length;
        while (start < end && (line[start] == '"' || line[start] == '{' || char.IsWhiteSpace(line[start]))) start++;
        while (end > start && (line[end - 1] == '"' || line[end - 1] == '{' || char.IsWhiteSpace(line[end - 1]))) end--;
        var name = line.Substring(start, end - start);
        if (keysToLower) name = name.ToLower();

        var newStruct = new GodotDict();
        var current = hierarchy[^1];

        if (current.TryGetValue(name, out var existing))
        {
            if (existing.VariantType == Variant.Type.Array)
            {
                var arr = existing.AsGodotArray();
                arr.Add(newStruct);
                current[name] = arr;
            }
            else
            {
                current[name] = new GodotArray { existing, newStruct };
            }
        }
        else
        {
            current[name] = newStruct;
        }
        hierarchy.Add(newStruct);
    }

    private static void DefineProperty(List<GodotDict> hierarchy, string line, bool keysToLower)
    {
        var m = PropRegex.Match(line);
        if (!m.Success) return;

        var propName = m.Groups[1].Value;
        if (keysToLower) propName = propName.ToLower();
        var propValue = ParseValue(m.Groups[2].Value);

        var dict = hierarchy[^1];
        if (dict.TryGetValue(propName, out var existing))
        {
            if (existing.VariantType == Variant.Type.Array)
            {
                var arr = existing.AsGodotArray();
                arr.Add(propValue);
                dict[propName] = arr;
            }
            else
            {
                dict[propName] = new GodotArray { existing, propValue };
            }
        }
        else
        {
            dict[propName] = propValue;
        }
    }

    // Strips trailing // comments (requires preceding whitespace, matching original behaviour).
    // Avoids a regex call on every line.
    private static string StripTrailingComment(string line)
    {
        int i = line.IndexOf("//", StringComparison.Ordinal);
        if (i > 0 && char.IsWhiteSpace(line[i - 1]))
            return line[..i].TrimEnd();
        return line;
    }

    public static GodotDict ParseFromString(string source, bool keysToLower = false)
    {
        var output = new GodotDict();
        var hierarchy = new List<GodotDict> { output };
        string previousLine = "";

        // StringReader avoids allocating the full line array that Split('\n') would produce.
        using var reader = new System.IO.StringReader(source);
        while (reader.ReadLine() is string rawLine)
        {
            var line = StripTrailingComment(rawLine.Trim());
            if (line.Length == 0 || line.StartsWith("//")) continue;

            if (line[0] == '{')
                DefineStructure(hierarchy, previousLine, keysToLower);
            else if (line[^1] == '{')
                DefineStructure(hierarchy, line, keysToLower);
            else if (line[0] == '}' || line[^1] == '}')
            {
                if (hierarchy.Count > 1)
                    hierarchy.RemoveAt(hierarchy.Count - 1);
            }
            else
                // DefineProperty calls PropRegex.Match internally and bails on no-match,
                // so the redundant IsMatch check is removed.
                DefineProperty(hierarchy, line, keysToLower);

            previousLine = line;
        }
        return output;
    }
}
