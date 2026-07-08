using Godot;

namespace GodotVMF;

[Tool]
public partial class filter_entity : VMFEntityNode
{
    public VMFEntityNode? GetEntity(Node? node)
    {
        while (node != null)
        {
            if (node is VMFEntityNode entity) return entity;
            node = node.GetParent();
        }
        return null;
    }

    public virtual bool IsPassed(Node3D node) => false;
}
