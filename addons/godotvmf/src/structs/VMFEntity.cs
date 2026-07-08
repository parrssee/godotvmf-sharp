using System.Collections.Generic;
using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

public class VMFEntity
{
    public int Id = -1;
    public GodotDict Data = new();
    public bool HasSolid;
    public List<VMFSolid> Solids = new();
    public string Classname = "";
    public string? Targetname;
    public string? Parentname;
    public Vector3 Angles = Vector3.Zero;
    public Vector3 Origin = Vector3.Zero;

    public VMFEntity(GodotDict raw)
    {
        Id = raw.TryGetValue("id", out var id) ? id.AsInt32() : -1;
        Classname = raw.TryGetValue("classname", out var cn) ? cn.AsString() : "";
        Data = raw;

        HasSolid = raw.ContainsKey("solid")
            && raw["solid"].VariantType == Variant.Type.Dictionary;

        Angles = GetVector3(raw, "angles");
        Origin = GetVector3(raw, "origin");

        if (HasSolid)
        {
            var rawSolids = raw["solid"];
            var arr = rawSolids.VariantType == Variant.Type.Array
                ? rawSolids.AsGodotArray()
                : new Godot.Collections.Array { rawSolids };

            foreach (var sv in arr)
                if (sv.VariantType == Variant.Type.Dictionary)
                    Solids.Add(new VMFSolid(sv.AsGodotDictionary()));
        }
    }

    private static Vector3 GetVector3(GodotDict raw, string key)
    {
        if (!raw.TryGetValue(key, out var val)) return Vector3.Zero;
        return val.VariantType == Variant.Type.Vector3 ? val.AsVector3() : Vector3.Zero;
    }
}
