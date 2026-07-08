using System.Collections.Generic;
using Godot;

namespace GodotVMF;

public class VTXReader
{
    public class VTXHeader
    {
        public long Address;
        public int Version, VertexCacheSize;
        public int MaxBonesPerStrip, MaxBonesPerTri, MaxBonesPerVertex;
        public int CheckSum, NumLods;
        public int MaterialReplacementListOffset;
        public int NumBodyParts, BodyPartOffset;

        public static VTXHeader Read(FileAccess file)
        {
            var r = new VTXHeader { Address = (long)file.GetPosition() };
            r.Version = ByteReader.ReadSignedInt(file);
            r.VertexCacheSize = ByteReader.ReadSignedInt(file);
            r.MaxBonesPerStrip = file.Get16();
            r.MaxBonesPerTri = file.Get16();
            r.MaxBonesPerVertex = ByteReader.ReadSignedInt(file);
            r.CheckSum = ByteReader.ReadSignedInt(file);
            r.NumLods = ByteReader.ReadSignedInt(file);
            r.MaterialReplacementListOffset = ByteReader.ReadSignedInt(file);
            r.NumBodyParts = ByteReader.ReadSignedInt(file);
            r.BodyPartOffset = ByteReader.ReadSignedInt(file);
            return r;
        }
    }

    public class VTXBodyPart
    {
        public long Address;
        public int NumModels, ModelOffset;
        public List<VTXModel> Models = new();

        public static VTXBodyPart Read(FileAccess file)
        {
            var r = new VTXBodyPart { Address = (long)file.GetPosition() };
            r.NumModels = ByteReader.ReadSignedInt(file);
            r.ModelOffset = ByteReader.ReadSignedInt(file);
            return r;
        }
    }

    public class VTXModel
    {
        public long Address;
        public int NumLods, LodOffset;
        public List<VTXLod> Lods = new();

        public static VTXModel Read(FileAccess file)
        {
            var r = new VTXModel { Address = (long)file.GetPosition() };
            r.NumLods = ByteReader.ReadSignedInt(file);
            r.LodOffset = ByteReader.ReadSignedInt(file);
            return r;
        }
    }

    public class VTXLod
    {
        public long Address;
        public int NumMeshes, MeshOffset;
        public float SwitchPoint;
        public List<VTXMesh> Meshes = new();

        public static VTXLod Read(FileAccess file)
        {
            var r = new VTXLod { Address = (long)file.GetPosition() };
            r.NumMeshes = ByteReader.ReadSignedInt(file);
            r.MeshOffset = ByteReader.ReadSignedInt(file);
            r.SwitchPoint = file.GetFloat();
            return r;
        }
    }

    public class VTXMesh
    {
        public long Address;
        public int NumStripGroups, StripGroupOffset, Flags;
        public int IdxBase;
        public List<VTXStripGroup> StripGroups = new();

        public static VTXMesh Read(FileAccess file)
        {
            var r = new VTXMesh { Address = (long)file.GetPosition() };
            r.NumStripGroups = ByteReader.ReadSignedInt(file);
            r.StripGroupOffset = ByteReader.ReadSignedInt(file);
            r.Flags = file.Get8();
            return r;
        }
    }

    public class VTXStripGroup
    {
        public long Address;
        public int NumVerts, VertOffset;
        public int NumIndices, IndexOffset;
        public int NumStrips, StripOffset, Flags;
        public List<int> Indices = new();
        public List<VTXVertex> Vertices = new();
        public List<VTXStripHeader> Strips = new();

        public static VTXStripGroup Read(FileAccess file)
        {
            var r = new VTXStripGroup { Address = (long)file.GetPosition() };
            r.NumVerts = ByteReader.ReadSignedInt(file);
            r.VertOffset = ByteReader.ReadSignedInt(file);
            r.NumIndices = ByteReader.ReadSignedInt(file);
            r.IndexOffset = ByteReader.ReadSignedInt(file);
            r.NumStrips = ByteReader.ReadSignedInt(file);
            r.StripOffset = ByteReader.ReadSignedInt(file);
            r.Flags = file.Get8();
            return r;
        }
    }

    public class VTXStripGroupCSGO : VTXStripGroup
    {
        public int NumTopologyIndices, TopologyOffset;

        public static new VTXStripGroupCSGO Read(FileAccess file)
        {
            var r = new VTXStripGroupCSGO { Address = (long)file.GetPosition() };
            r.NumVerts = ByteReader.ReadSignedInt(file);
            r.VertOffset = ByteReader.ReadSignedInt(file);
            r.NumIndices = ByteReader.ReadSignedInt(file);
            r.IndexOffset = ByteReader.ReadSignedInt(file);
            r.NumStrips = ByteReader.ReadSignedInt(file);
            r.StripOffset = ByteReader.ReadSignedInt(file);
            r.Flags = file.Get8();
            r.NumTopologyIndices = ByteReader.ReadSignedInt(file);
            r.TopologyOffset = ByteReader.ReadSignedInt(file);
            return r;
        }
    }

    public class VTXStripHeader
    {
        public long Address;
        public int NumIndices, IndexOffset, NumVerts, VertOffset;
        public int NumBones, Flags;
        public int NumBoneStateChanges, BoneStateChangeOffset;

        public static VTXStripHeader Read(FileAccess file)
        {
            var r = new VTXStripHeader { Address = (long)file.GetPosition() };
            r.NumIndices = ByteReader.ReadSignedInt(file);
            r.IndexOffset = ByteReader.ReadSignedInt(file);
            r.NumVerts = ByteReader.ReadSignedInt(file);
            r.VertOffset = ByteReader.ReadSignedInt(file);
            r.NumBones = ByteReader.ReadSignedShort(file);
            r.Flags = file.Get8();
            r.NumBoneStateChanges = ByteReader.ReadSignedInt(file);
            r.BoneStateChangeOffset = ByteReader.ReadSignedInt(file);
            return r;
        }
    }

