using Godot;

namespace GodotVMF;

/// Bridges entity scripts (C#) to the project's own singletons.
public static class GameBridge
{
    public static SoundManager? GetSoundManager(this Node node) =>
        node.GetTree()?.Root.GetNodeOrNull("SoundManager") as SoundManager;

    public static string PrecacheSound(this Node node, string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return "";
        return node.GetSoundManager()?.PrecacheSound(soundName) ?? "";
    }

    public static Node? PlaySound(this Node node, Vector3 position, string soundName, float volume = 1f, float pitch = 1f) =>
        node.GetSoundManager()?.PlaySound(position, soundName, volume, pitch);

    public static Node? PlayEverywhere(this Node node, string soundName, float volume = 1f, float pitch = 1f) =>
        node.GetSoundManager()?.PlayEverywhere(soundName, volume, pitch);
}
