using Godot;
using Godot.Collections;

namespace GodotVMF;

[Tool]
public partial class MDLImporter : EditorImportPlugin
{
    public override string _GetImporterName() => "MDL";
    public override string _GetVisibleName() => "MDL";
    public override string[] _GetRecognizedExtensions() => new[] { "mdl" };
    public override string _GetSaveExtension() => "scn";
    public override string _GetResourceType() => "PackedScene";
    public override float _GetPriority() => 1.0f;
    public override int _GetPresetCount() => 0;
    public override int _GetImportOrder() => 2;
    public override bool _CanImportThreaded() => false;

    public override Array<Dictionary> _GetImportOptions(string path, int presetIndex) => new()
    {
        new Dictionary { ["name"] = "use_global_scale",         ["default_value"] = true,         ["type"] = (int)Variant.Type.Bool },
        new Dictionary { ["name"] = "scale",                    ["default_value"] = 0.02f,        ["type"] = (int)Variant.Type.Float },
        new Dictionary { ["name"] = "additional_rotation",      ["default_value"] = Vector3.Zero, ["type"] = (int)Variant.Type.Vector3 },
        new Dictionary { ["name"] = "generate_occluder",        ["default_value"] = false,        ["type"] = (int)Variant.Type.Bool },
        new Dictionary { ["name"] = "generate_lods",            ["default_value"] = true,         ["type"] = (int)Variant.Type.Bool },
        new Dictionary { ["name"] = "primitive_occluder",       ["default_value"] = false,        ["type"] = (int)Variant.Type.Bool },
        new Dictionary { ["name"] = "primitive_occluder_scale", ["default_value"] = Vector3.One,  ["type"] = (int)Variant.Type.Vector3 },
        new Dictionary { ["name"] = "gi_mode",                  ["default_value"] = 1,            ["type"] = (int)Variant.Type.Int,
            ["property_hint"] = (int)PropertyHint.Enum, ["hint_string"] = "Disabled,Static,Dynamic" },
    };

    public override bool _GetOptionVisibility(string path, StringName optionName, Dictionary options) => true;

    public override Error _Import(string sourceFile, string savePath, Dictionary options,
        Array<string> platformVariants, Array<string> genFiles)
    {
        string vtxPath = sourceFile.Replace(".mdl", ".vtx");
        string vvdPath = sourceFile.Replace(".mdl", ".vvd");
        string phyPath = sourceFile.Replace(".mdl", ".phy");

        var mdl = new MDLReader(sourceFile);
        if (mdl.Header == null) { GD.PushError("Error while reading MDL file."); return Error.ParseError; }

        var vtx = new VTXReader(vtxPath, mdl.Header.Version);
        if (vtx.Header == null) { GD.PushError("Error while reading VTX file."); return Error.ParseError; }

        if (mdl.Header.Checksum != vtx.Header.CheckSum)
        {
            GD.PushError("MDL and VTX checksums do not match.");
            return Error.ParseError;
        }

        var vvd = new VVDReader(vvdPath);
        var phy = new PHYReader(phyPath);

        string modelName = System.IO.Path.GetFileNameWithoutExtension(sourceFile);
        var combiner = new MDLCombiner(mdl, vtx, vvd, phy, options);

        var scn = new PackedScene();
        combiner.MeshInstance.Name = modelName;
        scn.Pack(combiner.MeshInstance);
        combiner.MeshInstance.QueueFree();

        string pathToSave = savePath + "." + _GetSaveExtension();
        return ResourceSaver.Save(scn, pathToSave, ResourceSaver.SaverFlags.Compress);
    }
}
