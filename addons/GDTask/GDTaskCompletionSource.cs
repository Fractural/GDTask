using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Fractural.Tasks;

public interface IResolvePromise
{
    bool TrySetResult();
}

public interface IResolvePromise<T>
{
    bool TrySetResult(T value);
}

public interface IRejectPromise
{
    bool TrySetException(Exception exception);
}

public interface ICancelPromise
{
    bool TrySetCanceled(CancellationToken cancellationToken = default);
}

public interface IPromise<T> : IResolvePromise<T>, IRejectPromise, ICancelPromise { }

public interface IPromise : IResolvePromise, IRejectPromise, ICancelPromise { }

internal class ExceptionHolder
{
    private ExceptionDispatchInfo _exception;
    private bool _calledGet = false;

    public ExceptionHolder(ExceptionDispatchInfo exception)
    {
        _exception = exception;
    }

    public ExceptionDispatchInfo GetException()
    {
        if (!_calledGet)
        {
            _calledGet = true;
            GC.SuppressFinalize(this);
        }
        return _exception;
    }

    ~ExceptionHolder()
    {
        if (!_calledGet)
        {
            GDTaskScheduler.PublishUnobservedTaskException(_exception.SourceException);
        }
    }
}

[StructLayout(LayoutKind.Auto)]
public struct GDTaskCompletionSourceCore<TResult>
{
    // Struct Size: TResult + (8 + 2 + 1 + 1 + 8 + 8)

    private TResult result;
    private object error; // ExceptionHolder or OperationCanceledException
    private short version;
    private bool hasUnhandledError;
    private int completedCount; // 0: completed == false
    private Action<object> continuation;
    private object continuationState;

    [DebuggerHidden]
    public void Reset()
    {
        ReportUnhandledError();
        unchecked
        {
            version += 1; // incr version.
        }
        completedCount = 0;
        result = default;
        error = null;
        hasUnhandledError = false;
        continuation = null;
        continuationState = null;
    }

    private void ReportUnhandledError()
    {
        if (hasUnhandledError)
        {
            try
            {
                if (error is OperationCanceledException oc)
                {
                    GDTaskScheduler.PublishUnobservedTaskException(oc);
                }
                else if (error is ExceptionHolder e)
                {
                    GDTaskScheduler.PublishUnobservedTaskException(e.GetException().SourceException);
                }
            }
            catch { }
        }
    }

    internal void MarkHandled()
    {
        hasUnhandledError = false;
    }

    /// <summary>Completes with a successful result.</summary>
    /// <param name="result">The result.</param>
    [DebuggerHidden]
    public bool TrySetResult(TResult result)
    {
        if (Interlocked.Increment(ref completedCount) == 1)
        {
            // setup result
            this.result = result;

            if (continuation != null || Interlocked.CompareExchange(ref this.continuation, GDTaskCompletionSourceCoreShared.s_sentinel, null) != null)
            {
                continuation(continuationState);
                return true;
            }
        }

        return false;
    }

