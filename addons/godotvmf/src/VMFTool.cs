using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotVMF;

public static class VMFTool
{
    public static void GenerateCollisions(MeshInstance3D meshInstance, int physicsMask)
    {
        var mesh = meshInstance.Mesh;
        var surfaceProps = new Dictionary<string, ArrayMesh>();
        var corrector = new VMFGeometryCorrector();
        var extendCorrector = (Engine.GetMainLoop() as SceneTree)?.Root?
            .GetNodeOrNull("VMFExtendGeometryCorrector");

        for (int surfaceIdx = 0; surfaceIdx < mesh.GetSurfaceCount(); surfaceIdx++)
        {
            var material = mesh.SurfaceGetMaterial(surfaceIdx);
            var materialName = mesh.GetMeta("surface_material_" + surfaceIdx, "").AsString().ToLower();

            bool isIgnored = VMFConfig.Materials.Ignore.Any(rx => materialName.Match(rx.AsString().ToLower(), false));
            if (isIgnored) continue;

            var compileKeys = material?.GetMeta("compile_keys", new Godot.Collections.Array())
                .AsGodotArray() ?? new Godot.Collections.Array();
            var surfacePropVal = material?.GetMeta("surfaceprop", "default") ?? (Variant)"default";
            string surfaceProp = surfacePropVal.VariantType == Variant.Type.Array
                ? surfacePropVal.AsGodotArray()[0].AsString()
                : surfacePropVal.AsString();

            if (compileKeys.Count > 0)
                surfaceProp = "tool_" + compileKeys[0].AsString();

            bool isNoCollision = false;
            foreach (var keyVar in compileKeys)
            {
                string key = keyVar.AsString();
                if (extendCorrector != null)
                {
                    var ncList = extendCorrector.Get("nocollision");
                    if (ncList.VariantType == Variant.Type.Array &&
                        ncList.AsGodotArray().Any(k => k.AsString() == key))
                    { isNoCollision = true; break; }
                }
                if (corrector.HasNoCollision(key)) { isNoCollision = true; break; }
            }
            if (isNoCollision) continue;

            if (!surfaceProps.ContainsKey(surfaceProp))
                surfaceProps[surfaceProp] = new ArrayMesh();

            var arrayMesh = surfaceProps[surfaceProp];
            var arrays = mesh.SurfaceGetArrays(surfaceIdx);
            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

            if (compileKeys.Count > 0)
                arrayMesh.SetMeta("compile_keys", compileKeys);
        }

        foreach (var (spKey, spMesh) in surfaceProps)
        {
            var staticBody = new StaticBody3D { Name = "surface_prop_" + spKey };
            staticBody.CollisionLayer = (uint)physicsMask;
            staticBody.SetMeta("surface_prop", spKey);

            var compileKeys = spMesh.GetMeta("compile_keys", new Godot.Collections.Array()).AsGodotArray();
            foreach (var keyVar in compileKeys)
            {
                string key = keyVar.AsString();
                if (extendCorrector != null && extendCorrector.HasMethod(key))
                { extendCorrector.Call(key, staticBody); continue; }
                if (corrector.HasHandler(key))
                    corrector.CallHandler(key, staticBody);
            }

            var collision = new CollisionShape3D
            {
                Name = "collision",
                Shape = spMesh.CreateTrimeshShape(),
            };
            staticBody.AddChild(collision);
            collision.Owner = staticBody;

            meshInstance.AddChild(staticBody);
            VMFUtils.SetOwnerRecursive(staticBody, meshInstance.Owner);
        }
    }

