using Godot;

namespace GodotVMF;

[Tool]
public partial class VMFEntityContextMenu : EditorContextMenuPlugin
{
    private static bool IsEntityScript(string p) =>
        p.EndsWith(".gd") && p.StartsWith(VMFConfig.Import.EntitiesFolder);

    public override void _PopupMenu(string[] paths)
    {
        bool hasScripts = false;
        foreach (var p in paths) { if (IsEntityScript(p)) { hasScripts = true; break; } }
        if (!hasScripts) return;

        AddContextMenuItem("Create entity scene", Callable.From<string[]>(CreateEntityScene));
    }

    private static void CreateEntityScene(string[] paths)
    {
        foreach (var scriptFile in paths)
        {
            if (!IsEntityScript(scriptFile)) continue;
            string targetFile = scriptFile.Replace(".gd", ".tscn");
            var entityClass = ResourceLoader.Load<GDScript>(scriptFile);
            if (entityClass == null) continue;

            var node = entityClass.Call("new").AsGodotObject() as Node;
            if (node == null) continue;

            node.Name = entityClass.GetGlobalName();
            var scene = new PackedScene();
            scene.Pack(node);

            var error = ResourceSaver.Save(scene, targetFile);
            if (error != Error.Ok)
                GD.Print("Failed to create entity scene: " + targetFile);

            node.QueueFree();
        }
    }
}
