using Godot;

namespace GodotVMF;

[Tool]
public partial class filter_activator_name : filter_entity
{
    public string FilterName => Entity.GetString("filtername");
    public bool IsInverted => Entity.GetInt("Negated") == 1;

    public override bool IsPassed(Node3D node)
    {
        var targetEntity = GetEntity(node);
        if (targetEntity == null) return false;

        string targetName = targetEntity.Entity.GetString("targetname");
        return IsInverted ? targetName != FilterName : targetName == FilterName;
    }
}
