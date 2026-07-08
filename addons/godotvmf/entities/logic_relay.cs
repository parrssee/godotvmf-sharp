using Godot;

namespace GodotVMF;

[Tool]
public partial class logic_relay : VMFEntityNode
{
    public void Trigger(Variant _param = default) => TriggerOutput("OnTrigger");
}
