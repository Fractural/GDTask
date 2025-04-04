using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Fractural.Tasks;

public static class CancellationTokenExtensions
{
    private static readonly Action<object> cancellationTokenCallback = Callback;
    private static readonly Action<object> disposeCallback = DisposeCallback;

    public static CancellationToken ToCancellationToken(this GDTask task)
    {
        var cts = new CancellationTokenSource();
        ToCancellationTokenCore(task, cts).Forget();
        return cts.Token;
    }

    public static CancellationToken ToCancellationToken(this GDTask task, CancellationToken linkToken)
    {
        if (linkToken.IsCancellationRequested)
        {
            return linkToken;
        }

        if (!linkToken.CanBeCanceled)
        {
            return ToCancellationToken(task);
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(linkToken);
        ToCancellationTokenCore(task, cts).Forget();

        return cts.Token;
    }

    public static CancellationToken ToCancellationToken<T>(this GDTask<T> task)
    {
        return ToCancellationToken(task.AsGDTask());
    }

    public static CancellationToken ToCancellationToken<T>(this GDTask<T> task, CancellationToken linkToken)
    {
        return ToCancellationToken(task.AsGDTask(), linkToken);
    }

    private static async GDTaskVoid ToCancellationTokenCore(GDTask task, CancellationTokenSource cts)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            GDTaskScheduler.PublishUnobservedTaskException(ex);
        }
        cts.Cancel();
        cts.Dispose();
    }

    public static (GDTask, CancellationTokenRegistration) ToGDTask(this CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return (GDTask.FromCanceled(cancellationToken), default(CancellationTokenRegistration));
        }

        var promise = new GDTaskCompletionSource();
        return (promise.Task, cancellationToken.RegisterWithoutCaptureExecutionContext(cancellationTokenCallback, promise));
    }

    private static void Callback(object state)
    {
        var promise = (GDTaskCompletionSource)state;
        promise.TrySetResult();
    }

    public static CancellationTokenAwaitable WaitUntilCanceled(this CancellationToken cancellationToken)
    {
        return new CancellationTokenAwaitable(cancellationToken);
    }

    public static CancellationTokenRegistration RegisterWithoutCaptureExecutionContext(this CancellationToken cancellationToken, Action callback)
    {
        var restoreFlow = false;
        if (!ExecutionContext.IsFlowSuppressed())
        {
            ExecutionContext.SuppressFlow();
            restoreFlow = true;
        }

        try
        {
            return cancellationToken.Register(callback, false);
        }
        finally
        {
            if (restoreFlow)
            {
                ExecutionContext.RestoreFlow();
            }
        }
    }

    public static CancellationTokenRegistration RegisterWithoutCaptureExecutionContext(
        this CancellationToken cancellationToken,
        Action<object> callback,
        object state
    )
    {
        var restoreFlow = false;
        if (!ExecutionContext.IsFlowSuppressed())
        {
            ExecutionContext.SuppressFlow();
            restoreFlow = true;
        }

        try
        {
            return cancellationToken.Register(callback, state, false);
        }
        finally
        {
            if (restoreFlow)
            {
                ExecutionContext.RestoreFlow();
            }
        }
    }

    public static CancellationTokenRegistration AddTo(this IDisposable disposable, CancellationToken cancellationToken)
    {
        return cancellationToken.RegisterWithoutCaptureExecutionContext(disposeCallback, disposable);
    }

    private static void DisposeCallback(object state)
    {
        var d = (IDisposable)state;
        d.Dispose();
    }
}

public struct CancellationTokenAwaitable
{
    private CancellationToken _cancellationToken;

    public CancellationTokenAwaitable(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    public Awaiter GetAwaiter()
    {
        return new Awaiter(_cancellationToken);
    }

    public struct Awaiter : ICriticalNotifyCompletion
    {
        private CancellationToken _cancellationToken;

        public Awaiter(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public bool IsCompleted => !_cancellationToken.CanBeCanceled || _cancellationToken.IsCancellationRequested;

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            UnsafeOnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _cancellationToken.RegisterWithoutCaptureExecutionContext(continuation);
        }
    }
}
