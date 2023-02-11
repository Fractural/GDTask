using System;
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
		public static T GetImmediateChild<T>(this Node node, bool includeRoot = true)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (includeRoot && node is T castedRoot)
				return castedRoot;
			else
			{
				foreach (Node child in node.GetChildren())
					if (child is T castedChild) return castedChild;
			}
			return default(T);
		}

		public static T AddImmediateChild<T>(this Node node) where T : Node, new()
		{
			T child = new T();
			node.AddChild(child);
			return child;
		}

		public static T GetOrAddImmediateChild<T>(this Node node) where T : Node, new()
		{
			T child = GetImmediateChild<T>(node);
			if (child == null)
				child = AddImmediateChild<T>(node);
			return child;
		}

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

