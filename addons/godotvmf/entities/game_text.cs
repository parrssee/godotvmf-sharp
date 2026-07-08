using Godot;

namespace GodotVMF;

[Tool]
public partial class game_text : VMFEntityNode
{
    private Tween? _currentTween;

    private Label LabelNode => GetNode<Label>("%label");
    private Control PanelNode => GetNode<Control>("panel");

    public override void EntityReady()
    {
        LabelNode.Text = Entity.GetString("message").Replace("\\n", "\n");
        LabelNode.Visible = false;

        var color = Entity.GetVector3("color");
        LabelNode.AddThemeColorOverride("font_color", Color.Color8((byte)color.X, (byte)color.Y, (byte)color.Z, 255));
        LabelNode.Modulate = Color.Color8(255, 255, 255, 0);
    }

    private void RepositionText()
    {
        float xPercent = Entity.GetFloat("x");
        float yPercent = Entity.GetFloat("y");

        if (xPercent < 0) xPercent = 0.5f;
        if (yPercent < 0) yPercent = 0.5f;

        var pos = LabelNode.Position;
        pos.X = PanelNode.Size.X * xPercent - LabelNode.Size.X * xPercent;
        pos.Y = PanelNode.Size.Y * yPercent - LabelNode.Size.Y * yPercent;
        LabelNode.Position = pos;

        if (xPercent > 0.5f) LabelNode.HorizontalAlignment = HorizontalAlignment.Right;
        if (xPercent < 0.5f) LabelNode.HorizontalAlignment = HorizontalAlignment.Left;
    }

    public async void Display(Variant _param = default)
    {
        RepositionText();
        LabelNode.Visible = true;

        _currentTween?.Stop();
        _currentTween = null;

        _currentTween = CreateTween();
        _currentTween.TweenProperty(LabelNode, "modulate", Color.Color8(255, 255, 255, 255), Entity.GetFloat("fadein"));
        await ToSignal(_currentTween, Tween.SignalName.Finished);

        await ToSignal(GetTree().CreateTimer(Entity.GetFloat("holdtime")), SceneTreeTimer.SignalName.Timeout);

        _currentTween = CreateTween();
        _currentTween.TweenProperty(LabelNode, "modulate", Color.Color8(255, 255, 255, 0), Entity.GetFloat("fadeout"));
        await ToSignal(_currentTween, Tween.SignalName.Finished);

        _currentTween = null;
        LabelNode.Visible = false;
    }
}
