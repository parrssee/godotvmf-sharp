using System.Collections.Generic;
using Godot;

namespace GodotVMF;

/// <summary>
/// C# port of Source's WorldVertexTransition shader material, used for VMT-driven
/// displacement blend textures (two albedo/normal/metallic/roughness layers blended
/// via per-vertex color).
/// </summary>
/// <remarks>
/// NOTE: properties are exposed via _Set/_Get instead of [Export] because Godot's
/// auto-derived export names insert an underscore before trailing digits
/// (AlbedoTexture2 -> "albedo_texture_2"), which does not match the "albedo_texture2"
/// style names VMTTransformer sets by string (matching the shader uniforms 1:1).
/// </remarks>
[Tool]
public partial class WorldVertexTransitionMaterial : ShaderMaterial
{
    private static readonly Dictionary<string, string> _propertyToShaderParam = new(System.StringComparer.OrdinalIgnoreCase)
    {
        ["blend_modulate_texture"] = "blend_modulate_texture",
        ["convert_to_srgb"] = "convert_to_srgb",
        ["albedo_texture"] = "albedo_texture",
        ["albedo_texture2"] = "albedo_texture2",
        ["normal_texture"] = "normal_texture",
        ["normal_texture2"] = "normal_texture2",
        ["displacement_texture"] = "displacement_texture",
        ["displacement_texture2"] = "displacement_texture2",
        ["metallic"] = "metallic",
        ["specular"] = "specular",
        ["metallic_texture"] = "metallic_texture",
        ["metallic_texture2"] = "metallic_texture2",
        ["roughness"] = "roughness",
        ["roughness2"] = "roughness2",
        ["roughness_texture1"] = "roughness_texture",
        ["roughness_texture2"] = "roughness_texture2"
    };

    public override bool _Set(StringName property, Variant value)
    {
        if (!_propertyToShaderParam.TryGetValue(property.ToString(), out var shaderParam))
            return false;

        SetShaderParameter(shaderParam, value);
        EmitChanged();
        return true;
    }

    public override Variant _Get(StringName property)
    {
        if (!_propertyToShaderParam.TryGetValue(property.ToString(), out var shaderParam))
            return default;

        return GetShaderParameter(shaderParam);
    }

    public WorldVertexTransitionMaterial()
    {
        Shader = ResourceLoader.Load<Shader>("res://addons/godotvmf/shaders/WorldVertexTransitionMaterial.gdshader");
    }
}
