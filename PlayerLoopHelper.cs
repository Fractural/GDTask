#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Linq;
using Godot;
using GDTask.Internal;
using System.Threading;

namespace GDTask
{
    public static class GDTaskLoopRunners
    {
        public struct GDTaskLoopRunnerProcess { };
        public struct GDTaskLoopRunnerPhysicsProcess { };
    }

    public enum PlayerLoopTiming
    {
        Process = 1,
        PhysicsProcess = 2,
    }

    [Flags]
    public enum InjectPlayerLoopTimings
    {
        /// <summary>
        /// Preset: All loops(default).
        /// </summary>
        All = Process | PhysicsProcess,

        /// <summary>
        /// Preset: All without last except LastPostLateUpdate.
        /// </summary>
        Standard = Process | PhysicsProcess,

        /// <summary>
        /// Preset: Minimum pattern, Update | FixedUpdate | LastPostLateUpdate
        /// </summary>
        Minimum =
            Process | PhysicsProcess,

        // PlayerLoopTiming

        PhysicsProcess = 1,
        Process = 2,
    }

    public interface IPlayerLoopItem
    {
        bool MoveNext();
    }

    public static class PlayerLoopHelper
    {
        public static SynchronizationContext UnitySynchronizationContext => unitySynchronizationContext;
        public static int MainThreadId => mainThreadId;
        internal static string ApplicationDataPath => applicationDataPath;

        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId;

        static int mainThreadId;
        static string applicationDataPath;
        static SynchronizationContext unitySynchronizationContext;
        static ContinuationQueue[] yielders;
        static PlayerLoopRunner[] runners;
        internal static bool IsEditorApplicationQuitting { get; private set; }

        public static void AddAction(PlayerLoopTiming timing, IPlayerLoopItem action)
        {
            var runner = runners[(int)timing];
            if (runner == null)
            {
                ThrowInvalidLoopTiming(timing);
            }
            runner.AddAction(action);
        }

        static void ThrowInvalidLoopTiming(PlayerLoopTiming playerLoopTiming)
        {
            throw new InvalidOperationException("Target playerLoopTiming is not injected. Please check PlayerLoopHelper.Initialize. PlayerLoopTiming:" + playerLoopTiming);
        }

        // NOTE: Continuation means a asynchronous task invoked by another task after the other task finishes.
        public static void AddContinuation(PlayerLoopTiming timing, Action continuation)
        {
            var q = yielders[(int)timing];
            if (q == null)
            {
                ThrowInvalidLoopTiming(timing);
            }
            q.Enqueue(continuation);
        }
    }
}

