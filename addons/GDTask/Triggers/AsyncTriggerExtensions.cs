using System;
using System.Threading;
using Godot;

namespace Fractural.Tasks.Triggers;

public static partial class AsyncTriggerExtensions
{
    // Special for single operation.
    public static T GetImmediateChild<T>(this Node node, bool includeRoot = true)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (includeRoot && node is T castedRoot)
            return castedRoot;
        else
        {
            foreach (Node child in node.GetChildren())
                if (child is T castedChild)
                    return castedChild;
        }
        return default;
    }

    public static T AddImmediateChild<T>(this Node node)
        where T : Node, new()
    {
        var child = new T();
        node.AddChild(child);
        return child;
    }

    public static T GetOrAddImmediateChild<T>(this Node node)
        where T : Node, new()
    {
        T child = GetImmediateChild<T>(node);
        child ??= AddImmediateChild<T>(node);
        return child;
    }

    /// <summary>This function is called when the Node will be destroyed.</summary>
    public static GDTask OnDestroyAsync(this Node node)
    {
        return node.GetAsyncDestroyTrigger().OnDestroyAsync();
    }

    public static GDTask ReadyAsync(this Node node)
    {
        return node.GetAsyncReadyTrigger().ReadyAsync();
    }

    public static GDTask EnterTreeAsync(this Node node)
    {
        return node.GetAsyncEnterTreeTrigger().EnterTreeAsync();
    }

    public static CancellationToken GetCancellationTokenOnDestroy(this Node node)
    {
        return node.GetAsyncDestroyTrigger().CancellationToken;
    }
}
