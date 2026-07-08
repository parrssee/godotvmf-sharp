using System.Collections.Generic;
using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

public class VMFStructure
{
    public List<VMFSolid> Solids = new();
    public List<VMFEntity> Entities = new();

    public VMFStructure(GodotDict raw)
    {
        if (!raw.TryGetValue("world", out var worldVal)) return;
        var world = worldVal.AsGodotDictionary();

        var rawSolids = world.TryGetValue("solid", out var sv) ? sv : default;
        var solidsArr = rawSolids.VariantType == Variant.Type.Array
            ? rawSolids.AsGodotArray()
            : rawSolids.VariantType == Variant.Type.Nil
                ? new Godot.Collections.Array()
                : new Godot.Collections.Array { rawSolids };

        foreach (var s in solidsArr)
            if (s.VariantType == Variant.Type.Dictionary)
                Solids.Add(new VMFSolid(s.AsGodotDictionary()));

        var rawEntities = raw.TryGetValue("entity", out var ev) ? ev : default;
        var entitiesArr = rawEntities.VariantType == Variant.Type.Array
            ? rawEntities.AsGodotArray()
            : rawEntities.VariantType == Variant.Type.Nil
                ? new Godot.Collections.Array()
                : new Godot.Collections.Array { rawEntities };

        foreach (var e in entitiesArr)
            if (e.VariantType == Variant.Type.Dictionary)
                Entities.Add(new VMFEntity(e.AsGodotDictionary()));
    }
}
