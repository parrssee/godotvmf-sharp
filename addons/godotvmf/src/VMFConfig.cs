using Godot;
using GodotArray = Godot.Collections.Array;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

public static class VMFConfig
{
    public const string ConfigFilePath = "res://vmf.config.json";

    public class ModelsConfig
    {
        public bool Import
        {
            get => ProjectSettings.GetSetting("godot_vmf/models/import", false).AsBool();
            set => ProjectSettings.SetSetting("godot_vmf/models/import", value);
        }
        public float LightmapTexelSize =>
            ProjectSettings.GetSetting("godot_vmf/models/lightmap_texel_size", 0.4f).AsSingle();
        public string TargetFolder =>
            ProjectSettings.GetSetting("godot_vmf/models/target_folder", "res://").AsString();
    }

    public class MaterialsConfig
    {
        public enum ImportMode { UseExisting = 0, ImportFromModFolder = 1 }

        public ImportMode ImportModeValue =>
            (ImportMode)ProjectSettings.GetSetting("godot_vmf/materials/import_mode", 0).AsInt32();
        public string TargetFolder =>
            ProjectSettings.GetSetting("godot_vmf/materials/target_folder", "res://materials").AsString();
        public GodotArray Ignore =>
            ProjectSettings.GetSetting("godot_vmf/materials/ignore",
                new GodotArray { "tools/toolsnodraw", "tools/toolsskybox", "tools/toolsinvisible" })
            .AsGodotArray();
        public string FallbackMaterial =>
            ProjectSettings.GetSetting("godot_vmf/materials/fallback_material", "").AsString();
        public int DefaultTextureSize =>
            ProjectSettings.GetSetting("godot_vmf/materials/default_texture_size", 512).AsInt32();
    }

    public class ImportConfig
    {
        public float Scale =>
            ProjectSettings.GetSetting("godot_vmf/import/scale", 0.02f).AsSingle();
        public bool GenerateCollision =>
            ProjectSettings.GetSetting("godot_vmf/import/generate_collision", true).AsBool();
        public bool GenerateLightmapUv2 =>
            ProjectSettings.GetSetting("godot_vmf/import/generate_lightmap_uv2", true).AsBool();
        public float LightmapTexelSize =>
            ProjectSettings.GetSetting("godot_vmf/import/lightmap_texel_size", 0.2f).AsSingle();
        public int LightmapSize =>
            ProjectSettings.GetSetting("godot_vmf/import/lightmap_size", 1024).AsInt32();
        public string InstancesFolder =>
            ProjectSettings.GetSetting("godot_vmf/import/instances_folder", "res://instances").AsString();
        public string EntitiesFolder =>
            ProjectSettings.GetSetting("godot_vmf/import/entities_folder", "res://entities").AsString();
        public string GeometryFolder =>
            ProjectSettings.GetSetting("godot_vmf/import/geometry_folder", "res://geometry").AsString();
        public GodotDict EntityAliases =>
            ProjectSettings.GetSetting("godot_vmf/import/entity_aliases", new GodotDict()).AsGodotDictionary();
        public bool UseNavigationMesh =>
            ProjectSettings.GetSetting("godot_vmf/import/generate_navigation_mesh", false).AsBool();
        public string NavigationMeshPreset =>
            ProjectSettings.GetSetting("godot_vmf/import/navigation_mesh_preset", "").AsString();
        public string GameinfoPath =>
            ProjectSettings.GetSetting("godot_vmf/import/gameinfo_path", "res://").AsString();
    }

    public static string GameinfoPath =>
        ProjectSettings.GetSetting("godot_vmf/import/gameinfo_path", "res://").AsString();

    private static ModelsConfig? _models;
    public static ModelsConfig Models => _models ??= new ModelsConfig();

    private static MaterialsConfig? _materials;
    public static MaterialsConfig Materials => _materials ??= new MaterialsConfig();

    private static ImportConfig? _import;
    public static ImportConfig Import => _import ??= new ImportConfig();

    public static void UpdateConfigField()
    {
        _import = new ImportConfig();
        _models = new ModelsConfig();
        _materials = new MaterialsConfig();
    }

    public static void DetachSignals() =>
        ProjectSettings.SettingsChanged -= UpdateConfigField;

