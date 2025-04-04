using Godot;

namespace Fractural.Tasks.Triggers;

public static partial class AsyncTriggerExtensions
{
    public static AsyncEnterTreeTrigger GetAsyncEnterTreeTrigger(this Node node)
    {
        return node.GetOrAddImmediateChild<AsyncEnterTreeTrigger>();
    }
}

public sealed partial class AsyncEnterTreeTrigger : AsyncTriggerBase<AsyncUnit>
{
    public override void _EnterTree()
    {
        base._EnterTree();
        RaiseEvent(AsyncUnit.Default);
    }

    public GDTask EnterTreeAsync()
    {
        if (CalledEnterTree)
            return GDTask.CompletedTask;

        return ((IAsyncOneShotTrigger)new AsyncTriggerHandler<AsyncUnit>(this, true)).OneShotAsync();
    }
}
