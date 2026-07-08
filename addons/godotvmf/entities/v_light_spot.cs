using Godot;

namespace GodotVMF;

[Tool]
public partial class v_light_spot : v_light
{
    public override void EntitySetup(VMFEntity entity)
    {
        base.EntitySetup(entity);
        if (LightNode == null) return;

        var entityData = new LightType(entity);
        var spotLight = LightNode as SpotLight3D;
        if (spotLight == null) return;

        float scale = VMFConfig.Import.Scale;
        float radius = (1f / scale) * Mathf.Sqrt(LightNode.LightEnergy * scale);
        float attenuation = 1.44f;

        spotLight.SpotAngle = entityData.Cone;
        spotLight.LightEnergy = entityData.Light.VariantType == Variant.Type.Color
            ? entityData.Light.As<Color>().A
            : LightNode.LightEnergy;

        spotLight.SpotRange = radius;
        spotLight.SpotAngleAttenuation = attenuation;

        float defaultRange = spotLight.SpotRange / scale;
        spotLight.SpotRange = (entity.Data.TryGetValue("distance", out var distV) ? distV.AsSingle() : defaultRange) * scale;

        DefaultLightEnergy = LightNode.LightEnergy;
        entity.Angles = new Vector3(0f, entity.Angles.Y, entityData.Pitch);
        Basis = GetEntityBasis(entity);
        GlobalRotation = new Vector3(GlobalRotation.X, GlobalRotation.Y - Mathf.Pi / 2f, GlobalRotation.Z);
    }
}
