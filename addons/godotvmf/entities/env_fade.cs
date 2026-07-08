using Godot;

namespace GodotVMF;

[Tool]
public partial class env_fade : VMFEntityNode
{
    private const int FLAG_FADE_FROM = 1;

    public void Fade(Variant _param = default)
    {
        float duration = Entity.GetFloat("duration");
        var colorVector = Entity.GetVector3("rendercolor");
        float alpha = Entity.GetFloat("renderamt", 255f);
        var color = Color.Color8((byte)colorVector.X, (byte)colorVector.Y, (byte)colorVector.Z, (byte)alpha);

        TriggerOutput("OnBeginFade");
        HUD.Fade(color, duration, HasFlag(FLAG_FADE_FROM));
    }
}
