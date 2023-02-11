using System;
using System.Runtime.CompilerServices;

namespace Fractural.Tasks
{
    public enum GDTaskStatus
    {
        /// <summary>The operation has not yet completed.</summary>
        Pending = 0,
        /// <summary>The operation completed successfully.</summary>
        Succeeded = 1,
        /// <summary>The operation completed with an error.</summary>
        Faulted = 2,
        /// <summary>The operation completed due to cancellation.</summary>
        Canceled = 3
    }

    // General architecture:
    // Each GDTask holds a IGDTaskSource, which determines how the GDTask will run. This is basically a strategy pattern.
    // GDTask is a struct, so will be allocated on stack with no garbage collection. All IGDTaskSources will be pooled using
    // TaskPool<T>, so again, no garabage will be generated.
    //
    // Hence we achieve 0 memory allocation, making our tasks run really fast.

    /// <summary>
    /// GDTaskSource that has a void return (returns nothing).
    /// </summary>
    public interface IGDTaskSource
    {
        GDTaskStatus GetStatus(short token);
        void OnCompleted(Action<object> continuation, object state, short token);
        void GetResult(short token);

        GDTaskStatus UnsafeGetStatus(); // only for debug use.
    }

    /// <summary>
    /// GDTaskSource that has a return value of <see cref="T"/>
    /// </summary>
    /// <typeparam name="T">Return value of the task source</typeparam>
    public interface IGDTaskSource<out T> : IGDTaskSource
    {
        // Hide the original void GetResult method
        new T GetResult(short token);
    }

    // Extensions are all aggressive inlined so all calls are substituted with raw code for greatest performance.
    public static class GDTaskStatusExtensions
    {
        /// <summary>status != Pending.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompleted(this GDTaskStatus status)
        {
            return status != GDTaskStatus.Pending;
        }

        /// <summary>status == Succeeded.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompletedSuccessfully(this GDTaskStatus status)
        {
            return status == GDTaskStatus.Succeeded;
        }

        /// <summary>status == Canceled.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCanceled(this GDTaskStatus status)
        {
            return status == GDTaskStatus.Canceled;
        }

        /// <summary>status == Faulted.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFaulted(this GDTaskStatus status)
        {
            return status == GDTaskStatus.Faulted;
        }
    }
}