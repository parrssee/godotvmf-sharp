using System.Collections.Generic;
using Godot;

namespace GodotVMF;

public partial class VMFOutputExecutor : VMFEntityNode
{
    public string Target = "";
    public string Input = "";
    public string Param = "";
    public float Delay = 0.0f;
    public int Times = 1;
    public VMFEntityNode? Caller;

    public VMFOutputExecutor() { }

    public VMFOutputExecutor(string target, string input, string param, float delay, int times)
    {
        Target = target;
        Input = input;
        Param = param;
        Delay = delay;
        Times = times;
    }

    public override void _Ready()
    {
        if (Delay <= 0.0f)
            ExecuteTargetInput();
        else
            GetTree().CreateTimer(Delay).Timeout += ExecuteTargetInput;
    }

    private void ExecuteTargetInput()
    {
        if (Caller != null && IsInstanceValid(Caller)) Caller._pendingExecutors.Remove(this);

        List<Node> targets;
        if (Target.StartsWith("!"))
        {
            var single = GetTarget(Target);
            targets = single != null ? new List<Node> { single } : new List<Node>();
        }
        else
        {
            targets = GetAllTargets(Target);
        }

        foreach (var node in targets)
        {
            if (!IsInstanceValid(node)) continue;
            var obj = node as GodotObject;
            if (obj == null) continue;

            if (!obj.HasMethod(Input)) continue;

            obj.Set("activator", Caller);
            obj.Call(Input, Param);
        }

        QueueFree();
    }
}
