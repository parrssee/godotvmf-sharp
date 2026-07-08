using Godot;

namespace GodotVMF;

public partial class VMTShaderBasedMaterial : ShaderMaterial
{
    private string[] _uniforms = System.Array.Empty<string>();

    private bool Has(string name)
    {
        foreach (var u in _uniforms)
            if (u == name) return true;
        return false;
    }

    public Texture2D? AlbedoTexture
    {
        get => Has("albedo_texture") ? GetShaderParameter("albedo_texture").As<Texture2D>() : null;
        set { if (Has("albedo_texture")) SetShaderParameter("albedo_texture", Variant.From(value)); }
    }

    public float Transparency { get; set; } = 1.0f;
    public int CullMode { get; set; } = 1;
    public bool NormalEnabled { get; set; } = true;

    public Texture2D? NormalTexture
    {
        get => Has("normal_texture") ? GetShaderParameter("normal_texture").As<Texture2D>() : null;
        set { if (Has("normal_texture")) SetShaderParameter("normal_texture", Variant.From(value)); }
    }

    public float NormalScale
    {
        get => Has("normal_scale") ? GetShaderParameter("normal_scale").AsSingle() : 1.0f;
        set { if (Has("normal_scale")) SetShaderParameter("normal_scale", value); }
    }

    public bool DetailEnabled { get; set; } = false;

    public Texture2D? DetailMask
    {
        get => Has("detail_mask") ? GetShaderParameter("detail_mask").As<Texture2D>() : null;
        set { if (Has("detail_mask")) SetShaderParameter("detail_mask", Variant.From(value)); }
    }

    public float Roughness
    {
        get => Has("roughness") ? GetShaderParameter("roughness").AsSingle() : 0.0f;
        set { if (Has("roughness")) SetShaderParameter("roughness", value); }
    }

    public Texture2D? RoughnessTexture
    {
        get => Has("roughness_texture") ? GetShaderParameter("roughness_texture").As<Texture2D>() : null;
        set { if (Has("roughness_texture")) SetShaderParameter("roughness_texture", Variant.From(value)); }
    }

    public Texture2D? MetallicTexture
    {
        get => Has("metallic_texture") ? GetShaderParameter("metallic_texture").As<Texture2D>() : null;
        set { if (Has("metallic_texture")) SetShaderParameter("metallic_texture", Variant.From(value)); }
    }

    public float Metallic
    {
        get => Has("metallic") ? GetShaderParameter("metallic").AsSingle() : 0.0f;
        set { if (Has("metallic")) SetShaderParameter("metallic", value); }
    }

    public float MetallicSpecular
    {
        get => Has("metallic_specular") ? GetShaderParameter("metallic_specular").AsSingle() : 0.0f;
        set { if (Has("metallic_specular")) SetShaderParameter("metallic_specular", value); }
    }

    public bool EmissionEnabled { get; set; } = true;

    public Texture2D? EmissionTexture
    {
        get => Has("emission_texture") ? GetShaderParameter("emission_texture").As<Texture2D>() : null;
        set { if (Has("emission_texture")) SetShaderParameter("emission_texture", Variant.From(value)); }
    }

    public float EmissionEnergy
    {
        get => Has("emission_energy") ? GetShaderParameter("emission_energy").AsSingle() : 1.0f;
        set { if (Has("emission_energy_multiplier")) SetShaderParameter("emission_energy_multiplier", value); }
    }

    public Color Emission
    {
        get => Has("emission") ? GetShaderParameter("emission").As<Color>() : Colors.Black;
        set { if (Has("emission")) SetShaderParameter("emission", Variant.From(value)); }
    }

    public Vector2 Uv1Scale
    {
        get => Has("uv1_scale") ? GetShaderParameter("uv1_scale").AsVector2() : Vector2.One;
        set { if (Has("uv1_scale")) SetShaderParameter("uv1_scale", value); }
    }

    public Vector2 Uv1Offset
    {
        get => Has("uv1_offset") ? GetShaderParameter("uv1_offset").AsVector2() : Vector2.Zero;
        set { if (Has("uv1_offset")) SetShaderParameter("uv1_offset", value); }
    }

    public VMTShaderBasedMaterial Assign(Shader shader)
    {
        Shader = shader;
        var uniformList = shader.GetShaderUniformList();
        _uniforms = new string[uniformList.Count];
        for (int i = 0; i < uniformList.Count; i++)
            _uniforms[i] = uniformList[i].AsGodotDictionary()["name"].AsString();
        return this;
    }

    public static Material Load(string path)
    {
        if (!ResourceLoader.Exists(path))
        {
            GD.Print("VMTShaderBasedMaterial: Shader doesn't exist: " + path);
            return new StandardMaterial3D();
        }

        var shader = ResourceLoader.Load<Shader>(path);
        if (shader != null)
            return new VMTShaderBasedMaterial().Assign(shader);

        return new StandardMaterial3D();
    }
}