    public class VTXStripHeaderCSGO : VTXStripHeader
    {
        public int NumTopologyIndices, TopologyOffset;

        public static new VTXStripHeaderCSGO Read(FileAccess file)
        {
            var r = new VTXStripHeaderCSGO { Address = (long)file.GetPosition() };
            r.NumIndices = ByteReader.ReadSignedInt(file);
            r.IndexOffset = ByteReader.ReadSignedInt(file);
            r.NumVerts = ByteReader.ReadSignedInt(file);
            r.VertOffset = ByteReader.ReadSignedInt(file);
            r.NumBones = ByteReader.ReadSignedShort(file);
            r.Flags = file.Get8();
            r.NumBoneStateChanges = ByteReader.ReadSignedInt(file);
            r.BoneStateChangeOffset = ByteReader.ReadSignedInt(file);
            r.NumTopologyIndices = ByteReader.ReadSignedInt(file);
            r.TopologyOffset = ByteReader.ReadSignedInt(file);
            return r;
        }
    }

    public class VTXVertex
    {
        public long Address;
        public int[] BoneWeightIndex = new int[3];
        public int NumBones;
        public int OrigMeshVertId;
        public int[] BoneId = new int[3];

        public static VTXVertex Read(FileAccess file)
        {
            var r = new VTXVertex { Address = (long)file.GetPosition() };
            for (int i = 0; i < 3; i++) r.BoneWeightIndex[i] = file.Get8();
            r.NumBones = file.Get8();
            r.OrigMeshVertId = file.Get16();
            for (int i = 0; i < 3; i++) r.BoneId[i] = file.Get8();
            return r;
        }
    }

    public VTXHeader? Header;
    public List<VTXBodyPart> BodyParts = new();
    private int _mdlVersion;

    public VTXReader(string filePath, int mdlVersion)
    {
        _mdlVersion = mdlVersion;
        var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        if (file == null)
            file = FileAccess.Open(filePath.Replace(".vtx", ".dx90.vtx"), FileAccess.ModeFlags.Read);
        if (file == null) { GD.PushError("VTXReader: Failed to open " + filePath); return; }

        Header = VTXHeader.Read(file);
        ReadBodyParts(file);
        file.Close();
    }

    private void ReadBodyParts(FileAccess file)
    {
        if (Header == null) return;
        BodyParts = ByteReader.ReadArray(file, Header.Address, Header.BodyPartOffset, Header.NumBodyParts, VTXBodyPart.Read);
        foreach (var bp in BodyParts)
        {
            bp.Models = ByteReader.ReadArray(file, bp.Address, bp.ModelOffset, bp.NumModels, VTXModel.Read);
            foreach (var model in bp.Models)
                ReadLods(model, file);
        }
    }

    private void ReadLods(VTXModel model, FileAccess file)
    {
        var lod = VTXLod.Read(file);
        file.Seek((ulong)(model.Address + model.LodOffset));
        lod = VTXLod.Read(file);
        model.Lods.Add(lod);
        ReadMeshHeaders(lod, file);
    }

    private void ReadMeshHeaders(VTXLod lod, FileAccess file)
    {
        lod.Meshes = ByteReader.ReadArray(file, lod.Address, lod.MeshOffset, lod.NumMeshes, VTXMesh.Read);
        foreach (var mesh in lod.Meshes)
        {
            if (_mdlVersion < 49) ReadStripGroups(mesh, file);
            else ReadStripGroupsCSGO(mesh, file);
        }
    }

    private static void ReadStripGroups(VTXMesh mesh, FileAccess file)
    {
        var groups = ByteReader.ReadArray(file, mesh.Address, mesh.StripGroupOffset, mesh.NumStripGroups, VTXStripGroup.Read);
        foreach (var sg in groups)
        {
            ReadVertices(sg, file);
            ReadIndices(sg, mesh, file);
            sg.Strips = ByteReader.ReadArray(file, sg.Address, sg.StripOffset, sg.NumStrips, VTXStripHeader.Read);
            mesh.StripGroups.Add(sg);
        }
    }

    private static void ReadStripGroupsCSGO(VTXMesh mesh, FileAccess file)
    {
        var groups = ByteReader.ReadArray(file, mesh.Address, mesh.StripGroupOffset, mesh.NumStripGroups, VTXStripGroupCSGO.Read);
        foreach (var sg in groups)
        {
            ReadVertices(sg, file);
            ReadIndices(sg, mesh, file);
            sg.Strips = ByteReader.ReadArray<VTXStripHeader>(file, sg.Address, sg.StripOffset, sg.NumStrips, f => VTXStripHeaderCSGO.Read(f));
            mesh.StripGroups.Add(sg);
        }
    }

    private static void ReadVertices(VTXStripGroup sg, FileAccess file)
        => sg.Vertices = ByteReader.ReadArray(file, sg.Address, sg.VertOffset, sg.NumVerts, VTXVertex.Read);

    private static void ReadIndices(VTXStripGroup sg, VTXMesh mesh, FileAccess file)
    {
        file.Seek((ulong)(sg.Address + sg.IndexOffset));
        for (int j = 0; j < sg.NumIndices; j++)
            sg.Indices.Add(mesh.IdxBase + (int)file.Get16());
        mesh.IdxBase += sg.NumVerts;
    }
}
