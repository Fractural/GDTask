#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;
using UnityEngine;

namespace GDTask.Triggers
{
    public abstract class AsyncTriggerBase<T> : MonoBehaviour, IGDTaskAsyncEnumerable<T>
    {
        TriggerEvent<T> triggerEvent;

        internal protected bool calledAwake;
        internal protected bool calledDestroy;

        void Awake()
        {
            calledAwake = true;
        }

        void OnDestroy()
        {
            if (calledDestroy) return;
            calledDestroy = true;

            triggerEvent.SetCompleted();
        }

        internal void AddHandler(ITriggerHandler<T> handler)
        {
            if (!calledAwake)
            {
                PlayerLoopHelper.AddAction(PlayerLoopTiming.Process, new AwakeMonitor(this));
            }

            triggerEvent.Add(handler);
        }

        internal void RemoveHandler(ITriggerHandler<T> handler)
        {
            if (!calledAwake)
            {
                PlayerLoopHelper.AddAction(PlayerLoopTiming.Process, new AwakeMonitor(this));
            }

            triggerEvent.Remove(handler);
        }

        protected void RaiseEvent(T value)
        {
            triggerEvent.SetResult(value);
        }

        public IGDTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncTriggerEnumerator(this, cancellationToken);
        }

        sealed class AsyncTriggerEnumerator : MoveNextSource, IGDTaskAsyncEnumerator<T>, ITriggerHandler<T>
        {
            static Action<object> cancellationCallback = CancellationCallback;

            readonly AsyncTriggerBase<T> parent;
            CancellationToken cancellationToken;
            CancellationTokenRegistration registration;
            bool called;
            bool isDisposed;

            public AsyncTriggerEnumerator(AsyncTriggerBase<T> parent, CancellationToken cancellationToken)
            {
                this.parent = parent;
                this.cancellationToken = cancellationToken;
            }

            public void OnCanceled(CancellationToken cancellationToken = default)
            {
                completionSource.TrySetCanceled(cancellationToken);
            }

            public void OnNext(T value)
            {
                Current = value;
                completionSource.TrySetResult(true);
            }

            public void OnCompleted()
            {
                completionSource.TrySetResult(false);
            }

            public void OnError(Exception ex)
            {
                completionSource.TrySetException(ex);
            }

            static void CancellationCallback(object state)
            {
                var self = (AsyncTriggerEnumerator)state;
                self.DisposeAsync().Forget(); // sync

                self.completionSource.TrySetCanceled(self.cancellationToken);
            }

            public T Current { get; private set; }
            ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
            ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

            public GDTask<bool> MoveNextAsync()
            {
                cancellationToken.ThrowIfCancellationRequested();
                completionSource.Reset();

                if (!called)
                {
                    called = true;

                    TaskTracker.TrackActiveTask(this, 3);
                    parent.AddHandler(this);
                    if (cancellationToken.CanBeCanceled)
                    {
                        registration = cancellationToken.RegisterWithoutCaptureExecutionContext(cancellationCallback, this);
                    }
                }

                return new GDTask<bool>(this, completionSource.Version);
            }

            public GDTask DisposeAsync()
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    TaskTracker.RemoveTracking(this);
                    registration.Dispose();
                    parent.RemoveHandler(this);
                }

                return default;
            }
        }

        class AwakeMonitor : IPlayerLoopItem
        {
            readonly AsyncTriggerBase<T> trigger;

            public AwakeMonitor(AsyncTriggerBase<T> trigger)
            {
                this.trigger = trigger;
            }

            public bool MoveNext()
            {
                if (trigger.calledAwake) return false;
                if (trigger == null)
                {
                    trigger.OnDestroy();
                    return false;
                }
                return true;
            }
        }
    }

    public interface IAsyncOneShotTrigger
    {
        GDTask OneShotAsync();
    }

    public partial class AsyncTriggerHandler<T> : IAsyncOneShotTrigger
    {
        GDTask IAsyncOneShotTrigger.OneShotAsync()
        {
            core.Reset();
            return new GDTask((IGDTaskSource)this, core.Version);
        }
    }

    public sealed partial class AsyncTriggerHandler<T> : IGDTaskSource<T>, ITriggerHandler<T>, IDisposable
    {
        static Action<object> cancellationCallback = CancellationCallback;

        readonly AsyncTriggerBase<T> trigger;

        CancellationToken cancellationToken;
        CancellationTokenRegistration registration;
        bool isDisposed;
        bool callOnce;

        GDTaskCompletionSourceCore<T> core;

        internal CancellationToken CancellationToken => cancellationToken;

        ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
        ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

        internal AsyncTriggerHandler(AsyncTriggerBase<T> trigger, bool callOnce)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                isDisposed = true;
                return;
            }

            this.trigger = trigger;
            this.cancellationToken = default;
            this.registration = default;
            this.callOnce = callOnce;

            trigger.AddHandler(this);

            TaskTracker.TrackActiveTask(this, 3);
        }

        internal AsyncTriggerHandler(AsyncTriggerBase<T> trigger, CancellationToken cancellationToken, bool callOnce)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                isDisposed = true;
                return;
            }

            this.trigger = trigger;
            this.cancellationToken = cancellationToken;
            this.callOnce = callOnce;

            trigger.AddHandler(this);

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.RegisterWithoutCaptureExecutionContext(cancellationCallback, this);
            }

            TaskTracker.TrackActiveTask(this, 3);
        }

        static void CancellationCallback(object state)
        {
            var self = (AsyncTriggerHandler<T>)state;
            self.Dispose();

            self.core.TrySetCanceled(self.cancellationToken);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                TaskTracker.RemoveTracking(this);
                registration.Dispose();
                trigger.RemoveHandler(this);
            }
        }

        T IGDTaskSource<T>.GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
                if (callOnce)
                {
                    Dispose();
                }
            }
        }

        void ITriggerHandler<T>.OnNext(T value)
        {
            core.TrySetResult(value);
        }

        void ITriggerHandler<T>.OnCanceled(CancellationToken cancellationToken)
        {
            core.TrySetCanceled(cancellationToken);
        }

        void ITriggerHandler<T>.OnCompleted()
        {
            core.TrySetCanceled(CancellationToken.None);
        }

        void ITriggerHandler<T>.OnError(Exception ex)
        {
            core.TrySetException(ex);
        }

        void IGDTaskSource.GetResult(short token)
        {
            ((IGDTaskSource<T>)this).GetResult(token);
        }

        GDTaskStatus IGDTaskSource.GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        GDTaskStatus IGDTaskSource.UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void IGDTaskSource.OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }
    }
}