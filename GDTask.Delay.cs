#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GDTask.Internal;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;

namespace GDTask
{
    public enum DelayType
    {
        /// <summary>use Time.deltaTime.</summary>
        DeltaTime,
        /// <summary>Ignore timescale, use Time.unscaledDeltaTime.</summary>
        UnscaledDeltaTime,
        /// <summary>use Stopwatch.GetTimestamp().</summary>
        Realtime
    }

    public partial struct GDTask
    {
        public static YieldAwaitable Yield()
        {
            // optimized for single continuation
            return new YieldAwaitable(PlayerLoopTiming.Process);
        }

        public static YieldAwaitable Yield(PlayerLoopTiming timing)
        {
            // optimized for single continuation
            return new YieldAwaitable(timing);
        }

        public static GDTask Yield(CancellationToken cancellationToken)
        {
            return new GDTask(YieldPromise.Create(PlayerLoopTiming.Process, cancellationToken, out var token), token);
        }

        public static GDTask Yield(PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            return new GDTask(YieldPromise.Create(timing, cancellationToken, out var token), token);
        }

        /// <summary>
        /// Similar as GDTask.Yield but guaranteed run on next frame.
        /// </summary>
        public static GDTask NextFrame()
        {
            return new GDTask(NextFramePromise.Create(PlayerLoopTiming.Process, CancellationToken.None, out var token), token);
        }

        /// <summary>
        /// Similar as GDTask.Yield but guaranteed run on next frame.
        /// </summary>
        public static GDTask NextFrame(PlayerLoopTiming timing)
        {
            return new GDTask(NextFramePromise.Create(timing, CancellationToken.None, out var token), token);
        }

        /// <summary>
        /// Similar as GDTask.Yield but guaranteed run on next frame.
        /// </summary>
        public static GDTask NextFrame(CancellationToken cancellationToken)
        {
            return new GDTask(NextFramePromise.Create(PlayerLoopTiming.Process, cancellationToken, out var token), token);
        }

        /// <summary>
        /// Similar as GDTask.Yield but guaranteed run on next frame.
        /// </summary>
        public static GDTask NextFrame(PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            return new GDTask(NextFramePromise.Create(timing, cancellationToken, out var token), token);
        }

        [Obsolete("Use WaitForEndOfFrame(MonoBehaviour) instead or GDTask.Yield(PlayerLoopTiming.LastPostLateUpdate). Equivalent for coroutine's WaitForEndOfFrame requires MonoBehaviour(runner of Coroutine).")]
        public static YieldAwaitable WaitForEndOfFrame()
        {
            return GDTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        }

        [Obsolete("Use WaitForEndOfFrame(MonoBehaviour) instead or GDTask.Yield(PlayerLoopTiming.LastPostLateUpdate). Equivalent for coroutine's WaitForEndOfFrame requires MonoBehaviour(runner of Coroutine).")]
        public static GDTask WaitForEndOfFrame(CancellationToken cancellationToken)
        {
            return GDTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken);
        }

        public static GDTask WaitForEndOfFrame(MonoBehaviour coroutineRunner, CancellationToken cancellationToken = default)
        {
            var source = WaitForEndOfFramePromise.Create(coroutineRunner, cancellationToken, out var token);
            return new GDTask(source, token);
        }

        /// <summary>
        /// Same as GDTask.Yield(PlayerLoopTiming.LastFixedUpdate).
        /// </summary>
        public static YieldAwaitable WaitForFixedUpdate()
        {
            // use LastFixedUpdate instead of FixedUpdate
            // https://github.com/Cysharp/GDTask/issues/377
            return GDTask.Yield(PlayerLoopTiming.LastFixedUpdate);
        }

        /// <summary>
        /// Same as GDTask.Yield(PlayerLoopTiming.LastFixedUpdate, cancellationToken).
        /// </summary>
        public static GDTask WaitForFixedUpdate(CancellationToken cancellationToken)
        {
            return GDTask.Yield(PlayerLoopTiming.LastFixedUpdate, cancellationToken);
        }

        public static GDTask DelayFrame(int delayFrameCount, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (delayFrameCount < 0)
            {
                throw new ArgumentOutOfRangeException("Delay does not allow minus delayFrameCount. delayFrameCount:" + delayFrameCount);
            }

            return new GDTask(DelayFramePromise.Create(delayFrameCount, delayTiming, cancellationToken, out var token), token);
        }

