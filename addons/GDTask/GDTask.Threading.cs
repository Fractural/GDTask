using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fractural.Tasks.Internal;

namespace Fractural.Tasks;

public partial struct GDTask
{
    /// <summary>
    /// If running on mainthread, do nothing. Otherwise, same as GDTask.Yield(PlayerLoopTiming.Update).
    /// </summary>
    public static SwitchToMainThreadAwaitable SwitchToMainThread(CancellationToken cancellationToken = default)
    {
        return new SwitchToMainThreadAwaitable(PlayerLoopTiming.Process, cancellationToken);
    }

    /// <summary>
    /// If running on mainthread, do nothing. Otherwise, same as GDTask.Yield(timing).
    /// </summary>
    public static SwitchToMainThreadAwaitable SwitchToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
    {
        return new SwitchToMainThreadAwaitable(timing, cancellationToken);
    }

    /// <summary>
    /// Return to mainthread(same as await SwitchToMainThread) after using scope is closed.
    /// </summary>
    public static ReturnToMainThread ReturnToMainThread(CancellationToken cancellationToken = default)
    {
        return new ReturnToMainThread(PlayerLoopTiming.Process, cancellationToken);
    }

    /// <summary>
    /// Return to mainthread(same as await SwitchToMainThread) after using scope is closed.
    /// </summary>
    public static ReturnToMainThread ReturnToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
    {
        return new ReturnToMainThread(timing, cancellationToken);
    }

    /// <summary>
    /// Queue the action to PlayerLoop.
    /// </summary>
    public static void Post(Action action, PlayerLoopTiming timing = PlayerLoopTiming.Process)
    {
        GDTaskPlayerLoopAutoload.AddContinuation(timing, action);
    }

    public static SwitchToThreadPoolAwaitable SwitchToThreadPool()
    {
        return new SwitchToThreadPoolAwaitable();
    }

    /// <summary>
    /// Note: use SwitchToThreadPool is recommended.
    /// </summary>
    public static SwitchToTaskPoolAwaitable SwitchToTaskPool()
    {
        return new SwitchToTaskPoolAwaitable();
    }

    public static SwitchToSynchronizationContextAwaitable SwitchToSynchronizationContext(
        SynchronizationContext synchronizationContext,
        CancellationToken cancellationToken = default
    )
    {
        Error.ThrowArgumentNullException(synchronizationContext, nameof(synchronizationContext));
        return new SwitchToSynchronizationContextAwaitable(synchronizationContext, cancellationToken);
    }

    public static ReturnToSynchronizationContext ReturnToSynchronizationContext(
        SynchronizationContext synchronizationContext,
        CancellationToken cancellationToken = default
    )
    {
        return new ReturnToSynchronizationContext(synchronizationContext, false, cancellationToken);
    }

    public static ReturnToSynchronizationContext ReturnToCurrentSynchronizationContext(
        bool dontPostWhenSameContext = true,
        CancellationToken cancellationToken = default
    )
    {
        return new ReturnToSynchronizationContext(SynchronizationContext.Current, dontPostWhenSameContext, cancellationToken);
    }
}

public struct SwitchToMainThreadAwaitable
{
    private readonly PlayerLoopTiming _playerLoopTiming;
    private readonly CancellationToken _cancellationToken;

