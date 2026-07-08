using Godot;

namespace GodotVMF;

[Tool]
public partial class info_particle_system : VMFEntityNode
{
    public override void EntitySetup(VMFEntity entity)
    {
        string scenePath = $"res://particles/{entity.Data.GetString("effect_name")}.tscn";
        if (!ResourceLoader.Exists(scenePath))
        {
            GD.PushError($"Particle effect not found: {scenePath}");
            return;
        }

        var scene = GD.Load<PackedScene>(scenePath);
        var node = scene.Instantiate();
        node.Name = "particles";

        AddChild(node);
        node.Owner = GetOwner<Node>();
    }

    public override void EntityReady()
    {
        var particlesNode = GetNodeOrNull<Node>("particles")?.GetChildOrNull<Node>(0);
        if (particlesNode == null) return;
        particlesNode.Set("emitting", Entity.GetInt("start_active") == 1);
    }

    public void Start(Variant _param = default) =>
        GetNodeOrNull<Node>("particles")?.GetChildOrNull<Node>(0)?.Set("emitting", true);

    public void Stop(Variant _param = default) =>
        GetNodeOrNull<Node>("particles")?.GetChildOrNull<Node>(0)?.Set("emitting", false);
}
