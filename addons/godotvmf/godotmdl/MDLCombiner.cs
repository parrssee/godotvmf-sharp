using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotArray = Godot.Collections.Array;

namespace GodotVMF;

public class MDLCombiner
{
    private readonly SurfaceTool _st = new();
    public readonly ArrayMesh ArrayMesh = new();
    public readonly MeshInstance3D MeshInstance = new();
    private readonly Skeleton3D _skeleton = new();

    private readonly MDLReader _mdl;
    private readonly VTXReader _vtx;
    private readonly VVDReader _vvd;
    private readonly PHYReader _phy;
    private readonly Godot.Collections.Dictionary _options;

    private bool IsStaticBody => (_mdl.Header != null) && ((_mdl.Header.Flags & (int)MDLReader.MDLFlag.StaticProp) != 0);

    private float Scale => _options.TryGetValue("use_global_scale", out var useGlobal) && useGlobal.AsBool()
        ? VMFConfig.Import.Scale
        : (_options.TryGetValue("scale", out var s) ? s.AsSingle() : 0.02f);

    private Vector3 RotationRadians => _options.TryGetValue("additional_rotation", out var rot)
        ? rot.AsVector3() / 180f * MathF.PI
        : Vector3.Zero;

    private Basis AdditionalBasis => Basis.FromEuler(RotationRadians);

    public MDLCombiner(MDLReader mdl, VTXReader vtx, VVDReader vvd, PHYReader phy, Godot.Collections.Dictionary options)
    {
        _mdl = mdl; _vtx = vtx; _vvd = vvd; _phy = phy; _options = options;
        SetupMeshInstance();
        SetupSkeleton();
        GenerateLods();
        GenerateCollision();
        CreateOccluder();
        AssignMaterials();
    }

    private void SetupMeshInstance()
    {
        int bodyPartIndex = 0;
        foreach (var bp in _vtx.BodyParts)
        {
            ProcessBodyPart(bp, bodyPartIndex);
            bodyPartIndex++;
        }

        ArrayMesh.LightmapUnwrap(Transform3D.Identity, VMFConfig.Models.LightmapTexelSize);
        MeshInstance.Name = "mesh";
        MeshInstance.GIMode = (GeometryInstance3D.GIModeEnum)
            (_options.TryGetValue("gi_mode", out var gim) ? gim.AsInt32() : (int)GeometryInstance3D.GIModeEnum.Dynamic);
        MeshInstance.Mesh = ArrayMesh;
    }

    private void ProcessBodyPart(VTXReader.VTXBodyPart bp, int bpIdx)
    {
        int modelIdx = 0;
        foreach (var model in bp.Models) { ProcessModel(model, bpIdx, modelIdx); modelIdx++; }
    }

    private void ProcessModel(VTXReader.VTXModel model, int bpIdx, int modelIdx)
    {
        if (model.Lods.Count == 0) return;
        ProcessLod(model.Lods[0], bpIdx, modelIdx);
    }

    private void ProcessLod(VTXReader.VTXLod lod, int bpIdx, int modelIdx)
    {
        int meshIdx = 0;
        foreach (var mesh in lod.Meshes) { ProcessMesh(mesh, bpIdx, modelIdx, meshIdx); meshIdx++; }
    }

    private void ProcessMesh(VTXReader.VTXMesh mesh, int bpIdx, int modelIdx, int meshIdx)
    {
        if (_vvd.Header == null) return;
        var mdlModel = _mdl.BodyParts[bpIdx].Models[modelIdx];
        var mdlMesh = mdlModel.Meshes[meshIdx];
        var basis = AdditionalBasis;
        float scale = Scale;

        int modelVertexIndexStart = mdlModel.VertIndex / 0x30;

        foreach (var sg in mesh.StripGroups)
        {
            _st.Begin(Mesh.PrimitiveType.Triangles);
            foreach (var vertInfo in sg.Vertices)
            {
                int vid = _vvd.FindVertexIndex(modelVertexIndexStart + mdlMesh.VertexIndexStart + vertInfo.OrigMeshVertId);
                if (vid < 0 || vid >= _vvd.Vertices.Count) continue;
                var vert = _vvd.Vertices[vid];
                var tangent = vid < _vvd.Tangents.Count ? _vvd.Tangents[vid] : default;

                _st.SetNormal(vert.Normal * basis);
                _st.SetTangent(tangent);
                _st.SetUV(vert.Uv);
                if (vert.BoneWeight != null)
                {
                    _st.SetBones(vert.BoneWeight.BoneBytes);
                    _st.SetWeights(vert.BoneWeight.WeightBytes);
                }
                _st.AddVertex(vert.Position * basis.Scaled(Vector3.One * scale));
            }

            foreach (var idx in sg.Indices)
            {
                if (idx > sg.Vertices.Count - 1) break;
                _st.AddIndex(idx);
            }

            _st.Commit(ArrayMesh);
        }
    }

