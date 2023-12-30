using System.Threading;
using System;
using Fractural.Tasks.Internal;
using Godot;

namespace Fractural.Tasks
{
    public abstract class PlayerLoopTimer : IDisposable, IPlayerLoopItem
    {
        readonly CancellationToken cancellationToken;
        readonly Action<object> timerCallback;
        readonly object state;
        readonly PlayerLoopTiming playerLoopTiming;
        readonly bool periodic;

        bool isRunning;
        bool tryStop;
        bool isDisposed;

        protected PlayerLoopTimer(bool periodic, PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken, Action<object> timerCallback, object state)
        {
            this.periodic = periodic;
            this.playerLoopTiming = playerLoopTiming;
            this.cancellationToken = cancellationToken;
            this.timerCallback = timerCallback;
            this.state = state;
        }

        public static PlayerLoopTimer Create(TimeSpan interval, bool periodic, DelayType delayType, PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken, Action<object> timerCallback, object state)
        {
#if DEBUG
            // force use Realtime.
            if (GDTaskPlayerLoopAutoload.IsMainThread && Engine.IsEditorHint())
            {
                delayType = DelayType.Realtime;
            }
#endif

            switch (delayType)
            {
                case DelayType.Realtime:
                    return new RealtimePlayerLoopTimer(interval, periodic, playerLoopTiming, cancellationToken, timerCallback, state);
                case DelayType.DeltaTime:
                default:
                    return new DeltaTimePlayerLoopTimer(interval, periodic, playerLoopTiming, cancellationToken, timerCallback, state);
            }
        }

        public static PlayerLoopTimer StartNew(TimeSpan interval, bool periodic, DelayType delayType, PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken, Action<object> timerCallback, object state)
        {
            var timer = Create(interval, periodic, delayType, playerLoopTiming, cancellationToken, timerCallback, state);
            timer.Restart();
            return timer;
        }

        /// <summary>
        /// Restart(Reset and Start) timer.
        /// </summary>
        public void Restart()
        {
            if (isDisposed) throw new ObjectDisposedException(null);

            ResetCore(null); // init state
            if (!isRunning)
            {
                isRunning = true;
                GDTaskPlayerLoopAutoload.AddAction(playerLoopTiming, this);
            }
            tryStop = false;
        }

        /// <summary>
        /// Restart(Reset and Start) and change interval.
        /// </summary>
        public void Restart(TimeSpan interval)
        {
            if (isDisposed) throw new ObjectDisposedException(null);

            ResetCore(interval); // init state
            if (!isRunning)
            {
                isRunning = true;
                GDTaskPlayerLoopAutoload.AddAction(playerLoopTiming, this);
            }
            tryStop = false;
        }

        /// <summary>
        /// Stop timer.
        /// </summary>
        public void Stop()
        {
            tryStop = true;
        }

        protected abstract void ResetCore(TimeSpan? newInterval);

        public void Dispose()
        {
            isDisposed = true;
        }

        bool IPlayerLoopItem.MoveNext()
        {
            if (isDisposed)
            {
                isRunning = false;
                return false;
            }
            if (tryStop)
            {
                isRunning = false;
                return false;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                isRunning = false;
                return false;
            }

            if (!MoveNextCore())
            {
                timerCallback(state);

                if (periodic)
                {
                    ResetCore(null);
                    return true;
                }
                else
                {
                    isRunning = false;
                    return false;
                }
            }

            return true;
        }

        protected abstract bool MoveNextCore();
    }

    sealed class DeltaTimePlayerLoopTimer : PlayerLoopTimer
    {
        bool isMainThread;
        ulong initialFrame;
        double elapsed;
        double interval;

        public DeltaTimePlayerLoopTimer(TimeSpan interval, bool periodic, PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken, Action<object> timerCallback, object state)
            : base(periodic, playerLoopTiming, cancellationToken, timerCallback, state)
        {
            ResetCore(interval);
        }

        protected override bool MoveNextCore()
        {
            if (elapsed == 0.0)
            {
                if (isMainThread && initialFrame == Engine.GetProcessFrames())
                {
                    return true;
                }
            }

            elapsed += GDTaskPlayerLoopAutoload.Global.DeltaTime;
            if (elapsed >= interval)
            {
                return false;
            }

            return true;
        }

        protected override void ResetCore(TimeSpan? interval)
        {
            this.elapsed = 0.0;
            this.isMainThread = GDTaskPlayerLoopAutoload.IsMainThread;
            this.initialFrame = Engine.GetProcessFrames();
            if (interval != null)
            {
                this.interval = (float)interval.Value.TotalSeconds;
            }
        }
    }

    sealed class RealtimePlayerLoopTimer : PlayerLoopTimer
    {
        ValueStopwatch stopwatch;
        long intervalTicks;

        public RealtimePlayerLoopTimer(TimeSpan interval, bool periodic, PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken, Action<object> timerCallback, object state)
            : base(periodic, playerLoopTiming, cancellationToken, timerCallback, state)
        {
            ResetCore(interval);
        }

        protected override bool MoveNextCore()
        {
            if (stopwatch.ElapsedTicks >= intervalTicks)
            {
                return false;
            }

            return true;
        }

        protected override void ResetCore(TimeSpan? interval)
        {
            this.stopwatch = ValueStopwatch.StartNew();
            if (interval != null)
            {
                this.intervalTicks = interval.Value.Ticks;
            }
        }
    }
}

