using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

[Tool]
public partial class env_fog_controller : VMFEntityNode
{
    private Environment? _environment;

    public override int ApplyEntity(GodotDict e)
    {
        base.ApplyEntity(e);

        var worldEnvironment = FindOrCreateWorldEnvironment();
        _environment = worldEnvironment.Environment ??= new Environment();

        var fogColor = e.GetVector3("fogcolor", new Vector3(255f, 255f, 255f));
        float fogStart = e.GetFloat("fogstart") * VMFConfig.Import.Scale;
        float fogEnd = e.GetFloat("fogend") * VMFConfig.Import.Scale;

        _environment.FogMode = Environment.FogModeEnum.Depth;
        _environment.FogEnabled = e.GetInt("fogenable", 1) != 0;
        _environment.FogLightColor = Color.Color8((byte)fogColor.X, (byte)fogColor.Y, (byte)fogColor.Z);
        _environment.FogDepthBegin = fogStart;
        _environment.FogDepthEnd = fogEnd;
        _environment.FogDensity = e.GetFloat("fogmaxdensity", 1f);

        return 0;
    }

    public void TurnOn(Variant _param = default)
    {
        if (_environment != null) _environment.FogEnabled = true;
    }

    public void TurnOff(Variant _param = default)
    {
        if (_environment != null) _environment.FogEnabled = false;
    }
}
