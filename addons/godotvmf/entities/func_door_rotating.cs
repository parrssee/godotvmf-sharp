using System.Threading.Tasks;
using Godot;

namespace GodotVMF;

[Tool]
public partial class func_door_rotating : VMFEntityNode
{
    private const int FLAG_USE_OPENS = 256;
    private const int FLAG_REVERSE = 2;
    private const int FLAG_X_AXIS = 64;
    private const int FLAG_Z_AXIS = 128;
    private const int FLAG_STARTS_LOCKED = 2048;
    private const int FLAG_ONE_WAY = 16;
    private const int FLAG_TOUCH_OPENS = 1024;
    private const int FLAG_TOGGLE = 32;

    private bool _isOpen;
    private bool _isLocked;
    private bool _isPrevented;
    private Vector3 _initialRotationState = Vector3.Zero;
    private Tween? _currentTween;
    private string _startSound = "";
    private string _stopSound = "";
    private string _startCloseSound = "";
    private string _stopCloseSound = "";

    private void PrecacheSounds()
    {
        _startSound = GameBridge.PrecacheSound(this, Entity.GetString("noise1"));
        _stopSound = GameBridge.PrecacheSound(this, Entity.GetString("noise2"));
        _startCloseSound = GameBridge.PrecacheSound(this, Entity.GetString("startclosesound"));
        _stopCloseSound = GameBridge.PrecacheSound(this, Entity.GetString("closesound"));
    }

    public void _interact(Node player)
    {
        if (!HasFlag(FLAG_USE_OPENS)) return;
        if (!HasFlag(FLAG_TOGGLE) && _isOpen) return;

        if (_isLocked)
        {
            TriggerOutput("OnLockedUse");
            return;
        }

        Toggle();
    }

    public override void EntityReady()
    {
        _initialRotationState = Rotation;
        _isLocked = HasFlag(FLAG_STARTS_LOCKED);
        PrecacheSounds();
    }

    public override void EntitySetup(VMFEntity entity)
    {
        var mesh = GetMesh();
        if (mesh == null || mesh.GetSurfaceCount() == 0)
        {
            GD.Print("No mesh found for entity id " + Entity.GetString("id"));
            return;
        }

        GetNode<MeshInstance3D>("body/mesh").Mesh = mesh;
        GetNode<CollisionShape3D>("body/collision").Shape = mesh.CreateConvexShape();
    }

    private Vector3 GetTargetRotation(float progress)
    {
        float moveDirection = HasFlag(FLAG_REVERSE) ? -1f : 1f;
        float rotationDistanceRad = Entity.GetFloat("distance", 90f) / 180f * Mathf.Pi;

        if (HasFlag(FLAG_X_AXIS))
            return _initialRotationState + new Vector3(rotationDistanceRad, 0, 0) * progress * moveDirection;
        if (HasFlag(FLAG_Z_AXIS))
            return _initialRotationState + new Vector3(0, 0, rotationDistanceRad) * progress * moveDirection;

        return _initialRotationState + new Vector3(0, rotationDistanceRad, 0) * progress * moveDirection;
    }

    private async Task MoveDoor(float progress = 0f)
    {
        float speed = Entity.GetFloat("speed", 100f);
        float rotationDistance = Entity.GetFloat("distance", 90f);
        var targetRotation = GetTargetRotation(progress);

        if (_currentTween != null)
        {
            _isPrevented = true;
            _currentTween.Stop();
        }

        float moveTime = rotationDistance / speed;

        _currentTween = CreateTween();
        _currentTween.SetProcessMode(Tween.TweenProcessMode.Physics);
        _currentTween.TweenProperty(this, "rotation", targetRotation, moveTime);

        await ToSignal(_currentTween, Tween.SignalName.Finished);
        _currentTween = null;
    }

    public async void Open(Variant _param = default)
    {
        if (_isOpen) return;
        _isOpen = true;
        TriggerOutput("OnOpen");

        if (!string.IsNullOrEmpty(_startSound))
            GameBridge.PlaySound(this, GlobalPosition, _startSound);

        await MoveDoor(1.0f);

        if (!string.IsNullOrEmpty(_stopSound))
            GameBridge.PlaySound(this, GlobalPosition, _stopSound);

        if (!_isPrevented) TriggerOutput("OnFullyOpen");
        else _isPrevented = false;
    }

    public async void Close(Variant _param = default)
    {
        if (!_isOpen) return;
        _isOpen = false;
        TriggerOutput("OnClose");

        if (!string.IsNullOrEmpty(_startCloseSound))
            GameBridge.PlaySound(this, GlobalPosition, _startCloseSound);
        else if (!string.IsNullOrEmpty(_startSound))
            GameBridge.PlaySound(this, GlobalPosition, _startSound);

        await MoveDoor(0.0f);

        if (!string.IsNullOrEmpty(_stopCloseSound))
            GameBridge.PlaySound(this, GlobalPosition, _stopCloseSound);
        else if (!string.IsNullOrEmpty(_stopSound))
            GameBridge.PlaySound(this, GlobalPosition, _stopSound);

        if (!_isPrevented) TriggerOutput("OnFullyClosed");
        else _isPrevented = false;
    }

    public void Unlock(Variant _param = default)
    {
        _isLocked = false;
        TriggerOutput("OnUnlocked");
    }

    public void Lock(Variant _param = default)
    {
        _isLocked = true;
        TriggerOutput("OnLocked");
    }

    public new void Toggle(Variant _param = default)
    {
        if (_isOpen) Close(_param);
        else Open(_param);
    }
}
