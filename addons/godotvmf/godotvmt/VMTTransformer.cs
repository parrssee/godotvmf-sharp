using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Godot;

namespace GodotVMF;

public class VMTTransformer
{
    private readonly Dictionary<string, System.Action<Material, Variant>> _handlers;

    public VMTTransformer()
    {
        _handlers = new Dictionary<string, System.Action<Material, Variant>>
        {
            { "texturefilter",          Texturefilter },
            { "basetexture",            Basetexture },
            { "basetexture2",           Basetexture2 },
            { "bumpmap",                Bumpmap },
            { "bumpmap2",               Bumpmap2 },
            { "selfillum",              Selfillum },
            { "selfillummask",          Selfillummask },
            { "emissioncolor",          Emissioncolor },
            { "emissionenergy",         Emissionenergy },
            { "emissionoperator",       Emissionoperator },
            { "roughnesstexture",       Roughnesstexture },
            { "roughnesstexture2",      Roughnesstexture2 },
            { "roughnessfactor",        Roughnessfactor },
            { "roughnessfactor2",       Roughnessfactor2 },
            { "metalnesstexture",       Metalnesstexture },
            { "metalnesstexture2",      Metalnesstexture2 },
            { "ambientocclusiontexture",  Ambientocclusiontexture },
            { "ambientocclusiontexture2", Ambientocclusiontexture2 },
            { "bumpmapscale",           Bumpmapscale },
            { "nocull",                 Nocull },
            { "translucent",            Translucent },
            { "alphatest",              Alphatest },
            { "alphatestreference",     Alphatestreference },
            { "nextpass",               Nextpass },
            { "detail",                 Detail },
            { "detailblendmode",        Detailblendmode },
            { "surfaceprop",            Surfaceprop },
            { "basetexturetransform",   Basetexturetransform },
            { "basetexturetransform2",  Basetexturetransform2 },
            { "blendmodulatetexture",   Blendmodulatetexture },
            { "vertexcolor",            Vertexcolor },
            { "envmap",                 Envmap },
            { "envmaptint",             Envmaptint },
        };
    }

    public bool Has(string key) => _handlers.ContainsKey(key);
    public void Call(string key, Material material, Variant value) => _handlers[key](material, value);

    private static void Texturefilter(Material material, Variant value)
    {
        if (material is not BaseMaterial3D bm) return;
        string val = value.AsString().ToUpperInvariant();
        bm.TextureFilter = val switch
        {
            "NEAREST" => BaseMaterial3D.TextureFilterEnum.Nearest,
            "LINEAR" => BaseMaterial3D.TextureFilterEnum.Linear,
            "NEAREST_MIPMAP" => BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps,
            "LINEAR_MIPMAP" => BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
            "NEAREST_MIPMAP_ANISOTROPIC" => BaseMaterial3D.TextureFilterEnum.NearestWithMipmapsAnisotropic,
            "LINEAR_MIPMAP_ANISOTROPIC" => BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
            _ => bm.TextureFilter,
        };
    }

    private static void Basetexture(Material material, Variant value)
        => material.Set("albedo_texture", Variant.From(VTFLoader.GetTexture(value.AsString())));

    private static void Basetexture2(Material material, Variant value)
        => material.Set("albedo_texture2", Variant.From(VTFLoader.GetTexture(value.AsString())));

    private static void Bumpmap(Material material, Variant value)
    {
        material.Set("normal_texture", Variant.From(VTFLoader.GetTexture(value.AsString())));
        if (material is BaseMaterial3D bm) bm.NormalEnabled = true;
        else material.Set("normal_enabled", true);
    }

    private static void Bumpmap2(Material material, Variant value)
        => material.Set("normal_texture2", Variant.From(VTFLoader.GetTexture(value.AsString())));

    private static void Selfillum(Material material, Variant value)
    {
        if (material is BaseMaterial3D bm) bm.EmissionEnabled = value.AsInt32() == 1;
        else material.Set("emission_enabled", value.AsInt32() == 1);
    }

