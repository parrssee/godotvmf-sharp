using System.Collections.Generic;
using Godot;

namespace GodotVMF;

[Tool]
public partial class trigger_push : trigger_multiple
{
    private const int FLAG_PHYSICS_OBJECTS = 8;

    private readonly List<Node3D> _bodies = new();

    public Vector3 PushDir => GetMovementVector(Entity.GetVector3("pushdir"));
    public float Speed => Entity.GetFloat("speed") * VMFConfig.Import.Scale;
    public Vector3 Acceleration => Speed * PushDir;

    public override void EntityReady()
    {
        base.EntityReady();
        var area = GetNode<Area3D>("area");
        area.BodyEntered += OnBodyEnteredPush;
        area.BodyExited += OnBodyExitedPush;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) return;
        if (!Enabled) return;
        if (!HasFlag(FLAG_PHYSICS_OBJECTS) && !HasFlag(FLAG_CLIENTS)) return;

        foreach (var body in _bodies)
        {
            if (body is RigidBody3D rb)
            {
                rb.ApplyForce(Acceleration);
            }
            else
            {
                var velocity = body.Get("velocity");
                if (velocity.VariantType != Variant.Type.Nil)
                    body.Set("velocity", velocity.AsVector3() + Acceleration * (float)delta);
            }
        }
    }

    private void OnBodyEnteredPush(Node3D node)
    {
        if (_bodies.Contains(node)) return;
        if (node is not RigidBody3D) return;

        bool isClient = node.IsInGroup("Clients");
        bool isRigidBody = !isClient;

        if (isClient && !HasFlag(FLAG_CLIENTS)) return;
        if (isRigidBody && !HasFlag(FLAG_PHYSICS_OBJECTS)) return;

        _bodies.Add(node);
    }

    private void OnBodyExitedPush(Node3D node) => _bodies.Remove(node);
}
