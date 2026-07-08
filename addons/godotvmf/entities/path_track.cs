using Godot;

namespace GodotVMF;

[Tool]
public partial class path_track : VMFEntityNode
{
    public path_track? NextStopTarget => GetTarget(Entity.GetString("target")) as path_track;
    public path_track? PrevStopTarget { get; set; }

    public override void EntityReady()
    {
        if (NextStopTarget != null)
            NextStopTarget.PrevStopTarget = this;

        RotationDegrees = new Vector3(RotationDegrees.X, RotationDegrees.Y - 90f, RotationDegrees.Z);
    }

    public void Pass() => TriggerOutput("OnPass");
    public void Teleport() => TriggerOutput("OnTeleport");
}
