using Godot;

namespace GodotVMF;

[Tool]
public partial class func_instance : VMFEntityNode
{
    public override void EntitySetup(VMFEntity entity)
    {
        var instanceScene = VMFInstanceManager.ImportInstance(entity.Data);
        AssignInstance(instanceScene);
    }

    private void AssignInstance(PackedScene? instanceScene)
    {
        if (instanceScene == null)
        {
            VMFLogger.Error("Failed to load instance: " + Name);
            QueueFree();
            return;
        }

        var node = instanceScene.Instantiate(PackedScene.GenEditState.MainInherited) as VMFNode;
        if (node == null)
        {
            QueueFree();
            return;
        }

        int i = 1;
        foreach (var child in GetParent().GetChildren())
            if (child.Name.ToString().StartsWith(node.Name.ToString()))
                i++;

        node.Name = $"{node.Name}_{i}";
        AddChild(node);
        node.SetOwner(GetOwner<Node>());
    }
}
