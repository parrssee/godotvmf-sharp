using System;
using Godot;

namespace GodotVMF;

public static class VMFLogger
{
    public static void Log(string msg) => GD.Print("[GodotVMF] " + msg);
    public static void Error(string msg) => GD.PushError("[GodotVMF] " + msg);
    public static void Warn(string msg) => GD.PushWarning("[GodotVMF] " + msg);

    public static void Trace(string msg) => GD.Print("\n" + msg + "\n\n");

    public static T MeasureCall<T>(float breakpointTime, string message, Func<T> function)
    {
        ulong start = Time.GetTicksMsec();
        T result = function();
        ulong elapsed = Time.GetTicksMsec() - start;
        if (elapsed > breakpointTime)
            Warn(string.Format(message, elapsed));
        return result;
    }

    public static void MeasureCall(float breakpointTime, string message, Action action)
    {
        ulong start = Time.GetTicksMsec();
        action();
        ulong elapsed = Time.GetTicksMsec() - start;
        if (elapsed > breakpointTime)
            Warn(string.Format(message, elapsed));
    }
}
