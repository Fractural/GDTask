using Godot;

namespace Fractural.Tasks.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
        public static AsyncReadyTrigger GetAsyncReadyTrigger(this Node node)
        {
            return node.GetOrAddImmediateChild<AsyncReadyTrigger>();
        }
    }

    public sealed partial class AsyncReadyTrigger : AsyncTriggerBase<AsyncUnit>
    {
        bool called;

        public override void _Ready()
        {
            base._Ready();
            called = true;
            RaiseEvent(AsyncUnit.Default);
        }

        public GDTask ReadyAsync()
        {
            if (called) return GDTask.CompletedTask;

            return ((IAsyncOneShotTrigger)new AsyncTriggerHandler<AsyncUnit>(this, true)).OneShotAsync();
        }
    }
}