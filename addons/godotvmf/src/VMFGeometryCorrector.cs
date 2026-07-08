using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;

namespace GodotVMF;

public class VMFGeometryCorrector
{
    public static readonly string[] NoRender =
    {
        "compileclip", "compilenodraw", "compilesky",
        "npcclip", "compileplayerclip", "compilenpcclip",
    };

    public static readonly string[] NoCollision = { "compilesky" };

    private readonly Dictionary<string, Action<StaticBody3D>> _handlers;

    public VMFGeometryCorrector()
    {
        _handlers = new Dictionary<string, Action<StaticBody3D>>
        {
            ["compileplayerclip"] = body => { body.CollisionLayer = 1 << 1; body.CollisionMask = 1 << 1; },
            ["compilenpcclip"] = body => { body.CollisionLayer = 1 << 2; body.CollisionMask = 1 << 2; },
        };
    }

    public bool HasHandler(string key) => _handlers.Any(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    public void CallHandler(string key, StaticBody3D body)
    {
        var handler = _handlers.FirstOrDefault(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(handler.Key))
            return;

        handler.Value(body);
    }

    public bool HasNoCollision(string key) => Array.FindIndex(NoCollision, item => item.Equals(key, StringComparison.OrdinalIgnoreCase)) >= 0;

    public bool HasNoRender(string key) => Array.FindIndex(NoRender, item => item.Equals(key, StringComparison.OrdinalIgnoreCase)) >= 0;
}
