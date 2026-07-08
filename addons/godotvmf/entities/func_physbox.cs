using Godot;

namespace GodotVMF;

[Tool]
public partial class func_physbox : VMFEntityNode
{
    private const int FLAG_IGNORE_PICKUP = 8192;
    private const int FLAG_MOTION_DISABLED = 32768;

    private RigidBody3D Body => GetNode<RigidBody3D>("body");
    private MeshInstance3D MeshInstanceNode => GetNode<MeshInstance3D>("body/mesh");
    private CollisionShape3D Collision => GetNode<CollisionShape3D>("body/collision");

    public override void EntityReady()
    {
        Reparent(GetTree().CurrentScene);
        SetPhysicsProcess(true);
    }

    public void EnableMotion(Variant _param) => Body.Freeze = false;

    public void DisableMotion(Variant _param) => Body.Freeze = true;

    public override void EntitySetup(VMFEntity entity)
    {
        Body.Freeze = HasFlag(FLAG_MOTION_DISABLED);

        var mesh = GetMesh();
        MeshInstanceNode.Mesh = mesh;

        if (mesh == null || mesh.GetSurfaceCount() == 0)
        {
            VMFLogger.Warn($"func_physbox {Entity.GetString("id")}: no mesh generated, skipping collision setup");
            return;
        }

        Collision.Shape = mesh.CreateConvexShape(true);

        var size = mesh.GetAabb().Size;
        Body.Mass = size.X * size.Y * size.Z * 0.01f;
    }
}
