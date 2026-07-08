using Godot;

namespace GodotVMF;

[Tool]
public partial class prop_static : prop_studio
{
    public bool ScreenSpaceFade => Entity.TryGetValue("screenspacefade", out var v) && v.AsInt32() == 1;
    public float FadeMin => (Entity.TryGetValue("fademindist", out var mn) ? mn.AsSingle() : 0f) * VMFConfig.Import.Scale;
    public float FadeMax => (Entity.TryGetValue("fademaxdist", out var mx) ? mx.AsSingle() : 0f) * VMFConfig.Import.Scale;

    private Node3D GetStaticPropsNode()
    {
        var vmfNode = GetVmfNode();
        if (vmfNode == null) return this;

        var geometryNode = vmfNode.Geometry;
        if (geometryNode == null)
        {
            geometryNode = new MeshInstance3D { Name = "Geometry" };
            vmfNode.AddChild(geometryNode);
            geometryNode.SetOwner(vmfNode.GetOwner<Node>());
        }

        var staticPropsNode = geometryNode.GetNodeOrNull<Node3D>("StaticProps");
        if (staticPropsNode == null)
        {
            staticPropsNode = new Node3D { Name = "StaticProps" };
            geometryNode.AddChild(staticPropsNode);
            staticPropsNode.SetOwner(geometryNode.GetOwner<Node>());
        }

        return staticPropsNode;
    }

    private void AssignModelProperties()
    {
        var mi = ModelInstance;
        if (mi == null) return;
        mi.SetOwner(GetOwner<Node>());
        mi.Scale *= ModelScale;
        mi.GIMode = GeometryInstance3D.GIModeEnum.Static;

        float fadeMargin = FadeMax - FadeMin;
        mi.VisibilityRangeEnd = Mathf.Max(0f, FadeMax);
        mi.VisibilityRangeFadeMode = ScreenSpaceFade
            ? GeometryInstance3D.VisibilityRangeFadeModeEnum.Self
            : GeometryInstance3D.VisibilityRangeFadeModeEnum.Disabled;

        if (mi.VisibilityRangeFadeMode != GeometryInstance3D.VisibilityRangeFadeModeEnum.Disabled)
            mi.VisibilityRangeEndMargin = fadeMargin;
    }

    private void CheckSolidState()
    {
        bool isSolid = !(Entity.TryGetValue("solid", out var sv) && sv.AsInt32() == 0);
        if (isSolid) return;

        var mi = ModelInstance;
        if (mi == null) return;
        GetOwner<Node>()?.SetEditableInstance(mi, true);
        foreach (var child in mi.GetChildren())
        {
            if (child is StaticBody3D sb) sb.Free();
        }
    }

    private void ReparentToStaticProps()
    {
        var mi = ModelInstance;
        if (mi == null) return;
        var staticPropsNode = GetStaticPropsNode();
        mi.Name = ModelName + mi.GetInstanceId().ToString();
        mi.Reparent(staticPropsNode);
    }

    public override void EntitySetup(VMFEntity entity)
    {
        base.EntitySetup(entity);
        if (ModelInstance == null) return;

        AssignModelProperties();
        CheckSolidState();
        ReparentToStaticProps();
        QueueFree();
    }
}
