﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Threading;
using GDTask.Triggers;
using System;
using GDTask.Internal;
using Godot;

namespace GDTask
{

    public static partial class CancellationTokenSourceExtensions
    {
        readonly static Action<object> CancelCancellationTokenSourceStateDelegate = new Action<object>(CancelCancellationTokenSourceState);

        static void CancelCancellationTokenSourceState(object state)
        {
            var cts = (CancellationTokenSource)state;
            cts.Cancel();
        }

        public static IDisposable CancelAfterSlim(this CancellationTokenSource cts, int millisecondsDelay, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process)
        {
            return CancelAfterSlim(cts, TimeSpan.FromMilliseconds(millisecondsDelay), delayType, delayTiming);
        }

        public static IDisposable CancelAfterSlim(this CancellationTokenSource cts, TimeSpan delayTimeSpan, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process)
        {
            return PlayerLoopTimer.StartNew(delayTimeSpan, false, delayType, delayTiming, cts.Token, CancelCancellationTokenSourceStateDelegate, cts);
        }

        public static void RegisterRaiseCancelOnDestroy(this CancellationTokenSource cts, Node node)
        {
            var trigger = node.GetAsyncDestroyTrigger();
            trigger.CancellationToken.RegisterWithoutCaptureExecutionContext(CancelCancellationTokenSourceStateDelegate, cts);
        }
    }
}
