using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace GodotVMF;

[Tool]
public partial class point_viewcontrol : VMFEntityNode
{
    private const int FLAG_START_AT_PLAYER = 1;
    private const int FLAG_INFINITE_HOLD_TIME = 8;
    private const int FLAG_SET_FOV = 128;

    private Tween? _currentTween;
    private Transform3D _startTransform;
    private Camera3D? _oldCamera;
    private VMFEntityNode? _currentStopTarget;

    private Camera3D CameraNode => GetNode<Camera3D>("camera");

    private static Vector3 CatmulRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private static List<Vector3> CalculateSplinePath(List<Vector3> points, int subdivisions = 10)
    {
        var path = new List<Vector3>();

        for (int i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[i];
            var p1 = points[i + 1];
            var p2 = i + 2 < points.Count ? points[i + 2] : p1;
            var p3 = i + 3 < points.Count ? points[i + 3] : p2;

            for (int j = 0; j < subdivisions; j++)
            {
                float t = (float)j / subdivisions;
                path.Add(CatmulRom(p0, p1, p2, p3, t));
            }
        }

        return path;
    }

    public override void EntitySetup(VMFEntity entity)
    {
        if (HasFlag(FLAG_SET_FOV))
            CameraNode.Fov = entity.Data.GetFloat("fov", 90f);
    }

    public override void EntityReady()
    {
        _startTransform = GlobalTransform;
        _currentStopTarget = GetTarget(Entity.GetString("moveto")) as VMFEntityNode;
    }

    private async Task TweenCamera()
    {
        if (_oldCamera == null) return;

        if (_currentTween != null)
        {
            _currentTween.Stop();
            _currentTween = null;
        }

        var fromTransform = _oldCamera.GlobalTransform;
        var targetTransform = GlobalTransform;
        GlobalTransform = _oldCamera.GlobalTransform;

        float startFov = _oldCamera.Fov;
        float targetFov = HasFlag(FLAG_SET_FOV) ? CameraNode.Fov : _oldCamera.Fov;
        CameraNode.Fov = startFov;

        float acceleration = Entity.GetFloat("acceleration", 1f) * VMFConfig.Import.Scale;
        float time = targetTransform.Origin.DistanceTo(fromTransform.Origin) / acceleration;

        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "global_transform", targetTransform, time);
        _currentTween.Parallel().TweenProperty(CameraNode, "fov", targetFov, time);

        await ToSignal(_currentTween, Tween.SignalName.Finished);
    }

    private async void MoveThroughPath()
    {
        var corners = new List<Vector3>();

        while (_currentStopTarget != null)
        {
            corners.Add(_currentStopTarget.GlobalTransform.Origin);

            var oldTarget = _currentStopTarget;
            _currentStopTarget = GetTarget(_currentStopTarget.Entity.GetString("target")) as VMFEntityNode;

            if (_currentStopTarget == oldTarget)
                _currentStopTarget = null;
        }

        var pathPoints = CalculateSplinePath(corners, 30);
        await MoveToPoint(0, pathPoints);
    }

    private VMFEntityNode? FindCornerByPosition(Vector3 pos)
    {
        var target = GetTarget(Entity.GetString("moveto")) as VMFEntityNode;

        while (target != null)
        {
            float distance = target.GlobalTransform.Origin.DistanceTo(pos);
            if (distance < 0.001f) return target;

            target = GetTarget(target.Entity.GetString("target")) as VMFEntityNode;
        }

        return null;
    }

    private async Task MoveToPoint(int i, List<Vector3> points)
    {
        if (i >= points.Count) return;

        var targetPosition = points[i];
        var nextTarget = i + 1 < points.Count ? points[i + 1] : targetPosition;
        var toRotation = Basis.LookingAt((nextTarget - targetPosition).Normalized(), Vector3.Up);

        float acceleration = Entity.GetFloat("acceleration", 1f) * VMFConfig.Import.Scale;
        float time = targetPosition.DistanceTo(nextTarget) / acceleration;

        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "global_position", targetPosition, time);
        _currentTween.Parallel().TweenProperty(this, "global_transform:basis", toRotation, time);

        await ToSignal(_currentTween, Tween.SignalName.Finished);

        var passedCorner = FindCornerByPosition(targetPosition);
        if (passedCorner is path_track pathTrack) pathTrack.Pass();

        await MoveToPoint(i + 1, points);
    }

    public new async void Enable(Variant _param = default)
    {
        base.Enable(_param);

        var player = GetTarget("!player");
        var cameraProp = player?.Get("camera") ?? default;
        _oldCamera = cameraProp.VariantType != Variant.Type.Nil
            ? cameraProp.AsGodotObject() as Camera3D
            : GetViewport().GetCamera3D();

        CameraNode.MakeCurrent();
        GetNode<AudioListener3D>("camera/listener").MakeCurrent();

        if (HasFlag(FLAG_START_AT_PLAYER))
            await TweenCamera();

        MoveThroughPath();

        if (!HasFlag(FLAG_INFINITE_HOLD_TIME))
        {
            await ToSignal(GetTree().CreateTimer(Entity.GetFloat("wait")), SceneTreeTimer.SignalName.Timeout);
            Disable();
        }
    }

    public new void Disable(Variant _param = default)
    {
        base.Disable(_param);

        if (_oldCamera != null)
        {
            _oldCamera.MakeCurrent();
            var listener = _oldCamera.GetNodeOrNull<AudioListener3D>("listener");
            listener?.MakeCurrent();
        }

        _oldCamera = null;

        if (_currentTween != null)
        {
            _currentTween.Stop();
            _currentTween = null;
        }
    }
}
