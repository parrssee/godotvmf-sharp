using System.Collections.Generic;
using Godot;

namespace GodotVMF;

[Tool]
public partial class env_physexplosion : VMFEntityNode
{
    public float Magnitude => Entity.GetFloat("magnitude", 100f) * VMFConfig.Import.Scale;
    public string TargetEntityName => Entity.GetString("targetentityname");
    public List<Node> TargetEntities => GetAllTargets(TargetEntityName);

    public void Explode(Variant _void = default)
    {
        foreach (var t in TargetEntities)
        {
            if (t is not Node3D node3D) continue;
            if (node3D.GetNodeOrNull("body") is not RigidBody3D rigidbody) continue;

            var delta = node3D.GlobalPosition - GlobalPosition;
            float distance = delta.Length();
            float force = Magnitude / (distance * distance);
            var impulseVector = delta.Normalized() * force;

            rigidbody.ApplyCentralImpulse(impulseVector);
        }
    }
}
