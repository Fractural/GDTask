using System;
using System.Collections.Generic;
using System.Threading;
using Fractural.Tasks.Internal;
using Godot;

namespace Fractural.Tasks
{
    public partial struct GDTask
    {
        public static GDTask WaitUntil(GodotObject target, Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return new GDTask(WaitUntilPromise.Create(target, predicate, timing, cancellationToken, out var token), token);
        }
        public static GDTask WaitUntil(Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return WaitUntil(null, predicate, timing, cancellationToken);
        }

        public static GDTask WaitWhile(GodotObject target, Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return new GDTask(WaitWhilePromise.Create(target, predicate, timing, cancellationToken, out var token), token);
        }
        public static GDTask WaitWhile(Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
        {
            return WaitWhile(null, predicate, timing, cancellationToken);
        }

        public static GDTask WaitUntilCanceled(GodotObject target, CancellationToken cancellationToken, PlayerLoopTiming timing = PlayerLoopTiming.Process)
        {
            return new GDTask(WaitUntilCanceledPromise.Create(target, cancellationToken, timing, out var token), token);
        }
        public static GDTask WaitUntilCanceled(CancellationToken cancellationToken, PlayerLoopTiming timing = PlayerLoopTiming.Process)
        {
            return WaitUntilCanceled(null, cancellationToken, timing);
        }

        public static GDTask<U> WaitUntilValueChanged<T, U>(T target, Func<T, U> monitorFunction, PlayerLoopTiming monitorTiming = PlayerLoopTiming.Process, IEqualityComparer<U> equalityComparer = null, CancellationToken cancellationToken = default)
          where T : class
        {
            return new GDTask<U>(target is GodotObject
                ? WaitUntilValueChangedGodotObjectPromise<T, U>.Create(target, monitorFunction, equalityComparer, monitorTiming, cancellationToken, out var token)
                : WaitUntilValueChangedStandardObjectPromise<T, U>.Create(target, monitorFunction, equalityComparer, monitorTiming, cancellationToken, out token), token);
        }

        sealed class WaitUntilPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilPromise>
        {
            static TaskPool<WaitUntilPromise> pool;
            WaitUntilPromise nextNode;
            public ref WaitUntilPromise NextNode => ref nextNode;

            static WaitUntilPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilPromise), () => pool.Size);
            }

            GodotObject target;
            Func<bool> predicate;
            CancellationToken cancellationToken;

            GDTaskCompletionSourceCore<object> core;

            WaitUntilPromise()
            {
            }

            public static IGDTaskSource Create(GodotObject target, Func<bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WaitUntilPromise();
                }

                result.target = target;
                result.predicate = predicate;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskPlayerLoopAutoload.AddAction(timing, result);

