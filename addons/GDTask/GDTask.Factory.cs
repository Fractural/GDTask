using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Fractural.Tasks;

public partial struct GDTask
{
    private static readonly GDTask CanceledGDTask = new Func<GDTask>(() =>
    {
        return new GDTask(new CanceledResultSource(CancellationToken.None), 0);
    })();

    private static class CanceledGDTaskCache<T>
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
    public static GDTask Never(CancellationToken cancellationToken)
    {
        return new GDTask<AsyncUnit>(new NeverPromise<AsyncUnit>(cancellationToken), 0);
    }

    /// <summary>
    /// Never complete.
    /// </summary>
    public static GDTask<T> Never<T>(CancellationToken cancellationToken)
    {
        return new GDTask<T>(new NeverPromise<T>(cancellationToken), 0);
    }

    private sealed class ExceptionResultSource : IGDTaskSource
    {
        private readonly ExceptionDispatchInfo _exception;
        private bool _calledGet;

        public ExceptionResultSource(Exception exception)
        {
            _exception = ExceptionDispatchInfo.Capture(exception);
        }

        public void GetResult(short token)
        {
            if (!_calledGet)
            {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }
            _exception.Throw();
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
            if (!_calledGet)
            {
                GDTaskScheduler.PublishUnobservedTaskException(_exception.SourceException);
            }
        }
    }

    private sealed class ExceptionResultSource<T> : IGDTaskSource<T>
    {
        private readonly ExceptionDispatchInfo _exception;
        private bool _calledGet;

        public ExceptionResultSource(Exception exception)
        {
            _exception = ExceptionDispatchInfo.Capture(exception);
        }

        public T GetResult(short token)
        {
            if (!_calledGet)
            {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }
            _exception.Throw();
            return default;
        }

        void IGDTaskSource.GetResult(short token)
        {
            if (!_calledGet)
            {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }
            _exception.Throw();
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
            if (!_calledGet)
            {
                GDTaskScheduler.PublishUnobservedTaskException(_exception.SourceException);
            }
        }
    }

    private sealed class CanceledResultSource : IGDTaskSource
    {
        private readonly CancellationToken _cancellationToken;

        public CanceledResultSource(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public void GetResult(short token)
        {
            throw new OperationCanceledException(_cancellationToken);
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

    private sealed class CanceledResultSource<T> : IGDTaskSource<T>
    {
        private readonly CancellationToken _cancellationToken;

        public CanceledResultSource(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public T GetResult(short token)
        {
            throw new OperationCanceledException(_cancellationToken);
        }

        void IGDTaskSource.GetResult(short token)
        {
            throw new OperationCanceledException(_cancellationToken);
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

    private sealed class DeferPromise : IGDTaskSource
    {
        private Func<GDTask> _factory;
        private GDTask _task;
        private GDTask.Awaiter _awaiter;

        public DeferPromise(Func<GDTask> factory)
        {
            _factory = factory;
        }

        public void GetResult(short token)
        {
            _awaiter.GetResult();
        }

        public GDTaskStatus GetStatus(short token)
        {
            var f = Interlocked.Exchange(ref _factory, null);
            if (f is not null)
            {
                _task = f();
                _awaiter = _task.GetAwaiter();
            }

            return _task.Status;
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _awaiter.SourceOnCompleted(continuation, state);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _task.Status;
        }
    }

    private sealed class DeferPromise<T> : IGDTaskSource<T>
    {
        private Func<GDTask<T>> _factory;
        private GDTask<T> _task;
        private GDTask<T>.Awaiter _awaiter;

        public DeferPromise(Func<GDTask<T>> factory)
        {
            _factory = factory;
        }

        public T GetResult(short token)
        {
            return _awaiter.GetResult();
        }

        void IGDTaskSource.GetResult(short token)
        {
            _awaiter.GetResult();
        }

        public GDTaskStatus GetStatus(short token)
        {
            var f = Interlocked.Exchange(ref _factory, null);
            if (f is not null)
            {
                _task = f();
                _awaiter = _task.GetAwaiter();
            }

            return _task.Status;
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _awaiter.SourceOnCompleted(continuation, state);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _task.Status;
        }
    }

    private sealed class NeverPromise<T> : IGDTaskSource<T>
    {
        private static readonly Action<object> _cancellationCallback = CancellationCallback;

        private CancellationToken _cancellationToken;
        private GDTaskCompletionSourceCore<T> _core;

        public NeverPromise(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            if (_cancellationToken.CanBeCanceled)
            {
                _cancellationToken.RegisterWithoutCaptureExecutionContext(_cancellationCallback, this);
            }
        }

        private static void CancellationCallback(object state)
        {
            var self = (NeverPromise<T>)state;
            self._core.TrySetCanceled(self._cancellationToken);
        }

        public T GetResult(short token)
        {
            return _core.GetResult(token);
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

        void IGDTaskSource.GetResult(short token)
        {
            _core.GetResult(token);
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
