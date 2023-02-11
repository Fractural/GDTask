#pragma warning disable CS1591

using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Fractural.Tasks.CompilerServices
{
    internal interface IStateMachineRunner
    {
        Action MoveNext { get; }
        void Return();

        Action ReturnAction { get; }
    }

    internal interface IStateMachineRunnerPromise : IGDTaskSource
    {
        Action MoveNext { get; }
        GDTask Task { get; }
        void SetResult();
        void SetException(Exception exception);
    }

    internal interface IStateMachineRunnerPromise<T> : IGDTaskSource<T>
    {
        Action MoveNext { get; }
        GDTask<T> Task { get; }
        void SetResult(T result);
        void SetException(Exception exception);
    }

    internal static class StateMachineUtility
    {
        // Get AsyncStateMachine internal state to check IL2CPP bug
        public static int GetState(IAsyncStateMachine stateMachine)
        {
            var info = stateMachine.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .First(x => x.Name.EndsWith("__state"));
            return (int)info.GetValue(stateMachine);
        }
    }

    internal sealed class AsyncGDTaskVoid<TStateMachine> : IStateMachineRunner, ITaskPoolNode<AsyncGDTaskVoid<TStateMachine>>, IGDTaskSource
        where TStateMachine : IAsyncStateMachine
    {
        static TaskPool<AsyncGDTaskVoid<TStateMachine>> pool;

        public Action ReturnAction { get; }

        TStateMachine stateMachine;

        public Action MoveNext { get; }

        public AsyncGDTaskVoid()
        {
            MoveNext = Run;
#if ENABLE_IL2CPP
            ReturnAction = Return;
#endif
        }

        public static void SetStateMachine(ref TStateMachine stateMachine, ref IStateMachineRunner runnerFieldRef)
        {
            if (!pool.TryPop(out var result))
            {
                result = new AsyncGDTaskVoid<TStateMachine>();
            }
            TaskTracker.TrackActiveTask(result, 3);

            runnerFieldRef = result; // set runner before copied.
            result.stateMachine = stateMachine; // copy struct StateMachine(in release build).
        }

        static AsyncGDTaskVoid()
        {
            TaskPool.RegisterSizeGetter(typeof(AsyncGDTaskVoid<TStateMachine>), () => pool.Size);
        }

        AsyncGDTaskVoid<TStateMachine> nextNode;
        public ref AsyncGDTaskVoid<TStateMachine> NextNode => ref nextNode;

        public void Return()
        {
            TaskTracker.RemoveTracking(this);
            stateMachine = default;
            pool.TryPush(this);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Run()
        {
            stateMachine.MoveNext();
        }

        // dummy interface implementation for TaskTracker.

        GDTaskStatus IGDTaskSource.GetStatus(short token)
        {
            return GDTaskStatus.Pending;
        }

        GDTaskStatus IGDTaskSource.UnsafeGetStatus()
        {
            return GDTaskStatus.Pending;
        }

        void IGDTaskSource.OnCompleted(Action<object> continuation, object state, short token)
        {
        }

        void IGDTaskSource.GetResult(short token)
        {
        }
    }

    internal sealed class AsyncGDTask<TStateMachine> : IStateMachineRunnerPromise, IGDTaskSource, ITaskPoolNode<AsyncGDTask<TStateMachine>>
        where TStateMachine : IAsyncStateMachine
    {
        static TaskPool<AsyncGDTask<TStateMachine>> pool;

#if ENABLE_IL2CPP
        readonly Action returnDelegate;  
#endif
        public Action MoveNext { get; }

        TStateMachine stateMachine;
        GDTaskCompletionSourceCore<AsyncUnit> core;

        AsyncGDTask()
        {
            MoveNext = Run;
#if ENABLE_IL2CPP
            returnDelegate = Return;
#endif
        }

        public static void SetStateMachine(ref TStateMachine stateMachine, ref IStateMachineRunnerPromise runnerPromiseFieldRef)
        {
            if (!pool.TryPop(out var result))
            {
                result = new AsyncGDTask<TStateMachine>();
            }
            TaskTracker.TrackActiveTask(result, 3);

            runnerPromiseFieldRef = result; // set runner before copied.
            result.stateMachine = stateMachine; // copy struct StateMachine(in release build).
        }

        AsyncGDTask<TStateMachine> nextNode;
        public ref AsyncGDTask<TStateMachine> NextNode => ref nextNode;

        static AsyncGDTask()
        {
            TaskPool.RegisterSizeGetter(typeof(AsyncGDTask<TStateMachine>), () => pool.Size);
        }

        void Return()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            stateMachine = default;
            pool.TryPush(this);
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            stateMachine = default;
            return pool.TryPush(this);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Run()
        {
            stateMachine.MoveNext();
        }

        public GDTask Task
        {
            [DebuggerHidden]
            get
            {
                return new GDTask(this, core.Version);
            }
        }

        [DebuggerHidden]
        public void SetResult()
        {
            core.TrySetResult(AsyncUnit.Default);
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            core.TrySetException(exception);
        }

        [DebuggerHidden]
        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
#if ENABLE_IL2CPP
                // workaround for IL2CPP bug.
                PlayerLoopHelper.AddContinuation(PlayerLoopTiming.LastPostLateUpdate, returnDelegate);
#else
                TryReturn();
#endif
            }
        }

        [DebuggerHidden]
        public GDTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        [DebuggerHidden]
        public GDTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }
    }

    internal sealed class AsyncGDTask<TStateMachine, T> : IStateMachineRunnerPromise<T>, IGDTaskSource<T>, ITaskPoolNode<AsyncGDTask<TStateMachine, T>>
        where TStateMachine : IAsyncStateMachine
    {
        static TaskPool<AsyncGDTask<TStateMachine, T>> pool;

#if ENABLE_IL2CPP
        readonly Action returnDelegate;  
#endif

        public Action MoveNext { get; }

        TStateMachine stateMachine;
        GDTaskCompletionSourceCore<T> core;

        AsyncGDTask()
        {
            MoveNext = Run;
#if ENABLE_IL2CPP
            returnDelegate = Return;
#endif
        }

        public static void SetStateMachine(ref TStateMachine stateMachine, ref IStateMachineRunnerPromise<T> runnerPromiseFieldRef)
        {
            if (!pool.TryPop(out var result))
            {
                result = new AsyncGDTask<TStateMachine, T>();
            }
            TaskTracker.TrackActiveTask(result, 3);

            runnerPromiseFieldRef = result; // set runner before copied.
            result.stateMachine = stateMachine; // copy struct StateMachine(in release build).
        }

        AsyncGDTask<TStateMachine, T> nextNode;
        public ref AsyncGDTask<TStateMachine, T> NextNode => ref nextNode;

        static AsyncGDTask()
        {
            TaskPool.RegisterSizeGetter(typeof(AsyncGDTask<TStateMachine, T>), () => pool.Size);
        }

        void Return()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            stateMachine = default;
            pool.TryPush(this);
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            stateMachine = default;
            return pool.TryPush(this);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Run()
        {
            stateMachine.MoveNext();
        }

        public GDTask<T> Task
        {
            [DebuggerHidden]
            get
            {
                return new GDTask<T>(this, core.Version);
            }
        }

        [DebuggerHidden]
        public void SetResult(T result)
        {
            core.TrySetResult(result);
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            core.TrySetException(exception);
        }

        [DebuggerHidden]
        public T GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
#if ENABLE_IL2CPP
                // workaround for IL2CPP bug.
                PlayerLoopHelper.AddContinuation(PlayerLoopTiming.LastPostLateUpdate, returnDelegate);
#else
                TryReturn();
#endif
            }
        }

        [DebuggerHidden]
        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        [DebuggerHidden]
        public GDTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        [DebuggerHidden]
        public GDTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }
    }
}

