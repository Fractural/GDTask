using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Fractural.Tasks.Internal;
using Godot;

namespace Fractural.Tasks;

public enum DelayType
{
    /// <summary>use Time.deltaTime.</summary>
    DeltaTime,

    /// <summary>use Stopwatch.GetTimestamp().</summary>
    Realtime
}

public partial struct GDTask
{
    public static YieldAwaitable Yield()
    {
        // optimized for single continuation
        return new YieldAwaitable(PlayerLoopTiming.Process);
    }

    public static YieldAwaitable Yield(PlayerLoopTiming timing)
    {
        // optimized for single continuation
        return new YieldAwaitable(timing);
    }

    public static GDTask Yield(CancellationToken cancellationToken)
    {
        return new GDTask(YieldPromise.Create(PlayerLoopTiming.Process, cancellationToken, out var token), token);
    }

    public static GDTask Yield(PlayerLoopTiming timing, CancellationToken cancellationToken)
    {
        return new GDTask(YieldPromise.Create(timing, cancellationToken, out var token), token);
    }

    /// <summary>
    /// Similar as GDTask.Yield but guaranteed run on next frame.
    /// </summary>
    public static GDTask NextFrame()
    {
        return new GDTask(NextFramePromise.Create(PlayerLoopTiming.Process, CancellationToken.None, out var token), token);
    }

    /// <summary>
    /// Similar as GDTask.Yield but guaranteed run on next frame.
    /// </summary>
    public static GDTask NextFrame(PlayerLoopTiming timing)
    {
        return new GDTask(NextFramePromise.Create(timing, CancellationToken.None, out var token), token);
    }

    /// <summary>
    /// Similar as GDTask.Yield but guaranteed run on next frame.
    /// </summary>
    public static GDTask NextFrame(CancellationToken cancellationToken)
    {
        return new GDTask(NextFramePromise.Create(PlayerLoopTiming.Process, cancellationToken, out var token), token);
    }

    /// <summary>
    /// Similar as GDTask.Yield but guaranteed run on next frame.
    /// </summary>
    public static GDTask NextFrame(PlayerLoopTiming timing, CancellationToken cancellationToken)
    {
        return new GDTask(NextFramePromise.Create(timing, cancellationToken, out var token), token);
    }

    public static YieldAwaitable WaitForEndOfFrame()
    {
        return GDTask.Yield(PlayerLoopTiming.Process);
    }

    public static GDTask WaitForEndOfFrame(CancellationToken cancellationToken)
    {
        return GDTask.Yield(PlayerLoopTiming.Process, cancellationToken);
    }

    /// <summary>
    /// Same as GDTask.Yield(PlayerLoopTiming.PhysicsProcess).
    /// </summary>
    public static YieldAwaitable WaitForPhysicsProcess()
    {
        return GDTask.Yield(PlayerLoopTiming.PhysicsProcess);
    }

    /// <summary>
    /// Same as GDTask.Yield(PlayerLoopTiming.PhysicsProcess, cancellationToken).
    /// </summary>
    public static GDTask WaitForPhysicsProcess(CancellationToken cancellationToken)
    {
        return GDTask.Yield(PlayerLoopTiming.PhysicsProcess, cancellationToken);
    }

    public static GDTask DelayFrame(
        int delayFrameCount,
        PlayerLoopTiming delayTiming = PlayerLoopTiming.Process,
        CancellationToken cancellationToken = default
    )
    {
        if (delayFrameCount < 0)
        {
            throw new ArgumentOutOfRangeException("Delay does not allow minus delayFrameCount. delayFrameCount:" + delayFrameCount);
        }

        return new GDTask(DelayFramePromise.Create(delayFrameCount, delayTiming, cancellationToken, out var token), token);
    }

    public static GDTask Delay(
        int millisecondsDelay,
        PlayerLoopTiming delayTiming = PlayerLoopTiming.Process,
        CancellationToken cancellationToken = default
    )
    {
        var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
        return Delay(delayTimeSpan, delayTiming, cancellationToken);
    }

    public static GDTask Delay(
        TimeSpan delayTimeSpan,
        PlayerLoopTiming delayTiming = PlayerLoopTiming.Process,
        CancellationToken cancellationToken = default
    )
    {
        return Delay(delayTimeSpan, DelayType.DeltaTime, delayTiming, cancellationToken);
    }

    public static GDTask Delay(
        int millisecondsDelay,
        DelayType delayType,
        PlayerLoopTiming delayTiming = PlayerLoopTiming.Process,
        CancellationToken cancellationToken = default
    )
    {
        var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
        return Delay(delayTimeSpan, delayType, delayTiming, cancellationToken);
    }

