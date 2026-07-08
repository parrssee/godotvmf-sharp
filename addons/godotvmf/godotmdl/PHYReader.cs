using System;
using System.Collections.Generic;
using Godot;

namespace GodotVMF;

public class PHYReader
{
    public class PHYHeader
    {
        public long Address;
        public int Size, Id, SolidCount, Checksum;

        public static PHYHeader Read(FileAccess file)
        {
            var r = new PHYHeader { Address = (long)file.GetPosition() };
            r.Size = ByteReader.ReadSignedInt(file);
            r.Id = ByteReader.ReadSignedInt(file);
            r.SolidCount = ByteReader.ReadSignedInt(file);
            r.Checksum = ByteReader.ReadSignedInt(file);
            return r;
        }
    }

    public class PHYSurfaceHeader
    {
        public long Address;
        public int Size, Version, ModelType, SurfaceSize, AxisMapSize;
        public string Id = "";
        public Vector3 DragAxisAreas;
        public string Ivps = "";
        public List<PHYSolidHeader> Solids = new();
        public List<Vector3> Vertices = new();

        public static PHYSurfaceHeader Read(FileAccess file)
        {
            var r = new PHYSurfaceHeader { Address = (long)file.GetPosition() };
            r.Size = ByteReader.ReadSignedInt(file);
            r.Id = ByteReader.ReadStringFixed(file, 4);
            r.Version = ByteReader.ReadSignedShort(file);
            r.ModelType = ByteReader.ReadSignedShort(file);
            r.SurfaceSize = ByteReader.ReadSignedInt(file);
            r.DragAxisAreas = ByteReader.ReadVector3(file);
            r.AxisMapSize = ByteReader.ReadSignedInt(file);
            for (int i = 0; i < 11; i++) ByteReader.ReadSignedInt(file); // unused1
            r.Ivps = ByteReader.ReadStringFixed(file, 4);
            return r;
        }
    }

    public class PHYLegacySurfaceHeader
    {
        public long Address;
        public Vector3 MassCenter, RotationInertia;
        public float UpperLimitRadius;
        public int MaxDeviation, ByteSize;
        public string Ivps = "";

        public static PHYLegacySurfaceHeader Read(FileAccess file)
        {
            var r = new PHYLegacySurfaceHeader { Address = (long)file.GetPosition() };
            r.MassCenter = ByteReader.ReadVector3(file);
            r.RotationInertia = ByteReader.ReadVector3(file);
            r.UpperLimitRadius = file.GetFloat();
            r.MaxDeviation = ByteReader.ReadSignedInt(file);
            r.ByteSize = ByteReader.ReadSignedInt(file);
            ByteReader.ReadSignedInt(file); // dummy[0]
            ByteReader.ReadSignedInt(file); // dummy[1]
            r.Ivps = ByteReader.ReadStringFixed(file, 4);
            return r;
        }
    }

    public class PHYSolidHeader
    {
        public long Address;
        public int VerticesOffset, BoneIndex, Flags, FaceCount;
        public List<PHYTriangleData> Faces = new();

        public static PHYSolidHeader Read(FileAccess file)
        {
            var r = new PHYSolidHeader { Address = (long)file.GetPosition() };
            r.VerticesOffset = ByteReader.ReadSignedInt(file);
            r.BoneIndex = ByteReader.ReadSignedInt(file);
            r.Flags = ByteReader.ReadSignedInt(file);
            r.FaceCount = ByteReader.ReadSignedInt(file);
            return r;
        }
    }

    public class PHYTriangleData
    {
        public long Address;
        public int VertexIndex;
        public int V1, V2, V3;

        public static PHYTriangleData Read(FileAccess file)
        {
            var r = new PHYTriangleData { Address = (long)file.GetPosition() };
            r.VertexIndex = file.Get8();
            file.Get8();        // unused1
            file.Get16();       // unused2
            r.V1 = ByteReader.ReadSignedShort(file);
            file.Get16();       // unused3
            r.V2 = ByteReader.ReadSignedShort(file);
            file.Get16();       // unused4
            r.V3 = ByteReader.ReadSignedShort(file);
            file.Get16();       // unused5
            return r;
        }
    }

    public PHYHeader? Header;
    public List<PHYSurfaceHeader> Surfaces = new();
    public List<PHYLegacySurfaceHeader> LegacySurfaces = new();

    public PHYReader(string sourceFile)
    {
        var file = FileAccess.Open(sourceFile, FileAccess.ModeFlags.Read);
        if (file == null) return;

        Header = PHYHeader.Read(file);

        for (int i = 0; i < Header.SolidCount; i++)
        {
            var surfaceHeader = PHYSurfaceHeader.Read(file);
            Surfaces.Add(surfaceHeader);

            if (surfaceHeader.Id != "VPHY")
            {
                file.Seek((ulong)surfaceHeader.Address);
                LegacySurfaces.Add(PHYLegacySurfaceHeader.Read(file));
            }

            int verticesCount = 0;
            long verticesStart = long.MaxValue;

            while ((long)file.GetPosition() < verticesStart)
            {
                var solidHeader = PHYSolidHeader.Read(file);
                verticesStart = Math.Min(solidHeader.Address + solidHeader.VerticesOffset, verticesStart);
                surfaceHeader.Solids.Add(solidHeader);

                for (int j = 0; j < solidHeader.FaceCount; j++)
                {
                    var tri = PHYTriangleData.Read(file);
                    solidHeader.Faces.Add(tri);
                    verticesCount = Math.Max(verticesCount, Math.Max(tri.V1, Math.Max(tri.V2, tri.V3)));
                }
            }

            for (int j = 0; j <= verticesCount; j++)
            {
                var v = ByteReader.ReadVector3(file); // already y-up from ReadVector3
                file.GetFloat(); // w
                var vertex = new Vector3(v.X, v.Z, -v.Y) / 0.0254f;
                surfaceHeader.Vertices.Add(vertex);
            }

            file.Seek((ulong)(surfaceHeader.Address + 4 + surfaceHeader.Size));
        }

        file.Close();
    }
}
