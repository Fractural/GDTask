using System;
using System.Runtime.CompilerServices;

namespace Fractural.Tasks.Internal;

internal sealed class PooledDelegate<T> : ITaskPoolNode<PooledDelegate<T>>
{
    private static TaskPool<PooledDelegate<T>> _pool;

    private readonly Action<T> _runDelegate;
    private Action _continuation;

    private PooledDelegate<T> _nextNode;
    public ref PooledDelegate<T> NextNode => ref _nextNode;

    static PooledDelegate()
    {
        TaskPool.RegisterSizeGetter(typeof(PooledDelegate<T>), () => _pool.Size);
    }

    private PooledDelegate()
    {
        _runDelegate = Run;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action<T> Create(Action continuation)
    {
        if (!_pool.TryPop(out var item))
        {
            item = new PooledDelegate<T>();
        }

        item._continuation = continuation;
        return item._runDelegate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Run(T _)
    {
        var call = _continuation;
        _continuation = null;
        if (call is not null)
        {
            _pool.TryPush(this);
            call.Invoke();
        }
    }
}
