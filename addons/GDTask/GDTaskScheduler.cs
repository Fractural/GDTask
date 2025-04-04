using System;
using Godot;

namespace Fractural.Tasks;

// GDTask has no scheduler like TaskScheduler.
// Only handle unobserved exception.

public static class GDTaskScheduler
{
    public static event Action<Exception> UnobservedTaskException;

    /// <summary>
    /// Propagate OperationCanceledException to UnobservedTaskException when true. Default is false.
    /// </summary>
    public static bool PropagateOperationCanceledException { get; set; } = false;

    internal static void PublishUnobservedTaskException(Exception ex)
    {
        if (ex is null)
        {
            return;
        }

        if (!PropagateOperationCanceledException && ex is OperationCanceledException)
        {
            return;
        }

        if (UnobservedTaskException is null)
        {
            GD.PrintErr($"UnobservedTaskException: {ex}");
            return;
        }

        UnobservedTaskException.Invoke(ex);
    }
}
