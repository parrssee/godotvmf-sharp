using Godot;

namespace GodotVMF;

[Tool]
public partial class math_counter : VMFEntityNode
{
    public float Value;
    public float StartValue => Entity.GetFloat("startvalue");

    public override void EntityReady() => Value = StartValue;

    public void Add(Variant valueToAdd)
    {
        Value += valueToAdd.AsSingle();
        if (Value >= Entity.GetFloat("max")) TriggerOutput("OnHitMax");
        TriggerOutput("OutValue");
    }

    public void Subtract(Variant valueToSubtract)
    {
        Value -= valueToSubtract.AsSingle();
        if (Value <= Entity.GetFloat("min")) TriggerOutput("OnHitMin");
        TriggerOutput("OutValue");
    }

    public void Multiply(Variant valueToMultiply)
    {
        Value *= valueToMultiply.AsSingle();
        if (Value >= Entity.GetFloat("max")) TriggerOutput("OnHitMax");
        TriggerOutput("OutValue");
    }

    public void Divide(Variant valueToDivide)
    {
        Value /= valueToDivide.AsSingle();
        if (Value <= Entity.GetFloat("min")) TriggerOutput("OnHitMin");
        TriggerOutput("OutValue");
    }

    public void GetValue(Variant _param = default) => TriggerOutput("OnGetValue");

    public void SetValue(Variant _param = default)
    {
        if (Activator != null) Value = Activator.Get("value").AsSingle();
        TriggerOutput("OutValue");
    }

    public void SetMinValueNoFire(Variant valueToSet)
    {
        Entity["min"] = valueToSet;
        TriggerOutput("OutValue");
    }

    public void SetMaxValueNoFire(Variant valueToSet)
    {
        Entity["max"] = valueToSet;
        TriggerOutput("OutValue");
    }

    public void SetHitMinOutputNoFire(Variant outputToSet) => Entity["min"] = outputToSet;

    public void SetHitMaxOutputNoFire(Variant outputToSet) => Entity["max"] = outputToSet;
}
