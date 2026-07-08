using System.Collections.Generic;
using Godot;

namespace GodotVMF;

public class VVDReader
{
    public class VVDHeader
    {
        public long Address;
        public int Id, Version, Checksum, NumLods;
        public int[] NumLodsVertexes = new int[8];
        public int NumFixups, FixupTableOffset, VertexDataOffset, TangentDataOffset;

        public static VVDHeader Read(FileAccess file)
        {
            var r = new VVDHeader { Address = (long)file.GetPosition() };
            r.Id = ByteReader.ReadSignedInt(file);
            r.Version = ByteReader.ReadSignedInt(file);
            r.Checksum = ByteReader.ReadSignedInt(file);
            r.NumLods = ByteReader.ReadSignedInt(file);
            for (int i = 0; i < 8; i++) r.NumLodsVertexes[i] = ByteReader.ReadSignedInt(file);
            r.NumFixups = ByteReader.ReadSignedInt(file);
            r.FixupTableOffset = ByteReader.ReadSignedInt(file);
            r.VertexDataOffset = ByteReader.ReadSignedInt(file);
            r.TangentDataOffset = ByteReader.ReadSignedInt(file);
            return r;
        }
    }

    public class VVDFixupTable
    {
        public long Address;
        public int Lod, SourceVertexId, NumVertexes;
        public int DistIndex;

        public static VVDFixupTable Read(FileAccess file)
        {
            var r = new VVDFixupTable { Address = (long)file.GetPosition() };
            r.Lod = ByteReader.ReadSignedInt(file);
            r.SourceVertexId = ByteReader.ReadSignedInt(file);
            r.NumVertexes = ByteReader.ReadSignedInt(file);
            return r;
        }
    }

    public class VVDBoneWeight
    {
        public long Address;
        public float[] Weight = new float[3];
        public int[] Bone = new int[3];
        public int NumBones;
        public float[] WeightBytes = new float[4];
        public int[] BoneBytes = new int[4];

        public static VVDBoneWeight Read(FileAccess file)
        {
            var r = new VVDBoneWeight { Address = (long)file.GetPosition() };
            for (int i = 0; i < 3; i++) r.Weight[i] = file.GetFloat();
            for (int i = 0; i < 3; i++) r.Bone[i] = file.Get8();
            r.NumBones = file.Get8();
            // _on_read equivalent
            for (int i = 0; i < 3; i++) { r.WeightBytes[i] = r.Weight[i]; r.BoneBytes[i] = r.Bone[i]; }
            r.WeightBytes[3] = 0f;
            r.BoneBytes[3] = 0;
            return r;
        }
    }

    public class VVDVertexData
    {
        public long Address;
        public Vector3 Position, Normal;
        public Vector2 Uv;
        public VVDBoneWeight? BoneWeight;

        public static VVDVertexData Read(FileAccess file)
        {
            var r = new VVDVertexData { Address = (long)file.GetPosition() };
            r.Position = ByteReader.ReadVector3(file);
            r.Normal = ByteReader.ReadVector3(file);
            r.Uv = new Vector2(file.GetFloat(), file.GetFloat());
            return r;
        }
    }

    public VVDHeader? Header;
    public List<VVDFixupTable> Fixups = new();
    public List<VVDVertexData> Vertices = new();
    public List<Plane> Tangents = new();

    public VVDReader(string filePath)
    {
        var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        if (file == null) { GD.PushError("VVDReader: Can't open " + filePath); return; }

        Header = VVDHeader.Read(file);
        Fixups = ByteReader.ReadArray(file, Header.Address, Header.FixupTableOffset, Header.NumFixups, VVDFixupTable.Read);

        file.Seek((ulong)Header.VertexDataOffset);
        for (int i = 0; i < Header.NumLodsVertexes[0]; i++)
        {
            var bw = VVDBoneWeight.Read(file);
            var vd = VVDVertexData.Read(file);
            vd.BoneWeight = bw;
            Vertices.Add(vd);
        }

        file.Seek((ulong)Header.TangentDataOffset);
        for (int i = 0; i < Header.NumLodsVertexes[0]; i++)
            Tangents.Add(ByteReader.ReadPlane(file));

        int copyDistIndex = 0;
        foreach (var fixup in Fixups)
        {
            fixup.DistIndex = copyDistIndex;
            copyDistIndex += fixup.NumVertexes;
        }

        file.Close();
    }

    public int FindVertexIndex(int vertexId)
    {
        foreach (var fixup in Fixups)
        {
            int idx = vertexId - fixup.DistIndex;
            if (idx >= 0 && idx < fixup.NumVertexes)
                return fixup.SourceVertexId + idx;
        }
        return vertexId;
    }
}