    public static void DefineProjectSettings()
    {
        SetIfMissing("godot_vmf/import/gameinfo_path", "res://");
        SetIfMissing("godot_vmf/import/scale", 0.02f);
        SetIfMissing("godot_vmf/import/generate_lightmap_uv2", true);
        SetIfMissing("godot_vmf/import/generate_collision", true);
        SetIfMissing("godot_vmf/import/generate_navigation_mesh", false);
        SetIfMissing("godot_vmf/import/navigation_mesh_preset", "");
        SetIfMissing("godot_vmf/import/lightmap_texel_size", 0.2f);
        SetIfMissing("godot_vmf/import/instances_folder", "res://instances");
        SetIfMissing("godot_vmf/import/entities_folder", "res://entities");
        SetIfMissing("godot_vmf/import/geometry_folder", "res://geometry");
        SetIfMissing("godot_vmf/import/entity_aliases", new GodotDict());
        SetIfMissing("godot_vmf/models/import", false);
        SetIfMissing("godot_vmf/models/target_folder", "res://");
        SetIfMissing("godot_vmf/models/lightmap_texel_size", 0.4f);
        SetIfMissing("godot_vmf/materials/import_mode", 0);
        SetIfMissing("godot_vmf/materials/target_folder", "res://materials");
        SetIfMissing("godot_vmf/materials/ignore",
            new GodotArray { "tools/toolsnodraw", "tools/toolsskybox", "tools/toolsinvisible" });
        SetIfMissing("godot_vmf/materials/fallback_material", "");
        SetIfMissing("godot_vmf/materials/default_texture_size", 512);

        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/import/gameinfo_path",
            ["type"] = (int)Variant.Type.String,
            ["hint"] = (int)PropertyHint.GlobalFile,
            ["hint_string"] = "gameinfo.txt,GameInfo.txt",
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/import/scale",
            ["type"] = (int)Variant.Type.Float,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/import/generate_lightmap_uv2",
            ["type"] = (int)Variant.Type.Bool,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/import/generate_navigation_mesh",
            ["type"] = (int)Variant.Type.Bool,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/import/navigation_mesh_preset",
            ["type"] = (int)Variant.Type.String,
            ["hint"] = (int)PropertyHint.ResourceType,
            ["hint_string"] = "NavigationMesh",
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/import/generate_collision",
            ["type"] = (int)Variant.Type.Bool,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/import/lightmap_texel_size",
            ["type"] = (int)Variant.Type.Float,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/import/instances_folder",
            ["type"] = (int)Variant.Type.String,
            ["hint"] = (int)PropertyHint.Dir,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/import/entities_folder",
            ["type"] = (int)Variant.Type.String,
            ["hint"] = (int)PropertyHint.Dir,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/import/geometry_folder",
            ["type"] = (int)Variant.Type.String,
            ["hint"] = (int)PropertyHint.Dir,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/import/entity_aliases",
            ["type"] = (int)Variant.Type.Dictionary,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/models/import",
            ["type"] = (int)Variant.Type.Bool,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/models/target_folder",
            ["type"] = (int)Variant.Type.String,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/models/lightmap_texel_size",
            ["type"] = (int)Variant.Type.Float,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/materials/import_mode",
            ["type"] = (int)Variant.Type.Int,
            ["hint"] = (int)PropertyHint.Enum,
            ["hint_string"] = "Use Existing,Import from the GameInfo folder",
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/materials/target_folder",
            ["type"] = (int)Variant.Type.String,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/materials/ignore",
            ["type"] = (int)Variant.Type.Array,
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/materials/fallback_material",
            ["type"] = (int)Variant.Type.String,
            ["hint"] = (int)PropertyHint.ResourceType,
            ["hint_string"] = "Material",
        });
        ProjectSettings.AddPropertyInfo(new GodotDict
        {
            ["name"] = "godot_vmf/materials/default_texture_size",
            ["type"] = (int)Variant.Type.Int,
        });

        ProjectSettings.SettingsChanged += UpdateConfigField;
    }

    public static void LoadConfig()
    {
        if (!Engine.IsEditorHint()) return;
        if (!FileAccess.FileExists(ConfigFilePath)) return;
        // Legacy JSON config — project settings are the canonical source in C# port.
    }

    private static void SetIfMissing(string key, Variant value)
    {
        if (!ProjectSettings.HasSetting(key))
            ProjectSettings.SetSetting(key, value);
        ProjectSettings.SetInitialValue(key, value);
    }
}
