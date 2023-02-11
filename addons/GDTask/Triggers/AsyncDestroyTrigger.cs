#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Threading;
using Godot;
using Fractural.Utils;

namespace GDTask.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
        public static AsyncDestroyTrigger GetAsyncDestroyTrigger(this Node node)
        {
            return node.GetOrAddImmediateChild<AsyncDestroyTrigger>();
        }
    }

    public sealed class AsyncDestroyTrigger : Node
    {
        bool awakeCalled = false;
        bool called = false;
        CancellationTokenSource cancellationTokenSource;

        public CancellationToken CancellationToken
        {
            get
            {
                if (cancellationTokenSource == null)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                }

                if (!awakeCalled)
                {
                    GDTaskPlayerLoopManager.AddAction(PlayerLoopTiming.Process, new AwakeMonitor(this));
                }

                return cancellationTokenSource.Token;
            }
        }

        public override void _EnterTree()
        {
            awakeCalled = true;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
                OnDestroy();
        }

        void OnDestroy()
        {
            called = true;

            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

        public GDTask OnDestroyAsync()
        {
            if (called) return GDTask.CompletedTask;

            var tcs = new GDTaskCompletionSource();

            // OnDestroy = Called Cancel.
            CancellationToken.RegisterWithoutCaptureExecutionContext(state =>
            {
                var tcs2 = (GDTaskCompletionSource)state;
                tcs2.TrySetResult();
            }, tcs);

            return tcs.Task;
        }

        class AwakeMonitor : IPlayerLoopItem
        {
            readonly AsyncDestroyTrigger trigger;

            public AwakeMonitor(AsyncDestroyTrigger trigger)
            {
                this.trigger = trigger;
            }

            public bool MoveNext()
            {
                if (trigger.called) return false;
                if (trigger == null)
                {
                    trigger.OnDestroy();
                    return false;
                }
                return true;
            }
        }
    }
}