    /// <summary>Completes with an error.</summary>
    /// <param name="error">The exception.</param>
    [DebuggerHidden]
    public bool TrySetException(Exception error)
    {
        if (Interlocked.Increment(ref completedCount) == 1)
        {
            // setup result
            this.hasUnhandledError = true;
            if (error is OperationCanceledException)
            {
                this.error = error;
            }
            else
            {
                this.error = new ExceptionHolder(ExceptionDispatchInfo.Capture(error));
            }

            if (
                continuation is not null
                || Interlocked.CompareExchange(ref this.continuation, GDTaskCompletionSourceCoreShared.s_sentinel, null) is not null
            )
            {
                continuation(continuationState);
                return true;
            }
        }

        return false;
    }

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref completedCount) == 1)
        {
            // setup result
            this.hasUnhandledError = true;
            this.error = new OperationCanceledException(cancellationToken);

            if (
                continuation is not null
                || Interlocked.CompareExchange(ref this.continuation, GDTaskCompletionSourceCoreShared.s_sentinel, null) is not null
            )
            {
                continuation(continuationState);
                return true;
            }
        }

        return false;
    }

    /// <summary>Gets the operation version.</summary>
    [DebuggerHidden]
    public short Version => version;

    /// <summary>Gets the status of the operation.</summary>
    /// <param name="token">Opaque value that was provided to the <see cref="GDTask"/>'s constructor.</param>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GDTaskStatus GetStatus(short token)
    {
        ValidateToken(token);
        return (continuation is null || (completedCount is 0))
            ? GDTaskStatus.Pending
            : (error is null)
                ? GDTaskStatus.Succeeded
                : (error is OperationCanceledException)
                    ? GDTaskStatus.Canceled
                    : GDTaskStatus.Faulted;
    }

    /// <summary>Gets the status of the operation without token validation.</summary>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GDTaskStatus UnsafeGetStatus()
    {
        return (continuation is null || (completedCount is 0))
            ? GDTaskStatus.Pending
            : (error is null)
                ? GDTaskStatus.Succeeded
                : (error is OperationCanceledException)
                    ? GDTaskStatus.Canceled
                    : GDTaskStatus.Faulted;
    }

    /// <summary>Gets the result of the operation.</summary>
    /// <param name="token">Opaque value that was provided to the <see cref="GDTask"/>'s constructor.</param>
    // [StackTraceHidden]
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult GetResult(short token)
    {
        ValidateToken(token);
        if (completedCount is 0)
        {
            throw new InvalidOperationException("Not yet completed, GDTask only allow to use await.");
        }

        if (error is not null)
        {
            hasUnhandledError = false;
            if (error is OperationCanceledException oce)
            {
                throw oce;
            }
            else if (error is ExceptionHolder eh)
            {
                eh.GetException().Throw();
            }

            throw new InvalidOperationException("Critical: invalid exception type was held.");
        }

        return result;
    }

    /// <summary>Schedules the continuation action for this operation.</summary>
    /// <param name="continuation">The continuation to invoke when the operation has completed.</param>
    /// <param name="state">The state object to pass to <paramref name="continuation"/> when it's invoked.</param>
    /// <param name="token">Opaque value that was provided to the <see cref="GDTask"/>'s constructor.</param>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(
        Action<object> continuation,
        object state,
        short token /*, ValueTaskSourceOnCompletedFlags flags */
    )
    {
        if (continuation == null)
        {
            throw new ArgumentNullException(nameof(continuation));
        }
        ValidateToken(token);

        /* no use ValueTaskSourceOnCOmpletedFlags, always no capture ExecutionContext and SynchronizationContext. */

        /*
            PatternA: GetStatus=Pending => OnCompleted => TrySet*** => GetResult
            PatternB: TrySet*** => GetStatus=!Pending => GetResult
            PatternC: GetStatus=Pending => TrySet/OnCompleted(race condition) => GetResult
            C.1: win OnCompleted -> TrySet invoke saved continuation
            C.2: win TrySet -> should invoke continuation here.
        */

        // not set continuation yet.
        object oldContinuation = this.continuation;
        if (oldContinuation == null)
        {
            continuationState = state;
            oldContinuation = Interlocked.CompareExchange(ref this.continuation, continuation, null);
        }

        if (oldContinuation != null)
        {
            // already running continuation in TrySet.
            // It will cause call OnCompleted multiple time, invalid.
            if (!ReferenceEquals(oldContinuation, GDTaskCompletionSourceCoreShared.s_sentinel))
            {
                throw new InvalidOperationException("Already continuation registered, can not await twice or get Status after await.");
            }

            continuation(state);
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateToken(short token)
    {
        if (token != version)
        {
            throw new InvalidOperationException("Token version is not matched, can not await twice or get Status after await.");
        }
    }
}

internal static class GDTaskCompletionSourceCoreShared // separated out of generic to avoid unnecessary duplication
{
    internal static readonly Action<object> s_sentinel = CompletionSentinel;

    private static void CompletionSentinel(object _) // named method to aid debugging
    {
        throw new InvalidOperationException("The sentinel delegate should never be invoked.");
    }
}

public partial class AutoResetGDTaskCompletionSource : IGDTaskSource, ITaskPoolNode<AutoResetGDTaskCompletionSource>, IPromise
{
    private static TaskPool<AutoResetGDTaskCompletionSource> _pool;
    private AutoResetGDTaskCompletionSource _nextNode;
    private GDTaskCompletionSourceCore<AsyncUnit> _core;
    public ref AutoResetGDTaskCompletionSource NextNode => ref _nextNode;

    static AutoResetGDTaskCompletionSource()
    {
        TaskPool.RegisterSizeGetter(typeof(AutoResetGDTaskCompletionSource), () => _pool.Size);
    }

    private AutoResetGDTaskCompletionSource() { }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource Create()
    {
        if (!_pool.TryPop(out var result))
        {
            result = new AutoResetGDTaskCompletionSource();
        }
        TaskTracker.TrackActiveTask(result, 2);
        return result;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource CreateFromCanceled(CancellationToken cancellationToken, out short token)
    {
        var source = Create();
        source.TrySetCanceled(cancellationToken);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource CreateFromException(Exception exception, out short token)
    {
        var source = Create();
        source.TrySetException(exception);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource CreateCompleted(out short token)
    {
        var source = Create();
        source.TrySetResult();
        token = source._core.Version;
        return source;
    }

    public GDTask Task
    {
        [DebuggerHidden]
        get { return new GDTask(this, _core.Version); }
    }

    [DebuggerHidden]
    public bool TrySetResult()
    {
        return _core.TrySetResult(AsyncUnit.Default);
    }

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default)
    {
        return _core.TrySetCanceled(cancellationToken);
    }

    [DebuggerHidden]
    public bool TrySetException(Exception exception)
    {
        return _core.TrySetException(exception);
    }

    [DebuggerHidden]
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

    [DebuggerHidden]
    public GDTaskStatus GetStatus(short token)
    {
        return _core.GetStatus(token);
    }

    [DebuggerHidden]
    public GDTaskStatus UnsafeGetStatus()
    {
        return _core.UnsafeGetStatus();
    }

    [DebuggerHidden]
    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        _core.OnCompleted(continuation, state, token);
    }

    [DebuggerHidden]
    bool TryReturn()
    {
        TaskTracker.RemoveTracking(this);
        _core.Reset();
        return _pool.TryPush(this);
    }
}

public partial class AutoResetGDTaskCompletionSource<T> : IGDTaskSource<T>, ITaskPoolNode<AutoResetGDTaskCompletionSource<T>>, IPromise<T>
{
    private static TaskPool<AutoResetGDTaskCompletionSource<T>> _pool;
    private AutoResetGDTaskCompletionSource<T> _nextNode;
    private GDTaskCompletionSourceCore<T> _core;
    public ref AutoResetGDTaskCompletionSource<T> NextNode => ref _nextNode;

    static AutoResetGDTaskCompletionSource()
    {
        TaskPool.RegisterSizeGetter(typeof(AutoResetGDTaskCompletionSource<T>), () => _pool.Size);
    }

    private AutoResetGDTaskCompletionSource() { }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource<T> Create()
    {
        if (!_pool.TryPop(out var result))
        {
            result = new AutoResetGDTaskCompletionSource<T>();
        }
        TaskTracker.TrackActiveTask(result, 2);
        return result;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource<T> CreateFromCanceled(CancellationToken cancellationToken, out short token)
    {
        var source = Create();
        source.TrySetCanceled(cancellationToken);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource<T> CreateFromException(Exception exception, out short token)
    {
        var source = Create();
        source.TrySetException(exception);
        token = source._core.Version;
        return source;
    }

    [DebuggerHidden]
    public static AutoResetGDTaskCompletionSource<T> CreateFromResult(T result, out short token)
    {
        var source = Create();
        source.TrySetResult(result);
        token = source._core.Version;
        return source;
    }

    public GDTask<T> Task
    {
        [DebuggerHidden]
        get { return new GDTask<T>(this, _core.Version); }
    }

    [DebuggerHidden]
    public bool TrySetResult(T result)
    {
        return _core.TrySetResult(result);
    }

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default)
    {
        return _core.TrySetCanceled(cancellationToken);
    }

    [DebuggerHidden]
    public bool TrySetException(Exception exception)
    {
        return _core.TrySetException(exception);
    }

    [DebuggerHidden]
    public T GetResult(short token)
    {
        try
        {
            return _core.GetResult(token);
        }
        finally
        {
            TryReturn();
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
        return _core.GetStatus(token);
    }

    [DebuggerHidden]
    public GDTaskStatus UnsafeGetStatus()
    {
        return _core.UnsafeGetStatus();
    }

    [DebuggerHidden]
    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        _core.OnCompleted(continuation, state, token);
    }

    [DebuggerHidden]
    bool TryReturn()
    {
        TaskTracker.RemoveTracking(this);
        _core.Reset();
        return _pool.TryPush(this);
    }
}

public partial class GDTaskCompletionSource : IGDTaskSource, IPromise
{
    private CancellationToken _cancellationToken;
    private ExceptionHolder _exception;
    private object _gate;
    private Action<object> _singleContinuation;
    private object _singleState;
    private List<(Action<object>, object)> _secondaryContinuationList;

    private int _intStatus; // GDTaskStatus
    private bool _handled = false;

    public GDTaskCompletionSource()
    {
        TaskTracker.TrackActiveTask(this, 2);
    }

    [DebuggerHidden]
    internal void MarkHandled()
    {
        if (!_handled)
        {
            _handled = true;
            TaskTracker.RemoveTracking(this);
        }
    }

    public GDTask Task
    {
        [DebuggerHidden]
        get { return new GDTask(this, 0); }
    }

    [DebuggerHidden]
    public bool TrySetResult()
    {
        return TrySignalCompletion(GDTaskStatus.Succeeded);
    }

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default)
    {
        if (UnsafeGetStatus() is not GDTaskStatus.Pending)
            return false;

        _cancellationToken = cancellationToken;
        return TrySignalCompletion(GDTaskStatus.Canceled);
    }

    [DebuggerHidden]
    public bool TrySetException(Exception exception)
    {
        if (exception is OperationCanceledException oce)
        {
            return TrySetCanceled(oce.CancellationToken);
        }

        if (UnsafeGetStatus() is not GDTaskStatus.Pending)
            return false;

        _exception = new ExceptionHolder(ExceptionDispatchInfo.Capture(exception));
        return TrySignalCompletion(GDTaskStatus.Faulted);
    }

    [DebuggerHidden]
    public void GetResult(short token)
    {
        MarkHandled();

        var status = (GDTaskStatus)_intStatus;
        switch (status)
        {
            case GDTaskStatus.Succeeded:
                return;
            case GDTaskStatus.Faulted:
                _exception.GetException().Throw();
                return;
            case GDTaskStatus.Canceled:
                throw new OperationCanceledException(_cancellationToken);
            default:
            case GDTaskStatus.Pending:
                throw new InvalidOperationException("not yet completed.");
        }
    }

    [DebuggerHidden]
    public GDTaskStatus GetStatus(short token)
    {
        return (GDTaskStatus)_intStatus;
    }

    [DebuggerHidden]
    public GDTaskStatus UnsafeGetStatus()
    {
        return (GDTaskStatus)_intStatus;
    }

    [DebuggerHidden]
    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        if (_gate is null)
        {
            Interlocked.CompareExchange(ref _gate, new object(), null);
        }

        var lockGate = Thread.VolatileRead(ref _gate);
        lock (lockGate) // wait TrySignalCompletion, after status is not pending.
        {
            if ((GDTaskStatus)_intStatus is not GDTaskStatus.Pending)
            {
                continuation(state);
                return;
            }

            if (_singleContinuation is null)
            {
                _singleContinuation = continuation;
                _singleState = state;
            }
            else
            {
                _secondaryContinuationList ??= [];
                _secondaryContinuationList.Add((continuation, state));
            }
        }
    }

    [DebuggerHidden]
    private bool TrySignalCompletion(GDTaskStatus status)
    {
        if (Interlocked.CompareExchange(ref _intStatus, (int)status, (int)GDTaskStatus.Pending) == (int)GDTaskStatus.Pending)
        {
            if (_gate is null)
            {
                Interlocked.CompareExchange(ref _gate, new object(), null);
            }

            var lockGate = Thread.VolatileRead(ref _gate);
            lock (lockGate) // wait OnCompleted.
            {
                if (_singleContinuation is not null)
                {
                    try
                    {
                        _singleContinuation(_singleState);
                    }
                    catch (Exception ex)
                    {
                        GDTaskScheduler.PublishUnobservedTaskException(ex);
                    }
                }

                if (_secondaryContinuationList is not null)
                {
                    foreach (var (c, state) in _secondaryContinuationList)
                    {
                        try
                        {
                            c(state);
                        }
                        catch (Exception ex)
                        {
                            GDTaskScheduler.PublishUnobservedTaskException(ex);
                        }
                    }
                }

                _singleContinuation = null;
                _singleState = null;
                _secondaryContinuationList = null;
            }
            return true;
        }
        return false;
    }
}

public partial class GDTaskCompletionSource<T> : IGDTaskSource<T>, IPromise<T>
{
    private CancellationToken _cancellationToken;
    private T _result;
    private ExceptionHolder _exception;
    private object _gate;
    private Action<object> _singleContinuation;
    private object _singleState;
    private List<(Action<object>, object)> _secondaryContinuationList;

    private int _intStatus; // GDTaskStatus
    private bool _handled = false;

    public GDTaskCompletionSource()
    {
        TaskTracker.TrackActiveTask(this, 2);
    }

    [DebuggerHidden]
    internal void MarkHandled()
    {
        if (!_handled)
        {
            _handled = true;
            TaskTracker.RemoveTracking(this);
        }
    }

    public GDTask<T> Task
    {
        [DebuggerHidden]
        get { return new GDTask<T>(this, 0); }
    }

    [DebuggerHidden]
    public bool TrySetResult(T result)
    {
        if (UnsafeGetStatus() is not GDTaskStatus.Pending)
            return false;

        this._result = result;
        return TrySignalCompletion(GDTaskStatus.Succeeded);
    }

    [DebuggerHidden]
    public bool TrySetCanceled(CancellationToken cancellationToken = default)
    {
        if (UnsafeGetStatus() is not GDTaskStatus.Pending)
            return false;

        _cancellationToken = cancellationToken;
        return TrySignalCompletion(GDTaskStatus.Canceled);
    }

    [DebuggerHidden]
    public bool TrySetException(Exception exception)
    {
        if (exception is OperationCanceledException oce)
        {
            return TrySetCanceled(oce.CancellationToken);
        }

        if (UnsafeGetStatus() is not GDTaskStatus.Pending)
            return false;

        _exception = new ExceptionHolder(ExceptionDispatchInfo.Capture(exception));
        return TrySignalCompletion(GDTaskStatus.Faulted);
    }

    [DebuggerHidden]
    public T GetResult(short token)
    {
        MarkHandled();

        var status = (GDTaskStatus)_intStatus;
        switch (status)
        {
            case GDTaskStatus.Succeeded:
                return _result;
            case GDTaskStatus.Faulted:
                _exception.GetException().Throw();
                return default;
            case GDTaskStatus.Canceled:
                throw new OperationCanceledException(_cancellationToken);
            default:
            case GDTaskStatus.Pending:
                throw new InvalidOperationException("not yet completed.");
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
        return (GDTaskStatus)_intStatus;
    }

    [DebuggerHidden]
    public GDTaskStatus UnsafeGetStatus()
    {
        return (GDTaskStatus)_intStatus;
    }

    [DebuggerHidden]
    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        if (_gate is null)
        {
            Interlocked.CompareExchange(ref _gate, new object(), null);
        }

        var lockGate = Thread.VolatileRead(ref _gate);
        lock (lockGate) // wait TrySignalCompletion, after status is not pending.
        {
            if ((GDTaskStatus)_intStatus is not GDTaskStatus.Pending)
            {
                continuation(state);
                return;
            }

            if (_singleContinuation is null)
            {
                _singleContinuation = continuation;
                _singleState = state;
            }
            else
            {
                _secondaryContinuationList ??= [];
                _secondaryContinuationList.Add((continuation, state));
            }
        }
    }

    [DebuggerHidden]
    private bool TrySignalCompletion(GDTaskStatus status)
    {
        if (Interlocked.CompareExchange(ref _intStatus, (int)status, (int)GDTaskStatus.Pending) == (int)GDTaskStatus.Pending)
        {
            if (_gate is null)
            {
                Interlocked.CompareExchange(ref _gate, new object(), null);
            }

            var lockGate = Thread.VolatileRead(ref _gate);
            lock (lockGate) // wait OnCompleted.
            {
                if (_singleContinuation is not null)
                {
                    try
                    {
                        _singleContinuation(_singleState);
                    }
                    catch (Exception ex)
                    {
                        GDTaskScheduler.PublishUnobservedTaskException(ex);
                    }
                }

                if (_secondaryContinuationList is not null)
                {
                    foreach (var (c, state) in _secondaryContinuationList)
                    {
                        try
                        {
                            c(state);
                        }
                        catch (Exception ex)
                        {
                            GDTaskScheduler.PublishUnobservedTaskException(ex);
                        }
                    }
                }

                _singleContinuation = null;
                _singleState = null;
                _secondaryContinuationList = null;
            }
            return true;
        }
        return false;
    }
}
