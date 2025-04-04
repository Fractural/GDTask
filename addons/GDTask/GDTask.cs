using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Fractural.Tasks.CompilerServices;

namespace Fractural.Tasks;

internal static class AwaiterActions
{
    internal static readonly Action<object> InvokeContinuationDelegate = Continuation;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Continuation(object state)
    {
        ((Action)state).Invoke();
    }
}

/// <summary>
/// Lightweight Godot specific task-like object with a void return value.
/// </summary>
[AsyncMethodBuilder(typeof(AsyncGDTaskMethodBuilder))]
[StructLayout(LayoutKind.Auto)]
public readonly partial struct GDTask
{
    private readonly IGDTaskSource source;
    private readonly short token;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GDTask(IGDTaskSource source, short token)
    {
        this.source = source;
        this.token = token;
    }

    public GDTaskStatus Status
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (source is null)
                return GDTaskStatus.Succeeded;
            return source.GetStatus(token);
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Awaiter GetAwaiter()
    {
        return new Awaiter(this);
    }

    /// <summary>
    /// returns (bool IsCanceled) instead of throws OperationCanceledException.
    /// </summary>
    public GDTask<bool> SuppressCancellationThrow()
    {
        var status = Status;
        if (status is GDTaskStatus.Succeeded)
            return CompletedTasks.False;
        if (status is GDTaskStatus.Canceled)
            return CompletedTasks.True;
        return new GDTask<bool>(new IsCanceledSource(source), token);
    }

    public override string ToString()
    {
        if (source is null)
            return "()";
        return "(" + source.UnsafeGetStatus() + ")";
    }

    /// <summary>
    /// Memoizing inner IValueTaskSource. The result GDTask can await multiple.
    /// </summary>
    public GDTask Preserve()
    {
        if (source is null)
        {
            return this;
        }
        else
        {
            return new GDTask(new MemoizeSource(source), token);
        }
    }

    public GDTask<AsyncUnit> AsAsyncUnitGDTask()
    {
        if (this.source is null)
            return CompletedTasks.AsyncUnit;

        var status = this.source.GetStatus(this.token);
        if (status.IsCompletedSuccessfully())
        {
            this.source.GetResult(this.token);
            return CompletedTasks.AsyncUnit;
        }
        else if (this.source is IGDTaskSource<AsyncUnit> asyncUnitSource)
        {
            return new GDTask<AsyncUnit>(asyncUnitSource, this.token);
        }

        return new GDTask<AsyncUnit>(new AsyncUnitSource(this.source), this.token);
    }

    private sealed class AsyncUnitSource : IGDTaskSource<AsyncUnit>
    {
        private readonly IGDTaskSource _source;

        public AsyncUnitSource(IGDTaskSource source)
        {
            _source = source;
        }

        public AsyncUnit GetResult(short token)
        {
            _source.GetResult(token);
            return AsyncUnit.Default;
        }

        public GDTaskStatus GetStatus(short token)
        {
            return _source.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _source.OnCompleted(continuation, state, token);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _source.UnsafeGetStatus();
        }

        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    private sealed class IsCanceledSource : IGDTaskSource<bool>
    {
        private readonly IGDTaskSource _source;

        public IsCanceledSource(IGDTaskSource source)
        {
            _source = source;
        }

        public bool GetResult(short token)
        {
            if (_source.GetStatus(token) is GDTaskStatus.Canceled)
            {
                return true;
            }

            _source.GetResult(token);
            return false;
        }

        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        public GDTaskStatus GetStatus(short token)
        {
            return _source.GetStatus(token);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _source.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _source.OnCompleted(continuation, state, token);
        }
    }

    private sealed class MemoizeSource : IGDTaskSource
    {
        private IGDTaskSource _source;
        private ExceptionDispatchInfo _exception;
        private GDTaskStatus _status;

        public MemoizeSource(IGDTaskSource source)
        {
            _source = source;
        }

        public void GetResult(short token)
        {
            if (_source is null)
            {
                if (_exception is not null)
                {
                    _exception.Throw();
                }
            }
            else
            {
                try
                {
                    _source.GetResult(token);
                    _status = GDTaskStatus.Succeeded;
                }
                catch (Exception ex)
                {
                    _exception = ExceptionDispatchInfo.Capture(ex);
                    if (ex is OperationCanceledException)
                    {
                        _status = GDTaskStatus.Canceled;
                    }
                    else
                    {
                        _status = GDTaskStatus.Faulted;
                    }
                    throw;
                }
                finally
                {
                    _source = null;
                }
            }
        }

        public GDTaskStatus GetStatus(short token)
        {
            if (_source is null)
            {
                return _status;
            }

            return _source.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (_source is null)
            {
                continuation(state);
            }
            else
            {
                _source.OnCompleted(continuation, state, token);
            }
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            if (_source is null)
            {
                return _status;
            }

            return _source.UnsafeGetStatus();
        }
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly GDTask _task;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter(in GDTask task)
        {
            _task = task;
        }

        public bool IsCompleted
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _task.Status.IsCompleted(); }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult()
        {
            if (_task.source is null)
                return;
            _task.source.GetResult(_task.token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation)
        {
            if (_task.source is null)
            {
                continuation();
            }
            else
            {
                _task.source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, _task.token);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation)
        {
            if (_task.source is null)
            {
                continuation();
            }
            else
            {
                _task.source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, _task.token);
            }
        }

        /// <summary>
        /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SourceOnCompleted(Action<object> continuation, object state)
        {
            if (_task.source is null)
            {
                continuation(state);
            }
            else
            {
                _task.source.OnCompleted(continuation, state, _task.token);
            }
        }
    }
}

