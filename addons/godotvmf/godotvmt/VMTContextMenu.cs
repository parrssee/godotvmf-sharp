using System.Linq;
using Godot;

namespace GodotVMF;

[Tool]
public partial class VMTContextMenu : EditorContextMenuPlugin
{
    public override void _PopupMenu(string[] paths)
    {
        if (paths.Any(p => p.EndsWith(".vmt")))
            AddContextMenuItem("Create editable material", Callable.From<string[]>(CreateEditableMaterial));
    }

    private static void CreateEditableMaterial(string[] paths)
    {
        foreach (var vmtFile in paths.Where(p => p.EndsWith(".vmt")))
        {
            string targetFile = vmtFile.Replace(".vmt", ".tres");
            var vmt = ResourceLoader.Load<Material>(vmtFile);
            if (vmt == null) continue;
            var error = ResourceSaver.Save(vmt, targetFile);
            if (error != Error.Ok)
                GD.Print("Failed to create editable material: " + targetFile);
        }
    }
}
