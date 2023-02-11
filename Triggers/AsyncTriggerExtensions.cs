using System.Threading;
using Fractural.Tasks.Triggers;
using Godot;

namespace Fractural.Tasks
{
    public static class GDTaskCancellationExtensions
    {
        /// <summary>This CancellationToken is canceled when the Node will be destroyed.</summary>
        public static CancellationToken GetCancellationTokenOnDestroy(this Node node)
        {
            return node.GetAsyncDestroyTrigger().CancellationToken;
        }
    }
}

namespace Fractural.Tasks.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
        // Special for single operation.

        /// <summary>This function is called when the Node will be destroyed.</summary>
        public static GDTask OnDestroyAsync(this Node node)
        {
            return node.GetAsyncDestroyTrigger().OnDestroyAsync();
        }

        public static GDTask StartAsync(this Node node)
        {
            return node.GetAsyncStartTrigger().StartAsync();
        }

        public static GDTask AwakeAsync(this Node node)
        {
            return node.GetAsyncAwakeTrigger().AwakeAsync();
        }
    }
}

