using System.Threading;
using Godot;

namespace Fractural.Tasks.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
        public static AsyncEnterTreeTrigger GetAsyncAwakeTrigger(this Node node)
        {
            return node.GetOrAddImmediateChild<AsyncEnterTreeTrigger>();
        }
    }

    public sealed partial class AsyncEnterTreeTrigger : AsyncTriggerBase<AsyncUnit>
    {
        public GDTask AwakeAsync()
        {
            if (calledEnterTree) return GDTask.CompletedTask;

            return ((IAsyncOneShotTrigger)new AsyncTriggerHandler<AsyncUnit>(this, true)).OneShotAsync();
        }
    }
}

