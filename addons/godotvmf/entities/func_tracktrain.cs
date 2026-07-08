using System.Threading.Tasks;
using Godot;

namespace GodotVMF;

[Tool]
public partial class func_tracktrain : VMFEntityNode
{
    private enum MovementState { MovingForward, MovingBackward, Stopped }
    private enum MovementDirection { Forward, Backward }

    private path_track? _currentPoint;
    private Tween? _currentTween;

    private string _startSound = "";
    private string _stopSound = "";
    private string _moveSound = "";

    private MovementDirection _direction = MovementDirection.Forward;
    private MovementState _state = MovementState.Stopped;

    private AudioStreamPlayer3D? _movePlayer;

    private path_track? FirstTarget => GetTarget(Entity.GetString("target")) as path_track;

    public override void EntityReady()
    {
        if (_currentPoint == null)
        {
            var first = FirstTarget;
            if (first != null) TeleportToPoint(first.Targetname, false);
            GD.Print(first);
        }

        PrecacheSounds();
    }

    public override void EntitySetup(VMFEntity entity)
    {
        GetNode<MeshInstance3D>("body/mesh").Mesh = GetMesh();
        GetNode<CollisionShape3D>("body/collision").Shape = GetEntityShape();
    }

    public override void _PhysicsProcess(double _delta)
    {
        if (IsInstanceValid(_movePlayer))
            _movePlayer!.GlobalPosition = GlobalPosition;
    }

    private void PrecacheSounds()
    {
        _startSound = GameBridge.PrecacheSound(this, Entity.GetString("StartSound"));
        _stopSound = GameBridge.PrecacheSound(this, Entity.GetString("StopSound"));
        _moveSound = GameBridge.PrecacheSound(this, Entity.GetString("MoveSound"));
    }

    private void ResetTween()
    {
        if (_currentTween == null) return;

        _currentTween.Stop();
        _currentTween = null;

        if (_state == MovementState.Stopped)
        {
            GameBridge.PlaySound(this, GlobalTransform.Origin, _stopSound);
            if (IsInstanceValid(_movePlayer)) _movePlayer!.Stop();
            TriggerOutput("OnStop");
        }
    }

    private async Task MoveToCurrentPoint()
    {
        var targetPosition = _currentPoint!.GlobalTransform.Origin;
        float distance = (targetPosition - GlobalTransform.Origin).Length();
        float speed = Entity.GetFloat("speed") * VMFConfig.Import.Scale;

        if (speed == 0f)
        {
            GD.PushError("Speed is 0, cannot move. Entity id: " + Entity.GetString("id"));
            return;
        }

        float time = distance / speed;

        ResetTween();

        if (_state == MovementState.Stopped)
        {
            GameBridge.PlaySound(this, GlobalTransform.Origin, _startSound);
            _movePlayer = GameBridge.PlaySound(this, GlobalTransform.Origin, _moveSound) as AudioStreamPlayer3D;
            TriggerOutput("OnStart");
        }

        _state = _direction == MovementDirection.Forward ? MovementState.MovingForward : MovementState.MovingBackward;

        _currentTween = CreateTween();
        _currentTween.SetProcessMode(Tween.TweenProcessMode.Physics);
        _currentTween.TweenProperty(this, "global_transform:origin", targetPosition, time);

        await ToSignal(_currentTween, Tween.SignalName.Finished);

        _currentPoint.Pass();
    }

    private async void MoveToNextPoint()
    {
        if (_currentPoint == null)
        {
            Stop();
            GD.Print("Stopped");
            return;
        }

        _direction = MovementDirection.Forward;

        await MoveToCurrentPoint();

        if (_currentPoint.NextStopTarget == null)
        {
            Stop();
            return;
        }

        _currentPoint = _currentPoint.NextStopTarget;
        MoveToNextPoint();
    }

    private async void MoveToPreviousPoint()
    {
        if (_currentPoint == null)
        {
            Stop();
            return;
        }

        _direction = MovementDirection.Backward;

        await MoveToCurrentPoint();

        if (_currentPoint.PrevStopTarget == null)
        {
            Stop();
            return;
        }

        _currentPoint = _currentPoint.PrevStopTarget;
        MoveToPreviousPoint();
    }

    private void TeleportToPoint(string targetPoint, bool triggerOutput = false)
    {
        var track = GetTarget(targetPoint) as path_track;
        if (track == null)
        {
            GD.PushError($"Target path_track not found: {targetPoint}. Entity id: {Entity.GetString("id")}");
            return;
        }

        _currentPoint = track;
        var t = GlobalTransform;
        t.Origin = track.GlobalTransform.Origin;
        GlobalTransform = t;

        if (triggerOutput) track.Teleport();
    }

    public void StartForward(Variant _param = default)
    {
        if (_direction != MovementDirection.Forward)
            _currentPoint = _currentPoint?.NextStopTarget;

        MoveToNextPoint();
    }

    public void StartBackward(Variant _param = default)
    {
        if (_direction != MovementDirection.Backward)
            _currentPoint = _currentPoint?.PrevStopTarget;

        MoveToPreviousPoint();
    }

    public new void Toggle(Variant _param = default)
    {
        if (_state == MovementState.Stopped)
        {
            if (_direction == MovementDirection.Forward) StartForward();
            else StartBackward();
        }
        else
        {
            Stop();
        }
    }

    public void Stop(Variant _param = default)
    {
        _state = MovementState.Stopped;
        TriggerOutput("OnStop");
        ResetTween();
    }

    public void TeleportToPathTrack(string targetPathTrack) => TeleportToPoint(targetPathTrack, true);
}
