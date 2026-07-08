using Godot;

namespace GodotVMF;

[Tool]
public partial class trigger_multiple : VMFEntityNode
{
    protected const int FLAG_CLIENTS = 1;

    public filter_entity? GetFilterEntity() => GetTarget(Entity.GetString("filtername")) as filter_entity;

    public override void EntitySetup(VMFEntity entity)
    {
        GetNode<CollisionShape3D>("area/collision").Shape = GetEntityShape();
    }

    public override void EntityReady()
    {
        var area = GetNode<Area3D>("area");
        area.BodyEntered += OnBodyEntered;
        area.BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node3D body)
    {
        bool isClientPassed = HasFlag(FLAG_CLIENTS) && Aliases.TryGetValue("!player", out var player) && player == body;
        var filter = GetFilterEntity();
        bool isFilterPassed = filter != null && filter.IsPassed(body);

        if (isClientPassed || isFilterPassed)
        {
            TriggerOutput("OnTrigger");
            TriggerOutput("OnStartTouch");
        }
    }

    private void OnBodyExited(Node3D body)
    {
        bool isClientPassed = HasFlag(FLAG_CLIENTS) && Aliases.TryGetValue("!player", out var player) && player == body;
        var filter = GetFilterEntity();
        bool isFilterPassed = filter != null && filter.IsPassed(body);

        if (isClientPassed || isFilterPassed)
            TriggerOutput("OnEndTouch");
    }
}
