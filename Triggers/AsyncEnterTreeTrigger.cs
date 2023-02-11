#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Threading;
using Godot;
using Fractural.Utils;

namespace GDTask.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
        public static AsyncEnterTreeTrigger GetAsyncAwakeTrigger(this Node node)
        {
            return node.GetOrAddImmediateChild<AsyncEnterTreeTrigger>();
        }
    }

    public sealed class AsyncEnterTreeTrigger : AsyncTriggerBase<AsyncUnit>
    {
        public GDTask AwakeAsync()
        {
            if (calledEnterTree) return GDTask.CompletedTask;

            return ((IAsyncOneShotTrigger)new AsyncTriggerHandler<AsyncUnit>(this, true)).OneShotAsync();
        }
    }
}