/// <summary>
/// Lightweight Godot specified task-like object with a return value.
/// </summary>
/// <typeparam name="T">Return value of the task</typeparam>
[AsyncMethodBuilder(typeof(AsyncGDTaskMethodBuilder<>))]
[StructLayout(LayoutKind.Auto)]
public readonly struct GDTask<T>
{
    private readonly IGDTaskSource<T> source;
    private readonly T result;
    private readonly short token;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GDTask(T result)
    {
        this.source = default;
        this.token = default;
        this.result = result;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GDTask(IGDTaskSource<T> source, short token)
    {
        this.source = source;
        this.token = token;
        this.result = default;
    }

    public GDTaskStatus Status
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (source is null) ? GDTaskStatus.Succeeded : source.GetStatus(token); }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Awaiter GetAwaiter()
    {
        return new Awaiter(this);
    }

    /// <summary>
    /// Memoizing inner IValueTaskSource. The result GDTask can await multiple.
    /// </summary>
    public GDTask<T> Preserve()
    {
        if (source is null)
        {
            return this;
        }
        else
        {
            return new GDTask<T>(new MemoizeSource(source), token);
        }
    }

    public GDTask AsGDTask()
    {
        if (this.source is null)
            return GDTask.CompletedTask;

        var status = this.source.GetStatus(this.token);
        if (status.IsCompletedSuccessfully())
        {
            this.source.GetResult(this.token);
            return GDTask.CompletedTask;
        }

        // Converting GDTask<T> -> GDTask is zero overhead.
        return new GDTask(this.source, this.token);
    }

    public static implicit operator GDTask(GDTask<T> self)
    {
        return self.AsGDTask();
    }

    /// <summary>
    /// returns (bool IsCanceled, T Result) instead of throws OperationCanceledException.
    /// </summary>
    public GDTask<(bool IsCanceled, T Result)> SuppressCancellationThrow()
    {
        if (source is null)
        {
            return new GDTask<(bool IsCanceled, T Result)>((false, result));
        }

        return new GDTask<(bool, T)>(new IsCanceledSource(source), token);
    }

    public override string ToString()
    {
        return (this.source is null) ? result?.ToString() : "(" + this.source.UnsafeGetStatus() + ")";
    }

    private sealed class IsCanceledSource : IGDTaskSource<(bool, T)>
    {
        private readonly IGDTaskSource<T> _source;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IsCanceledSource(IGDTaskSource<T> source)
        {
            _source = source;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (bool, T) GetResult(short token)
        {
            if (_source.GetStatus(token) is GDTaskStatus.Canceled)
            {
                return (true, default);
            }

            var result = _source.GetResult(token);
            return (false, result);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GDTaskStatus GetStatus(short token)
        {
            return _source.GetStatus(token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GDTaskStatus UnsafeGetStatus()
        {
            return _source.UnsafeGetStatus();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _source.OnCompleted(continuation, state, token);
        }
    }

    private sealed class MemoizeSource : IGDTaskSource<T>
    {
        private IGDTaskSource<T> _source;
        private T _result;
        private ExceptionDispatchInfo _exception;
        private GDTaskStatus _status;

        public MemoizeSource(IGDTaskSource<T> source)
        {
            _source = source;
        }

        public T GetResult(short token)
        {
            if (_source is null)
            {
                if (_exception is not null)
                {
                    _exception.Throw();
                }
                return _result;
            }
            else
            {
                try
                {
                    _result = _source.GetResult(token);
                    _status = GDTaskStatus.Succeeded;
                    return _result;
                }
                catch (Exception ex)
                {
                    _exception = ExceptionDispatchInfo.Capture(ex);
                    if (ex is OperationCanceledException)
                    {
                        _status = GDTaskStatus.Canceled;
                    }
                    else
                    {
                        _status = GDTaskStatus.Faulted;
                    }
                    throw;
                }
                finally
                {
                    _source = null;
                }
            }
        }

        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        public GDTaskStatus GetStatus(short token)
        {
            if (_source is null)
            {
                return _status;
            }

            return _source.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (_source is null)
            {
                continuation(state);
            }
            else
            {
                _source.OnCompleted(continuation, state, token);
            }
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            if (_source is null)
            {
                return _status;
            }

            return _source.UnsafeGetStatus();
        }
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly GDTask<T> _task;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter(in GDTask<T> task)
        {
            _task = task;
        }

        public bool IsCompleted
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _task.Status.IsCompleted(); }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult()
        {
            var s = _task.source;
            if (s is null)
            {
                return _task.result;
            }
            else
            {
                return s.GetResult(_task.token);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation)
        {
            var s = _task.source;
            if (s is null)
            {
                continuation();
            }
            else
            {
                s.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, _task.token);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation)
        {
            var s = _task.source;
            if (s is null)
            {
                continuation();
            }
            else
            {
                s.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, _task.token);
            }
        }

        /// <summary>
        /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SourceOnCompleted(Action<object> continuation, object state)
        {
            var s = _task.source;
            if (s is null)
            {
                continuation(state);
            }
            else
            {
                s.OnCompleted(continuation, state, _task.token);
            }
        }
    }
}
