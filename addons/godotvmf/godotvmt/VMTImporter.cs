using Godot;

namespace GodotVMF;

[Tool]
public partial class VMTImporter : EditorImportPlugin
{
    public override string _GetImporterName() => "VMT";
    public override string _GetVisibleName() => "VMT Importer";
    public override string[] _GetRecognizedExtensions() => new[] { "vmt" };
    public override string _GetSaveExtension() => "vmt.res";
    public override string _GetResourceType() => "Material";
    public override int _GetPresetCount() => 0;
    public override int _GetImportOrder() => 1;
    public override float _GetPriority() => 1.0f;
    public override bool _CanImportThreaded() => false;

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetImportOptions(string path, int presetIndex)
        => new Godot.Collections.Array<Godot.Collections.Dictionary>();

    public override bool _GetOptionVisibility(string path, StringName optionName, Godot.Collections.Dictionary options) => true;

    public override Error _Import(string sourceFile, string savePath, Godot.Collections.Dictionary options,
        Godot.Collections.Array<string> platformVariants, Godot.Collections.Array<string> genFiles)
    {
        var material = VMTLoader.Load(sourceFile);
        if (material == null) return Error.Failed;

        string pathToSave = savePath + "." + _GetSaveExtension();
        if (ResourceLoader.Exists(pathToSave))
            DirAccess.RemoveAbsolute(pathToSave);

        var error = ResourceSaver.Save(material, pathToSave, ResourceSaver.SaverFlags.Compress);
        if (error == Error.Ok)
            material.TakeOverPath(pathToSave);

        return error;
    }
}
