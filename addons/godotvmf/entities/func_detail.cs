using Godot;

namespace GodotVMF;

[Tool]
public partial class func_detail : VMFEntityNode
{
    public override void EntitySetup(VMFEntity entity)
    {
        var mesh = GetMesh(true, false, Vector3.Zero);

        if (mesh == null || mesh.GetSurfaceCount() == 0)
        {
            QueueFree();
            return;
        }

        if (VMFConfig.Import.GenerateLightmapUv2)
        {
            var unwrapErr = mesh.LightmapUnwrap(GlobalTransform, VMFConfig.Import.LightmapTexelSize);
            if (unwrapErr != Error.Ok)
                VMFLogger.Warn($"func_detail {entity.Id}: lightmap_unwrap failed ({unwrapErr}), skipping UV2");
        }

        var meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");
        meshInstance.CastShadow = entity.Data.TryGetValue("disableshadows", out var ds) && ds.AsInt32() != 0
            ? GeometryInstance3D.ShadowCastingSetting.Off
            : GeometryInstance3D.ShadowCastingSetting.On;
        meshInstance.Mesh = mesh;

        if (VMFConfig.Import.GenerateCollision)
        {
            var collision = GetNode<CollisionShape3D>("MeshInstance3D/StaticBody3D/CollisionShape3D");
            collision.Shape = meshInstance.Mesh.CreateTrimeshShape();
        }
    }
}
