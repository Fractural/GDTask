using GDTask.CompilerServices;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace GDTask
{
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
        readonly IGDTaskSource source;
        readonly short token;

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
                if (source == null) return GDTaskStatus.Succeeded;
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
            if (status == GDTaskStatus.Succeeded) return CompletedTasks.False;
            if (status == GDTaskStatus.Canceled) return CompletedTasks.True;
            return new GDTask<bool>(new IsCanceledSource(source), token);
        }
        public override string ToString()
        {
            if (source == null) return "()";
            return "(" + source.UnsafeGetStatus() + ")";
        }

        /// <summary>
        /// Memoizing inner IValueTaskSource. The result GDTask can await multiple.
        /// </summary>
        public GDTask Preserve()
        {
            if (source == null)
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
            if (this.source == null) return CompletedTasks.AsyncUnit;

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

        sealed class AsyncUnitSource : IGDTaskSource<AsyncUnit>
        {
            readonly IGDTaskSource source;

            public AsyncUnitSource(IGDTaskSource source)
            {
                this.source = source;
            }

            public AsyncUnit GetResult(short token)
            {
                source.GetResult(token);
                return AsyncUnit.Default;
            }

            public GDTaskStatus GetStatus(short token)
            {
                return source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                source.OnCompleted(continuation, state, token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return source.UnsafeGetStatus();
            }

            void IGDTaskSource.GetResult(short token)
            {
                GetResult(token);
            }
        }

        sealed class IsCanceledSource : IGDTaskSource<bool>
        {
            readonly IGDTaskSource source;

            public IsCanceledSource(IGDTaskSource source)
            {
                this.source = source;
            }

            public bool GetResult(short token)
            {
                if (source.GetStatus(token) == GDTaskStatus.Canceled)
                {
                    return true;
                }

                source.GetResult(token);
                return false;
            }

            void IGDTaskSource.GetResult(short token)
            {
                GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return source.GetStatus(token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return source.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                source.OnCompleted(continuation, state, token);
            }
        }

        sealed class MemoizeSource : IGDTaskSource
        {
            IGDTaskSource source;
            ExceptionDispatchInfo exception;
            GDTaskStatus status;

            public MemoizeSource(IGDTaskSource source)
            {
                this.source = source;
            }

            public void GetResult(short token)
            {
                if (source == null)
                {
                    if (exception != null)
                    {
                        exception.Throw();
                    }
                }
                else
                {
                    try
                    {
                        source.GetResult(token);
                        status = GDTaskStatus.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        exception = ExceptionDispatchInfo.Capture(ex);
                        if (ex is OperationCanceledException)
                        {
                            status = GDTaskStatus.Canceled;
                        }
                        else
                        {
                            status = GDTaskStatus.Faulted;
                        }
                        throw;
                    }
                    finally
                    {
                        source = null;
                    }
                }
            }

            public GDTaskStatus GetStatus(short token)
            {
                if (source == null)
                {
                    return status;
                }

                return source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                if (source == null)
                {
                    continuation(state);
                }
                else
                {
                    source.OnCompleted(continuation, state, token);
                }
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                if (source == null)
                {
                    return status;
                }

                return source.UnsafeGetStatus();
            }
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            readonly GDTask task;

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Awaiter(in GDTask task)
            {
                this.task = task;
            }

            public bool IsCompleted
            {
                [DebuggerHidden]
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return task.Status.IsCompleted();
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult()
            {
                if (task.source == null) return;
                task.source.GetResult(task.token);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                if (task.source == null)
                {
                    continuation();
                }
                else
                {
                    task.source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, task.token);
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                if (task.source == null)
                {
                    continuation();
                }
                else
                {
                    task.source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, task.token);
                }
            }

            /// <summary>
            /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SourceOnCompleted(Action<object> continuation, object state)
            {
                if (task.source == null)
                {
                    continuation(state);
                }
                else
                {
                    task.source.OnCompleted(continuation, state, task.token);
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
        readonly IGDTaskSource<T> source;
        readonly T result;
        readonly short token;

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
            get
            {
                return (source == null) ? GDTaskStatus.Succeeded : source.GetStatus(token);
            }
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
            if (source == null)
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
            if (this.source == null) return GDTask.CompletedTask;

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
            if (source == null)
            {
                return new GDTask<(bool IsCanceled, T Result)>((false, result));
            }

            return new GDTask<(bool, T)>(new IsCanceledSource(source), token);
        }

        public override string ToString()
        {
            return (this.source == null) ? result?.ToString()
                 : "(" + this.source.UnsafeGetStatus() + ")";
        }

        sealed class IsCanceledSource : IGDTaskSource<(bool, T)>
        {
            readonly IGDTaskSource<T> source;

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IsCanceledSource(IGDTaskSource<T> source)
            {
                this.source = source;
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public (bool, T) GetResult(short token)
            {
                if (source.GetStatus(token) == GDTaskStatus.Canceled)
                {
                    return (true, default);
                }

                var result = source.GetResult(token);
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
                return source.GetStatus(token);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public GDTaskStatus UnsafeGetStatus()
            {
                return source.UnsafeGetStatus();
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                source.OnCompleted(continuation, state, token);
            }
        }

        sealed class MemoizeSource : IGDTaskSource<T>
        {
            IGDTaskSource<T> source;
            T result;
            ExceptionDispatchInfo exception;
            GDTaskStatus status;

            public MemoizeSource(IGDTaskSource<T> source)
            {
                this.source = source;
            }

            public T GetResult(short token)
            {
                if (source == null)
                {
                    if (exception != null)
                    {
                        exception.Throw();
                    }
                    return result;
                }
                else
                {
                    try
                    {
                        result = source.GetResult(token);
                        status = GDTaskStatus.Succeeded;
                        return result;
                    }
                    catch (Exception ex)
                    {
                        exception = ExceptionDispatchInfo.Capture(ex);
                        if (ex is OperationCanceledException)
                        {
                            status = GDTaskStatus.Canceled;
                        }
                        else
                        {
                            status = GDTaskStatus.Faulted;
                        }
                        throw;
                    }
                    finally
                    {
                        source = null;
                    }
                }
            }

            void IGDTaskSource.GetResult(short token)
            {
                GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                if (source == null)
                {
                    return status;
                }

                return source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                if (source == null)
                {
                    continuation(state);
                }
                else
                {
                    source.OnCompleted(continuation, state, token);
                }
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                if (source == null)
                {
                    return status;
                }

                return source.UnsafeGetStatus();
            }
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            readonly GDTask<T> task;

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Awaiter(in GDTask<T> task)
            {
                this.task = task;
            }

            public bool IsCompleted
            {
                [DebuggerHidden]
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return task.Status.IsCompleted();
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T GetResult()
            {
                var s = task.source;
                if (s == null)
                {
                    return task.result;
                }
                else
                {
                    return s.GetResult(task.token);
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                var s = task.source;
                if (s == null)
                {
                    continuation();
                }
                else
                {
                    s.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, task.token);
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                var s = task.source;
                if (s == null)
                {
                    continuation();
                }
                else
                {
                    s.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, task.token);
                }
            }

            /// <summary>
            /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SourceOnCompleted(Action<object> continuation, object state)
            {
                var s = task.source;
                if (s == null)
                {
                    continuation(state);
                }
                else
                {
                    s.OnCompleted(continuation, state, task.token);
                }
            }
        }
    }
}