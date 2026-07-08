using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

public static class VMFInstanceManager
{
    private static readonly Dictionary<string, PackedScene> InstancesCache = new();

    public static string GetInstancePath(GodotDict entity)
    {
        string instancePath = entity.TryGetValue("file", out var f) ? f.AsString() : "";
        if (!instancePath.EndsWith(".vmf")) instancePath += ".vmf";
        string instanceFilename = System.IO.Path.GetFileName(instancePath);
        string mapBaseFolder = entity.TryGetValue("vmf", out var vmfVal)
            ? System.IO.Path.GetDirectoryName(vmfVal.AsString()) ?? ""
            : "";

        string mapsFolder = VMFUtils.NormalizePath(VMFConfig.GameinfoPath + "/maps");
        string mapsrcFolder = VMFUtils.NormalizePath(VMFConfig.GameinfoPath + "/mapsrc");

        var paths = new[]
        {
            VMFUtils.NormalizePath(mapBaseFolder + "/instances/" + instanceFilename),
            VMFUtils.NormalizePath(mapBaseFolder + "/" + instanceFilename),
            VMFUtils.NormalizePath(mapBaseFolder + "/instances/" + instancePath),
            VMFUtils.NormalizePath(mapBaseFolder + "/" + instancePath),
            VMFUtils.NormalizePath(mapsFolder + "/instances/" + instancePath),
            VMFUtils.NormalizePath(mapsFolder + "/" + instancePath),
            VMFUtils.NormalizePath(mapsrcFolder + "/instances/" + instancePath),
            VMFUtils.NormalizePath(mapsrcFolder + "/" + instancePath),
        };

        foreach (var path in paths)
            if (FileAccess.FileExists(path)) return path;

        return "";
    }

    public static Godot.Collections.Array GetSubinstances(GodotDict structure, GodotDict entitySource)
    {
        var result = new Godot.Collections.Array();
        if (!structure.ContainsKey("entity")) return result;

        var entVal = structure["entity"];
        var entities = entVal.VariantType == Variant.Type.Array
            ? entVal.AsGodotArray()
            : new Godot.Collections.Array { entVal };

        foreach (var ev in entities)
        {
            if (ev.VariantType != Variant.Type.Dictionary) continue;
            var ent = ev.AsGodotDictionary();
            if (ent.TryGetValue("classname", out var cn) && cn.AsString() == "func_instance")
            {
                ent["vmf"] = entitySource.TryGetValue("file", out var fv) ? fv : (Variant)"";
                result.Add(ent);
            }
        }
        return result;
    }

    public static PackedScene? LoadInstance(string instancePath)
    {
        if (!ResourceLoader.Exists(instancePath))
        {
            VMFLogger.Error("Failed to find instance file: " + instancePath);
            return null;
        }
        if (InstancesCache.TryGetValue(instancePath, out var cached)) return cached;

        var scn = ResourceLoader.Load<PackedScene>(instancePath);
        if (scn != null) InstancesCache[instancePath] = scn;
        else VMFLogger.Error("Failed to load instance resource: " + instancePath);
        return scn;
    }

    public static PackedScene? ImportInstance(GodotDict entity)
    {
        string instanceVmfFile = GetInstancePath(entity);
        if (string.IsNullOrEmpty(instanceVmfFile))
        {
            VMFLogger.Error("Failed to find instance file for entity: " +
                (entity.TryGetValue("file", out var fv) ? fv.AsString() : "unknown"));
            return null;
        }

        string instanceName = System.IO.Path.GetFileNameWithoutExtension(instanceVmfFile);
        string mapPath = entity.TryGetValue("vmf", out var vmfV)
            ? VMFUtils.NormalizePath(System.IO.Path.GetDirectoryName(vmfV.AsString()) ?? "")
            : "unknown_map";
        string relPath = instanceVmfFile
            .Replace(".vmf", ".scn")
            .Replace(mapPath + "/", "")
            .Replace("instances/", "");
        string instanceScenePath = VMFConfig.Import.InstancesFolder + "/" + relPath;
        instanceScenePath = VMFUtils.NormalizePath(instanceScenePath);

        if (FileAccess.FileExists(instanceScenePath))
            return LoadInstance(instanceScenePath);

        var structure = VDFParser.Parse(instanceVmfFile);
        if (structure != null)
        {
            var subinstances = GetSubinstances(structure, entity);
            foreach (var sv in subinstances)
                if (sv.VariantType == Variant.Type.Dictionary)
                    ImportInstance(sv.AsGodotDictionary());
        }

        var scn = new PackedScene();
        var node = new VMFNode();
        node.SetMeta("instance", true);
        node.Vmf = instanceVmfFile;
        node.Name = instanceName + "_instance";
        node.SaveGeometry = false;
        node.SaveCollision = false;
        node.ImportMap();

        scn.Pack(node);

        string dir = VMFUtils.NormalizePath(System.IO.Path.GetDirectoryName(instanceScenePath) ?? "");
        if (!DirAccess.DirExistsAbsolute(dir))
            DirAccess.MakeDirRecursiveAbsolute(dir);

        var err = ResourceSaver.Save(scn, instanceScenePath, ResourceSaver.SaverFlags.Compress);
        if (err != Error.Ok)
        {
            VMFLogger.Error("Failed to save instance resource: " + err);
            node.QueueFree();
            return null;
        }

        node.QueueFree();
        return LoadInstance(instanceScenePath);
    }
}
