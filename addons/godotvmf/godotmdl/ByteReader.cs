using System;
using System.Collections.Generic;
using System.Text;
using Godot;

namespace GodotVMF;

public static class ByteReader
{
    public static int ReadSignedInt(FileAccess file)
    {
        uint v = file.Get32();
        return v > 2147483647u ? (int)(v - 4294967296u) : (int)v;
    }

    public static int ReadSignedShort(FileAccess file)
    {
        uint v = file.Get16();
        return v > 32767u ? (int)v - 65536 : (int)v;
    }

    public static string ReadString(FileAccess file, long offset = -1)
    {
        if (offset >= 0) file.Seek((ulong)offset);
        var sb = new StringBuilder();
        int index = 0;
        byte b = file.Get8();
        while (b != 0 && index < 100) { sb.Append((char)b); b = file.Get8(); index++; }
        return sb.ToString();
    }

    public static string ReadStringFixed(FileAccess file, int count)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++) { byte b = file.Get8(); if (b != 0) sb.Append((char)b); }
        return sb.ToString();
    }

    public static Vector3 ReadVector3(FileAccess file)
    {
        float x = file.GetFloat(), y = file.GetFloat(), z = file.GetFloat();
        return new Vector3(x, z, -y);
    }

    public static Quaternion ReadQuaternion(FileAccess file)
    {
        float x = file.GetFloat(), y = file.GetFloat(), z = file.GetFloat(), w = file.GetFloat();
        return new Quaternion(x, z, -y, w);
    }

    public static Transform3D ReadTransform3D(FileAccess file)
    {
        var yup = new Transform3D(new Basis(new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, -1, 0)), Vector3.Zero);
        float x0 = file.GetFloat(), x1 = file.GetFloat(), x2 = file.GetFloat();
        float y0 = file.GetFloat(), y1 = file.GetFloat(), y2 = file.GetFloat();
        float z0 = file.GetFloat(), z1 = file.GetFloat(), z2 = file.GetFloat();
        float t0 = file.GetFloat(), t1 = file.GetFloat(), t2 = file.GetFloat();
        var t = new Transform3D(
            new Basis(new Vector3(x0, x1, x2), new Vector3(y0, y1, y2), new Vector3(z0, z1, z2)),
            new Vector3(t0, t1, t2));
        return (t * yup).Orthonormalized();
    }

    public static Plane ReadPlane(FileAccess file)
    {
        float x = file.GetFloat(), y = file.GetFloat(), z = file.GetFloat(), d = file.GetFloat();
        return new Plane(x, z, y, d);
    }

    public static Vector3 ReadEulerVector(FileAccess file)
        => new Vector3(file.GetFloat(), file.GetFloat(), file.GetFloat());

    public static List<T> ReadArray<T>(FileAccess file, long baseAddress, long offset, int count, Func<FileAccess, T> factory)
    {
        file.Seek((ulong)(baseAddress + offset));
        var result = new List<T>(count);
        for (int i = 0; i < count; i++) result.Add(factory(file));
        return result;
    }
}
