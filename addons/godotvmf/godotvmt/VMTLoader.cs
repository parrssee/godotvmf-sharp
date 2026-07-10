using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using GodotArray = Godot.Collections.Array;

namespace GodotVMF;

public static partial class VMTLoader
{
    public struct TransformData
    {
        public Vector2 Center;
        public Vector2 Scale;
        public float Rotate;
        public Vector2 Translate;
    }

    private static readonly Regex TransformRegex = new(
        @"^""?center\s+([0-9-.]+)\s+([0-9-.]+)\s+scale\s+([0-9-.]+)\s+([0-9-.]+)\s+rotate\s+([0-9-.]+)\s+translate\s+([0-9-.]+)\s+([0-9-.]+)""?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsFileValid(string path)
    {
        string importPath = path + ".import";
        if (!FileAccess.FileExists(importPath)) return false;
        using var file = FileAccess.Open(importPath, FileAccess.ModeFlags.Read);
        if (file == null) return false;
        bool isInvalid = file.GetAsText().Contains("valid=false");
        return !isInvalid;
    }

    public static TransformData? ParseTransform(string transformData)
    {
        var m = TransformRegex.Match(transformData.Trim());
        if (!m.Success) return null;

        float Parse(int g) => float.Parse(m.Groups[g].Value, CultureInfo.InvariantCulture);
        return new TransformData
        {
            Center = new Vector2(Parse(1), Parse(2)),
            Scale = new Vector2(Parse(3), Parse(4)),
            Rotate = Parse(5),
            Translate = new Vector2(Parse(6), Parse(7)),
        };
    }

    public static Material? Load(string path)
    {
        var structure = VDFParser.Parse(path, true);
        if (structure == null || structure.Count == 0) return null;

        string shaderName = structure.Keys.First().AsString().Trim().ToLower();
        var details = structure.Values.First().AsGodotDictionary();

        bool isBlendTexture = shaderName == "worldvertextransition";
        Material material;

        if (details.ContainsKey("insert"))
        {
            var insert = details["insert"].AsGodotDictionary();
            foreach (var k in insert.Keys) details[k] = insert[k];
        }

        if (details.TryGetValue(">=dx90_20b", out var dx90val) && dx90val.VariantType == Variant.Type.Dictionary)
        {
            var dx90 = dx90val.AsGodotDictionary();
            foreach (var k in dx90.Keys) details[k] = dx90[k];
        }

        if (details.TryGetValue("$shader", out var shaderVal))
        {
            string ext = System.IO.Path.GetExtension(shaderVal.AsString()) == "" ? ".gdshader" : "";
            string shaderPath = "res://" + shaderVal.AsString().Replace("res://", "") + ext;

            if (ResourceLoader.Exists(shaderPath))
            {
                var sm = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>(shaderPath) };
                material = sm;
            }
            else
            {
                VMFLogger.Warn($"Shader {shaderPath} doesn't exist for {path}");
                material = new StandardMaterial3D();
            }
        }
        else
        {
            material = isBlendTexture ? new WorldVertexTransitionMaterial() : new StandardMaterial3D();
        }

        if (material is StandardMaterial3D stdMat)
        {
            if (shaderName == "unlitgeneric")
                stdMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            else if (shaderName == "vertexlitgeneric")
                stdMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerVertex;
        }

        var transformer = new VMTTransformer();
        var extendTransformer = (Engine.GetMainLoop() as SceneTree)?.Root?.GetNodeOrNull("VMTExtend");

        Godot.Collections.Array uniforms = new();
        if (material is ShaderMaterial shaderMat2 && shaderMat2.Shader != null)
            uniforms = shaderMat2.Shader.GetShaderUniformList();

        foreach (var keyVar in details.Keys)
        {
            string key = keyVar.AsString();
            var value = details[keyVar];
            bool isCompileKey = key.StartsWith("%");
            key = key.Replace("$", "").Replace("%", "").ToLower();

            if (isCompileKey && value.AsBool() && key != "keywords")
            {
                var compileKeys = material.GetMeta("compile_keys", new GodotArray()).AsGodotArray();
                compileKeys.Add(key);
                material.SetMeta("compile_keys", compileKeys);
            }

            if (material is ShaderMaterial sm && !isBlendTexture)
            {
                int uniformIdx = -1;
                for (int i = 0; i < uniforms.Count; i++)
                {
                    if (uniforms[i].AsGodotDictionary()["name"].AsString() == key) { uniformIdx = i; break; }
                }
                if (uniformIdx == -1) continue;

                var uDict = uniforms[uniformIdx].AsGodotDictionary();
                bool isTexture = uDict["hint_string"].AsString() == "Texture2D";
                bool isBoolean = uDict["type"].AsInt32() == (int)Variant.Type.Bool;
                Variant finalValue = isBoolean ? Variant.From(value.AsString() == "true") : value;
                sm.SetShaderParameter(key, isTexture ? Variant.From(VTFLoader.GetTexture(value.AsString())) : finalValue);
                continue;
            }

            if (extendTransformer != null && extendTransformer.HasMethod(key))
            {
                extendTransformer.Call(key, material, value);
                continue;
            }

            if (transformer.Has(key))
                transformer.Call(key, material, value);
        }

        material.SetMeta("details", details);
        return material;
    }

