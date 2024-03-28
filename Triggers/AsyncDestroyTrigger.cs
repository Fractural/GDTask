using Godot;
using System.Threading;

namespace Fractural.Tasks.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
        public static AsyncDestroyTrigger GetAsyncDestroyTrigger(this Node node)
        {
            return node.GetOrAddImmediateChild<AsyncDestroyTrigger>();
        }
    }

    public sealed partial class AsyncDestroyTrigger : Node
    {
        bool enterTreeCalled = false;
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

                return cancellationTokenSource.Token;
            }
        }

        public override void _EnterTree()
        {
            enterTreeCalled = true;
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
    }
}

