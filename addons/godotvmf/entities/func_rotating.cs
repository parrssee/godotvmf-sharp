using Godot;

namespace GodotVMF;

[Tool]
public partial class func_rotating : VMFEntityNode
{
    private const int FLAG_START_ON = 1;
    private const int FLAG_REVERSE_DIRECTION = 2;
    private const int FLAG_X_AXIS = 4;
    private const int FLAG_Y_AXIS = 8;
    private const int FLAG_NONSOLID = 64;

    private Tween? _currentTween;

    private AudioStreamPlayer3D LoopPlayer => GetNode<AudioStreamPlayer3D>("body/loop_player");
    public float MaxSpeed => Entity.GetFloat("maxspeed");
    public string SoundPath => Entity.GetString("message");

    public override void EntitySetup(VMFEntity entity)
    {
        var mesh = GetNode<MeshInstance3D>("body/mesh");
        var collision = GetNode<CollisionShape3D>("body/collision");

        mesh.Mesh = GetMesh();
        mesh.CastShadow = entity.Data.GetInt("disableshadows") == 0
            ? GeometryInstance3D.ShadowCastingSetting.On
            : GeometryInstance3D.ShadowCastingSetting.Off;
        mesh.GIMode = GeometryInstance3D.GIModeEnum.Dynamic;
        AssignSound();

        if (HasFlag(FLAG_NONSOLID))
            collision.QueueFree();
        else
            collision.Shape = GetEntityShape();
    }

    private void AssignSound()
    {
        string resourcePath = VMFUtils.NormalizePath($"res://sound/{SoundPath}");

        if (!ResourceLoader.Exists(resourcePath))
        {
            VMFLogger.Warn($"func_rotating: Sound doesnt exists {resourcePath}");
            return;
        }

        LoopPlayer.Stream = GD.Load<AudioStream>(resourcePath);
    }

    public override void _PhysicsProcess(double dt)
    {
        if (!Enabled) return;

        float speed = MaxSpeed * (float)dt;
        if (HasFlag(FLAG_REVERSE_DIRECTION)) speed *= -1;

        var rot = RotationDegrees;
        if (HasFlag(FLAG_X_AXIS)) rot.X += speed;
        else if (HasFlag(FLAG_Y_AXIS)) rot.Z += speed;
        else rot.Y += speed;
        RotationDegrees = rot;
    }

    public override void EntityReady() => Enabled = HasFlag(FLAG_START_ON);

    public void Start(Variant _param = default)
    {
        Enabled = true;
        LoopPlayer.Play();
    }

    public void Stop(Variant _param = default)
    {
        Enabled = false;
        LoopPlayer.Stop();
    }

    public async void RotateBy(float deg)
    {
        if (_currentTween != null) return;

        _currentTween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        var target = Vector3.Zero;

        if (HasFlag(FLAG_X_AXIS)) target.X = deg;
        else if (HasFlag(FLAG_Y_AXIS)) target.Z = deg;
        else target.Y = deg;

        var endRot = RotationDegrees + target;

        _currentTween.TweenProperty(this, "rotation_degrees", endRot, 2.0);
        _currentTween.Play();

        await ToSignal(_currentTween, Tween.SignalName.Finished);
        _currentTween = null;
    }
}
