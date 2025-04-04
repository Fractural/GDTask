using System;
using System.Threading;

namespace Fractural.Tasks;

public partial class AsyncLazy
{
    private static Action<object> continuation = SetCompletionSource;

    private Func<GDTask> _taskFactory;
    private GDTaskCompletionSource _completionSource;
    private GDTask.Awaiter _awaiter;

    private object _syncLock;
    private bool _initialized;

    public AsyncLazy(Func<GDTask> taskFactory)
    {
        _taskFactory = taskFactory;
        _completionSource = new GDTaskCompletionSource();
        _syncLock = new object();
        _initialized = false;
    }

    internal AsyncLazy(GDTask task)
    {
        _taskFactory = null;
        _completionSource = new GDTaskCompletionSource();
        _syncLock = null;
        _initialized = true;

        var awaiter = task.GetAwaiter();
        if (awaiter.IsCompleted)
        {
            SetCompletionSource(awaiter);
        }
        else
        {
            _awaiter = awaiter;
            awaiter.SourceOnCompleted(continuation, this);
        }
    }

    public GDTask Task
    {
        get
        {
            EnsureInitialized();
            return _completionSource.Task;
        }
    }

    public GDTask.Awaiter GetAwaiter() => Task.GetAwaiter();

    private void EnsureInitialized()
    {
        if (Volatile.Read(ref _initialized))
        {
            return;
        }

        EnsureInitializedCore();
    }

    private void EnsureInitializedCore()
    {
        lock (_syncLock)
        {
            if (!Volatile.Read(ref _initialized))
            {
                var f = Interlocked.Exchange(ref _taskFactory, null);
                if (f is not null)
                {
                    var task = f();
                    var awaiter = task.GetAwaiter();
                    if (awaiter.IsCompleted)
                    {
                        SetCompletionSource(awaiter);
                    }
                    else
                    {
                        _awaiter = awaiter;
                        awaiter.SourceOnCompleted(continuation, this);
                    }

                    Volatile.Write(ref _initialized, true);
                }
            }
        }
    }

    private void SetCompletionSource(in GDTask.Awaiter awaiter)
    {
        try
        {
            awaiter.GetResult();
            _completionSource.TrySetResult();
        }
        catch (Exception ex)
        {
            _completionSource.TrySetException(ex);
        }
    }

    private static void SetCompletionSource(object state)
    {
        var self = (AsyncLazy)state;
        try
        {
            self._awaiter.GetResult();
            self._completionSource.TrySetResult();
        }
        catch (Exception ex)
        {
            self._completionSource.TrySetException(ex);
        }
        finally
        {
            self._awaiter = default;
        }
    }
}

public partial class AsyncLazy<T>
{
    private static Action<object> _continuation = SetCompletionSource;

    private Func<GDTask<T>> _taskFactory;
    private GDTaskCompletionSource<T> _completionSource;
    private GDTask<T>.Awaiter _awaiter;

    private object _syncLock;
    private bool _initialized;

    public AsyncLazy(Func<GDTask<T>> taskFactory)
    {
        _taskFactory = taskFactory;
        _completionSource = new GDTaskCompletionSource<T>();
        _syncLock = new object();
        _initialized = false;
    }

    internal AsyncLazy(GDTask<T> task)
    {
        _taskFactory = null;
        _completionSource = new GDTaskCompletionSource<T>();
        _syncLock = null;
        _initialized = true;

        var awaiter = task.GetAwaiter();
        if (awaiter.IsCompleted)
        {
            SetCompletionSource(awaiter);
        }
        else
        {
            _awaiter = awaiter;
            awaiter.SourceOnCompleted(_continuation, this);
        }
    }

    public GDTask<T> Task
    {
        get
        {
            EnsureInitialized();
            return _completionSource.Task;
        }
    }

    public GDTask<T>.Awaiter GetAwaiter() => Task.GetAwaiter();

    private void EnsureInitialized()
    {
        if (Volatile.Read(ref _initialized))
        {
            return;
        }

        EnsureInitializedCore();
    }

    private void EnsureInitializedCore()
    {
        lock (_syncLock)
        {
            if (!Volatile.Read(ref _initialized))
            {
                var f = Interlocked.Exchange(ref _taskFactory, null);
                if (f is not null)
                {
                    var task = f();
                    var awaiter = task.GetAwaiter();
                    if (awaiter.IsCompleted)
                    {
                        SetCompletionSource(awaiter);
                    }
                    else
                    {
                        _awaiter = awaiter;
                        awaiter.SourceOnCompleted(_continuation, this);
                    }

                    Volatile.Write(ref _initialized, true);
                }
            }
        }
    }

    private void SetCompletionSource(in GDTask<T>.Awaiter awaiter)
    {
        try
        {
            var result = awaiter.GetResult();
            _completionSource.TrySetResult(result);
        }
        catch (Exception ex)
        {
            _completionSource.TrySetException(ex);
        }
    }

    private static void SetCompletionSource(object state)
    {
        var self = (AsyncLazy<T>)state;
        try
        {
            var result = self._awaiter.GetResult();
            self._completionSource.TrySetResult(result);
        }
        catch (Exception ex)
        {
            self._completionSource.TrySetException(ex);
        }
        finally
        {
            self._awaiter = default;
        }
    }
}
