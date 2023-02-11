using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fractural.Tasks.Internal;

namespace Fractural.Tasks
{
    public partial struct GDTask
    {

        /// <summary>
        /// If running on mainthread, do nothing. Otherwise, same as GDTask.Yield(PlayerLoopTiming.Update).
        /// </summary>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(CancellationToken cancellationToken = default)
        {
            return new SwitchToMainThreadAwaitable(PlayerLoopTiming.Process, cancellationToken);
        }

        /// <summary>
        /// If running on mainthread, do nothing. Otherwise, same as GDTask.Yield(timing).
        /// </summary>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return new SwitchToMainThreadAwaitable(timing, cancellationToken);
        }

        /// <summary>
        /// Return to mainthread(same as await SwitchToMainThread) after using scope is closed.
        /// </summary>
        public static ReturnToMainThread ReturnToMainThread(CancellationToken cancellationToken = default)
        {
            return new ReturnToMainThread(PlayerLoopTiming.Process, cancellationToken);
        }

        /// <summary>
        /// Return to mainthread(same as await SwitchToMainThread) after using scope is closed.
        /// </summary>
        public static ReturnToMainThread ReturnToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return new ReturnToMainThread(timing, cancellationToken);
        }

        /// <summary>
        /// Queue the action to PlayerLoop.
        /// </summary>
        public static void Post(Action action, PlayerLoopTiming timing = PlayerLoopTiming.Process)
        {
            GDTaskPlayerLoopAutoload.AddContinuation(timing, action);
        }


        public static SwitchToThreadPoolAwaitable SwitchToThreadPool()
        {
            return new SwitchToThreadPoolAwaitable();
        }

        /// <summary>
        /// Note: use SwitchToThreadPool is recommended.
        /// </summary>
        public static SwitchToTaskPoolAwaitable SwitchToTaskPool()
        {
            return new SwitchToTaskPoolAwaitable();
        }

        public static SwitchToSynchronizationContextAwaitable SwitchToSynchronizationContext(SynchronizationContext synchronizationContext, CancellationToken cancellationToken = default)
        {
            Error.ThrowArgumentNullException(synchronizationContext, nameof(synchronizationContext));
            return new SwitchToSynchronizationContextAwaitable(synchronizationContext, cancellationToken);
        }

        public static ReturnToSynchronizationContext ReturnToSynchronizationContext(SynchronizationContext synchronizationContext, CancellationToken cancellationToken = default)
        {
            return new ReturnToSynchronizationContext(synchronizationContext, false, cancellationToken);
        }

        public static ReturnToSynchronizationContext ReturnToCurrentSynchronizationContext(bool dontPostWhenSameContext = true, CancellationToken cancellationToken = default)
        {
            return new ReturnToSynchronizationContext(SynchronizationContext.Current, dontPostWhenSameContext, cancellationToken);
        }
    }

    public struct SwitchToMainThreadAwaitable
    {
        readonly PlayerLoopTiming playerLoopTiming;
        readonly CancellationToken cancellationToken;

        public SwitchToMainThreadAwaitable(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
        {
            this.playerLoopTiming = playerLoopTiming;
            this.cancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter() => new Awaiter(playerLoopTiming, cancellationToken);

        public struct Awaiter : ICriticalNotifyCompletion
        {
            readonly PlayerLoopTiming playerLoopTiming;
            readonly CancellationToken cancellationToken;

            public Awaiter(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
            {
                this.playerLoopTiming = playerLoopTiming;
                this.cancellationToken = cancellationToken;
            }

            public bool IsCompleted
            {
                get
                {
                    var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    if (GDTaskPlayerLoopAutoload.MainThreadId == currentThreadId)
                    {
                        return true; // run immediate.
                    }
                    else
                    {
                        return false; // register continuation.
                    }
                }
            }

            public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

            public void OnCompleted(Action continuation)
            {
                GDTaskPlayerLoopAutoload.AddContinuation(playerLoopTiming, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                GDTaskPlayerLoopAutoload.AddContinuation(playerLoopTiming, continuation);
            }
        }
    }

    public struct ReturnToMainThread
    {
        readonly PlayerLoopTiming playerLoopTiming;
        readonly CancellationToken cancellationToken;

        public ReturnToMainThread(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
        {
            this.playerLoopTiming = playerLoopTiming;
            this.cancellationToken = cancellationToken;
        }

        public Awaiter DisposeAsync()
        {
            return new Awaiter(playerLoopTiming, cancellationToken); // run immediate.
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            readonly PlayerLoopTiming timing;
            readonly CancellationToken cancellationToken;

            public Awaiter(PlayerLoopTiming timing, CancellationToken cancellationToken)
            {
                this.timing = timing;
                this.cancellationToken = cancellationToken;
            }

            public Awaiter GetAwaiter() => this;

            public bool IsCompleted => GDTaskPlayerLoopAutoload.MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId;

            public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

            public void OnCompleted(Action continuation)
            {
                GDTaskPlayerLoopAutoload.AddContinuation(timing, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                GDTaskPlayerLoopAutoload.AddContinuation(timing, continuation);
            }
        }
    }


    public struct SwitchToThreadPoolAwaitable
    {
        public Awaiter GetAwaiter() => new Awaiter();

        public struct Awaiter : ICriticalNotifyCompletion
        {
            static readonly WaitCallback switchToCallback = Callback;

            public bool IsCompleted => false;
            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                ThreadPool.QueueUserWorkItem(switchToCallback, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                ThreadPool.UnsafeQueueUserWorkItem(switchToCallback, continuation);
            }

            static void Callback(object state)
            {
                var continuation = (Action)state;
                continuation();
            }
        }
    }

    public struct SwitchToTaskPoolAwaitable
    {
        public Awaiter GetAwaiter() => new Awaiter();

        public struct Awaiter : ICriticalNotifyCompletion
        {
            static readonly Action<object> switchToCallback = Callback;

            public bool IsCompleted => false;
            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                Task.Factory.StartNew(switchToCallback, continuation, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                Task.Factory.StartNew(switchToCallback, continuation, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            static void Callback(object state)
            {
                var continuation = (Action)state;
                continuation();
            }
        }
    }

    public struct SwitchToSynchronizationContextAwaitable
    {
        readonly SynchronizationContext synchronizationContext;
        readonly CancellationToken cancellationToken;

        public SwitchToSynchronizationContextAwaitable(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
        {
            this.synchronizationContext = synchronizationContext;
            this.cancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter() => new Awaiter(synchronizationContext, cancellationToken);

        public struct Awaiter : ICriticalNotifyCompletion
        {
            static readonly SendOrPostCallback switchToCallback = Callback;
            readonly SynchronizationContext synchronizationContext;
            readonly CancellationToken cancellationToken;

            public Awaiter(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
            {
                this.synchronizationContext = synchronizationContext;
                this.cancellationToken = cancellationToken;
            }

            public bool IsCompleted => false;
            public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

            public void OnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            static void Callback(object state)
            {
                var continuation = (Action)state;
                continuation();
            }
        }
    }

    public struct ReturnToSynchronizationContext
    {
        readonly SynchronizationContext syncContext;
        readonly bool dontPostWhenSameContext;
        readonly CancellationToken cancellationToken;

        public ReturnToSynchronizationContext(SynchronizationContext syncContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
        {
            this.syncContext = syncContext;
            this.dontPostWhenSameContext = dontPostWhenSameContext;
            this.cancellationToken = cancellationToken;
        }

        public Awaiter DisposeAsync()
        {
            return new Awaiter(syncContext, dontPostWhenSameContext, cancellationToken);
        }

        public struct Awaiter : ICriticalNotifyCompletion
        {
            static readonly SendOrPostCallback switchToCallback = Callback;

            readonly SynchronizationContext synchronizationContext;
            readonly bool dontPostWhenSameContext;
            readonly CancellationToken cancellationToken;

            public Awaiter(SynchronizationContext synchronizationContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
            {
                this.synchronizationContext = synchronizationContext;
                this.dontPostWhenSameContext = dontPostWhenSameContext;
                this.cancellationToken = cancellationToken;
            }

            public Awaiter GetAwaiter() => this;

            public bool IsCompleted
            {
                get
                {
                    if (!dontPostWhenSameContext) return false;

                    var current = SynchronizationContext.Current;
                    if (current == synchronizationContext)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

            public void OnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                synchronizationContext.Post(switchToCallback, continuation);
            }

            static void Callback(object state)
            {
                var continuation = (Action)state;
                continuation();
            }
        }
    }
}
