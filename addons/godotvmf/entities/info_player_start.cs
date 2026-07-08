using Godot;

namespace GodotVMF;

[Tool]
public partial class info_player_start : VMFEntityNode
{
    [Export] public PackedScene? PlayerScene { get; set; }

    public Node3D? Instance { get; private set; }

    public override void EntityReady()
    {
        var existingPlayer = GetTarget("!player");
        if (existingPlayer != null)
        {
            // Player already lives in the tree (placed directly in this scene, or
            // persisted from a previous level) - just move it here, don't reparent it.
            Instance = existingPlayer;
            PositionPlayer(existingPlayer);
            GetParent().RemoveChild(this);
            return;
        }

        var sceneRoot = PlayerScene?.Instantiate<Node3D>();
        if (sceneRoot == null) return;

        GetTree().CurrentScene.AddChild(sceneRoot);

        // AddChild runs the new subtree's _Ready synchronously, so if the player
        // registers its own "!player" alias (e.g. PlayerInteractionComponent), it is
        // already resolvable here and more precisely targeted than the scene root.
        Instance = GetTarget("!player") ?? sceneRoot;
        PositionPlayer(Instance);

        GetParent().RemoveChild(this);
    }

    private void PositionPlayer(Node3D player)
    {
        player.GlobalTransform = GlobalTransform;
        player.Basis *= new Basis(Vector3.Up, Mathf.Pi * -0.5f);
    }
}
