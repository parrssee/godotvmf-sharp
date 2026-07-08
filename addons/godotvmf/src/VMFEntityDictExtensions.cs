using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

public static class VMFEntityDictExtensions
{
    public static string GetString(this GodotDict d, string key, string def = "") =>
        d.TryGetValue(key, out var v) ? v.AsString() : def;

    public static float GetFloat(this GodotDict d, string key, float def = 0f) =>
        d.TryGetValue(key, out var v) ? v.AsSingle() : def;

    public static int GetInt(this GodotDict d, string key, int def = 0) =>
        d.TryGetValue(key, out var v) ? v.AsInt32() : def;

    public static Vector3 GetVector3(this GodotDict d, string key, Vector3 def = default) =>
        d.TryGetValue(key, out var v) && v.VariantType == Variant.Type.Vector3 ? v.AsVector3() : def;
}