        public static GDTask Delay(int millisecondsDelay, bool ignoreTimeScale = false, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default(CancellationToken))
        {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, ignoreTimeScale, delayTiming, cancellationToken);
        }

        public static GDTask Delay(TimeSpan delayTimeSpan, bool ignoreTimeScale = false, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default(CancellationToken))
        {
            var delayType = ignoreTimeScale ? DelayType.UnscaledDeltaTime : DelayType.DeltaTime;
            return Delay(delayTimeSpan, delayType, delayTiming, cancellationToken);
        }

        public static GDTask Delay(int millisecondsDelay, DelayType delayType, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default(CancellationToken))
        {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, delayType, delayTiming, cancellationToken);
        }

        public static GDTask Delay(TimeSpan delayTimeSpan, DelayType delayType, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (delayTimeSpan < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("Delay does not allow minus delayTimeSpan. delayTimeSpan:" + delayTimeSpan);
            }

#if DEBUG
            // force use Realtime.
            if (PlayerLoopHelper.IsMainThread && !UnityEditor.EditorApplication.isPlaying)
            {
                delayType = DelayType.Realtime;
            }
#endif

            switch (delayType)
            {
                case DelayType.UnscaledDeltaTime:
                    {
                        return new GDTask(DelayIgnoreTimeScalePromise.Create(delayTimeSpan, delayTiming, cancellationToken, out var token), token);
                    }
                case DelayType.Realtime:
                    {
                        return new GDTask(DelayRealtimePromise.Create(delayTimeSpan, delayTiming, cancellationToken, out var token), token);
                    }
                case DelayType.DeltaTime:
                default:
                    {
                        return new GDTask(DelayPromise.Create(delayTimeSpan, delayTiming, cancellationToken, out var token), token);
                    }
            }
        }

        sealed class YieldPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<YieldPromise>
        {
            static TaskPool<YieldPromise> pool;
            YieldPromise nextNode;
            public ref YieldPromise NextNode => ref nextNode;

            static YieldPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(YieldPromise), () => pool.Size);
            }

            CancellationToken cancellationToken;
            GDTaskCompletionSourceCore<object> core;

            YieldPromise()
            {
            }

            public static IGDTaskSource Create(PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new YieldPromise();
                }


                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

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
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                core.TrySetResult(null);
                return false;
            }

            bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        sealed class NextFramePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<NextFramePromise>
        {
            static TaskPool<NextFramePromise> pool;
            NextFramePromise nextNode;
            public ref NextFramePromise NextNode => ref nextNode;

            static NextFramePromise()
            {
                TaskPool.RegisterSizeGetter(typeof(NextFramePromise), () => pool.Size);
            }

            int frameCount;
            CancellationToken cancellationToken;
            GDTaskCompletionSourceCore<AsyncUnit> core;

            NextFramePromise()
            {
            }

            public static IGDTaskSource Create(PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new NextFramePromise();
                }

                result.frameCount = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

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
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (frameCount == Time.frameCount)
                {
                    return true;
                }

                core.TrySetResult(AsyncUnit.Default);
                return false;
            }

            bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        sealed class WaitForEndOfFramePromise : IGDTaskSource, ITaskPoolNode<WaitForEndOfFramePromise>, System.Collections.IEnumerator
        {
            static TaskPool<WaitForEndOfFramePromise> pool;
            WaitForEndOfFramePromise nextNode;
            public ref WaitForEndOfFramePromise NextNode => ref nextNode;

            static WaitForEndOfFramePromise()
            {
                TaskPool.RegisterSizeGetter(typeof(WaitForEndOfFramePromise), () => pool.Size);
            }

            CancellationToken cancellationToken;
            GDTaskCompletionSourceCore<object> core;

            WaitForEndOfFramePromise()
            {
            }

            public static IGDTaskSource Create(MonoBehaviour coroutineRunner, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new WaitForEndOfFramePromise();
                }

                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                coroutineRunner.StartCoroutine(result);

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

            bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                Reset(); // Reset Enumerator
                cancellationToken = default;
                return pool.TryPush(this);
            }

            // Coroutine Runner implementation

            static readonly WaitForEndOfFrame waitForEndOfFrameYieldInstruction = new WaitForEndOfFrame();
            bool isFirst = true;

            object IEnumerator.Current => waitForEndOfFrameYieldInstruction;

            bool IEnumerator.MoveNext()
            {
                if (isFirst)
                {
                    isFirst = false;
                    return true; // start WaitForEndOfFrame
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                core.TrySetResult(null);
                return false;
            }

            public void Reset()
            {
                isFirst = true;
            }
        }

        sealed class DelayFramePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayFramePromise>
        {
            static TaskPool<DelayFramePromise> pool;
            DelayFramePromise nextNode;
            public ref DelayFramePromise NextNode => ref nextNode;

            static DelayFramePromise()
            {
                TaskPool.RegisterSizeGetter(typeof(DelayFramePromise), () => pool.Size);
            }

            int initialFrame;
            int delayFrameCount;
            CancellationToken cancellationToken;

            int currentFrameCount;
            GDTaskCompletionSourceCore<AsyncUnit> core;

            DelayFramePromise()
            {
            }

            public static IGDTaskSource Create(int delayFrameCount, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new DelayFramePromise();
                }

                result.delayFrameCount = delayFrameCount;
                result.cancellationToken = cancellationToken;
                result.initialFrame = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

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
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (currentFrameCount == 0)
                {
                    if (delayFrameCount == 0) // same as Yield
                    {
                        core.TrySetResult(AsyncUnit.Default);
                        return false;
                    }

                    // skip in initial frame.
                    if (initialFrame == Time.frameCount)
                    {
#if DEBUG
                        // force use Realtime.
                        if (PlayerLoopHelper.IsMainThread && !UnityEditor.EditorApplication.isPlaying)
                        {
                            //goto ++currentFrameCount
                        }
                        else
                        {
                            return true;
                        }
#else
                        return true;
#endif
                    }
                }

                if (++currentFrameCount >= delayFrameCount)
                {
                    core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                return true;
            }

            bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                currentFrameCount = default;
                delayFrameCount = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        sealed class DelayPromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayPromise>
        {
            static TaskPool<DelayPromise> pool;
            DelayPromise nextNode;
            public ref DelayPromise NextNode => ref nextNode;

            static DelayPromise()
            {
                TaskPool.RegisterSizeGetter(typeof(DelayPromise), () => pool.Size);
            }

            int initialFrame;
            float delayTimeSpan;
            float elapsed;
            CancellationToken cancellationToken;

            GDTaskCompletionSourceCore<object> core;

            DelayPromise()
            {
            }

            public static IGDTaskSource Create(TimeSpan delayTimeSpan, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new DelayPromise();
                }

                result.elapsed = 0.0f;
                result.delayTimeSpan = (float)delayTimeSpan.TotalSeconds;
                result.cancellationToken = cancellationToken;
                result.initialFrame = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

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
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (elapsed == 0.0f)
                {
                    if (initialFrame == Time.frameCount)
                    {
                        return true;
                    }
                }

                elapsed += Time.deltaTime;
                if (elapsed >= delayTimeSpan)
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
                delayTimeSpan = default;
                elapsed = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        sealed class DelayIgnoreTimeScalePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayIgnoreTimeScalePromise>
        {
            static TaskPool<DelayIgnoreTimeScalePromise> pool;
            DelayIgnoreTimeScalePromise nextNode;
            public ref DelayIgnoreTimeScalePromise NextNode => ref nextNode;

            static DelayIgnoreTimeScalePromise()
            {
                TaskPool.RegisterSizeGetter(typeof(DelayIgnoreTimeScalePromise), () => pool.Size);
            }

            float delayFrameTimeSpan;
            float elapsed;
            int initialFrame;
            CancellationToken cancellationToken;

            GDTaskCompletionSourceCore<object> core;

            DelayIgnoreTimeScalePromise()
            {
            }

            public static IGDTaskSource Create(TimeSpan delayFrameTimeSpan, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new DelayIgnoreTimeScalePromise();
                }

                result.elapsed = 0.0f;
                result.delayFrameTimeSpan = (float)delayFrameTimeSpan.TotalSeconds;
                result.initialFrame = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

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
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (elapsed == 0.0f)
                {
                    if (initialFrame == Time.frameCount)
                    {
                        return true;
                    }
                }

                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= delayFrameTimeSpan)
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
                delayFrameTimeSpan = default;
                elapsed = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }

        sealed class DelayRealtimePromise : IGDTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayRealtimePromise>
        {
            static TaskPool<DelayRealtimePromise> pool;
            DelayRealtimePromise nextNode;
            public ref DelayRealtimePromise NextNode => ref nextNode;

            static DelayRealtimePromise()
            {
                TaskPool.RegisterSizeGetter(typeof(DelayRealtimePromise), () => pool.Size);
            }

            long delayTimeSpanTicks;
            ValueStopwatch stopwatch;
            CancellationToken cancellationToken;

            GDTaskCompletionSourceCore<AsyncUnit> core;

            DelayRealtimePromise()
            {
            }

            public static IGDTaskSource Create(TimeSpan delayTimeSpan, PlayerLoopTiming timing, CancellationToken cancellationToken, out short token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AutoResetGDTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!pool.TryPop(out var result))
                {
                    result = new DelayRealtimePromise();
                }

                result.stopwatch = ValueStopwatch.StartNew();
                result.delayTimeSpanTicks = delayTimeSpan.Ticks;
                result.cancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

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
                if (cancellationToken.IsCancellationRequested)
                {
                    core.TrySetCanceled(cancellationToken);
                    return false;
                }

                if (stopwatch.IsInvalid)
                {
                    core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                if (stopwatch.ElapsedTicks >= delayTimeSpanTicks)
                {
                    core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                return true;
            }

            bool TryReturn()
            {
                TaskTracker.RemoveTracking(this);
                core.Reset();
                stopwatch = default;
                cancellationToken = default;
                return pool.TryPush(this);
            }
        }
    }

    public readonly struct YieldAwaitable
    {
        readonly PlayerLoopTiming timing;

        public YieldAwaitable(PlayerLoopTiming timing)
        {
            this.timing = timing;
        }

        public Awaiter GetAwaiter()
        {
            return new Awaiter(timing);
        }

        public GDTask ToGDTask()
        {
            return GDTask.Yield(timing, CancellationToken.None);
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            readonly PlayerLoopTiming timing;

            public Awaiter(PlayerLoopTiming timing)
            {
                this.timing = timing;
            }

            public bool IsCompleted => false;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                PlayerLoopHelper.AddContinuation(timing, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                PlayerLoopHelper.AddContinuation(timing, continuation);
            }
        }
    }
}
