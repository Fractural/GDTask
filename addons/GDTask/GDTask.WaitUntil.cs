using System;
using System.Collections.Generic;
using System.Threading;
using Fractural.Tasks.Internal;
using Godot;

namespace Fractural.Tasks;

public partial struct GDTask
{
    public static GDTask WaitUntil(
        GodotObject target,
        Func<bool> predicate,
        PlayerLoopTiming timing = PlayerLoopTiming.Process,
        CancellationToken cancellationToken = default
    )
    {
        return new GDTask(WaitUntilPromise.Create(target, predicate, timing, cancellationToken, out var token), token);
    }

    public static GDTask WaitUntil(
        Func<bool> predicate,
        PlayerLoopTiming timing = PlayerLoopTiming.Process,
        CancellationToken cancellationToken = default
    )
    {
        return WaitUntil(null, predicate, timing, cancellationToken);
    }

    public static GDTask WaitWhile(
        GodotObject target,
        Func<bool> predicate,
        PlayerLoopTiming timing = PlayerLoopTiming.Process,
        CancellationToken cancellationToken = default
    )
    {
        return new GDTask(WaitWhilePromise.Create(target, predicate, timing, cancellationToken, out var token), token);
    }

    public static GDTask WaitWhile(
        Func<bool> predicate,
        PlayerLoopTiming timing = PlayerLoopTiming.Process,
        CancellationToken cancellationToken = default
    )
    {
        return WaitWhile(null, predicate, timing, cancellationToken);
    }

    public static GDTask WaitUntilCanceled(
        GodotObject target,
        CancellationToken cancellationToken,
        PlayerLoopTiming timing = PlayerLoopTiming.Process
    )
    {
        return new GDTask(WaitUntilCanceledPromise.Create(target, cancellationToken, timing, out var token), token);
    }

    public static GDTask WaitUntilCanceled(CancellationToken cancellationToken, PlayerLoopTiming timing = PlayerLoopTiming.Process)
    {
        return WaitUntilCanceled(null, cancellationToken, timing);
    }

    public static GDTask<U> WaitUntilValueChanged<T, U>(
        T target,
        Func<T, U> monitorFunction,
        PlayerLoopTiming monitorTiming = PlayerLoopTiming.Process,
        IEqualityComparer<U> equalityComparer = null,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return new GDTask<U>(
            target is GodotObject
                ? WaitUntilValueChangedGodotObjectPromise<T, U>.Create(
                    target,
                    monitorFunction,
                    equalityComparer,
                    monitorTiming,
                    cancellationToken,
                    out var token
                )
                : WaitUntilValueChangedStandardObjectPromise<T, U>.Create(
                    target,
                    monitorFunction,
                    equalityComparer,
                    monitorTiming,
                    cancellationToken,
                    out token
                ),
            token
        );
    }

    private sealed class WaitUntilPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilPromise>
    {
        private static TaskPool<WaitUntilPromise> _pool;
        private WaitUntilPromise _nextNode;

        private GodotObject _target;
        private Func<bool> _predicate;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<object> _core;

        public ref WaitUntilPromise NextNode => ref _nextNode;

