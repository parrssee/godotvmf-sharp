using Godot;

namespace GodotVMF;

[Tool]
public partial class VTFImporter : EditorImportPlugin
{
    public override string _GetImporterName() => "VTF";
    public override string _GetVisibleName() => "VTF Importer";
    public override string[] _GetRecognizedExtensions() => new[] { "vtf" };
    public override string _GetSaveExtension() => "vtf.res";
    public override string _GetResourceType() => "Texture2D";
    public override int _GetPresetCount() => 0;
    public override int _GetImportOrder() => 0;
    public override float _GetPriority() => 1.0f;
    public override bool _CanImportThreaded() => false;

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetImportOptions(string path, int presetIndex)
    {
        return new Godot.Collections.Array<Godot.Collections.Dictionary>
        {
            new Godot.Collections.Dictionary
            {
                ["name"] = "srgb_conversion_method",
                ["default_value"] = 1,
                ["type"] = (int)Variant.Type.Int,
                ["property_hint"] = (int)PropertyHint.Enum,
                ["hint_string"] = "Disabled,During import,Process in shader",
            }
        };
    }

    public override bool _GetOptionVisibility(string path, StringName optionName, Godot.Collections.Dictionary options) => true;

    public override Error _Import(string sourceFile, string savePath, Godot.Collections.Dictionary options,
        Godot.Collections.Array<string> platformVariants, Godot.Collections.Array<string> genFiles)
    {
        string pathToSave = savePath + "." + _GetSaveExtension();
        var vtf = VTFLoader.Create(sourceFile, 0.033f);
        if (vtf == null) return Error.FileUnrecognized;

        int srgbMethod = options.TryGetValue("srgb_conversion_method", out var sv) ? sv.AsInt32() : 1;
        var texture = vtf.CompileTexture((VTFLoader.SRGBConversionMethod)srgbMethod);
        if (texture == null) return Error.FileCantRead;

        texture.SetMeta("srgb_conversion_method", srgbMethod);
        return ResourceSaver.Save(texture, pathToSave, ResourceSaver.SaverFlags.Compress);
    }
}
