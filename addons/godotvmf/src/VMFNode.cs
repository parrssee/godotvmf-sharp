using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace GodotVMF;

[Tool]
[Icon("res://addons/godotvmf/icon.svg")]
public partial class VMFNode : Node3D
{
    [ExportCategory("VMF File")]
    [Export]
    public bool UseExternalFile
    {
        get => _useExternalFile;
        set { _useExternalFile = value; NotifyPropertyListChanged(); }
    }
    private bool _useExternalFile = false;

    [Export(PropertyHint.File, "*.vmf")]
    public string Vmf
    {
        get => _vmf;
        set { _vmf = value; }
    }
    private string _vmf = "";

    [ExportCategory("Import")]
    [Export]
    public bool Import
    {
        get => false;
        set { if (value) { ImportMap(); } }
    }

    [Export] public bool DoubleSidedShadowCast { get; set; } = false;

    [ExportCategory("Resource Generation")]
    [Export] public bool RemoveMergedFaces { get; set; } = true;
    [Export] public bool SaveGeometry { get; set; } = true;
    [Export] public bool SaveCollision { get; set; } = true;
    [Export(PropertyHint.Layers3DPhysics)] public int DefaultPhysicsMask { get; set; } = 1;

    public bool IsRuntime { get; set; } = false;
    public VMFStructure? VmfStructure { get; private set; }

    private Node3D? _owner => GetOwner<Node3D>() ?? this;

    public MeshInstance3D? Geometry
    {
        get
        {
            var g = GetNodeOrNull<MeshInstance3D>("Geometry");
            if (g != null) return g;
            return GetNodeOrNull<MeshInstance3D>("NavigationMesh/Geometry");
        }
    }

