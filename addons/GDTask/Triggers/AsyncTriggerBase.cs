#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;
using Godot;

namespace GDTask.Triggers
{
    public abstract class AsyncTriggerBase<T> : Node
    {
        TriggerEvent<T> triggerEvent;

        internal protected bool calledEnterTree;
        internal protected bool calledDestroy;

        public override void _EnterTree()
        {
            calledEnterTree = true;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
                OnDestroy();
        }

        void OnDestroy()
        {
            if (calledDestroy) return;
            calledDestroy = true;

            triggerEvent.SetCompleted();
        }

        internal void AddHandler(ITriggerHandler<T> handler)
        {
            if (!calledEnterTree)
            {
                GDTaskPlayerLoopManager.AddAction(PlayerLoopTiming.Process, new AwakeMonitor(this));
            }

            triggerEvent.Add(handler);
        }

        internal void RemoveHandler(ITriggerHandler<T> handler)
        {
            if (!calledEnterTree)
            {
                GDTaskPlayerLoopManager.AddAction(PlayerLoopTiming.Process, new AwakeMonitor(this));
            }

            triggerEvent.Remove(handler);
        }

        protected void RaiseEvent(T value)
        {
            triggerEvent.SetResult(value);
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
                if (trigger.calledEnterTree) return false;
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