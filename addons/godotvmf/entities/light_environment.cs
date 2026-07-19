using Godot;
using GodotDict = Godot.Collections.Dictionary;

namespace GodotVMF;

[Tool]
public partial class light_environment : VMFEntityNode
{
    public override int ApplyEntity(GodotDict e)
    {
        base.ApplyEntity(e);

        if (GetParent()?.GetNodeOrNull("light_environment") != null)
        {
            QueueFree();
            return 0;
        }

        var d = GetNode<DirectionalLight3D>("DirectionalLight3D");
        var lightColor = e.TryGetValue("_light", out var lv) ? lv.As<Color>() : Colors.White;
        d.LightColor = new Color(lightColor.R, lightColor.G, lightColor.B);
        d.LightEnergy = lightColor.A;

        float pitch = e.TryGetValue("pitch", out var pv) ? pv.AsSingle() : 0f;
        GlobalRotationDegrees = new Vector3(pitch, GlobalRotationDegrees.Y, GlobalRotationDegrees.Z);
        GlobalRotation = new Vector3(GlobalRotation.X, GlobalRotation.Y - Mathf.Pi / 2f, GlobalRotation.Z);

        if (e.TryGetValue("_ambient", out var av))
        {
            var ambientColor = av.As<Color>();
            var worldEnvironment = FindOrCreateWorldEnvironment();
            var environment = worldEnvironment.Environment ??= new Environment();
            environment.AmbientLightSource = Environment.AmbientSource.Color;
            environment.AmbientLightColor = new Color(ambientColor.R, ambientColor.G, ambientColor.B);
            environment.AmbientLightEnergy = ambientColor.A;
            environment.AmbientLightSkyContribution = 0f;
        }

        Name = "light_environment";
        return 0;
    }
}
