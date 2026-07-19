using Godot;

namespace GodotVMF;

[Tool]
public partial class ambient_generic : VMFEntityNode
{
    private const int FLAG_PLAY_EVERYWHERE = 1;
    private const int FLAG_START_SILENT = 16;
    private const int FLAG_IS_NOT_LOOPED = 32;

    private string _sound = "";
    private Node? _soundInstance;
    private float _volume = 1f;
    private bool _isPlaying;

    public override void EntityReady()
    {
        _sound = Entity.GetString("message");
        _volume = Entity.GetFloat("health", 10f) / 10f;

        if (!string.IsNullOrEmpty(_sound))
            GameBridge.PrecacheSound(this, _sound);

        if (!HasFlag(FLAG_START_SILENT))
            PlaySoundInternal(_volume);
    }

    private async void PlaySoundInternal(float volume)
    {
        if (_isPlaying) return;
        _isPlaying = true;

        _soundInstance = HasFlag(FLAG_PLAY_EVERYWHERE)
            ? GameBridge.PlayEverywhere(this, _sound, volume)
            : GameBridge.PlaySound(this, GlobalTransform.Origin, _sound, volume);

        if (_soundInstance != null)
            await ToSignal(_soundInstance, "finished");

        _isPlaying = false;
    }

    private void StopSoundInternal()
    {
        _isPlaying = false;

        if (IsInstanceValid(_soundInstance))
        {
            _soundInstance!.Call("stop");
            _soundInstance.QueueFree();
            _soundInstance = null;
        }
    }

    public void PlaySound(Variant _param = default) => PlaySoundInternal(_volume);

    public void StopSound(Variant _param = default) => StopSoundInternal();

    public void FadeIn(Variant time = default)
    {
        PlaySoundInternal(0.001f);

        float targetVolume = Mathf.LinearToDb(_volume);
        var tween = CreateTween();
        if (_soundInstance != null)
            tween.TweenProperty(_soundInstance, "volume_db", targetVolume, time.AsSingle());
    }

    public async void FadeOut(Variant time = default)
    {
        if (_soundInstance == null) return;

        float targetVolume = Mathf.LinearToDb(0f);
        var tween = CreateTween();
        if (_soundInstance != null)
            tween.TweenProperty(_soundInstance, "volume_db", targetVolume, time.AsSingle());

        await ToSignal(tween, Tween.SignalName.Finished);
        StopSoundInternal();
    }
}
