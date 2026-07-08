using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotVMF;

[Tool]
public partial class GodotVMFPlugin : EditorPlugin
{
    private Control? _dock;
    private MDLImporter? _mdlPlugin;
    private VTFImporter? _vtfPlugin;
    private VMTImporter? _vmtPlugin;
    private VMTContextMenu? _vmtContextPlugin;
    private VMFMaterialConversionContextMenu? _conversionContextPlugin;
    private VMFEntityContextMenu? _entityContextPlugin;

    public override void _EnterTree()
    {
        _dock = GD.Load<PackedScene>("res://addons/godotvmf/plugin.tscn").Instantiate<Control>();
        AddControlToContainer(CustomControlContainer.SpatialEditorMenu, _dock);

        _dock.GetNode<Button>("ReimportVMF").Pressed += ReimportVMF;
        _dock.GetNode<Button>("ReimportEntities").Pressed += ReimportEntities;
        _dock.GetNode<Button>("ReimportGeometry").Pressed += ReimportGeometry;
        _dock.GetNode<Button>("Docs").Pressed += () => OS.ShellOpen("https://github.com/H2xDev/GodotVMF/wiki");
        _dock.GetNode<Button>("DiscordSupport").Pressed += () => OS.ShellOpen("https://discord.gg/wtSK94fPxd");

        _mdlPlugin = new MDLImporter();
        _vtfPlugin = new VTFImporter();
        _vmtPlugin = new VMTImporter();

        AddImportPlugin(_mdlPlugin);
        AddImportPlugin(_vtfPlugin);
        AddImportPlugin(_vmtPlugin);

        var icon = GD.Load<Texture2D>("res://addons/godotvmf/hammer.png");
        AddCustomType("VMFNode", "Node3D", GD.Load<Script>("res://addons/godotvmf/src/VMFNode.cs"), icon);
        AddCustomType("VMFEntityNode", "Node3D", GD.Load<Script>("res://addons/godotvmf/src/VMFEntityNode.cs"), icon);

        _vmtContextPlugin = new VMTContextMenu();
        _entityContextPlugin = new VMFEntityContextMenu();
        _conversionContextPlugin = new VMFMaterialConversionContextMenu();

        var filesystem = (EditorContextMenuPlugin.ContextMenuSlot)1; // CONTEXT_SLOT_FILESYSTEM
        AddContextMenuPlugin(filesystem, _vmtContextPlugin);
        AddContextMenuPlugin(filesystem, _entityContextPlugin);
        AddContextMenuPlugin(filesystem, _conversionContextPlugin);

        VMFConfig.DefineProjectSettings();
        VMFConfig.LoadConfig();
    }

    public override void _ExitTree()
    {
        VMFConfig.DetachSignals();

        RemoveCustomType("VMFNode");
        RemoveCustomType("VMFEntityNode");

        if (_dock != null) { RemoveControlFromContainer(CustomControlContainer.SpatialEditorMenu, _dock); _dock.Free(); _dock = null; }
        if (_mdlPlugin != null) { RemoveImportPlugin(_mdlPlugin); _mdlPlugin = null; }
        if (_vtfPlugin != null) { RemoveImportPlugin(_vtfPlugin); _vtfPlugin = null; }
        if (_vmtPlugin != null) { RemoveImportPlugin(_vmtPlugin); _vmtPlugin = null; }
        if (_vmtContextPlugin != null) { RemoveContextMenuPlugin(_vmtContextPlugin); _vmtContextPlugin = null; }
        if (_entityContextPlugin != null) { RemoveContextMenuPlugin(_entityContextPlugin); _entityContextPlugin = null; }
        if (_conversionContextPlugin != null) { RemoveContextMenuPlugin(_conversionContextPlugin); _conversionContextPlugin = null; }
    }

    private List<VMFNode> GetExistingVMFNodes()
    {
        if (!IsInsideTree()) return new List<VMFNode>();
        return GetTree().GetNodesInGroup("vmfnode_group")
            .OfType<VMFNode>()
            .Where(n => !n.GetMeta("instance", Variant.From(false)).AsBool())
            .ToList();
    }

    private async void ReimportVMF()
    {
        var nodes = GetExistingVMFNodes();
        _dock?.GetNode<HBoxContainer>("ProgressBar").Show();
        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
        foreach (var node in nodes) node.ImportMap();
        _dock?.GetNode<HBoxContainer>("ProgressBar").Hide();
    }

    private async void ReimportEntities()
    {
        var nodes = GetExistingVMFNodes();
        _dock?.GetNode<HBoxContainer>("ProgressBar").Show();
        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
        foreach (var node in nodes) node.ReimportEntities();
        _dock?.GetNode<HBoxContainer>("ProgressBar").Hide();
    }

    private async void ReimportGeometry()
    {
        var nodes = GetExistingVMFNodes();
        _dock?.GetNode<HBoxContainer>("ProgressBar").Show();
        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
        foreach (var node in nodes) node.ReimportGeometry();
        _dock?.GetNode<HBoxContainer>("ProgressBar").Hide();
    }
}
