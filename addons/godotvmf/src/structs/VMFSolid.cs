using System;
using System.Collections.Generic;
using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

public class VMFSolid
{
    public int Id = -1;
    public List<VMFSide> Sides = new();
    public bool HasDisplacement;
    public Vector3 Min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
    public Vector3 Max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
    public List<Vector3> Vertices = new();

    public VMFSolid(GodotDict raw)
    {
        if (raw.Count == 0) return;
        Id = raw.TryGetValue("id", out var id) ? id.AsInt32() : -1;
        DefineSides(raw);
    }

    private void DefineSides(GodotDict raw)
    {
        if (!raw.ContainsKey("side")) return;

        var sideVal = raw["side"];
        var sidesArr = sideVal.VariantType == Variant.Type.Array
            ? sideVal.AsGodotArray()
            : new Godot.Collections.Array { sideVal };

        foreach (var sv in sidesArr)
        {
            if (sv.VariantType != Variant.Type.Dictionary) continue;
            var side = new VMFSide(sv.AsGodotDictionary(), this);
            Sides.Add(side);
            if (side.IsDisplacement) HasDisplacement = true;
        }

        bool needsComputation = false;
        foreach (var s in Sides)
        {
            if (s.Vertices.Length == 0) { needsComputation = true; break; }
        }
        if (needsComputation) ComputeVerticesFromPlanes();

        foreach (var side in Sides)
            side.CalculateVertices();

        float minX = Min.X, minY = Min.Y, minZ = Min.Z;
        float maxX = Max.X, maxY = Max.Y, maxZ = Max.Z;
        foreach (var side in Sides)
        {
            foreach (var v in side.Vertices)
            {
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Z < minZ) minZ = v.Z;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
                if (v.Z > maxZ) maxZ = v.Z;
            }
        }
        Min = new Vector3(minX, minY, minZ);
        Max = new Vector3(maxX, maxY, maxZ);
    }

    private void ComputeVerticesFromPlanes()
    {
        int n = Sides.Count;
        var sideVerts = new Dictionary<int, Vector3>[n];
        for (int i = 0; i < n; i++) sideVerts[i] = new Dictionary<int, Vector3>();

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                for (int k = j + 1; k < n; k++)
                {
                    var v = Sides[i].Plane.Intersect3(Sides[j].Plane, Sides[k].Plane);
                    if (v == null) continue;
                    var vertex = v.Value;

                    bool valid = true;
                    foreach (var s in Sides)
                    {
                        if (s.Plane.DistanceTo(vertex) > 0.001f) { valid = false; break; }
                    }
                    if (!valid) continue;

                    var vi = new Vector3I(
                        Mathf.RoundToInt(vertex.X),
                        Mathf.RoundToInt(vertex.Y),
                        Mathf.RoundToInt(vertex.Z));
                    int vhash = vi.GetHashCode();
                    sideVerts[i][vhash] = vertex;
                    sideVerts[j][vhash] = vertex;
                    sideVerts[k][vhash] = vertex;
                }
            }
        }

        for (int i = 0; i < n; i++)
        {
            if (Sides[i].Vertices.Length > 0) continue;
            var raw = new List<Vector3>(sideVerts[i].Values);
            var normal = Sides[i].Plane.Normal;
            var pp = Sides[i].PlanePoints;
            var center = (pp[0] + pp[1] + pp[2]) / 3f;
            var sorter = new VMFVectorSorter(normal, center);
            raw.Sort(sorter);
            Sides[i].Vertices = raw.ToArray();
        }
    }
}