        static WaitUntilPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitUntilPromise), () => _pool.Size);
        }

        private WaitUntilPromise() { }

        public static IGDTaskSource Create(
            GodotObject target,
            Func<bool> predicate,
            PlayerLoopTiming timing,
            CancellationToken cancellationToken,
            out short token
        )
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!_pool.TryPop(out var result))
            {
                result = new WaitUntilPromise();
            }

            result._target = target;
            result._predicate = predicate;
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
            if (_cancellationToken.IsCancellationRequested || (_target is not null && !GodotObject.IsInstanceValid(_target))) // Cancel when destroyed
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            try
            {
                if (!_predicate())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _core.TrySetException(ex);
                return false;
            }

            _core.TrySetResult(null);
            return false;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _predicate = default;
            _cancellationToken = default;
            return _pool.TryPush(this);
        }
    }

    private sealed class WaitWhilePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitWhilePromise>
    {
        private static TaskPool<WaitWhilePromise> _pool;
        private WaitWhilePromise _nextNode;

        private GodotObject _target;
        private Func<bool> _predicate;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<object> _core;

        public ref WaitWhilePromise NextNode => ref _nextNode;

        static WaitWhilePromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitWhilePromise), () => _pool.Size);
        }

        private WaitWhilePromise() { }

        public static IGDTaskSource Create(
            GodotObject target,
            Func<bool> predicate,
            PlayerLoopTiming timing,
            CancellationToken cancellationToken,
            out short token
        )
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!_pool.TryPop(out var result))
            {
                result = new WaitWhilePromise();
            }

            result._target = target;
            result._predicate = predicate;
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
            if (_cancellationToken.IsCancellationRequested || (_target is not null && !GodotObject.IsInstanceValid(_target))) // Cancel when destroyed
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            try
            {
                if (_predicate())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _core.TrySetException(ex);
                return false;
            }

            _core.TrySetResult(null);
            return false;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _predicate = default;
            _cancellationToken = default;
            return _pool.TryPush(this);
        }
    }

    private sealed class WaitUntilCanceledPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilCanceledPromise>
    {
        private static TaskPool<WaitUntilCanceledPromise> _pool;
        private WaitUntilCanceledPromise _nextNode;

        private GodotObject _target;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<object> _core;

        public ref WaitUntilCanceledPromise NextNode => ref _nextNode;

        static WaitUntilCanceledPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitUntilCanceledPromise), () => _pool.Size);
        }

        private WaitUntilCanceledPromise() { }

        public static IGDTaskSource Create(GodotObject target, CancellationToken cancellationToken, PlayerLoopTiming timing, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!_pool.TryPop(out var result))
            {
                result = new WaitUntilCanceledPromise();
            }

            result._target = target;
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
            if (_cancellationToken.IsCancellationRequested || (_target is not null && !GodotObject.IsInstanceValid(_target))) // Cancel when destroyed
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
            _cancellationToken = default;
            return _pool.TryPush(this);
        }
    }

    // Cannot add `where T : GodotObject` because `WaitUntilValueChanged` doesn't have the constraint.
    private sealed class WaitUntilValueChangedGodotObjectPromise<T, U>
        : IGDTaskSource<U>,
            IPlayerLoopItem,
            ITaskPoolNode<WaitUntilValueChangedGodotObjectPromise<T, U>>
    {
        private static TaskPool<WaitUntilValueChangedGodotObjectPromise<T, U>> _pool;
        private WaitUntilValueChangedGodotObjectPromise<T, U> _nextNode;

        private T _target;
        private GodotObject _targetGodotObject;
        private U _currentValue;
        private Func<T, U> _monitorFunction;
        private IEqualityComparer<U> _equalityComparer;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<U> _core;

        public ref WaitUntilValueChangedGodotObjectPromise<T, U> NextNode => ref _nextNode;

        static WaitUntilValueChangedGodotObjectPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitUntilValueChangedGodotObjectPromise<T, U>), () => _pool.Size);
        }

        private WaitUntilValueChangedGodotObjectPromise() { }

        public static IGDTaskSource<U> Create(
            T target,
            Func<T, U> monitorFunction,
            IEqualityComparer<U> equalityComparer,
            PlayerLoopTiming timing,
            CancellationToken cancellationToken,
            out short token
        )
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetGDTaskCompletionSource<U>.CreateFromCanceled(cancellationToken, out token);
            }

            if (!_pool.TryPop(out var result))
            {
                result = new WaitUntilValueChangedGodotObjectPromise<T, U>();
            }

            result._target = target;
            result._targetGodotObject = target as GodotObject;
            result._monitorFunction = monitorFunction;
            result._currentValue = monitorFunction(target);
            result._equalityComparer = equalityComparer ?? GodotEqualityComparer.GetDefault<U>();
            result._cancellationToken = cancellationToken;

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskPlayerLoopAutoload.AddAction(timing, result);

            token = result._core.Version;
            return result;
        }

        public U GetResult(short token)
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

        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
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
            if (_cancellationToken.IsCancellationRequested || (_target is not null && !GodotObject.IsInstanceValid(_targetGodotObject))) // Cancel when destroyed
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            U nextValue = default;
            try
            {
                nextValue = _monitorFunction(_target);
                if (_equalityComparer.Equals(_currentValue, nextValue))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _core.TrySetException(ex);
                return false;
            }

            _core.TrySetResult(nextValue);
            return false;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _target = default;
            _currentValue = default;
            _monitorFunction = default;
            _equalityComparer = default;
            _cancellationToken = default;
            return _pool.TryPush(this);
        }
    }

    private sealed class WaitUntilValueChangedStandardObjectPromise<T, U>
        : IGDTaskSource<U>,
            IPlayerLoopItem,
            ITaskPoolNode<WaitUntilValueChangedStandardObjectPromise<T, U>>
        where T : class
    {
        private static TaskPool<WaitUntilValueChangedStandardObjectPromise<T, U>> _pool;
        private WaitUntilValueChangedStandardObjectPromise<T, U> _nextNode;

        private WeakReference<T> _target;
        private U _currentValue;
        private Func<T, U> _monitorFunction;
        private IEqualityComparer<U> _equalityComparer;
        private CancellationToken _cancellationToken;

        private GDTaskCompletionSourceCore<U> _core;

        public ref WaitUntilValueChangedStandardObjectPromise<T, U> NextNode => ref _nextNode;

        static WaitUntilValueChangedStandardObjectPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitUntilValueChangedStandardObjectPromise<T, U>), () => _pool.Size);
        }

        private WaitUntilValueChangedStandardObjectPromise() { }

        public static IGDTaskSource<U> Create(
            T target,
            Func<T, U> monitorFunction,
            IEqualityComparer<U> equalityComparer,
            PlayerLoopTiming timing,
            CancellationToken cancellationToken,
            out short token
        )
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetGDTaskCompletionSource<U>.CreateFromCanceled(cancellationToken, out token);
            }

            if (!_pool.TryPop(out var result))
            {
                result = new WaitUntilValueChangedStandardObjectPromise<T, U>();
            }

            result._target = new WeakReference<T>(target, false); // wrap in WeakReference.
            result._monitorFunction = monitorFunction;
            result._currentValue = monitorFunction(target);
            result._equalityComparer = equalityComparer ?? GodotEqualityComparer.GetDefault<U>();
            result._cancellationToken = cancellationToken;

            TaskTracker.TrackActiveTask(result, 3);

            GDTaskPlayerLoopAutoload.AddAction(timing, result);

            token = result._core.Version;
            return result;
        }

        public U GetResult(short token)
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

        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
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
            if (_cancellationToken.IsCancellationRequested || !_target.TryGetTarget(out var t)) // doesn't find = cancel.
            {
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            U nextValue = default;
            try
            {
                nextValue = _monitorFunction(t);
                if (_equalityComparer.Equals(_currentValue, nextValue))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _core.TrySetException(ex);
                return false;
            }

            _core.TrySetResult(nextValue);
            return false;
        }

        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _target = default;
            _currentValue = default;
            _monitorFunction = default;
            _equalityComparer = default;
            _cancellationToken = default;
            return _pool.TryPush(this);
        }
    }
}