    private void GenerateLods()
    {
        if (!(_options.TryGetValue("generate_lods", out var genLods) && genLods.AsBool())) return;

        var importerMesh = new ImporterMesh();
        for (int i = 0; i < ArrayMesh.GetSurfaceCount(); i++)
        {
            importerMesh.AddSurface(
                Mesh.PrimitiveType.Triangles,
                ArrayMesh.SurfaceGetArrays(i),
                new Godot.Collections.Array<Godot.Collections.Array>(),
                new Godot.Collections.Dictionary(),
                ArrayMesh.SurfaceGetMaterial(i),
                "surface_" + i);
        }

        importerMesh.GenerateLods(60f, -1f, new GodotArray());
        var mesh = importerMesh.GetMesh();
        if (mesh == null) return;

        foreach (StringName meta in mesh.GetMetaList())
            mesh.SetMeta(meta, mesh.GetMeta(meta));

        MeshInstance.Mesh = mesh;
    }

    private void GenerateCollision()
    {
        var yupBasis = new Basis(Vector3.Right, Mathf.Pi / 2f);
        var basis = AdditionalBasis;
        float scale = Scale;
        StaticBody3D? staticBody = null;

        for (int si = 0; si < _phy.Surfaces.Count; si++)
        {
            var surface = _phy.Surfaces[si];
            for (int solidIdx = 0; solidIdx < surface.Solids.Count; solidIdx++)
            {
                if (solidIdx == surface.Solids.Count - 1 && surface.Solids.Count > 1) break;
                var solid = surface.Solids[solidIdx];

                if (!IsStaticBody)
                {
                    staticBody = new StaticBody3D { Name = $"solid_{si}_{solidIdx}" };
                }
                else
                {
                    bool isNew = staticBody == null;
                    staticBody ??= new StaticBody3D();
                    if (isNew) staticBody.Name = "static_body";
                }

                var collision = new CollisionShape3D { Name = $"collision_{si}_{solidIdx}" };
                var shape = new ConvexPolygonShape3D();

                staticBody!.Basis *= basis;

                var vertices = new List<Vector3>();
                foreach (var face in solid.Faces)
                {
                    if (face.V1 < surface.Vertices.Count) vertices.Add(surface.Vertices[face.V1] * basis.Scaled(Vector3.One * scale));
                    if (face.V2 < surface.Vertices.Count) vertices.Add(surface.Vertices[face.V2] * basis.Scaled(Vector3.One * scale));
                    if (face.V3 < surface.Vertices.Count) vertices.Add(surface.Vertices[face.V3] * basis.Scaled(Vector3.One * scale));
                }

                shape.Points = vertices.ToArray();
                collision.Shape = shape;

                if (!IsStaticBody)
                {
                    var boneAttach = new BoneAttachment3D
                    {
                        Name = $"bone_attachment_{si}_{solidIdx}",
                        BoneIdx = Math.Max(0, solid.BoneIndex - 1)
                    };
                    boneAttach.AddChild(staticBody);
                    _skeleton.AddChild(boneAttach);
                    boneAttach.Owner = MeshInstance;
                    staticBody.Owner = MeshInstance;
                }
                else
                {
                    MeshInstance.AddChild(staticBody);
                    staticBody.Owner = MeshInstance;
                }

                staticBody.AddChild(collision);
                collision.Owner = MeshInstance;
            }
        }
    }

    private void CreateOccluder()
    {
        if (!(_options.TryGetValue("generate_occluder", out var genOcc) && genOcc.AsBool())) return;

        var occluder = new OccluderInstance3D { Name = "occluder" };
        Occluder3D? box;

        bool primitiveOccluder = _options.TryGetValue("primitive_occluder", out var prim) && prim.AsBool();

        if (!primitiveOccluder)
        {
            var colliders = new List<CollisionShape3D>();
            CollectChildren(MeshInstance, colliders);

            var st = new SurfaceTool();
            var am = new ArrayMesh();
            int beginVid = 0;
            st.Begin(Mesh.PrimitiveType.Triangles);

            foreach (var child in colliders)
            {
                if (child.Shape is not ConcavePolygonShape3D cps) continue;
                var points = cps.GetFaces();
                foreach (var p in points) st.AddVertex(p);
                for (int i = 0; i < points.Length; i++) st.AddIndex(beginVid + i);
                beginVid += points.Length;
            }

            st.Commit(am);
            if (am.GetSurfaceCount() > 0)
            {
                var arrays = am.SurfaceGetArrays(0);
                var ao = new ArrayOccluder3D();
                ao.Vertices = arrays[(int)ArrayMesh.ArrayType.Vertex].As<Vector3[]>();
                ao.Indices = arrays[(int)ArrayMesh.ArrayType.Index].As<int[]>();
                box = ao;
            }
            else
            {
                box = new ArrayOccluder3D();
            }
        }
        else
        {
            var bo = new BoxOccluder3D();
            var aabb = MeshInstance.Mesh?.GetAabb() ?? default;
            var occScale = _options.TryGetValue("primitive_occluder_scale", out var ps) ? ps.AsVector3() : Vector3.One;
            bo.Size = aabb.Size * occScale;
            occluder.Position = aabb.Position + aabb.Size / 2f;
            box = bo;
        }

        occluder.Occluder = box;
        MeshInstance.AddChild(occluder);
        occluder.Owner = MeshInstance;
    }

