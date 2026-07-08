using Godot;

namespace GodotVMF;

[Tool]
public partial class point_teleport : VMFEntityNode
{
    public void Teleport(Variant _param = default)
    {
        var target = GetTarget(Entity.GetString("target"));
        if (target == null)
        {
            GD.PushWarning("point_teleport: Target not found");
            return;
        }

        target.GlobalTransform = GlobalTransform;
        var rot = target.Rotation;
        rot.Y = ConvertDirection(Entity.GetVector3("angles")).Y - Mathf.Pi / 2f;
        target.Rotation = rot;

        if (target.Get("velocity").VariantType != Variant.Type.Nil)
            target.Set("velocity", Vector3.Zero);
    }
}
