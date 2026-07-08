using Godot;

namespace GodotVMF;

public partial class HUD : Control
{
    public static HUD? Instance { get; private set; }

    private Tween? _fadeTween;

    private Panel FadePanel => GetNode<Panel>("fade_panel");

    public override void _Ready()
    {
        Instance = this;
    }

    public static void Fade(Color color, float duration, bool from = false) =>
        Instance?.FadeInternal(color, duration, from);

    private void FadeInternal(Color color, float duration, bool from = false)
    {
        _fadeTween?.Stop();
        _fadeTween = null;

        var targetColor = color;

        if (from)
        {
            FadePanel.Modulate = color;
            targetColor = new Color(color.R, color.G, color.B, 0);
        }
        else
        {
            FadePanel.Modulate = new Color(color.R, color.G, color.B, 0);
        }

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(FadePanel, "modulate", targetColor, duration);
    }
}
