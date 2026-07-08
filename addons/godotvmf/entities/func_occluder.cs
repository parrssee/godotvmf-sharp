using System.Collections.Generic;
using Godot;

namespace GodotVMF;

/// This entity will merge all func_occluder instances into one during import
[Tool]
public partial class func_occluder : VMFEntityNode
{
    public OccluderInstance3D OccluderInstanceNode => GetNode<OccluderInstance3D>("occluder");

    public override void EntitySetup(VMFEntity entity)
    {
        var existingOccluder = GetParent().GetNodeOrNull("occluder") as func_occluder;
        Name = "occluder";

        var occluderNode = existingOccluder == null ? OccluderInstanceNode : existingOccluder.OccluderInstanceNode;
        var shape = occluderNode.Occluder as ArrayOccluder3D ?? new ArrayOccluder3D();

        var newVertices = GetEntityTrimeshShape()?.GetFaces() ?? System.Array.Empty<Vector3>();
        var vertices = new List<Vector3>(shape.Vertices);
        vertices.AddRange(newVertices);

        var indices = new int[vertices.Count];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;

        shape.SetArrays(vertices.ToArray(), indices);
        occluderNode.Occluder = shape;

        if (existingOccluder != null)
            QueueFree();
    }
}
