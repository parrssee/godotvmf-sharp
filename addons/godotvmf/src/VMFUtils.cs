using System.Collections.Generic;
using Godot;

namespace GodotVMF;

public static class VMFUtils
{
    public static string NormalizePath(string path) =>
        path.Replace('\\', '/')
            .Replace("//", "/")
            .Replace("//", "/")
            .Replace("res:/", "res://")
            .Replace("res:///", "res://");

    public static List<Node> GetChildrenRecursive(Node node)
    {
        var children = new List<Node>();
        foreach (var child in node.GetChildren())
        {
            children.Add(child);
            children.AddRange(GetChildrenRecursive(child));
        }
        return children;
    }

    public static void SetOwnerRecursive(Node node, Node owner)
    {
        node.Owner = owner;
        foreach (var child in node.GetChildren())
            SetOwnerRecursive(child, owner);
    }

    public static void ObjectAssign(GodotObject target, Godot.Collections.Dictionary source)
    {
        foreach (var key in source.Keys)
            target.Set(key.AsString(), source[key]);
    }
}
