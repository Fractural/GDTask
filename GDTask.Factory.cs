using Fractural.Tasks.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Fractural.Tasks
{
    public partial struct GDTask
    {
        static readonly GDTask CanceledGDTask = new Func<GDTask>(() =>
        {
            return new GDTask(new CanceledResultSource(CancellationToken.None), 0);
        })();

        static class CanceledGDTaskCache<T>
        {
            public static readonly GDTask<T> Task;

            static CanceledGDTaskCache()
            {
                Task = new GDTask<T>(new CanceledResultSource<T>(CancellationToken.None), 0);
            }
        }

        public static readonly GDTask CompletedTask = new GDTask();

        public static GDTask FromException(Exception ex)
        {
            if (ex is OperationCanceledException oce)
            {
                return FromCanceled(oce.CancellationToken);
            }

            return new GDTask(new ExceptionResultSource(ex), 0);
        }

        public static GDTask<T> FromException<T>(Exception ex)
        {
            if (ex is OperationCanceledException oce)
            {
                return FromCanceled<T>(oce.CancellationToken);
            }

            return new GDTask<T>(new ExceptionResultSource<T>(ex), 0);
        }

        public static GDTask<T> FromResult<T>(T value)
        {
            return new GDTask<T>(value);
        }

        public static GDTask FromCanceled(CancellationToken cancellationToken = default)
        {
            if (cancellationToken == CancellationToken.None)
            {
                return CanceledGDTask;
            }
            else
            {
                return new GDTask(new CanceledResultSource(cancellationToken), 0);
            }
        }

        public static GDTask<T> FromCanceled<T>(CancellationToken cancellationToken = default)
        {
            if (cancellationToken == CancellationToken.None)
            {
                return CanceledGDTaskCache<T>.Task;
            }
            else
            {
                return new GDTask<T>(new CanceledResultSource<T>(cancellationToken), 0);
            }
        }

        public static GDTask Create(Func<GDTask> factory)
        {
            return factory();
        }

        public static GDTask<T> Create<T>(Func<GDTask<T>> factory)
        {
            return factory();
        }

        public static AsyncLazy Lazy(Func<GDTask> factory)
        {
            return new AsyncLazy(factory);
        }

        public static AsyncLazy<T> Lazy<T>(Func<GDTask<T>> factory)
        {
            return new AsyncLazy<T>(factory);
        }

        /// <summary>
        /// helper of fire and forget void action.
        /// </summary>
        public static void Void(Func<GDTaskVoid> asyncAction)
        {
            asyncAction().Forget();
        }

        /// <summary>
        /// helper of fire and forget void action.
        /// </summary>
        public static void Void(Func<CancellationToken, GDTaskVoid> asyncAction, CancellationToken cancellationToken)
        {
            asyncAction(cancellationToken).Forget();
        }

        /// <summary>
        /// helper of fire and forget void action.
        /// </summary>
        public static void Void<T>(Func<T, GDTaskVoid> asyncAction, T state)
        {
            asyncAction(state).Forget();
        }

        /// <summary>
        /// helper of create add GDTaskVoid to delegate.
        /// For example: FooAction = GDTask.Action(async () => { /* */ })
        /// </summary>
        public static Action Action(Func<GDTaskVoid> asyncAction)
        {
            return () => asyncAction().Forget();
        }

        /// <summary>
        /// helper of create add GDTaskVoid to delegate.
        /// </summary>
        public static Action Action(Func<CancellationToken, GDTaskVoid> asyncAction, CancellationToken cancellationToken)
        {
            return () => asyncAction(cancellationToken).Forget();
        }

        /// <summary>
        /// Defer the task creation just before call await.
        /// </summary>
        public static GDTask Defer(Func<GDTask> factory)
        {
            return new GDTask(new DeferPromise(factory), 0);
        }

        /// <summary>
        /// Defer the task creation just before call await.
        /// </summary>
        public static GDTask<T> Defer<T>(Func<GDTask<T>> factory)
        {
            return new GDTask<T>(new DeferPromise<T>(factory), 0);
        }

        /// <summary>
        /// Never complete.
        /// </summary>
        public static GDTask Never(CancellationToken cancellationToken = default)
        {
            return new GDTask<AsyncUnit>(new NeverPromise<AsyncUnit>(cancellationToken), 0);
        }

        /// <summary>
        /// Never complete.
        /// </summary>
        public static GDTask<T> Never<T>(CancellationToken cancellationToken = default)
        {
            return new GDTask<T>(new NeverPromise<T>(cancellationToken), 0);
        }

        sealed class ExceptionResultSource : IGDTaskSource
        {
            readonly ExceptionDispatchInfo exception;
            bool calledGet;

            public ExceptionResultSource(Exception exception)
            {
                this.exception = ExceptionDispatchInfo.Capture(exception);
            }

            public void GetResult(short token)
            {
                if (!calledGet)
                {
                    calledGet = true;
                    GC.SuppressFinalize(this);
                }
                exception.Throw();
            }

            public GDTaskStatus GetStatus(short token)
            {
                return GDTaskStatus.Faulted;
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return GDTaskStatus.Faulted;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                continuation(state);
            }

            ~ExceptionResultSource()
            {
                if (!calledGet)
                {
                    GDTaskScheduler.PublishUnobservedTaskException(exception.SourceException);
                }
            }
        }

        sealed class ExceptionResultSource<T> : IGDTaskSource<T>
        {
            readonly ExceptionDispatchInfo exception;
            bool calledGet;

            public ExceptionResultSource(Exception exception)
            {
                this.exception = ExceptionDispatchInfo.Capture(exception);
            }

            public T GetResult(short token)
            {
                if (!calledGet)
                {
                    calledGet = true;
                    GC.SuppressFinalize(this);
                }
                exception.Throw();
                return default;
            }

            void IGDTaskSource.GetResult(short token)
            {
                if (!calledGet)
                {
                    calledGet = true;
                    GC.SuppressFinalize(this);
                }
                exception.Throw();
            }

            public GDTaskStatus GetStatus(short token)
            {
                return GDTaskStatus.Faulted;
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return GDTaskStatus.Faulted;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                continuation(state);
            }

            ~ExceptionResultSource()
            {
                if (!calledGet)
                {
                    GDTaskScheduler.PublishUnobservedTaskException(exception.SourceException);
                }
            }
        }

        sealed class CanceledResultSource : IGDTaskSource
        {
            readonly CancellationToken cancellationToken;

            public CanceledResultSource(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public void GetResult(short token)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return GDTaskStatus.Canceled;
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return GDTaskStatus.Canceled;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                continuation(state);
            }
        }

        sealed class CanceledResultSource<T> : IGDTaskSource<T>
        {
            readonly CancellationToken cancellationToken;

            public CanceledResultSource(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public T GetResult(short token)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            void IGDTaskSource.GetResult(short token)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return GDTaskStatus.Canceled;
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return GDTaskStatus.Canceled;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                continuation(state);
            }
        }

        sealed class DeferPromise : IGDTaskSource
        {
            Func<GDTask> factory;
            GDTask task;
            GDTask.Awaiter awaiter;

            public DeferPromise(Func<GDTask> factory)
            {
                this.factory = factory;
            }

            public void GetResult(short token)
            {
                awaiter.GetResult();
            }

            public GDTaskStatus GetStatus(short token)
            {
                var f = Interlocked.Exchange(ref factory, null);
                if (f != null)
                {
                    task = f();
                    awaiter = task.GetAwaiter();
                }

                return task.Status;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                awaiter.SourceOnCompleted(continuation, state);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return task.Status;
            }
        }

        sealed class DeferPromise<T> : IGDTaskSource<T>
        {
            Func<GDTask<T>> factory;
            GDTask<T> task;
            GDTask<T>.Awaiter awaiter;

            public DeferPromise(Func<GDTask<T>> factory)
            {
                this.factory = factory;
            }

            public T GetResult(short token)
            {
                return awaiter.GetResult();
            }

            void IGDTaskSource.GetResult(short token)
            {
                awaiter.GetResult();
            }

            public GDTaskStatus GetStatus(short token)
            {
                var f = Interlocked.Exchange(ref factory, null);
                if (f != null)
                {
                    task = f();
                    awaiter = task.GetAwaiter();
                }

                return task.Status;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                awaiter.SourceOnCompleted(continuation, state);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return task.Status;
            }
        }

        sealed class NeverPromise<T> : IGDTaskSource<T>
        {
            static readonly Action<object> cancellationCallback = CancellationCallback;

            CancellationToken cancellationToken;
            GDTaskCompletionSourceCore<T> core;

            public NeverPromise(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
                if (this.cancellationToken.CanBeCanceled)
                {
                    this.cancellationToken.RegisterWithoutCaptureExecutionContext(cancellationCallback, this);
                }
            }

            static void CancellationCallback(object state)
            {
                var self = (NeverPromise<T>)state;
                self.core.TrySetCanceled(self.cancellationToken);
            }

            public T GetResult(short token)
            {
                return core.GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }

            void IGDTaskSource.GetResult(short token)
            {
                core.GetResult(token);
            }
        }
    }

    internal static class CompletedTasks
    {
        public static readonly GDTask<AsyncUnit> AsyncUnit = GDTask.FromResult(Fractural.Tasks.AsyncUnit.Default);
        public static readonly GDTask<bool> True = GDTask.FromResult(true);
        public static readonly GDTask<bool> False = GDTask.FromResult(false);
        public static readonly GDTask<int> Zero = GDTask.FromResult(0);
        public static readonly GDTask<int> MinusOne = GDTask.FromResult(-1);
        public static readonly GDTask<int> One = GDTask.FromResult(1);
    }
}
