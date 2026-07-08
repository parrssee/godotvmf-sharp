using System;
using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

public class VMFSide
{
    public int Id = -1;
    public string Material = "";
    public float Rotation;
    public float LightmapScale = 16f;
    public int SmoothingGroups;

    public Plane Plane;
    public VMFTexCoord VAxis = null!;
    public VMFTexCoord UAxis = null!;
    public Vector3[] PlanePoints = Array.Empty<Vector3>();
    public Vector3[] Vertices = Array.Empty<Vector3>();
    public bool IsDisplacement;
    public VMFDisplacementInfo? DispInfo;
    public VMFSolid Solid = null!;

    private const float Epsilon = 0.01f;

    public VMFSide(GodotDict raw, VMFSolid solid)
    {
        Id = raw.TryGetValue("id", out var id) ? id.AsInt32() : -1;
        Material = raw.TryGetValue("material", out var mat) ? mat.AsString() : "";
        IsDisplacement = raw.ContainsKey("dispinfo");
        SmoothingGroups = raw.TryGetValue("smoothing_groups", out var sg) ? sg.AsInt32() : 0;
        Solid = solid;

        var planeDict = raw["plane"].AsGodotDictionary();
        Plane = planeDict["value"].AsPlane();

        var pts = planeDict["points"].AsGodotArray();
        PlanePoints = new[] { pts[0].AsVector3(), pts[1].AsVector3(), pts[2].AsVector3() };

        UAxis = new VMFTexCoord(raw["uaxis"].AsGodotDictionary());
        VAxis = new VMFTexCoord(raw["vaxis"].AsGodotDictionary());

        if (raw.ContainsKey("vertices_plus"))
        {
            var vpDict = raw["vertices_plus"].AsGodotDictionary();
            if (vpDict.TryGetValue("v", out var vVal))
            {
                if (vVal.VariantType == Variant.Type.Array)
                    Vertices = System.Linq.Enumerable.ToArray(
                        System.Linq.Enumerable.Select(vVal.AsGodotArray(), v => v.AsVector3()));
                else if (vVal.VariantType == Variant.Type.Vector3)
                    Vertices = new[] { vVal.AsVector3() };
            }
            FilterExistingVertices(solid);
        }

        if (IsDisplacement)
            DispInfo = new VMFDisplacementInfo(raw["dispinfo"].AsGodotDictionary(), this, solid);
    }

    private void FilterExistingVertices(VMFSolid solid)
    {
        var newVertices = new System.Collections.Generic.List<Vector3>();
        foreach (var vertex in Vertices)
        {
            bool exists = false;
            for (int i = 0; i < solid.Vertices.Count; i++)
            {
                if (vertex.DistanceTo(solid.Vertices[i]) < Epsilon)
                {
                    newVertices.Add(solid.Vertices[i]);
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                solid.Vertices.Add(vertex);
                newVertices.Add(vertex);
            }
        }
        Vertices = newVertices.ToArray();
    }

    public void CalculateVertices()
    {
        if (IsDisplacement) DispInfo?.CalculateVertices();
    }

    public Vector2 GetUv(Vector3 vertex)
    {
        float uScale = UAxis.Scale;
        float uShift = UAxis.Shift * uScale;
        float vScale = VAxis.Scale;
        float vShift = VAxis.Shift * vScale;

        Vector2 tSize = VMTLoader.GetTextureSize(Material);

        var uv = new Vector3(UAxis.X, UAxis.Y, UAxis.Z);
        var vv = new Vector3(VAxis.X, VAxis.Y, VAxis.Z);

        float u = (vertex.Dot(uv) + uShift) / tSize.X / uScale;
        float v = (vertex.Dot(vv) + vShift) / tSize.Y / vScale;
        return new Vector2(u, v);
    }

    public bool IsPointInside(Vector3 point)
    {
        if (Vertices.Length < 3) return false;
        int prevSign = 0;
        int count = Vertices.Length;
        for (int i = 0; i < count; i++)
        {
            var a = Vertices[i];
            var b = Vertices[(i + 1) % count];
            var edge = (b - a).Normalized();
            var toPoint = (point - a).Normalized();
            int sign = Math.Sign(edge.Cross(toPoint).Dot(Plane.Normal));
            if (sign == 0) continue;
            if (prevSign == 0) { prevSign = sign; continue; }
            if (sign != prevSign) return false;
        }
        return true;
    }

    public bool IsInsideOfFace(VMFSide other)
    {
        if (Vertices.Length < 3 || other.Vertices.Length < 3) return false;
        if (Plane.GetCenter().DistanceTo(other.Plane.GetCenter()) > 0.01f) return false;
        foreach (var vertex in Vertices)
            if (!other.IsPointInside(vertex)) return false;
        return true;
    }

    public bool IsEqualTo(VMFSide other)
    {
        if (Vertices.Length != other.Vertices.Length) return false;
        int merged = 0;
        foreach (var va in Vertices)
        {
            foreach (var vb in other.Vertices)
            {
                if (va.DistanceTo(vb) < 0.1f) { merged++; break; }
            }
        }
        return merged == Vertices.Length;
    }
}
