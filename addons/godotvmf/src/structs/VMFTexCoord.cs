using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

public class VMFTexCoord
{
    public float X;
    public float Y;
    public float Z;
    public float Shift;
    public float Scale;

    public VMFTexCoord(GodotDict raw)
    {
        X = raw.TryGetValue("x", out var x) ? x.AsSingle() : 0f;
        Y = raw.TryGetValue("y", out var y) ? y.AsSingle() : 0f;
        Z = raw.TryGetValue("z", out var z) ? z.AsSingle() : 0f;
        Shift = raw.TryGetValue("shift", out var shift) ? shift.AsSingle() : 0f;
        Scale = raw.TryGetValue("scale", out var scale) ? scale.AsSingle() : 0f;
    }
}
