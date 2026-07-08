using Godot;

namespace GodotVMF;

[Tool]
public partial class func_brush : VMFEntityNode
{
    private enum Solidity { Toggle, Never, Always }

    public override void EntitySetup(VMFEntity entity)
    {
        GetNode<MeshInstance3D>("body/mesh").Mesh = GetMesh();

        if (entity.Data.GetInt("Solidity") == (int)Solidity.Never)
            GetNode("body/collision").QueueFree();
        else
            GetNode<CollisionShape3D>("body/collision").Shape = GetEntityShape();
    }

    public override void _Process(double _dt)
    {
        if (Engine.IsEditorHint()) return;

        var body = GetNode<Node3D>("body");
        if (body.HasNode("collision"))
            GetNode<CollisionShape3D>("body/collision").Disabled = !Enabled;

        body.Visible = Enabled;
    }
}