    public static bool HasMaterial(string material)
    {
        string path = VMFUtils.NormalizePath(VMFConfig.Materials.TargetFolder + "/" + material + ".tres").ToLower();
        if (!ResourceLoader.Exists(path)) path = path.Replace(".tres", ".vmt");
        return ResourceLoader.Exists(path);
    }

    public static Material? GetMaterial(string material)
    {
        var cached = VMFCache.GetCached(material);
        if (cached.VariantType != Variant.Type.Nil)
            return cached.As<Material>();

        string materialPath = VMFUtils.NormalizePath(VMFConfig.Materials.TargetFolder + "/" + material + ".tres").ToLower();
        if (!ResourceLoader.Exists(materialPath))
            materialPath = materialPath.Replace(".tres", ".vmt");

        if (!ResourceLoader.Exists(materialPath))
        {
            if (!VMFCache.IsFileLogged(materialPath))
            {
                VMFLogger.Warn("Material not found: " + materialPath);
                VMFCache.AddLoggedFile(materialPath);
            }

            materialPath = VMFConfig.Materials.FallbackMaterial;
            if (string.IsNullOrEmpty(materialPath) || !ResourceLoader.Exists(materialPath))
                return null;
        }

        var loaded = ResourceLoader.Load<Material>(materialPath);
        if (loaded != null) VMFCache.AddCached(material, Variant.From(loaded));
        return loaded;
    }

    public static Vector2 GetTextureSize(string sideMaterial)
    {
        int defaultSize = VMFConfig.Materials.DefaultTextureSize;
        string cacheKey = "texture_size_" + sideMaterial;

        var cached = VMFCache.GetCached(cacheKey);
        if (cached.VariantType != Variant.Type.Nil)
            return cached.AsVector2();

        var mat = GetMaterial(sideMaterial);
        if (mat == null)
        {
            var def = new Vector2(defaultSize, defaultSize);
            VMFCache.AddCached(cacheKey, Variant.From(def));
            return def;
        }

        Texture2D? texture = null;
        if (mat is BaseMaterial3D bm)
            texture = bm.AlbedoTexture;
        else if (mat is ShaderMaterial sm)
        {
            texture = sm.GetShaderParameter("albedo_texture").As<Texture2D>();
            if (texture == null)
                texture = sm.GetShaderParameter("basetexture").As<Texture2D>();
        }

        var size = texture != null
            ? texture.GetSize()
            : new Vector2(defaultSize, defaultSize);

        VMFCache.AddCached(cacheKey, Variant.From(size));
        return size;
    }
}
