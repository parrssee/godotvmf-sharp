using Godot;

namespace GodotVMF;

[Tool]
public partial class VMFPluginUI : MarginContainer
{
    public override void _EnterTree()
    {
        GetNode<Button>("ReimportButton").Pressed += OnReimportPressed;
    }

    private void OnReimportPressed()
    {
        var root = GetTree().GetEditedSceneRoot();
        if (root == null) return;
        foreach (var child in root.GetChildren())
        {
            if (child is VMFNode node)
                node.ImportMap();
        }
    }
}
