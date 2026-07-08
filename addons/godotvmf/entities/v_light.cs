using System.Collections.Generic;
using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

[Tool]
public partial class v_light : VMFEntityNode
{
    public enum Appearance
    {
        Normal = 0,
        FlickerA = 1,
        SlowStrongPulse = 2,
        CandleA = 3,
        FastStrobe = 4,
        GentlePulse = 5,
        FlickerB = 6,
        CandleB = 7,
        CandleC = 8,
        SlowStrobe = 9,
        FluorescentFlicker = 10,
        SlowPulse = 11,
    }

    public static readonly Dictionary<Appearance, string> Patterns = new()
    {
        { Appearance.Normal,              "m" },
        { Appearance.FlickerA,            "mmnmmommommnonmmonqnmmo" },
        { Appearance.SlowStrongPulse,     "abcdefghijklmnopqrstuvwxyzyxwvutsrqponmlkjihgfedcba" },
        { Appearance.CandleA,             "mmmmmaaaaammmmmaaaaaabcdefgabcdefg" },
        { Appearance.FastStrobe,          "mamamamama" },
        { Appearance.GentlePulse,         "jklmnopqrstuvwxyzyxwvutsrqponmlkj" },
        { Appearance.FlickerB,            "nmonqnmomnmomomno" },
        { Appearance.CandleB,             "mmmaaaabcdefgmmmmaaaammmaamm" },
        { Appearance.CandleC,             "mmmaaammmaaabcdefgmmmaaaammmmmaaaa" },
        { Appearance.SlowStrobe,          "aaaaaaaazzzzzzzz" },
        { Appearance.FluorescentFlicker,  "mmamammmmammamamaaamammma" },
        { Appearance.SlowPulse,           "abcdefghijklmnoonmlkjihgfedcba" },
    };

    private const int MaxPatternValue = 12;
    private const int CharA = 97;
    public const int FlagInitiallyDark = 1;

    [Export] public Appearance Style { get; set; } = Appearance.Normal;
    [Export] public float DefaultLightEnergy { get; set; } = 0.0f;

    protected Light3D? LightNode;
    private float _timePassed = 0.0f;

    public override void _Ready()
    {
        base._Ready();
        LightNode = GetNode<Light3D>("light");
    }

    public override void EntityReady()
    {
        if (LightNode != null)
            LightNode.Visible = !HasFlag(FlagInitiallyDark);
    }

    public override void _Process(double delta)
    {
        AnimateLight((float)delta);
    }

    private static float EaseInOutCirc(float x)
    {
        if (x < 0.5f)
            return (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * x, 2f))) / 2f;
        return (Mathf.Sqrt(1f - Mathf.Pow(-2f * x + 2f, 2f)) + 1f) / 2f;
    }

    private void AnimateLight(float delta)
    {
        if (LightNode == null) return;
        _timePassed += delta;

        string pattern = Patterns.TryGetValue(Style, out var p) ? p : "m";
        if (pattern == "m")
        {
            LightNode.LightEnergy = DefaultLightEnergy;
            return;
        }

        int patternLength = pattern.Length;
        int currentIdx = (int)(_timePassed * 10f) % patternLength;
        int previousIdx = (currentIdx - 1 + patternLength) % patternLength;
        float interpolation = (_timePassed * 10f) - Mathf.Floor(_timePassed * 10f);
        float currentBrightness = (pattern[currentIdx] - CharA) / (float)MaxPatternValue;
        float previousBrightness = (pattern[previousIdx] - CharA) / (float)MaxPatternValue;
        LightNode.LightEnergy = DefaultLightEnergy * Mathf.Lerp(previousBrightness, currentBrightness, EaseInOutCirc(interpolation));
    }

    public override void EntitySetup(VMFEntity entity)
    {
        if (LightNode == null) return;
        var entityData = new LightType(entity);

        bool isVec3 = entityData.Light.VariantType == Variant.Type.Vector3;
        var colorVec3 = isVec3 ? entityData.Light.AsVector3() : Vector3.Zero;
        var color = isVec3 ? new Color(colorVec3.X / 255f, colorVec3.Y / 255f, colorVec3.Z / 255f) : entityData.Light.As<Color>();

        bool hasDynamic = !string.IsNullOrEmpty(entity.Targetname)
            || entity.Data.ContainsKey("parentname")
            || HasFlag(FlagInitiallyDark);
        LightNode.LightBakeMode = hasDynamic ? Light3D.BakeMode.Dynamic : Light3D.BakeMode.Static;

        if (isVec3)
        {
            LightNode.LightColor = new Color(colorVec3.X / 255f, colorVec3.Y / 255f, colorVec3.Z / 255f);
            LightNode.LightEnergy = 1.0f;
        }
        else if (entityData.Light.VariantType == Variant.Type.Color)
        {
            LightNode.LightColor = new Color(color.R, color.G, color.B);
            LightNode.LightEnergy = color.A;
        }
        else
        {
            VMFLogger.Error("Invalid light: " + entity.Id);
            GetParent().RemoveChild(this);
            QueueFree();
            return;
        }

        if (LightNode is OmniLight3D omniLight)
        {
            float scale = VMFConfig.Import.Scale;
            float radius = (1f / scale) * Mathf.Sqrt(LightNode.LightEnergy * scale);
            float attenuation = 1.44f;

            if (entityData.FiftyPercentDistance > 0f || entityData.ZeroPercentDistance > 0f)
            {
                float dist50 = Mathf.Min(entityData.FiftyPercentDistance, entityData.ZeroPercentDistance) * scale;
                float dist0 = Mathf.Max(entityData.FiftyPercentDistance, entityData.ZeroPercentDistance) * scale;
                attenuation = 1f / ((dist0 - dist50) / dist0);
                radius = Mathf.Exp(dist0);
            }

            omniLight.OmniRange = radius;
            omniLight.OmniAttenuation = attenuation;
        }

        LightNode.ShadowEnabled = true;
        DefaultLightEnergy = LightNode.LightEnergy;
        Style = entityData.Style;
    }

    public void TurnOff(Variant _param = default) { if (LightNode != null) LightNode.Visible = false; }
    public void TurnOn(Variant _param = default) { if (LightNode != null) LightNode.Visible = true; }

    public class LightType
    {
        public Variant Light;
        public float FiftyPercentDistance;
        public float ZeroPercentDistance;
        public Appearance Style;
        public float Cone;
        public float Pitch;

        public LightType(VMFEntity entity)
        {
            var d = entity.Data;
            Light = d.TryGetValue("_light", out var l) ? l : Variant.From(Colors.White);
            FiftyPercentDistance = d.TryGetValue("_fifty_percent_distance", out var f50) ? f50.AsSingle() : 0f;
            ZeroPercentDistance = d.TryGetValue("_zero_percent_distance", out var z0) ? z0.AsSingle() : 0f;
            Style = d.TryGetValue("style", out var s) ? (Appearance)s.AsInt32() : Appearance.Normal;
            Cone = d.TryGetValue("_cone", out var cone) ? cone.AsSingle() : 0f;
            Pitch = d.TryGetValue("pitch", out var pitch) ? pitch.AsSingle() : -90f;
        }
    }
}
