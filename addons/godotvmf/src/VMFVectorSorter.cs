using System.Collections.Generic;
using Godot;

namespace GodotVMF;

public class VMFVectorSorter : IComparer<Vector3>
{
    private readonly Vector3 _normal;
    private readonly Vector3 _center;
    private readonly Vector3 _pp;
    private readonly Vector3 _qp;

    public VMFVectorSorter(Vector3 normal, Vector3 center)
    {
        _normal = normal;
        _center = center;

        var i = normal.Cross(new Vector3(1, 0, 0));
        var j = normal.Cross(new Vector3(0, 1, 0));
        var k = normal.Cross(new Vector3(0, 0, 1));

        _pp = Longer(i, Longer(j, k));
        _qp = normal.Cross(_pp);
    }

    private static Vector3 Longer(Vector3 a, Vector3 b) => a.Length() > b.Length() ? a : b;

    private float GetOrder(Vector3 v)
    {
        var normalized = (v - _center).Normalized();
        return Mathf.Atan2(
            _normal.Dot(normalized.Cross(_pp)),
            _normal.Dot(normalized.Cross(_qp)));
    }

    public int Compare(Vector3 a, Vector3 b) => GetOrder(a).CompareTo(GetOrder(b));
}