    private static void CollectChildren(Node node, List<CollisionShape3D> result)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is CollisionShape3D cs) result.Add(cs);
            CollectChildren(child, result);
        }
    }

    private void SetupSkeleton()
    {
        if (Engine.GetVersionInfo()["minor"].AsInt32() < 4) return;
        if (IsStaticBody) return;

        var basis = AdditionalBasis;
        _skeleton.Basis = basis.Inverse();

        foreach (var bone in _mdl.Bones)
            _skeleton.AddBone(bone.Name);

        foreach (var bone in _mdl.Bones)
        {
            if (bone.Parent != -1)
                _skeleton.SetBoneParent(bone.Id, bone.Parent);

            var parentBone = bone.Parent >= 0 && bone.Parent < _mdl.Bones.Count ? _mdl.Bones[bone.Parent]
                           : (_mdl.Bones.Count > 0 ? _mdl.Bones[^1] : null);
            var parentTransform = parentBone?.PosToBone ?? Transform3D.Identity;
            var targetTransform = bone.PosToBone * parentTransform;

            _skeleton.Call("set_bone_global_pose_override", bone.Id, targetTransform, 1.0f);

            float scale = Scale;
            var transform = new Transform3D(new Basis(bone.Quat), bone.Pos);
            transform = transform.Scaled(Vector3.One * scale);

            _skeleton.SetBonePosePosition(bone.Id, transform.Origin);
            _skeleton.SetBonePoseRotation(bone.Id, transform.Basis.GetRotationQuaternion());

            var targetRestPose = _skeleton.GetBonePose(bone.Id);
            _skeleton.SetBoneRest(bone.Id, targetRestPose);
            _skeleton.ResetBonePose(bone.Id);
        }

        var skin = _skeleton.CreateSkinFromRestTransforms();
        _skeleton.Name = "skeleton";
        MeshInstance.Set("skeleton", new NodePath("skeleton"));
        MeshInstance.AddChild(_skeleton);
        MeshInstance.Skin = skin;
        _skeleton.Owner = MeshInstance;
    }

    private void AssignMaterials()
    {
        var materials = new List<Material?>();

        foreach (var tex in _mdl.Textures)
        {
            foreach (var dir in _mdl.TextureDirs)
            {
                var path = VMFUtils.NormalizePath(dir + "/" + tex.Name).ToLower();
                if (!VMTLoader.HasMaterial(path)) continue;
                var mat = VMTLoader.GetMaterial(path);
                if (mat == null) continue;
                materials.Add(mat);
            }
        }

        int surfaces = ArrayMesh.GetSurfaceCount();
        for (int skinId = 0; skinId < _mdl.SkinFamilies.Count; skinId++)
        {
            var skinFamily = _mdl.SkinFamilies[skinId];
            var skinMaterials = new Material?[surfaces];
            for (int i = 0; i < surfaces; i++)
            {
                if (i >= skinFamily.Count) continue;
                int matIdx = skinFamily[i];
                if (matIdx >= materials.Count) continue;
                skinMaterials[i] = materials[matIdx];
            }
            MeshInstance.SetMeta("skin_" + skinId, skinMaterials);
        }

        ApplySkin(MeshInstance, 0, true);
    }

    public static void ApplySkin(Node instance, int skinId, bool directly = false)
    {
        if (instance is not MeshInstance3D meshInstance) return;
        string metaKey = "skin_" + skinId;
        if (!meshInstance.HasMeta(metaKey)) return;

        var materials = meshInstance.GetMeta(metaKey).AsGodotArray();
        int surfaceCount = meshInstance.Mesh?.GetSurfaceCount() ?? 0;

        for (int i = 0; i < surfaceCount; i++)
        {
            if (i >= materials.Count) break;
            var mat = materials[i].As<Material>();
            if (mat == null) continue;
            if (directly)
                meshInstance.Mesh!.SurfaceSetMaterial(i, mat);
            else
                meshInstance.SetSurfaceOverrideMaterial(i, mat);
        }
    }
}
