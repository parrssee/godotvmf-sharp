using Godot;

namespace GodotVMF;

[Tool]
public partial class trigger_once : VMFEntityNode
{
    public override void EntitySetup(VMFEntity entity)
    {
        GetNode<CollisionShape3D>("area/collision").Shape = GetEntityShape();
    }

    public override void EntityReady()
    {
        // NOTE: Call deferred is used to prevent unexpected trigger after player spawn
        CallDeferred(GodotObject.MethodName.Call, "ConnectBodyEntered");
    }

    private void ConnectBodyEntered()
    {
        GetNode<Area3D>("area").BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (!Enabled) return;
        if (Aliases.TryGetValue("!player", out var player) && player == body)
        {
            GD.Print("Triggered by player");
            TriggerOutput("OnTrigger");
            QueueFree();
        }
    }
}
