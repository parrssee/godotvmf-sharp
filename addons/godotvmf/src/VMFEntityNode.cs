using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

[Tool]
public partial class VMFEntityNode : Node3D
{
    private const string GroupPrefix = "vmf:";

    public static readonly Dictionary<string, List<Node>> NamedEntities = new();
    public static readonly Dictionary<string, Node> Aliases = new();

    private readonly Dictionary<string, List<Callable>> _userSignals = new();

    [Export] public GodotDict Entity { get; set; } = new();
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int Flags { get; set; } = 0;

    public bool HasSolid => Entity.ContainsKey("solid");
    public VMFEntity? Reference { get; set; }
    public bool IsRuntime { get; set; } = false;
    public Node? Activator { get; set; }

    public string Targetname => Entity.TryGetValue("targetname", out var v) ? v.AsString() : "";

    public static void DefineAlias(string name, Node value)
    {
        if (name == "!self")
        {
            VMFLogger.Error("The alias \"" + name + "\" is already defined");
            return;
        }
        Aliases[name] = value;
    }

    public static void RemoveAlias(string name) => Aliases.Remove(name);

    public void Toggle(Variant _param = default) => Enabled = !Enabled;
    public void Enable(Variant _param = default) => Enabled = true;
    public void Disable(Variant _param = default) => Enabled = false;

    public void Kill(Variant _param = default)
    {
        if (Entity.TryGetValue("targetname", out var tn))
            NamedEntities.Remove(GroupPrefix + tn.AsString());
        GetParent().RemoveChild(this);
        QueueFree();
    }

    public virtual void EntityReady() { }

    private void Reparent()
    {
        if (!Entity.TryGetValue("parentname", out var pn)) return;
        var parentNode = GetTarget(pn.AsString());
        if (parentNode != null) Reparent(parentNode, true);
    }

    public override void _Ready()
    {
        if (IsRuntime)
        {
            EntityPreSetup(Reference!);
            if (ApplyEntity(Reference!.Data) == -1)
                EntitySetup(Reference!);
        }
        else
        {
            SetProcess(false);
            SetPhysicsProcess(false);
        }

        if (Engine.IsEditorHint()) return;

        if (Entity.TryGetValue("targetname", out var tnVal) && !string.IsNullOrEmpty(tnVal.AsString()))
            AddNamedEntity(tnVal.AsString(), this);

        ParseConnections();

        SetProcess(true);
        SetPhysicsProcess(true);

        CallDeferred(GodotObject.MethodName.Call, "Reparent");
        CallDeferred(GodotObject.MethodName.Call, "EntityReady");
    }

    public virtual int ApplyEntity(GodotDict entityStructure) => -1;
    public virtual void EntitySetup(VMFEntity vmfEntity) { }

    public void EntityPreSetup(VMFEntity ent)
    {
        Reference = ent;
        Entity = ent.Data;
        Flags = ent.Data.TryGetValue("spawnflags", out var sf) ? sf.AsInt32() : 0;
        Transform = GetEntityTransform(ent);
        Enabled = !(ent.Data.TryGetValue("StartDisabled", out var sd) && sd.AsInt32() != 0);

        if (ent.Data.TryGetValue("targetname", out var tn) && !string.IsNullOrEmpty(tn.AsString()))
            AddNamedEntity(tn.AsString(), this);

        AssignName();
    }

    public void AssignName()
    {
        string id = Entity.TryGetValue("id", out var idVal) ? idVal.AsString() : "no_name";
        string classname = Entity.TryGetValue("classname", out var cn) ? cn.AsString() : "entity";

        if (!Entity.TryGetValue("targetname", out var tn) || string.IsNullOrEmpty(tn.AsString()))
        {
            Name = classname + "_" + id;
            return;
        }
        Name = tn.AsString() + "_" + id;
    }

    public static void AddNamedEntity(string name, Node node)
    {
        string groupName = GroupPrefix + name;
        node.CallDeferred("add_to_group", groupName, true);

        if (!NamedEntities.ContainsKey(groupName))
            NamedEntities[groupName] = new List<Node>();

        NamedEntities[groupName].Add(node);
    }

