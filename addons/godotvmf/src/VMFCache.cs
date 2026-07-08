using System.Collections.Generic;
using Godot;

namespace GodotVMF;

public static class VMFCache
{
    private static readonly Dictionary<string, Variant> Cache = new();
    private static readonly List<string> Logs = new();

    public static Variant GetCached(string path) =>
        Cache.TryGetValue(path, out var val) ? val : default;

    public static void AddCached(string path, Variant resource) => Cache[path] = resource;
    public static bool IsFileLogged(string path) => Logs.Contains(path);
    public static void AddLoggedFile(string path) { if (!Logs.Contains(path)) Logs.Add(path); }
    public static void Clear() { Cache.Clear(); Logs.Clear(); }
}