                token = result.core.Version;
                return result;
            }

            public void GetResult(short token)
            {
                try
                {
                    core.GetResult(token);
                }
                finally
                {
                    TryReturn();
                }
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

            public bool MoveNext()
            {
                if (cancellationToken.IsCancellationRequested || (target is not null && !GodotObject.IsInstanceValid(target))) // Cancel when destroyed
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                try
                {
                    if (!predicate())
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                    return false;
                }

                core.TrySetResult(null);
                return false;
            }

            bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                predicate = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        sealed class WaitWhilePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitWhilePromise>
        {
            static TaskPool<WaitWhilePromise> pool;
            WaitWhilePromise nextNode;
            public ref WaitWhilePromise NextNode => ref nextNode;

            static WaitWhilePromise()
            {
                TaskPool.RegisterSizeGetter(typeof(WaitWhilePromise), () => pool.Size);
            }

            GodotObject target;
            Func<bool> predicate;
            CancellationToken cancellationToken;

            GDTaskCompletionSourceCore<object> core;

            WaitWhilePromise()
            {
            }

            public static IGDTaskSource Create(GodotObject target, Func<bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WaitWhilePromise();
                }

                result.target = target;
                result.predicate = predicate;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskPlayerLoopAutoload.AddAction(timing, result);

                token = result.core.Version;
                return result;
            }

            public void GetResult(short token)
            {
                try
                {
                    core.GetResult(token);
                }
                finally
                {
                    TryReturn();
                }
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

            public bool MoveNext()
            {
                if (cancellationToken.IsCancellationRequested || (target is not null && !GodotObject.IsInstanceValid(target))) // Cancel when destroyed
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                try
                {
                    if (predicate())
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                    return false;
                }

                core.TrySetResult(null);
                return false;
            }

            bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                predicate = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        sealed class WaitUntilCanceledPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilCanceledPromise>
        {
            static TaskPool<WaitUntilCanceledPromise> pool;
            WaitUntilCanceledPromise nextNode;
            public ref WaitUntilCanceledPromise NextNode => ref nextNode;

            static WaitUntilCanceledPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilCanceledPromise), () => pool.Size);
            }

            GodotObject target;
            CancellationToken cancellationToken;

            GDTaskCompletionSourceCore<object> core;

            WaitUntilCanceledPromise()
            {
            }

            public static IGDTaskSource Create(GodotObject target, CancellationToken cancellationToken, PlayerLoopTiming timing, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WaitUntilCanceledPromise();
                }

                result.target = target;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskPlayerLoopAutoload.AddAction(timing, result);

                token = result.core.Version;
                return result;
            }

            public void GetResult(short token)
            {
                try
                {
                    core.GetResult(token);
                }
                finally
                {
                    TryReturn();
                }
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

            public bool MoveNext()
            {
                if (cancellationToken.IsCancellationRequested || (target is not null && !GodotObject.IsInstanceValid(target))) // Cancel when destroyed
                {
                    core.TrySetResult(null);
                    return false;
                }

                return true;
            }

            bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        // Cannot add `where T : GodotObject` because `WaitUntilValueChanged` doesn't have the constraint.
        sealed class WaitUntilValueChangedGodotObjectPromise<T, U> : IGDTaskSource<U>, IPlayerLoopItem, ITaskPoolNode<WaitUntilValueChangedGodotObjectPromise<T, U>>
        {
            static TaskPool<WaitUntilValueChangedGodotObjectPromise<T, U>> pool;
            WaitUntilValueChangedGodotObjectPromise<T, U> nextNode;
            public ref WaitUntilValueChangedGodotObjectPromise<T, U> NextNode => ref nextNode;

            static WaitUntilValueChangedGodotObjectPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilValueChangedGodotObjectPromise<T, U>), () => pool.Size);
            }

            T target;
            GodotObject targetGodotObject;
            U currentValue;
            Func<T, U> monitorFunction;
            IEqualityComparer<U> equalityComparer;
            CancellationToken cancellationToken;

            GDTaskCompletionSourceCore<U> core;

            WaitUntilValueChangedGodotObjectPromise()
            {
            }

            public static IGDTaskSource<U> Create(T target, Func<T, U> monitorFunction, IEqualityComparer<U> equalityComparer, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource<U>.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WaitUntilValueChangedGodotObjectPromise<T, U>();
                }

                result.target = target;
                result.targetGodotObject = target as GodotObject;
                result.monitorFunction = monitorFunction;
                result.currentValue = monitorFunction(target);
                result.equalityComparer = equalityComparer ?? GodotEqualityComparer.GetDefault<U>();
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskPlayerLoopAutoload.AddAction(timing, result);

                token = result.core.Version;
                return result;
            }

            public U GetResult(short token)
            {
                try
                {
                    return core.GetResult(token);
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

            public bool MoveNext()
            {
                if (cancellationToken.IsCancellationRequested || (target is not null && !GodotObject.IsInstanceValid(targetGodotObject))) // Cancel when destroyed
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                U nextValue = default;
                try
                {
                    nextValue = monitorFunction(target);
                    if (equalityComparer.Equals(currentValue, nextValue))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                    return false;
                }

                core.TrySetResult(nextValue);
                return false;
            }

            bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                target = default;
                currentValue = default;
                monitorFunction = default;
                equalityComparer = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        sealed class WaitUntilValueChangedStandardObjectPromise<T, U> : IGDTaskSource<U>, IPlayerLoopItem, ITaskPoolNode<WaitUntilValueChangedStandardObjectPromise<T, U>>
            where T : class
        {
            static TaskPool<WaitUntilValueChangedStandardObjectPromise<T, U>> pool;
            WaitUntilValueChangedStandardObjectPromise<T, U> nextNode;
            public ref WaitUntilValueChangedStandardObjectPromise<T, U> NextNode => ref nextNode;

            static WaitUntilValueChangedStandardObjectPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilValueChangedStandardObjectPromise<T, U>), () => pool.Size);
            }

            WeakReference<T> target;
            U currentValue;
            Func<T, U> monitorFunction;
            IEqualityComparer<U> equalityComparer;
            CancellationToken cancellationToken;

            GDTaskCompletionSourceCore<U> core;

            WaitUntilValueChangedStandardObjectPromise()
            {
            }

            public static IGDTaskSource<U> Create(T target, Func<T, U> monitorFunction, IEqualityComparer<U> equalityComparer, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource<U>.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WaitUntilValueChangedStandardObjectPromise<T, U>();
                }

                result.target = new WeakReference<T>(target, false); // wrap in WeakReference.
                result.monitorFunction = monitorFunction;
                result.currentValue = monitorFunction(target);
                result.equalityComparer = equalityComparer ?? GodotEqualityComparer.GetDefault<U>();
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                GDTaskPlayerLoopAutoload.AddAction(timing, result);

                token = result.core.Version;
                return result;
            }

            public U GetResult(short token)
            {
                try
                {
                    return core.GetResult(token);
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

            public bool MoveNext()
            {
                if (cancellationToken.IsCancellationRequested || !target.TryGetTarget(out var t)) // doesn't find = cancel.
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                U nextValue = default;
                try
                {
                    nextValue = monitorFunction(t);
                    if (equalityComparer.Equals(currentValue, nextValue))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                    return false;
                }

                core.TrySetResult(nextValue);
                return false;
            }

            bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                target = default;
                currentValue = default;
                monitorFunction = default;
                equalityComparer = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }
    }
}
