using System.Collections.Generic;
using Godot;

namespace GodotVMF;

[Tool]
public partial class env_shake : VMFEntityNode
{
    private const int FLAG_GLOBAL_SHAKE = 1;
    private const int FLAG_PHYSICS = 8;

    private readonly List<Node3D> _bodies = new();

    public override void EntityReady()
    {
        Disable();

        if (!HasFlag(FLAG_GLOBAL_SHAKE))
        {
            var area = GetNode<Area3D>("area");
            area.BodyEntered += body =>
            {
                bool isRigidBody = body is RigidBody3D && HasFlag(FLAG_PHYSICS);
                bool isCharacterBody = body is CharacterBody3D;
                if (isRigidBody || isCharacterBody) _bodies.Add(body);
            };
            area.BodyExited += body => _bodies.Remove(body);
        }
    }

    public override void EntitySetup(VMFEntity entity)
    {
        var collision = GetNode<CollisionShape3D>("area/collision");
        ((SphereShape3D)collision.Shape!).Radius = entity.Data.GetFloat("radius", 0.1f) * VMFConfig.Import.Scale;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Enabled) return;

        float freq = Entity.GetFloat("frequency", 0.1f) / 255f * 100f;
        float amp = Entity.GetFloat("amplitude", 0.1f);
        const float radius = 2f;
        float msec = (float)Time.GetTicksMsec();

        foreach (var body in _bodies)
        {
            var forceVector = new Vector3(
                Mathf.Sin(msec * freq * 1.345092f) * (float)GD.RandRange(-amp, amp),
                Mathf.Cos(msec * freq * 1.232092f) * (float)GD.RandRange(-amp, amp),
                Mathf.Cos(msec * freq * 1.584098f) * (float)GD.RandRange(-amp, amp)
            ) * freq;

            float influence = 1f - Mathf.Clamp(body.GlobalPosition.DistanceTo(GlobalPosition) / radius, 0f, 1f);

            if (body is RigidBody3D rb)
            {
                rb.ApplyForce(forceVector * (float)delta * influence, Vector3.Zero);
                rb.ApplyTorque(forceVector * (float)delta * influence * 0.1f);
            }
            else if (body is CharacterBody3D cb && cb.IsOnFloor())
            {
                var shakeValue = cb.Get("shake");
                if (shakeValue.VariantType != Variant.Type.Nil)
                    cb.Set("shake", shakeValue.AsSingle() + amp * (float)delta);
            }
        }
    }

    public async void StartShake(Variant _param = default)
    {
        Enable();
        await ToSignal(GetTree().CreateTimer(Entity.GetFloat("duration", 1f)), SceneTreeTimer.SignalName.Timeout);
        Disable();
    }

    public void Stop(Variant _param = default) => Disable();
}