    public static GDTask Delay(
        TimeSpan delayTimeSpan,
        DelayType delayType,
        PlayerLoopTiming delayTiming = PlayerLoopTiming.Process,
        CancellationToken cancellationToken = default
    )
    {
        if (delayTimeSpan < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException("Delay does not allow minus delayTimeSpan. delayTimeSpan:" + delayTimeSpan);
        }

#if DEBUG
        // force use Realtime.
        if (GDTaskPlayerLoopAutoload.IsMainThread && Engine.IsEditorHint())
        {
            delayType = DelayType.Realtime;
        }
#endif

        return delayType switch
        {
            DelayType.Realtime => new GDTask(DelayRealtimePromise.Create(delayTimeSpan, delayTiming, cancellationToken, out var token), token),
            DelayType.DeltaTime or _ => new GDTask(DelayPromise.Create(delayTimeSpan, delayTiming, cancellationToken, out var token), token)
        };
    }

    private sealed class YieldPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<YieldPromise>
    {
        private static TaskPool<YieldPromise> _pool;
        private YieldPromise _nextNode;

        private CancellationToken _cancellationToken;
        private GDTaskCompletionSourceCore<object> _core;

        public ref YieldPromise NextNode => ref _nextNode;

        static YieldPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(YieldPromise), () => _pool.Size);
        }

        private YieldPromise() { }

        public static IGDTaskSource Create(PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!_pool.TryPop(out var result))
            {
                result = new YieldPromise();
            }

            result._cancellationToken = cancellationToken;

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskPlayerLoopAutoload.AddAction(timing, result);

            token = result._core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                _core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }
        }

        public GDTaskStatus GetStatus(short token)
        {
            return _core.GetStatus(token);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            _core.TrySetResult(null);
            return false;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _cancellationToken = default;
            return _pool.TryPush(this);
        }
    }

    private sealed class NextFramePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<NextFramePromise>
    {
        private static TaskPool<NextFramePromise> _pool;
        private NextFramePromise _nextNode;

        private bool _isMainThread;
        private ulong _frameCount;
        private CancellationToken _cancellationToken;
        private GDTaskCompletionSourceCore<AsyncUnit> _core;

        public ref NextFramePromise NextNode => ref _nextNode;

        static NextFramePromise()
        {
            TaskPool.RegisterSizeGetter(typeof(NextFramePromise), () => _pool.Size);
        }

        private NextFramePromise() { }

        public static IGDTaskSource Create(PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!_pool.TryPop(out var result))
            {
                result = new NextFramePromise();
            }

            result._isMainThread = GDTaskPlayerLoopAutoload.IsMainThread;
            if (result._isMainThread)
                result._frameCount = Engine.GetProcessFrames();
            result._cancellationToken = cancellationToken;

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskPlayerLoopAutoload.AddAction(timing, result);

            token = result._core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                _core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }
        }

        public GDTaskStatus GetStatus(short token)
        {
            return _core.GetStatus(token);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            if (_isMainThread && _frameCount == Engine.GetProcessFrames())
            {
                return true;
            }

            _core.TrySetResult(AsyncUnit.Default);
            return false;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _cancellationToken = default;
            return _pool.TryPush(this);
        }
    }

    private sealed class DelayFramePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayFramePromise>
    {
        private static TaskPool<DelayFramePromise> _pool;
        private DelayFramePromise _nextNode;

        private bool _isMainThread;
        private ulong _initialFrame;
        private int _delayFrameCount;
        private CancellationToken _cancellationToken;

        private int _currentFrameCount;
        private GDTaskCompletionSourceCore<AsyncUnit> _core;

        public ref DelayFramePromise NextNode => ref _nextNode;

        static DelayFramePromise()
        {
            TaskPool.RegisterSizeGetter(typeof(DelayFramePromise), () => _pool.Size);
        }

        private DelayFramePromise() { }

        public static IGDTaskSource Create(int delayFrameCount, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!_pool.TryPop(out var result))
            {
                result = new DelayFramePromise();
            }

            result._delayFrameCount = delayFrameCount;
            result._cancellationToken = cancellationToken;
            result._isMainThread = GDTaskPlayerLoopAutoload.IsMainThread;
            if (result._isMainThread)
                result._initialFrame = Engine.GetProcessFrames();

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskPlayerLoopAutoload.AddAction(timing, result);

            token = result._core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                _core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }
        }

        public GDTaskStatus GetStatus(short token)
        {
            return _core.GetStatus(token);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            if (_currentFrameCount is 0)
            {
                if (_delayFrameCount is 0) // same as Yield
                {
                    _core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                // skip in initial frame.
                if (_isMainThread && _initialFrame == Engine.GetProcessFrames())
                {
#if DEBUG
                    // force use Realtime.
                    if (GDTaskPlayerLoopAutoload.IsMainThread && Engine.IsEditorHint())
                    {
                        //goto ++currentFrameCount
                    }
                    else
                    {
                        return true;
                    }
#else
                    return true;
#endif
                }
            }

            if (++_currentFrameCount >= _delayFrameCount)
            {
                _core.TrySetResult(AsyncUnit.Default);
                return false;
            }

            return true;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _currentFrameCount = default;
            _delayFrameCount = default;
            _cancellationToken = default;
            return _pool.TryPush(this);
        }
    }

    private sealed class DelayPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayPromise>
    {
        private static TaskPool<DelayPromise> _pool;
        private DelayPromise _nextNode;

        private bool _isMainThread;
        private ulong _initialFrame;
        private double _delayTimeSpan;
        private double _elapsed;
        private PlayerLoopTiming _timing;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<object> _core;

        public ref DelayPromise NextNode => ref _nextNode;

        static DelayPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(DelayPromise), () => _pool.Size);
        }

        private DelayPromise() { }

        public static IGDTaskSource Create(TimeSpan delayTimeSpan, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!_pool.TryPop(out var result))
            {
                result = new DelayPromise();
            }

            result._elapsed = 0.0f;
            result._delayTimeSpan = (float)delayTimeSpan.TotalSeconds;
            result._cancellationToken = cancellationToken;
            result._isMainThread = GDTaskPlayerLoopAutoload.IsMainThread;
            result._timing = timing;
            if (result._isMainThread)
                result._initialFrame = Engine.GetProcessFrames();

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskPlayerLoopAutoload.AddAction(timing, result);

            token = result._core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                _core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }
        }

        public GDTaskStatus GetStatus(short token)
        {
            return _core.GetStatus(token);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            if (_elapsed == 0.0f)
            {
                if (_isMainThread && _initialFrame == Engine.GetProcessFrames())
                {
                    return true;
                }
            }

            if (_timing == PlayerLoopTiming.Process || _timing == PlayerLoopTiming.PauseProcess)
                _elapsed += GDTaskPlayerLoopAutoload.Global.DeltaTime;
            else
                _elapsed += GDTaskPlayerLoopAutoload.Global.PhysicsDeltaTime;

            if (_elapsed >= _delayTimeSpan)
            {
                _core.TrySetResult(null);
                return false;
            }

            return true;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _delayTimeSpan = default;
            _elapsed = default;
            _cancellationToken = default;
            return _pool.TryPush(this);
        }
    }

    private sealed class DelayRealtimePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayRealtimePromise>
    {
        private static TaskPool<DelayRealtimePromise> _pool;
        private DelayRealtimePromise _nextNode;

        private long _delayTimeSpanTicks;
        private ValueStopwatch _stopwatch;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<AsyncUnit> _core;

        public ref DelayRealtimePromise NextNode => ref _nextNode;

        static DelayRealtimePromise()
        {
            TaskPool.RegisterSizeGetter(typeof(DelayRealtimePromise), () => _pool.Size);
        }

        private DelayRealtimePromise() { }

        public static IGDTaskSource Create(TimeSpan delayTimeSpan, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!_pool.TryPop(out var result))
            {
                result = new DelayRealtimePromise();
            }

            result._stopwatch = ValueStopwatch.StartNew();
            result._delayTimeSpanTicks = delayTimeSpan.Ticks;
            result._cancellationToken = cancellationToken;

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskPlayerLoopAutoload.AddAction(timing, result);

            token = result._core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                _core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }
        }

        public GDTaskStatus GetStatus(short token)
        {
            return _core.GetStatus(token);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            if (_stopwatch.IsInvalid)
            {
                _core.TrySetResult(AsyncUnit.Default);
                return false;
            }

            if (_stopwatch.ElapsedTicks >= _delayTimeSpanTicks)
            {
                _core.TrySetResult(AsyncUnit.Default);
                return false;
            }

            return true;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _stopwatch = default;
            _cancellationToken = default;
            return _pool.TryPush(this);
        }
    }
}

public readonly struct YieldAwaitable
{
    private readonly PlayerLoopTiming _timing;

    public YieldAwaitable(PlayerLoopTiming timing)
    {
        _timing = timing;
    }

    public Awaiter GetAwaiter()
    {
        return new Awaiter(_timing);
    }

    public GDTask ToGDTask()
    {
        return GDTask.Yield(_timing, CancellationToken.None);
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly PlayerLoopTiming _timing;

        public Awaiter(PlayerLoopTiming timing)
        {
            _timing = timing;
        }

        public bool IsCompleted => false;

        public void GetResult() { }

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
