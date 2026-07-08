using System.Collections.Generic;
using Godot;

namespace GodotVMF;

public partial class SoundManager : Node
{
    private const string SoundFolder = "res://sound/";

    private readonly Dictionary<string, AudioStream> _soundCache = new();

    /// <summary>
    /// Play a sound at a given position.
    /// </summary>
    public AudioStreamPlayer3D? PlaySound(Vector3 position, string soundName, float volume = 1.0f, float pitch = 1.0f)
    {
        if (soundName == "")
            return null;

        if (!_soundCache.TryGetValue(soundName, out var sound))
        {
            GD.PushError($"Sound {soundName} is not cached. Precache it first.");
            return null;
        }

        var soundPlayer = new AudioStreamPlayer3D();

        GetTree().CurrentScene.AddChild(soundPlayer);
        soundPlayer.GlobalTransform = new Transform3D(soundPlayer.GlobalTransform.Basis, position);

        soundPlayer.Stream = sound;
        soundPlayer.VolumeDb = Mathf.LinearToDb(volume);
        soundPlayer.PitchScale = pitch;
        soundPlayer.Connect(AudioStreamPlayer3D.SignalName.Finished, Callable.From(soundPlayer.QueueFree));
        soundPlayer.Play(0.0f);
        soundPlayer.MaxDistance = 20.0f;
        return soundPlayer;
    }

    /// <summary>
    /// Play a random sound from a list of sound names.
    /// </summary>
    public AudioStreamPlayer3D? PlayRandomSound(Vector3 position, string[] soundList, float volume = 1.0f, float pitch = 1.0f)
    {
        string soundName = soundList[GD.Randi() % soundList.Length];

        return PlaySound(position, soundName, volume, pitch);
    }

    public AudioStreamPlayer? PlayEverywhere(string soundName, float volume = 1.0f, float pitch = 1.0f)
    {
        if (soundName == "")
        {
            GD.PushError("Sound name is empty.");
            return null;
        }

        if (!_soundCache.TryGetValue(soundName, out var sound))
        {
            GD.PushError($"Sound {soundName} is not cached. Precache it first.");
            return null;
        }

        var soundPlayer = new AudioStreamPlayer();

        GetTree().CurrentScene.AddChild(soundPlayer);

        soundPlayer.Stream = sound;
        soundPlayer.VolumeDb = Mathf.LinearToDb(volume);
        soundPlayer.PitchScale = pitch;
        soundPlayer.Connect(AudioStreamPlayer.SignalName.Finished, Callable.From(soundPlayer.QueueFree));
        soundPlayer.Play(0.0f);
        return soundPlayer;
    }

    public string PrecacheSound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName))
            return "";

        GD.Print($"Precaching sound {soundName}");
        _soundCache[soundName] = GD.Load<AudioStream>(SoundFolder + soundName);

        return soundName;
    }
}