    public static ArrayMesh? CleanupMesh(ArrayMesh originalMesh)
    {
        bool is44 = false;
        if (Engine.GetVersionInfo().TryGetValue("minor", out var minor))
            is44 = minor.AsInt32() >= 4;

        var duplicatedMesh = new ArrayMesh();
        var corrector = new VMFGeometryCorrector();
        var extendCorrector = (Engine.GetMainLoop() as SceneTree)?.Root?
            .GetNodeOrNull("VMFExtendGeometryCorrector");

        var mt = new MeshDataTool();

        int surfaceCount = originalMesh.GetSurfaceCount();
        int surfaceRemoved = 0;
        for (int surfaceIdx = 0; surfaceIdx < surfaceCount; surfaceIdx++)
        {
            int adjustedIdx = surfaceIdx - surfaceRemoved;
            originalMesh.SetMeta(
                "surface_material_" + adjustedIdx,
                originalMesh.GetMeta("surface_material_" + surfaceIdx, ""));

            var materialName = originalMesh.GetMeta("surface_material_" + adjustedIdx, "").AsString().ToLower();
            var material = originalMesh.SurfaceGetMaterial(adjustedIdx);
            var compileKeys = material?.GetMeta("compile_keys", new Godot.Collections.Array())
                .AsGodotArray() ?? new Godot.Collections.Array();

            bool isIgnored = VMFConfig.Materials.Ignore.Any(rx => materialName.Match(rx.AsString().ToLower(), false));
            if (isIgnored && is44)
            {
                originalMesh.SurfaceRemove(adjustedIdx);
                surfaceRemoved++;
                continue;
            }

            bool isNoRender = false;
            foreach (var keyVar in compileKeys)
            {
                if (isIgnored) break;
                string key = keyVar.AsString();
                if (extendCorrector != null)
                {
                    var nrList = extendCorrector.Get("norender");
                    if (nrList.VariantType == Variant.Type.Array
                        && nrList.AsGodotArray()
                            .Any(k => k.AsString().Equals(key, StringComparison.OrdinalIgnoreCase)))
                    {
                        isNoRender = true;
                        break;
                    }
                }
                if (corrector.HasNoRender(key)) { isNoRender = true; break; }
            }

            if (isNoRender && is44)
            {
                originalMesh.SurfaceRemove(adjustedIdx);
                surfaceRemoved++;
                continue;
            }

            if (isNoRender || is44)
                continue;

            mt.CreateFromSurface(originalMesh, adjustedIdx);
            mt.CommitToSurface(duplicatedMesh, (ulong)adjustedIdx);
            duplicatedMesh.SetMeta("surface_material_" + adjustedIdx, materialName);
        }

        return is44 ? originalMesh : duplicatedMesh;
    }

    public static bool IsMaterialTransparent(Material? material)
    {
        if (material is ShaderMaterial) return true;
        if (material is BaseMaterial3D bm)
            return bm.Transparency != BaseMaterial3D.TransparencyEnum.Disabled;
        return false;
    }

    public static void RemoveMergedFaces(VMFSolid brushA, List<VMFSolid> brushes)
    {
        foreach (var brushB in brushes)
        {
            if (brushA == brushB) continue;
            if (brushA.Max.X < brushB.Min.X || brushB.Max.X < brushA.Min.X) continue;
            if (brushA.Max.Y < brushB.Min.Y || brushB.Max.Y < brushA.Min.Y) continue;
            if (brushA.Max.Z < brushB.Min.Z || brushB.Max.Z < brushA.Min.Z) continue;

            for (int ai = brushA.Sides.Count - 1; ai >= 0; ai--)
            {
                var sideA = brushA.Sides[ai];
                bool aRemoved = false;

                for (int bi = brushB.Sides.Count - 1; bi >= 0; bi--)
                {
                    var sideB = brushB.Sides[bi];
                    if (sideA.Plane.Normal.Dot(sideB.Plane.Normal) > -0.99f) continue;
                    if (sideA.Plane.GetCenter().DistanceTo(sideB.Plane.GetCenter()) > 0.01f) continue;

                    var matA = VMTLoader.GetMaterial(sideA.Material);
                    var matB = VMTLoader.GetMaterial(sideB.Material);
                    if (IsMaterialTransparent(matA) || IsMaterialTransparent(matB)) continue;

                    if (sideA.IsEqualTo(sideB))
                    {
                        brushB.Sides.RemoveAt(bi);
                        brushA.Sides.RemoveAt(ai);
                        aRemoved = true;
                        break;
                    }
                    if (sideA.IsInsideOfFace(sideB))
                    {
                        brushA.Sides.RemoveAt(ai);
                        aRemoved = true;
                        break;
                    }
                }
                if (aRemoved) break;
            }
        }
    }

