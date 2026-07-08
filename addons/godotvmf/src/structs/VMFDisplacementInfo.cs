using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

public class VMFDisplacementInfo
{
    public List<Vector3> Normals = new();
    public List<float> Distances = new();
    public List<Vector3> Offsets = new();
    public List<Vector3> OffsetNormals = new();
    public List<float> Alphas = new();

    public float VertsCount;
    public float EdgesCount;
    public Vector3 StartPoint;
    public VMFSide Side;
    public VMFSolid Brush;
    public Vector3[] Vertices = Array.Empty<Vector3>();
    public float Elevation;

    public VMFDisplacementInfo(GodotDict raw, VMFSide side, VMFSolid brush)
    {
        Side = side;
        Brush = brush;

        var startStr = raw["startposition"].AsString().Trim().TrimStart('[').TrimEnd(']');
        var sp = startStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        StartPoint = new Vector3(
            float.Parse(sp[0], CultureInfo.InvariantCulture),
            float.Parse(sp[1], CultureInfo.InvariantCulture),
            float.Parse(sp[2], CultureInfo.InvariantCulture));

        int power = raw.TryGetValue("power", out var pv) ? pv.AsInt32() : 2;
        VertsCount = Mathf.Pow(2, power) + 1;
        EdgesCount = VertsCount - 1;

        Normals = ParseVectors(raw, "normals");
        Distances = ParseFloats(raw, "distances");
        Offsets = ParseVectors(raw, "offsets");
        OffsetNormals = ParseVectors(raw, "offset_normals");
        Alphas = ParseFloats(raw, "alphas");
        Elevation = raw.TryGetValue("elevation", out var ev) ? ev.AsSingle() : 0f;
    }

    public void CalculateVertices() => Vertices = GetVertices();

    public Vector3 GetNormal(int x, int y)
    {
        int index = y + x * (int)VertsCount;
        return Normals.Count == 0 ? Vector3.Zero : Normals[index];
    }

    public Vector3 GetOffset(int x, int y)
    {
        int index = y + x * (int)VertsCount;
        return Offsets.Count == 0 ? Vector3.Zero : Offsets[index];
    }

    public Vector3 GetDistance(int x, int y)
    {
        int index = y + x * (int)VertsCount;
        return Distances.Count == 0 ? Vector3.Zero : GetNormal(x, y) * Distances[index];
    }

    public Color GetColor(int x, int y)
    {
        int index = y + x * (int)VertsCount;
        if (Alphas.Count == 0) return new Color(1, 0, 0);
        return new Color(Alphas[index] / 255f, 0, 0);
    }

    public Vector3[] GetVertices()
    {
        if (Side.Vertices.Length < 3) return Array.Empty<Vector3>();

        int startIndex = 1;
        for (int vi = 0; vi < Side.Vertices.Length; vi++)
        {
            if (Side.Vertices[vi].DistanceTo(StartPoint) < 0.2f) break;
            startIndex++;
        }

        var tl = Side.Vertices[(0 + startIndex) % 4];
        var tr = Side.Vertices[(1 + startIndex) % 4];
        var br = Side.Vertices[(2 + startIndex) % 4];
        var bl = Side.Vertices[(3 + startIndex) % 4];

        int vc = (int)VertsCount;
        int ec = (int)EdgesCount;
        var res = new List<Vector3>(vc * vc);

        for (int i = 0; i < vc * vc; i++)
        {
            int x = i / vc;
            int y = i % vc;

            float rblend = 1f - (float)x / ec;
            float cblend = (float)y / ec;

            var vl = tl.Lerp(bl, rblend);
            var vr = tr.Lerp(br, rblend);
            var vert = vl.Lerp(vr, cblend);

            vert += GetDistance(x, y) + GetOffset(x, y) + Side.Plane.Normal * Elevation;
            res.Add(vert);
        }
        return res.ToArray();
    }

    private static List<Vector3> ParseVectors(GodotDict raw, string key)
    {
        var result = new List<Vector3>();
        if (!raw.TryGetValue(key, out var val)) return result;

        var dict = val.AsGodotDictionary();
        foreach (var row in dict.Values)
        {
            var parts = row.AsString().Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 2 < parts.Length; i += 3)
            {
                result.Add(new Vector3(
                    float.Parse(parts[i], CultureInfo.InvariantCulture),
                    float.Parse(parts[i + 1], CultureInfo.InvariantCulture),
                    float.Parse(parts[i + 2], CultureInfo.InvariantCulture)));
            }
        }
        return result;
    }

    private static List<float> ParseFloats(GodotDict raw, string key)
    {
        var result = new List<float>();
        if (!raw.TryGetValue(key, out var val)) return result;

        var dict = val.AsGodotDictionary();
        foreach (var row in dict.Values)
        {
            foreach (var part in row.AsString().Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (float.TryParse(part, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float f))
                    result.Add(f);
            }
        }
        return result;
    }
}