    public SwitchToMainThreadAwaitable(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
    {
        _playerLoopTiming = playerLoopTiming;
        _cancellationToken = cancellationToken;
    }

    public Awaiter GetAwaiter() => new Awaiter(_playerLoopTiming, _cancellationToken);

    public struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly PlayerLoopTiming _playerLoopTiming;
        private readonly CancellationToken _cancellationToken;

        public Awaiter(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
        {
            _playerLoopTiming = playerLoopTiming;
            _cancellationToken = cancellationToken;
        }

        public bool IsCompleted
        {
            get
            {
                var currentThreadId = Thread.CurrentThread.ManagedThreadId;
                if (GDTaskPlayerLoopAutoload.MainThreadId == currentThreadId)
                {
                    return true; // run immediate.
                }
                else
                {
                    return false; // register continuation.
                }
            }
        }

        public void GetResult()
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        public void OnCompleted(Action continuation)
        {
            GDTaskPlayerLoopAutoload.AddContinuation(_playerLoopTiming, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            GDTaskPlayerLoopAutoload.AddContinuation(_playerLoopTiming, continuation);
        }
    }
}

public struct ReturnToMainThread
{
    private readonly PlayerLoopTiming _playerLoopTiming;
    private readonly CancellationToken _cancellationToken;

    public ReturnToMainThread(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
    {
        _playerLoopTiming = playerLoopTiming;
        _cancellationToken = cancellationToken;
    }

    public Awaiter DisposeAsync()
    {
        return new Awaiter(_playerLoopTiming, _cancellationToken); // run immediate.
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly PlayerLoopTiming _timing;
        private readonly CancellationToken _cancellationToken;

        public Awaiter(PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            _timing = timing;
            _cancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter() => this;

        public bool IsCompleted => GDTaskPlayerLoopAutoload.MainThreadId == Thread.CurrentThread.ManagedThreadId;

        public void GetResult()
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        public void OnCompleted(Action continuation)
        {
            GDTaskPlayerLoopAutoload.AddContinuation(_timing, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            GDTaskPlayerLoopAutoload.AddContinuation(_timing, continuation);
        }
    }
}

public struct SwitchToThreadPoolAwaitable
{
    public Awaiter GetAwaiter() => new Awaiter();

    public struct Awaiter : ICriticalNotifyCompletion
    {
        private static readonly WaitCallback _switchToCallback = Callback;

        public bool IsCompleted => false;

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            ThreadPool.QueueUserWorkItem(_switchToCallback, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            ThreadPool.UnsafeQueueUserWorkItem(_switchToCallback, continuation);
        }

        private static void Callback(object state)
        {
            var continuation = (Action)state;
            continuation();
        }
    }
}

public struct SwitchToTaskPoolAwaitable
{
    public Awaiter GetAwaiter() => new Awaiter();

    public struct Awaiter : ICriticalNotifyCompletion
    {
        private static readonly Action<object> _switchToCallback = Callback;

        public bool IsCompleted => false;

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            Task.Factory.StartNew(
                _switchToCallback,
                continuation,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default
            );
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            Task.Factory.StartNew(
                _switchToCallback,
                continuation,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default
            );
        }

        private static void Callback(object state)
        {
            var continuation = (Action)state;
            continuation();
        }
    }
}

public struct SwitchToSynchronizationContextAwaitable
{
    private readonly SynchronizationContext _synchronizationContext;
    private readonly CancellationToken _cancellationToken;

    public SwitchToSynchronizationContextAwaitable(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
    {
        _synchronizationContext = synchronizationContext;
        _cancellationToken = cancellationToken;
    }

    public Awaiter GetAwaiter() => new Awaiter(_synchronizationContext, _cancellationToken);

    public struct Awaiter : ICriticalNotifyCompletion
    {
        private static readonly SendOrPostCallback _switchToCallback = Callback;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly CancellationToken _cancellationToken;

        public Awaiter(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
        {
            _synchronizationContext = synchronizationContext;
            _cancellationToken = cancellationToken;
        }

        public bool IsCompleted => false;

        public void GetResult()
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        public void OnCompleted(Action continuation)
        {
            _synchronizationContext.Post(_switchToCallback, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _synchronizationContext.Post(_switchToCallback, continuation);
        }

        private static void Callback(object state)
        {
            var continuation = (Action)state;
            continuation();
        }
    }
}

public struct ReturnToSynchronizationContext
{
    private readonly SynchronizationContext _syncContext;
    private readonly bool _dontPostWhenSameContext;
    private readonly CancellationToken _cancellationToken;

    public ReturnToSynchronizationContext(SynchronizationContext syncContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
    {
        _syncContext = syncContext;
        _dontPostWhenSameContext = dontPostWhenSameContext;
        _cancellationToken = cancellationToken;
    }

    public Awaiter DisposeAsync()
    {
        return new Awaiter(_syncContext, _dontPostWhenSameContext, _cancellationToken);
    }

    public struct Awaiter : ICriticalNotifyCompletion
    {
        private static readonly SendOrPostCallback _switchToCallback = Callback;

        private readonly SynchronizationContext _synchronizationContext;
        private readonly bool _dontPostWhenSameContext;
        private readonly CancellationToken _cancellationToken;

        public Awaiter(SynchronizationContext synchronizationContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
        {
            _synchronizationContext = synchronizationContext;
            _dontPostWhenSameContext = dontPostWhenSameContext;
            _cancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter() => this;

        public bool IsCompleted
        {
            get
            {
                if (!_dontPostWhenSameContext)
                    return false;

                var current = SynchronizationContext.Current;
                if (current == _synchronizationContext)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void GetResult()
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        public void OnCompleted(Action continuation)
        {
            _synchronizationContext.Post(_switchToCallback, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _synchronizationContext.Post(_switchToCallback, continuation);
        }

        private static void Callback(object state)
        {
            var continuation = (Action)state;
            continuation();
        }
    }
}
