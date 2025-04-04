using System;
using Godot;

namespace Fractural.Tasks;

public partial class ProcessListener : Node
{
    public event Action<double> OnProcess;
    public event Action<double> OnPhysicsProcess;

    public override void _Process(double delta)
    {
        OnProcess?.Invoke(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        OnPhysicsProcess?.Invoke(delta);
    }
}