    public static ArrayMesh? CreateMesh(VMFStructure vmfStructure, Vector3 offset = default, bool optimized = true)
    {
        float importScale = VMFConfig.Import.Scale;

        if (vmfStructure.Solids.Count == 0) return null;

        var brushes = vmfStructure.Solids;
        var materialSides = new Dictionary<string, List<VMFSide>>();
        var mesh = new ArrayMesh();

        foreach (var brush in brushes)
        {
            if (optimized) RemoveMergedFaces(brush, brushes);
            foreach (var side in brush.Sides)
            {
                string material = side.Material.ToUpper();
                if (!materialSides.ContainsKey(material))
                    materialSides[material] = new List<VMFSide>();
                materialSides[material].Add(side);
            }
        }

        foreach (var sides in materialSides.Values)
        {
            var sf = new SurfaceTool();
            sf.Begin(Mesh.PrimitiveType.Triangles);
            int index = 0;

            foreach (var side in sides)
            {
                int baseIndex = index;

                if (!side.IsDisplacement && side.Solid.HasDisplacement) continue;
                if (side.Vertices.Length < 3)
                {
                    VMFLogger.Error("Side corrupted: " + side.Id);
                    continue;
                }

                if (!side.IsDisplacement)
                {
                    var normal = side.Plane.Normal;
                    uint sg = side.SmoothingGroups == 0 ? uint.MaxValue : (uint)side.SmoothingGroups;
                    sf.SetNormal(new Vector3(normal.X, normal.Z, -normal.Y));
                    sf.SetColor(Colors.White);
                    sf.SetSmoothGroup(sg);

                    foreach (var v in side.Vertices)
                    {
                        sf.SetUV(side.GetUv(v));
                        sf.AddVertex(new Vector3(v.X, v.Z, -v.Y) * importScale - offset);
                        index++;
                    }
                    for (int i = 1; i < side.Vertices.Length - 1; i++)
                    {
                        sf.AddIndex(baseIndex);
                        sf.AddIndex(baseIndex + i);
                        sf.AddIndex(baseIndex + i + 1);
                    }
                }
                else
                {
                    var disp = side.DispInfo!;
                    int vc = (int)disp.VertsCount;
                    int ec = (int)disp.EdgesCount;
                    sf.SetSmoothGroup(1);

                    for (int i = 0; i < disp.Vertices.Length; i++)
                    {
                        int x = i / vc;
                        int y = i % vc;
                        var v = disp.Vertices[i];
                        var normal = disp.GetNormal(x, y);
                        var dist = disp.GetDistance(x, y);
                        var voffset = disp.GetOffset(x, y);
                        var uv = side.GetUv(v - dist - voffset);

                        sf.SetUV(uv);
                        sf.SetColor(disp.GetColor(x, y));
                        sf.SetNormal(new Vector3(normal.X, normal.Z, -normal.Y));
                        sf.AddVertex(new Vector3(v.X, v.Z, -v.Y) * importScale - offset);
                        index++;
                    }

                    for (int i = 0; i < ec * ec; i++)
                    {
                        int x = i / ec;
                        int y = i % ec;
                        bool isOdd = (x + y) % 2 == 1;

                        if (isOdd)
                        {
                            sf.AddIndex(baseIndex + x + 1 + y * vc);
                            sf.AddIndex(baseIndex + x + (y + 1) * vc);
                            sf.AddIndex(baseIndex + x + 1 + (y + 1) * vc);
                            sf.AddIndex(baseIndex + x + y * vc);
                            sf.AddIndex(baseIndex + x + (y + 1) * vc);
                            sf.AddIndex(baseIndex + x + 1 + y * vc);
                        }
                        else
                        {
                            sf.AddIndex(baseIndex + x + y * vc);
                            sf.AddIndex(baseIndex + x + (y + 1) * vc);
                            sf.AddIndex(baseIndex + x + 1 + (y + 1) * vc);
                            sf.AddIndex(baseIndex + x + y * vc);
                            sf.AddIndex(baseIndex + x + 1 + (y + 1) * vc);
                            sf.AddIndex(baseIndex + x + 1 + y * vc);
                        }
                    }
                }
            }

            if (index == 0) continue;

            var material = VMTLoader.GetMaterial(sides[0].Material);
            if (material != null) sf.SetMaterial(material);

            if (optimized) sf.OptimizeIndicesForCache();
            sf.GenerateNormals();
            sf.GenerateTangents();
            sf.Commit(mesh);
            mesh.SetMeta("surface_material_" + (mesh.GetSurfaceCount() - 1), sides[0].Material);
        }

        return mesh;
    }

    public static ArrayMesh GenerateLods(ArrayMesh mesh)
    {
        if (mesh.GetSurfaceCount() == 0) return mesh;

        var importerMesh = new ImporterMesh();
        for (int i = 0; i < mesh.GetSurfaceCount(); i++)
        {
            importerMesh.AddSurface(
                Mesh.PrimitiveType.Triangles,
                mesh.SurfaceGetArrays(i),
                new Godot.Collections.Array<Godot.Collections.Array>(),
                new Godot.Collections.Dictionary(),
                mesh.SurfaceGetMaterial(i),
                "surface_" + i);
        }
        importerMesh.GenerateLods(60, 60, new Godot.Collections.Array());
        return importerMesh.GetMesh();
    }
}
