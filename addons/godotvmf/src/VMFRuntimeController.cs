using Godot;

namespace GodotVMF;

[Tool]
public partial class VMFRuntimeController : Node3D
{
    [Export] public string DefaultMapName { get; set; } = "";
    [Export(PropertyHint.Dir)] public string MapsFolder { get; set; } = "res://maps";

    private const string ImportantMessage =
        "In hammer do following steps:\n" +
        "\t1. Open Tools -> Options -> Build programs tab. In the \"Game Executable\" specify path to the Godot Engine launcher.\n" +
        "\t2. Open Run Map window (F9).\n" +
        "\t3. Click \"Edit\" and add new configuration.\n" +
        "\t4. Select the created configuration in Configurations field\n" +
        "\t5. Click \"New\" and add into \"Command\" field this - $game_exe\n" +
        "\t6. Add into \"Parameters\" field this\n" +
        "\t --path $gamedir {scene_path} --vmf $file";

    private const string ProcessFile = ".current_process";

    [Export(PropertyHint.MultilineText)] public string HammerSetup { get; set; } = "";

    public static VMFRuntimeController? Instance { get; private set; }

    private void KillExistingProcess()
    {
        using var pid = FileAccess.Open(ProcessFile, FileAccess.ModeFlags.Read);
        if (pid != null)
        {
            int processToKill = (int)long.Parse(pid.GetLine());
            if (processToKill != 0)
            {
                GD.Print($"Killing existing process: {processToKill}");
                OS.Kill(processToKill);
            }
        }

        using var file = FileAccess.Open(ProcessFile, FileAccess.ModeFlags.Write);
        file?.StoreString(OS.GetProcessId().ToString());
    }

    private void LaunchMap()
    {
        var args = OS.GetCmdlineArgs();
        int vmfArgIdx = System.Array.IndexOf(args, "--vmf");
        string mapName = vmfArgIdx >= 0 && vmfArgIdx + 1 < args.Length
            ? args[vmfArgIdx + 1]
            : DefaultMapName;

        string mapPath = $"{MapsFolder}/{mapName}.vmf";
        if (!FileAccess.FileExists(mapPath))
        {
            GD.PushError($"Map file not found: {mapPath}");
            return;
        }

        GD.Print($"Loading map: {mapPath}");

        var scene = GetTree().CurrentScene;
        var vmf = new VMFNode
        {
            Vmf = mapPath,
            Name = mapName,
            SaveGeometry = false,
            SaveCollision = false,
            IsRuntime = true,
        };

        scene.AddChild(vmf);
        vmf.ImportMap();
        vmf.SetOwner(scene);
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            string scenePath = GetTree().EditedSceneRoot?.SceneFilePath?.Replace("res://", "") ?? "";
            HammerSetup = ImportantMessage.Replace("{scene_path}", scenePath);
            return;
        }

        Instance = this;
        KillExistingProcess();
        LaunchMap();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            DirAccess.RemoveAbsolute(ProcessFile);
            GetTree().Quit();
        }
    }
}