    public Node3D? Entities => GetNodeOrNull<Node3D>("Entities");
    public NavigationRegion3D? Navmesh => GetNodeOrNull<NavigationRegion3D>("NavigationMesh");

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        if (property["name"].AsString() == "vmf")
            property["hint"] = (int)(UseExternalFile ? PropertyHint.GlobalFile : PropertyHint.File);
    }

    public override void _Ready() => AddToGroup("vmfnode_group");

    public void ClearSceneGroups()
    {
        var tree = GetTree();
        var groups = tree?.EditedSceneRoot?.GetGroups() ?? GetGroups();
        foreach (var group in groups)
        {
            var nodes = tree?.GetNodesInGroup(group) ?? new Godot.Collections.Array<Node>();
            foreach (var node in nodes)
                node.RemoveFromGroup(group);
        }
    }

    public async void ReimportGeometry()
    {
        VMFConfig.LoadConfig();
        ReadVmf();
        VMFResourceManager.ImportMaterials(VmfStructure!, IsRuntime);
        await VMFResourceManager.ForResourceImport();
        ImportGeometry();
    }

    public void ImportGeometry()
    {
        ulong t = Time.GetTicksMsec();

        Navmesh?.Free();
        Geometry?.Free();

        var mesh = VMFTool.CreateMesh(VmfStructure!, Vector3.Zero, RemoveMergedFaces);
        if (mesh == null) return;

        var geometryMesh = new MeshInstance3D { Name = "Geometry" };
        geometryMesh.SetDisplayFolded(true);

        if (DoubleSidedShadowCast)
            geometryMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided;

        AddChild(geometryMesh);
        geometryMesh.SetOwner(_owner);

        geometryMesh.Mesh = mesh;

        if (VMFConfig.Import.GenerateCollision)
        {
            VMFTool.GenerateCollisions(geometryMesh, DefaultPhysicsMask);
            SaveCollisionFile();
        }

        if (!GetMeta("instance", false).AsBool())
            GenerateNavmesh(geometryMesh);

        geometryMesh.Mesh = VMFTool.CleanupMesh((ArrayMesh)geometryMesh.Mesh!)!;

        if (VMFConfig.Import.GenerateLightmapUv2 && !IsRuntime)
            ((ArrayMesh)geometryMesh.Mesh).LightmapUnwrap(geometryMesh.GlobalTransform, VMFConfig.Import.LightmapTexelSize);

        geometryMesh.Mesh = SaveGeometryFile(geometryMesh.Mesh);

        VMFLogger.Log($"import_geometry took {Time.GetTicksMsec() - t} ms");
    }

    private void GenerateNavmesh(MeshInstance3D geometryMesh)
    {
        if (!VMFConfig.Import.UseNavigationMesh) return;

        var navreg = new NavigationRegion3D { Name = "NavigationMesh" };
        string preset = VMFConfig.Import.NavigationMeshPreset;

        if (string.IsNullOrEmpty(preset))
            navreg.NavigationMesh = new NavigationMesh();
        else if (ResourceLoader.Exists(preset))
        {
            var res = ResourceLoader.Load(preset);
            navreg.NavigationMesh = res is NavigationMesh nm ? nm : new NavigationMesh();
            if (res is not NavigationMesh)
                VMFLogger.Warn($"Navigation mesh preset \"{preset}\" is not a NavigationMesh resource.");
        }
        else
        {
            VMFLogger.Warn($"Navigation mesh preset \"{preset}\" not found. Falling back to default.");
            navreg.NavigationMesh = new NavigationMesh();
        }

        AddChild(navreg);
        navreg.SetOwner(_owner);
        geometryMesh.Reparent(navreg);
        navreg.CallDeferred("bake_navigation_mesh");
    }

    private Mesh SaveGeometryFile(Mesh targetMesh)
    {
        if (!SaveGeometry) return targetMesh;
        string resourcePath = $"{VMFConfig.Import.GeometryFolder}/{VmfIdentifier()}_import.mesh";

        if (!DirAccess.DirExistsAbsolute(VMFConfig.Import.GeometryFolder))
            DirAccess.MakeDirRecursiveAbsolute(VMFConfig.Import.GeometryFolder);

        var err = ResourceSaver.Save(targetMesh, resourcePath, ResourceSaver.SaverFlags.Compress);
        if (err != Error.Ok)
        {
            VMFLogger.Error($"Failed to save geometry resource: {err}");
            return targetMesh;
        }

        targetMesh.TakeOverPath(resourcePath);
        return targetMesh;
    }

    private void SaveCollisionFile()
    {
        if (!SaveCollision) return;
        var geomNode = GetNodeOrNull("Geometry");
        if (geomNode == null) return;

        foreach (var child in geomNode.GetChildren())
        {
            if (child is not StaticBody3D body) continue;
            var collision = body.GetNodeOrNull<CollisionShape3D>("collision");
            if (collision == null) continue;

            if (!DirAccess.DirExistsAbsolute(VMFConfig.Import.GeometryFolder))
                DirAccess.MakeDirRecursiveAbsolute(VMFConfig.Import.GeometryFolder);

            string savePath = $"{VMFConfig.Import.GeometryFolder}/{VmfIdentifier()}_collision_{body.Name}.res";
            var error = ResourceSaver.Save(collision.Shape, savePath, ResourceSaver.SaverFlags.Compress);
            if (error != Error.Ok)
            {
                VMFLogger.Error($"Failed to save collision resource: {error}");
                continue;
            }

            collision.Shape!.TakeOverPath(savePath);
            collision.Shape = ResourceLoader.Load<Shape3D>(savePath);
        }
    }

    private string VmfIdentifier() => Vmf.Split('/').Last().Replace('.', '_');

    public void ClearStructure()
    {
        VmfStructure = null;
        foreach (var n in GetChildren())
        {
            RemoveChild(n);
            n.QueueFree();
        }
    }

    public void ReadVmf()
    {
        VMFLogger.MeasureCall(5000, "VMF reading took {0} ms", () =>
        {
            VmfStructure = new VMFStructure(VDFParser.Parse(Vmf)!);
        });
    }

    private PackedScene? GetEntityScene(string clazz)
    {
        string resPath = (VMFConfig.Import.EntitiesFolder + "/" + clazz + ".tscn")
            .Replace("//", "/").Replace("res:/", "res://");

        if (!ResourceLoader.Exists(resPath))
        {
            var aliases = VMFConfig.Import.EntityAliases;
            resPath = aliases.TryGetValue(clazz, out var aliasVal) ? aliasVal.AsString() : "";
        }

        if (!ResourceLoader.Exists(resPath))
            resPath = "res://addons/godotvmf/entities/" + clazz + ".tscn";

        if (!ResourceLoader.Exists(resPath)) return null;
        return ResourceLoader.Load<PackedScene>(resPath);
    }

    private void PushEntityToGroup(string classname, Node targetNode)
    {
        if (Entities == null) return;
        string groupName = classname.Split('_')[0] + "s";
        var group = Entities.GetNodeOrNull<Node3D>(groupName);

        if (group == null)
        {
            group = new Node3D { Name = groupName };
            Entities.AddChild(group);
            group.SetOwner(_owner);
            group.SetDisplayFolded(true);
        }

        group.AddChild(targetNode);
        targetNode.SetOwner(_owner);
        if (targetNode is Node3D n3d) n3d.SetDisplayFolded(true);
    }

    private void ResetEntitiesNode()
    {
        Entities?.Free();
        var enode = new Node3D { Name = "Entities" };
        AddChild(enode);
        enode.SetOwner(_owner);
    }

    public void ReimportEntities()
    {
        ReadVmf();
        ImportEntities();
    }

    public void ImportEntities()
    {
        ResetEntitiesNode();
        if (VmfStructure == null) return;

        foreach (var ent in VmfStructure.Entities)
        {
            ent.Data["vmf"] = Vmf;

            var tscn = GetEntityScene(ent.Classname);
            if (tscn == null) continue;

            var node = tscn.Instantiate(PackedScene.GenEditState.MainInherited);

            if (node is VMFEntityNode entityNode)
            {
                entityNode.IsRuntime = IsRuntime;
                entityNode.Reference = ent;
            }

            PushEntityToGroup(ent.Classname, node);
            SetEditableInstance(node, true);

            if (!IsRuntime && node is VMFEntityNode en)
            {
                en.EntityPreSetup(ent);
                if (en.ApplyEntity(ent.Data) == -1)
                    en.EntitySetup(ent);
            }
        }
    }

    public async void ImportMap()
    {
        if (string.IsNullOrEmpty(Vmf)) return;

        VMFCache.Clear();
        VMFConfig.LoadConfig();

        ClearStructure();
        ClearSceneGroups();
        ReadVmf();

        VMFResourceManager.ImportMaterials(VmfStructure!, IsRuntime);
        VMFResourceManager.ImportModels(VmfStructure!);
        await VMFResourceManager.ForResourceImport();

        ImportGeometry();
        ImportEntities();
    }
}
