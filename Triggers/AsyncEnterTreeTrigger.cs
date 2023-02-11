﻿using System.Threading;
using Godot;
using Fractural.Utils;

namespace Fractural.Tasks.Triggers
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