    private static void Selfillummask(Material material, Variant value)
    {
        material.Set("emission_texture", Variant.From(VTFLoader.GetTexture(value.AsString())));
        if (material is BaseMaterial3D bm) bm.EmissionEnabled = true;
        else material.Set("emission_enabled", true);
    }

    private static void Emissioncolor(Material material, Variant value)
        => material.Set("emission", value);

    private static void Emissionenergy(Material material, Variant value)
        => material.Set("emission_energy_multiplier", value);

    private static void Emissionoperator(Material material, Variant value)
        => material.Set("emission_operator", value);

    private static void Roughnesstexture(Material material, Variant value)
        => material.Set("roughness_texture", Variant.From(VTFLoader.GetTexture(value.AsString())));

    private static void Roughnesstexture2(Material material, Variant value)
        => material.Set("roughness_texture2", Variant.From(VTFLoader.GetTexture(value.AsString())));

    private static void Roughnessfactor(Material material, Variant value)
        => material.Set("roughness", value);

    private static void Roughnessfactor2(Material material, Variant value)
        => material.Set("roughness2", value);

    private static void Metalnesstexture(Material material, Variant value)
        => material.Set("metallic_texture", Variant.From(VTFLoader.GetTexture(value.AsString())));

    private static void Metalnesstexture2(Material material, Variant value)
        => material.Set("metallic_texture2", Variant.From(VTFLoader.GetTexture(value.AsString())));

    private static void Ambientocclusiontexture(Material material, Variant value)
    {
        material.Set("ao_texture", Variant.From(VTFLoader.GetTexture(value.AsString())));
        if (material is BaseMaterial3D bm) bm.Set("ao_enabled", true);
    }

    private static void Ambientocclusiontexture2(Material material, Variant value)
    {
        material.Set("ao_texture2", Variant.From(VTFLoader.GetTexture(value.AsString())));
        if (material is BaseMaterial3D bm) bm.Set("ao_enabled", true);
    }

    private static void Bumpmapscale(Material material, Variant value)
        => material.Set("normal_scale", value);

    private static void Nocull(Material material, Variant value)
    {
        bool disabled = value.AsInt32() == 1;
        if (material is BaseMaterial3D bm)
            bm.CullMode = disabled ? BaseMaterial3D.CullModeEnum.Disabled : BaseMaterial3D.CullModeEnum.Back;
        else
            material.Set("cull_mode", disabled ? 0 : 1);
    }

    private static void Translucent(Material material, Variant value)
    {
        if (material is BaseMaterial3D bm)
            bm.Transparency = value.AsInt32() == 1
                ? BaseMaterial3D.TransparencyEnum.Alpha
                : BaseMaterial3D.TransparencyEnum.Disabled;
        else
            material.Set("transparency", value.AsInt32() == 1 ? 1.0f : 0.0f);
    }

    private static void Alphatest(Material material, Variant value)
    {
        if (material is BaseMaterial3D bm)
            bm.Transparency = value.AsInt32() == 1
                ? BaseMaterial3D.TransparencyEnum.AlphaScissor
                : BaseMaterial3D.TransparencyEnum.Disabled;
    }

    private static void Alphatestreference(Material material, Variant value)
    {
        if (material is BaseMaterial3D bm) bm.AlphaScissorThreshold = value.AsSingle();
    }

    private static void Nextpass(Material material, Variant value)
    {
        var shaderMat = VMTShaderBasedMaterial.Load("res://" + value.AsString() + ".gdshader");
        material.NextPass = shaderMat;
    }

    private static void Detail(Material material, Variant value)
    {
        if (material is not BaseMaterial3D bm) return;
        bm.DetailEnabled = true;
        bm.DetailAlbedo = VTFLoader.GetTexture(value.AsString());
    }

    private static void Detailblendmode(Material material, Variant value)
    {
        if (material is BaseMaterial3D bm)
            bm.DetailBlendMode = MapDetailBlendMode(value.AsInt32());
        else
            material.Set("detail_blend_mode", value);
    }

