#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;
using Godot;

namespace GDTask
{
    // GDTask has no scheduler like TaskScheduler.
    // Only handle unobserved exception.

    public static class GDTaskScheduler
    {
        public static event Action<Exception> UnobservedTaskException;

        /// <summary>
        /// Propagate OperationCanceledException to UnobservedTaskException when true. Default is false.
        /// </summary>
        public static bool PropagateOperationCanceledException = false;

        internal static void PublishUnobservedTaskException(Exception ex)
        {
            if (ex != null)
            {
                if (!PropagateOperationCanceledException && ex is OperationCanceledException)
                {
                    return;
                }

                if (UnobservedTaskException != null)
                {
                    UnobservedTaskException.Invoke(ex);
                }
                else
                {
                    GD.PrintErr("UnobservedTaskException: " + ex.ToString());
                }
            }
        }
    }
}

