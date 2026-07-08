using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotVMF;

public static class VMFResourceManager
{
    private static readonly string[] MaterialKeysToImport =
    {
        "$basetexture", "$basetexture2", "$bumpmap", "$bumpmap2", "$selfillummask",
    };

    public static bool HasImportedResources;

    private static EditorInterface? GetEditorInterface() =>
        Engine.HasSingleton("EditorInterface")
            ? Engine.GetSingleton("EditorInterface") as EditorInterface
            : null;

    public static async System.Threading.Tasks.Task ForResourceImport()
    {
        if (!HasImportedResources) return;
        var ei = GetEditorInterface();
        if (ei == null) return;
        var fs = ei.GetResourceFilesystem();
        if (fs == null) return;
        fs.Scan();
        await ei.ToSignal(fs, EditorFileSystem.SignalName.ResourcesReimported);
        HasImportedResources = false;
    }

    public static bool ImportModels(VMFStructure vmfStructure)
    {
        if (!VMFConfig.Models.Import) return false;
        if (vmfStructure.Entities.Count == 0) return false;

        foreach (var entity in vmfStructure.Entities)
        {
            if (!entity.Data.ContainsKey("model")) continue;
            if (entity.Classname != "prop_static") continue;

            string modelPath = entity.Data["model"].AsString().ToLower();
            modelPath = System.IO.Path.GetFileNameWithoutExtension(modelPath);
            if (string.IsNullOrEmpty(modelPath)) continue;

            string mdlPath = VMFUtils.NormalizePath(VMFConfig.GameinfoPath + "/" + modelPath + ".mdl");
            string vtxPath = VMFUtils.NormalizePath(VMFConfig.GameinfoPath + "/" + modelPath + ".vtx");
            string vtxDx90Path = VMFUtils.NormalizePath(VMFConfig.GameinfoPath + "/" + modelPath + ".dx90.vtx");
            string vvdPath = VMFUtils.NormalizePath(VMFConfig.GameinfoPath + "/" + modelPath + ".vvd");
            string phyPath = VMFUtils.NormalizePath(VMFConfig.GameinfoPath + "/" + modelPath + ".phy");
            string targetPath = VMFUtils.NormalizePath(VMFConfig.Models.TargetFolder + "/" + modelPath);

            if (ResourceLoader.Exists(targetPath + ".mdl")) continue;
            if (!FileAccess.FileExists(mdlPath)) continue;
            if (!FileAccess.FileExists(vtxPath)) vtxPath = vtxDx90Path;
            if (!FileAccess.FileExists(vtxPath)) continue;
            if (!FileAccess.FileExists(vvdPath)) continue;

            var modelMaterials = new MDLReader(mdlPath).GetPossibleMaterialPaths();
            foreach (var matPath in modelMaterials)
            {
                ImportTextures(matPath);
                ImportMaterial(matPath);
            }

            DirAccess.MakeDirRecursiveAbsolute(System.IO.Path.GetDirectoryName(targetPath)!);
            DirAccess.CopyAbsolute(vtxPath, targetPath + ".dx90.vtx");
            DirAccess.CopyAbsolute(vvdPath, targetPath + ".vvd");
            if (FileAccess.FileExists(phyPath))
                DirAccess.CopyAbsolute(phyPath, targetPath + ".phy");
            DirAccess.CopyAbsolute(mdlPath, targetPath + ".mdl");
            HasImportedResources = true;
        }
        return HasImportedResources;
    }

    public static bool ImportMaterial(string material)
    {
        material = material.ToLower();
        string vmtPath = VMFUtils.NormalizePath(VMFConfig.GameinfoPath + "/materials/" + material + ".vmt");
        string targetPath = VMFUtils.NormalizePath(VMFConfig.Materials.TargetFolder + "/" + material + ".vmt");

        if (ResourceLoader.Exists(targetPath)) return false;
        if (!FileAccess.FileExists(vmtPath)) return false;

        DirAccess.MakeDirRecursiveAbsolute(System.IO.Path.GetDirectoryName(targetPath)!);
        return DirAccess.CopyAbsolute(vmtPath, targetPath) == Error.Ok;
    }

    public static void ImportMaterials(VMFStructure vmfStructure, bool isRuntime = false)
    {
        if (VMFConfig.Materials.ImportModeValue == VMFConfig.MaterialsConfig.ImportMode.UseExisting)
            return;

        var list = new List<string>();
        var ignoreList = VMFConfig.Materials.Ignore.Select(rx => rx.AsString());

        foreach (var solid in vmfStructure.Solids)
        {
            foreach (var side in solid.Sides)
            {
                if (ignoreList.Any(rx => side.Material.Match(rx, false))) continue;
                if (!list.Contains(side.Material)) list.Add(side.Material);
            }
        }

        foreach (var entity in vmfStructure.Entities)
        {
            if (!entity.HasSolid) continue;
            foreach (var solid in entity.Solids)
            {
                foreach (var side in solid.Sides)
                {
                    if (ignoreList.Any(rx => side.Material.Match(rx, false))) continue;
                    if (!list.Contains(side.Material)) list.Add(side.Material);
                }
            }
        }

        if (!isRuntime && GetEditorInterface() != null)
        {
            foreach (var mat in list) ImportTextures(mat);
            foreach (var mat in list) ImportMaterial(mat);
        }
    }

    public static bool ImportTextures(string material)
    {
        material = material.ToLower();
        string targetPath = VMFUtils.NormalizePath(VMFConfig.GameinfoPath + "/materials/" + material + ".vmt");
        if (!FileAccess.FileExists(targetPath))
        {
            VMFLogger.Error("Material not found: " + targetPath);
            return false;
        }

        var details = VDFParser.Parse(targetPath, true);
        if (details == null || details.Count == 0) return false;

        var firstVal = details.Values.First();
        if (firstVal.VariantType != Variant.Type.Dictionary) return false;
        var detailsDict = firstVal.AsGodotDictionary();

        if (detailsDict.ContainsKey("insert"))
        {
            var insert = detailsDict["insert"].AsGodotDictionary();
            foreach (var k in insert.Keys) detailsDict[k] = insert[k];
        }

        foreach (var key in MaterialKeysToImport)
        {
            if (!detailsDict.TryGetValue(key, out var texVal)) continue;
            string vtfPath = VMFUtils.NormalizePath(VMFConfig.GameinfoPath + "/materials/" + texVal.AsString().ToLower() + ".vtf");
            string targetVtfPath = VMFUtils.NormalizePath(VMFConfig.Materials.TargetFolder + "/" + texVal.AsString().ToLower() + ".vtf");

            if (!FileAccess.FileExists(vtfPath)) continue;
            if (ResourceLoader.Exists(targetVtfPath)) continue;

            DirAccess.MakeDirRecursiveAbsolute(System.IO.Path.GetDirectoryName(targetVtfPath)!);
            if (DirAccess.CopyAbsolute(vtfPath, targetVtfPath) == Error.Ok)
                HasImportedResources = true;
        }
        return HasImportedResources;
    }
}