    // Source's $detailblendmode has ~12 modes; Godot's BlendModeEnum only has 4.
    // This is a documented approximation, not a 1:1 mapping.
    private static BaseMaterial3D.BlendModeEnum MapDetailBlendMode(int sourceMode) => sourceMode switch
    {
        0 or 8 => BaseMaterial3D.BlendModeEnum.Mul, // DECAL_MODULATE / MULTIPLY
        1 or 5 or 6 => BaseMaterial3D.BlendModeEnum.Add, // ADDITIVE variants
        _ => BaseMaterial3D.BlendModeEnum.Mix,
    };

    private static void Surfaceprop(Material material, Variant value)
        => material.SetMeta("surfaceprop", value);

    private static void Basetexturetransform(Material material, Variant value)
    {
        var t = VMTLoader.ParseTransform(value.AsString());
        if (t == null) return;
        material.Set("uv1_scale", new Vector3(t.Value.Scale.X, t.Value.Scale.Y, 1f));
        material.Set("uv1_offset", new Vector3(t.Value.Translate.X, t.Value.Translate.Y, 0f));
    }

    private static void Basetexturetransform2(Material material, Variant value)
    {
        var t = VMTLoader.ParseTransform(value.AsString());
        if (t == null) return;
        material.Set("uv1_scale2", new Vector3(t.Value.Scale.X, t.Value.Scale.Y, 1f));
        material.Set("uv1_offset2", new Vector3(t.Value.Translate.X, t.Value.Translate.Y, 0f));
    }

    private static void Vertexcolor(Material material, Variant value)
    {
        if (material is BaseMaterial3D bm) bm.VertexColorUseAsAlbedo = value.AsInt32() == 1;
    }

    private static void Envmap(Material material, Variant value)
    {
        // Approximation: Godot has no reflection-cubemap equivalent to $envmap, so we just
        // enable PBR reflections via metallic. $envmaptint (if present) scales this down.
        if (material is BaseMaterial3D bm) bm.Metallic = 1.0f;
    }

    // Matches the 3-number bracketed vector syntax (e.g. "[ .2 .2 .2 ]") that VDFParser leaves as a
    // raw string, since its UvRegex only matches the 4-number+scale UV-transform syntax.
    private static readonly Regex Bracket3Regex = new(
        @"\[\s*([-\d\.e]+)\s+([-\d\.e]+)\s+([-\d\.e]+)\s*\]", RegexOptions.Compiled);

    private static void Envmaptint(Material material, Variant value)
    {
        if (material is not BaseMaterial3D bm) return;
        float magnitude = value.VariantType switch
        {
            Variant.Type.Vector3 => (value.AsVector3().X + value.AsVector3().Y + value.AsVector3().Z) / 3f,
            Variant.Type.Color => (value.AsColor().R + value.AsColor().G + value.AsColor().B) / 3f,
            Variant.Type.String => Bracket3TintMagnitude(value.AsString()),
            _ => 1.0f,
        };
        bm.Metallic = Mathf.Clamp(magnitude, 0f, 1f);
    }

    private static float Bracket3TintMagnitude(string raw)
    {
        var m = Bracket3Regex.Match(raw);
        if (!m.Success) return 1.0f;
        float x = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        float y = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        float z = float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
        return (x + y + z) / 3f;
    }

    private static void Blendmodulatetexture(Material material, Variant value)
    {
        var texture = VTFLoader.GetTexture(value.AsString());
        if (texture != null)
        {
            var srgbMeta = texture.GetMeta("srgb_conversion_method", Variant.From(0));
            bool processInShader = srgbMeta.AsInt32() == (int)VTFLoader.SRGBConversionMethod.ProcessInShader;
            material.Set("convert_to_srgb", processInShader);
        }
        material.Set("blend_modulate_texture", Variant.From(texture));
    }
}
