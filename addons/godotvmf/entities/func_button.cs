using Godot;

namespace GodotVMF;

[Tool]
public partial class func_button : func_door
{
    private const int FLAG_DONT_MOVE = 1;
    private const int FLAG_TOUCH_ACTIVATES = 256;
    private const int FLAG_DAMAGE_ACTIVATES = 512;
    private const int FLAG_USE_ACTIVATES = 1024;
    private const int FLAG_SPARKS = 4096;

    private float _waitTime;
    private string _clickSound = "";
    private string _lockSound = "";
    private bool _isPressed;

    public override void EntityReady()
    {
        base.EntityReady();

        _clickSound = Entity.GetString("click_sound");
        _lockSound = Entity.GetString("lock_sound");

        if (!string.IsNullOrEmpty(_clickSound)) GameBridge.PrecacheSound(this, _clickSound);
        if (!string.IsNullOrEmpty(_lockSound)) GameBridge.PrecacheSound(this, _lockSound);
    }

    public void _interact(Node player)
    {
        if (!HasFlag(FLAG_USE_ACTIVATES)) return;

        if (IsLocked)
        {
            if (!string.IsNullOrEmpty(_lockSound))
                GameBridge.PlaySound(this, GlobalTransform.Origin, _lockSound);
            TriggerOutput("OnUseLocked");
            return;
        }

        if (HasFlag(FLAG_TOGGLE))
        {
            if (_isPressed) PressIn();
            else PressOut();
        }
        else if (!_isPressed)
        {
            PressIn();
        }

        if (!string.IsNullOrEmpty(_clickSound))
            GameBridge.PlaySound(this, GlobalTransform.Origin, _clickSound);
        TriggerOutput("OnPressed");
    }

    public override void _Process(double delta)
    {
        if (_waitTime > 0.0)
        {
            _waitTime -= (float)delta;
            if (_waitTime <= 0.0)
            {
                _isPressed = false;
                TriggerOutput("OnOut");
                Close();
            }
        }
    }

    public void PressIn(Variant _param = default)
    {
        if (!HasFlag(FLAG_DONT_MOVE))
        {
            if (_waitTime > 0.0) return;
            _waitTime = Entity.GetFloat("wait");
            Open();
        }

        _isPressed = true;
        TriggerOutput("OnIn");
    }

    public void PressOut(Variant _param = default)
    {
        if (!HasFlag(FLAG_DONT_MOVE))
        {
            if (_waitTime > 0.0) return;
            _waitTime = Entity.GetFloat("wait");
            Close();
        }

        _isPressed = false;
        TriggerOutput("OnOut");
    }
}
