using Godot;

namespace GodotVMF;

[Tool]
public partial class prop_studio : VMFEntityNode
{
    public string Model => Entity.TryGetValue("model", out var m) ? m.AsString() : "";
    public MeshInstance3D? ModelInstance => GetNodeOrNull<MeshInstance3D>("model");
    public string ModelName => System.IO.Path.GetFileNameWithoutExtension(
        System.IO.Path.GetFileName(Entity.TryGetValue("model", out var m) ? m.AsString() : "prop_static"));
    public float ModelScale => Entity.TryGetValue("modelscale", out var s) ? s.AsSingle() : 1.0f;
    public int Skin => Entity.TryGetValue("skin", out var sk) ? sk.AsInt32() : 0;

    public override void EntitySetup(VMFEntity entity)
    {
        string modelPath = VMFUtils.NormalizePath(VMFConfig.Models.TargetFolder + "/" + Model);

        var cached = VMFCache.GetCached(Model);
        var modelScene = cached.VariantType != Variant.Type.Nil ? cached.As<PackedScene>() : null;

        if (modelScene == null)
        {
            if (!ResourceLoader.Exists(modelPath))
            {
                if (VMFCache.IsFileLogged(Model)) return;
                VMFLogger.Warn("Model not found: " + modelPath);
                VMFCache.AddLoggedFile(Model);
                return;
            }
            modelScene = ResourceLoader.Load<PackedScene>(modelPath);
            if (modelScene != null) VMFCache.AddCached(Model, Variant.From(modelScene));
            if (modelScene == null)
            {
                VMFLogger.Error("Failed to load model scene: " + modelPath);
                return;
            }
        }

        var editState = Engine.IsEditorHint() ? PackedScene.GenEditState.MainInherited : PackedScene.GenEditState.Disabled;
        var instance = modelScene.Instantiate(editState);
        instance.Name = "model";
        ((Node3D)instance).Scale *= ModelScale;
        MDLCombiner.ApplySkin(instance, Skin);
        AddChild(instance);
        ModelInstance?.SetOwner(GetOwner<Node>());
    }
}
