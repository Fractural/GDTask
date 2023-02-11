#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Godot;
using Fractural.Utils;

namespace GDTask.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
        public static AsyncReadyTrigger GetAsyncStartTrigger(this Node node)
        {
            return node.GetOrAddImmediateChild<AsyncReadyTrigger>();
        }
    }

    public sealed class AsyncReadyTrigger : AsyncTriggerBase<AsyncUnit>
    {
        bool called;

        public override void _Ready()
        {
            base._Ready();
            called = true;
            RaiseEvent(AsyncUnit.Default);
        }

        public GDTask StartAsync()
        {
            if (called) return GDTask.CompletedTask;

            return ((IAsyncOneShotTrigger)new AsyncTriggerHandler<AsyncUnit>(this, true)).OneShotAsync();
        }
    }
}