using System.Threading.Tasks;
using Godot;

namespace GodotVMF;

[Tool]
public partial class func_door : VMFEntityNode
{
    protected const int FLAG_NON_SOLID = 4;
    protected const int FLAG_PASSABLE = 8;
    protected const int FLAG_TOGGLE = 32;
    protected const int FLAG_USE_OPENS = 256;
    protected const int FLAG_NPC_CANT = 512;
    protected const int FLAG_TOUCH_OPENS = 1024;
    protected const int FLAG_STARTS_LOCKED = 2048;
    protected const int FLAG_SILENT = 4096;

    private Vector3? _moveDirection;
    public Vector3 MoveDirection => _moveDirection ??= GetMovementVector(Entity.GetVector3("movedir"));

    private Vector3? _moveDistance;
    public Vector3 MoveDistance
    {
        get
        {
            if (_moveDistance != null) return _moveDistance.Value;

            var mesh = GetNode<MeshInstance3D>("body/mesh").Mesh;
            if (mesh == null)
            {
                GD.PushError($"Entity {Entity.GetString("id")} has no mesh assigned!");
                return Vector3.Zero;
            }

            _moveDistance = mesh.GetAabb().Size * MoveDirection;
            return _moveDistance.Value;
        }
    }

    private Vector3? _lipVector;
    public Vector3 LipVector => _lipVector ??= MoveDirection * Entity.GetFloat("lip") * VMFConfig.Import.Scale;

    private float? _speed;
    public float Speed => _speed ??= Entity.GetFloat("speed", 100f) * VMFConfig.Import.Scale;

    private float? _volume;
    public float Volume => _volume ??= Entity.GetFloat("volume", 10f) / 10f;

    private float? _radius;
    public float Radius => _radius ??= Entity.GetFloat("radius", 100f / VMFConfig.Import.Scale) * VMFConfig.Import.Scale;

    protected Vector3 StartPosition = Vector3.Zero;
    protected bool IsOpen;
    protected bool IsLocked;
    protected string OpenSound = "";
    protected string CloseSound = "";
    private Tween? _currentTween;

    public override void EntitySetup(VMFEntity entity)
    {
        var mesh = GetMesh();
        if (mesh == null)
            GD.PushError($"Invalid entity {Entity.GetString("id")}");

        GetNode<MeshInstance3D>("body/mesh").Mesh = mesh;

        if (!HasFlag(FLAG_NON_SOLID))
            GetNode<CollisionShape3D>("body/collision").Shape = GetEntityShape();
        else
            GetNode("body/collision").QueueFree();
    }

    public override async void EntityReady()
    {
        StartPosition = Position;
        IsLocked = HasFlag(FLAG_STARTS_LOCKED);

        int spawnpos = Entity.GetInt("spawnpos");
        IsOpen = spawnpos == 1;

        // NOTE: Wait for proper reparenting;
        await ToSignal(GetTree().CreateTimer(0.001), SceneTreeTimer.SignalName.Timeout);
        CallDeferred(GodotObject.MethodName.Call, "MoveDoorDeferred", (float)spawnpos, true);

        OpenSound = GameBridge.PrecacheSound(this, Entity.GetString("noise1"));
        CloseSound = GameBridge.PrecacheSound(this, Entity.GetString("startclosesound"));
    }

    private void MoveDoorDeferred(float targetValue, bool instant) => _ = MoveDoor(targetValue, instant);

    /// 0.0 = closed, 1.0 = open
    protected async Task MoveDoor(float targetValue = 0f, bool instant = false)
    {
        var targetPosition = StartPosition + MoveDistance * targetValue - LipVector * targetValue;
        float time = (targetPosition - Position).Length() / Speed;

        if (instant)
        {
            Position = targetPosition;
            return;
        }

        if (_currentTween != null)
        {
            _currentTween.Stop();
            _currentTween = null;
        }

        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "position", targetPosition, time);
        await ToSignal(_currentTween, Tween.SignalName.Finished);
    }

    public async void Open(Variant _param = default)
    {
        if (IsOpen) return;
        IsOpen = true;

        TriggerOutput("OnOpen");

        if (!string.IsNullOrEmpty(OpenSound))
        {
            var snd = GameBridge.PlaySound(this, GlobalTransform.Origin, OpenSound, Volume) as AudioStreamPlayer3D;
            if (snd != null) snd.MaxDistance = Radius;
        }

        await MoveDoor(1.0f);
        TriggerOutput("OnFullyOpen");
    }

    public void Unlock(Variant _param = default)
    {
        IsLocked = false;
        TriggerOutput("OnUnlocked");
    }

    public void Lock(Variant _param = default)
    {
        IsLocked = true;
        TriggerOutput("OnLocked");
    }

    public async void Close(Variant _param = default)
    {
        if (!IsOpen) return;
        IsOpen = false;

        TriggerOutput("OnClose");

        AudioStreamPlayer3D? snd = null;
        if (!string.IsNullOrEmpty(CloseSound))
            snd = GameBridge.PlaySound(this, GlobalTransform.Origin, CloseSound, Volume) as AudioStreamPlayer3D;
        else if (!string.IsNullOrEmpty(OpenSound))
            snd = GameBridge.PlaySound(this, GlobalTransform.Origin, OpenSound, Volume) as AudioStreamPlayer3D;

        if (snd != null) snd.MaxDistance = Radius;

        await MoveDoor(0.0f);
        TriggerOutput("OnFullyClosed");
    }

    public new void Toggle(Variant _param = default)
    {
        if (IsOpen) Close(_param);
        else Open(_param);
    }
}