    public void CallTargetInput(string target, string input, string param, float delay, VMFEntityNode caller)
    {
        if (!caller.Enabled) return;

        var executor = new VMFOutputExecutor(target, input, param, delay, 1);
        executor.Caller = caller;
        GetTree().Root.AddChild(executor);
    }

    public Node3D? GetTarget(string n)
    {
        if (Aliases.TryGetValue(n, out var aliased))
            return aliased as Node3D;

        var nodes = GetAllTargets(n);
        if (nodes.Count == 0) return null;

        var node = nodes[0] as Node3D;
        if (!IsInstanceValid(node))
        {
            string groupName = GroupPrefix + n;
            if (NamedEntities.ContainsKey(groupName))
                NamedEntities[groupName].Remove(nodes[0]);
            return GetTarget(n);
        }
        return node;
    }

    public List<Node> GetAllTargets(string targetName)
    {
        string groupName = GroupPrefix + targetName;
        var tree = GetTree();

        if (tree != null && tree.HasGroup(groupName))
            return tree.GetNodesInGroup(groupName).ToList();

        return NamedEntities.TryGetValue(groupName, out var list) ? list : new List<Node>();
    }

    public void ParseConnections()
    {
        if (!Entity.ContainsKey("connections")) return;
        var connections = Entity["connections"].AsGodotDictionary();

        foreach (var outputKey in connections.Keys)
        {
            string output = outputKey.AsString();
            var connVal = connections[outputKey];
            var connList = connVal.VariantType == Variant.Type.Array
                ? connVal.AsGodotArray()
                : new Godot.Collections.Array { connVal };

            if (!_userSignals.ContainsKey(output))
                _userSignals[output] = new List<Callable>();

            foreach (var connData in connList)
            {
                var arr = connData.AsString().Split(',');
                string target = arr.Length > 0 ? arr[0] : "";
                string input = arr.Length > 1 ? arr[1] : "";
                string param = arr.Length > 2 ? arr[2] : "";
                float delay = arr.Length > 3 && float.TryParse(arr[3], out var d) ? d : 0.0f;

                if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(target)) continue;

                string capturedTarget = target, capturedInput = input, capturedParam = param;
                float capturedDelay = delay;
                _userSignals[output].Add(Callable.From(() =>
                    CallTargetInput(capturedTarget, capturedInput, capturedParam, capturedDelay, this)));
            }
        }
    }

    public void TriggerOutput(string output)
    {
        if (!Enabled) return;
        if (!_userSignals.TryGetValue(output, out var callbacks)) return;
        foreach (var cb in callbacks) cb.Call();
    }

    public VMFNode? GetVmfNode()
    {
        var p = GetParent();
        while (p != null)
        {
            if (p is VMFNode vmf) return vmf;
            p = p.GetParent();
        }
        return null;
    }

    public bool HasFlag(int flag)
    {
        if (!Entity.TryGetValue("spawnflags", out var sf)) return false;
        int flags = sf.AsInt32();
        return (flags & flag) != 0;
    }

    public ArrayMesh? GetMesh(bool cleanup = true, bool lods = true, Vector3? offset = null)
    {
        if (!HasSolid) return null;

        var solidVal = Entity["solid"];
        var solids = solidVal.VariantType == Variant.Type.Array
            ? solidVal.AsGodotArray()
            : new Godot.Collections.Array { solidVal };

        var worldDict = new GodotDict { { "solid", solids } };
        var sourceStr = (Entity.TryGetValue("classname", out var cn) ? cn.AsString() : "entity")
                      + "_" + (Entity.TryGetValue("id", out var id) ? id.AsString() : "0");
        var raw = new GodotDict { { "source", sourceStr }, { "world", worldDict } };

        var effectiveOffset = offset ?? GlobalPosition;
        var vmfStruct = new VMFStructure(raw);
        var mesh = cleanup
            ? VMFTool.CleanupMesh(VMFTool.CreateMesh(vmfStruct, effectiveOffset))
            : VMFTool.CreateMesh(vmfStruct, effectiveOffset);

        if (mesh == null) return null;
        return lods ? VMFTool.GenerateLods(mesh) : mesh;
    }

    public static Vector3 ConvertVector(Vector3 v) => new Vector3(v.X, v.Z, -v.Y);

    public static Vector3 ConvertDirection(Vector3 v)
    {
        var rad = v / 180.0f * Mathf.Pi;
        return Basis.FromEuler(new Vector3(rad.Z, rad.Y, -rad.X), (EulerOrder)3).GetEuler();
    }

    public static Transform3D GetEntityTransform(VMFEntity ent)
    {
        var a = ent.Angles / 180.0f * Mathf.Pi;
        var angles = new Vector3(a.Z, a.Y, -a.X);
        var basis = Basis.FromEuler(angles, (EulerOrder)3);
        var pos = ent.Origin * VMFConfig.Import.Scale;
        pos = new Vector3(pos.X, pos.Z, -pos.Y);
        return new Transform3D(basis, pos);
    }

    public static Basis GetEntityBasis(VMFEntity ent) => GetEntityTransform(ent).Basis;

    public static Vector3 GetMovementVector(Vector3 v)
    {
        var b = Basis.FromEuler(new Vector3(v.X, -v.Y, -v.Z) / 180.0f * Mathf.Pi);
        var m = b.Z;
        return new Vector3(m.Z, m.Y, m.X);
    }

    public Shape3D? GetEntityShape()
    {
        if (!HasSolid) return null;
        var solidVal = Entity["solid"];
        bool useConvex = solidVal.VariantType == Variant.Type.Dictionary
                      || (solidVal.VariantType == Variant.Type.Array && solidVal.AsGodotArray().Count == 1);
        return useConvex ? (Shape3D?)GetEntityConvexShape() : GetEntityTrimeshShape();
    }

    public ConvexPolygonShape3D? GetEntityConvexShape()
    {
        if (!HasSolid) return null;
        var solidVal = Entity["solid"];
        var solids = solidVal.VariantType == Variant.Type.Array
            ? solidVal.AsGodotArray()
            : new Godot.Collections.Array { solidVal };

        var raw = new GodotDict { { "world", new GodotDict { { "solid", solids } } } };
        var mesh = VMFTool.CreateMesh(new VMFStructure(raw), GlobalPosition);
        if (mesh == null || mesh.GetSurfaceCount() == 0) return null;
        return mesh.CreateConvexShape();
    }

    public ConcavePolygonShape3D? GetEntityTrimeshShape()
    {
        if (!HasSolid) return null;
        var solidVal = Entity["solid"];
        var solids = solidVal.VariantType == Variant.Type.Array
            ? solidVal.AsGodotArray()
            : new Godot.Collections.Array { solidVal };

        var combiner = new CsgCombiner3D();
        foreach (var solid in solids)
        {
            var raw = new GodotDict { { "world", new GodotDict { { "solid", new Godot.Collections.Array { solid } } } } };
            var mesh = VMFTool.CreateMesh(new VMFStructure(raw), GlobalPosition);
            if (mesh == null || mesh.GetSurfaceCount() == 0) continue;
            var csgMesh = new CsgMesh3D { Mesh = mesh };
            combiner.AddChild(csgMesh);
        }

        combiner.Call("_update_shape");
        var meshes = combiner.GetMeshes();
        var shape = (meshes[1].As<ArrayMesh>()).CreateTrimeshShape();
        combiner.QueueFree();
        return shape;
    }

    public Godot.Collections.Array<CollisionShape3D> GetSeparatedCollisions()
    {
        var collisions = new Godot.Collections.Array<CollisionShape3D>();
        if (!HasSolid) return collisions;

        var solidVal = Entity["solid"];
        var solids = solidVal.VariantType == Variant.Type.Array
            ? solidVal.AsGodotArray()
            : new Godot.Collections.Array { solidVal };

        foreach (var solid in solids)
        {
            var raw = new GodotDict { { "world", new GodotDict { { "solid", new Godot.Collections.Array { solid } } } } };
            var mesh = VMFTool.CreateMesh(new VMFStructure(raw), GlobalPosition);
            if (mesh == null) continue;
            var cs = new CollisionShape3D { Shape = mesh.CreateConvexShape(true) };
            collisions.Add(cs);
        }
        return collisions;
    }
}
