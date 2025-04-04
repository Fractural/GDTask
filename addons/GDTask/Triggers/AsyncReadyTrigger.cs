using Godot;

namespace Fractural.Tasks.Triggers;

public static partial class AsyncTriggerExtensions
{
    public static AsyncReadyTrigger GetAsyncReadyTrigger(this Node node)
    {
        return node.GetOrAddImmediateChild<AsyncReadyTrigger>();
    }
}

public sealed partial class AsyncReadyTrigger : AsyncTriggerBase<AsyncUnit>
{
    private bool _called;

    public override void _Ready()
    {
        base._Ready();
        _called = true;
        RaiseEvent(AsyncUnit.Default);
    }

    public GDTask ReadyAsync()
    {
        if (_called)
            return GDTask.CompletedTask;

        return ((IAsyncOneShotTrigger)new AsyncTriggerHandler<AsyncUnit>(this, true)).OneShotAsync();
    }
}
