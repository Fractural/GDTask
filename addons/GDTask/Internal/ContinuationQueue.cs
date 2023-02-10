using Godot;
using System;
using System.Threading;

namespace GDTask.Internal
{
    internal sealed class ContinuationQueue
    {
        const int MaxArrayLength = 0X7FEFFFFF;
        const int InitialSize = 16;

        readonly PlayerLoopTiming timing;

        SpinLock gate = new SpinLock(false);
        bool dequing = false;

        int actionListCount = 0;
        Action[] actionList = new Action[InitialSize];

        int waitingListCount = 0;
        Action[] waitingList = new Action[InitialSize];

        public ContinuationQueue(PlayerLoopTiming timing)
        {
            this.timing = timing;
        }

        public void Enqueue(Action continuation)
        {
            bool lockTaken = false;
            try
            {
                gate.Enter(ref lockTaken);

                if (dequing)
                {
                    // Ensure Capacity
                    if (waitingList.Length == waitingListCount)
                    {
                        var newLength = waitingListCount * 2;
                        if ((uint)newLength > MaxArrayLength) newLength = MaxArrayLength;

                        var newArray = new Action[newLength];
                        Array.Copy(waitingList, newArray, waitingListCount);
                        waitingList = newArray;
                    }
                    waitingList[waitingListCount] = continuation;
                    waitingListCount++;
                }
                else
                {
                    // Ensure Capacity
                    if (actionList.Length == actionListCount)
                    {
                        var newLength = actionListCount * 2;
                        if ((uint)newLength > MaxArrayLength) newLength = MaxArrayLength;

                        var newArray = new Action[newLength];
                        Array.Copy(actionList, newArray, actionListCount);
                        actionList = newArray;
                    }
                    actionList[actionListCount] = continuation;
                    actionListCount++;
                }
            }
            finally
            {
                if (lockTaken) gate.Exit(false);
            }
        }

        public int Clear()
        {
            var rest = actionListCount + waitingListCount;

            actionListCount = 0;
            actionList = new Action[InitialSize];

            waitingListCount = 0;
            waitingList = new Action[InitialSize];

            return rest;
        }

        // delegate entrypoint.
        public void Run()
        {
            // for debugging, create named stacktrace.
#if DEBUG
            switch (timing)
            {
                case PlayerLoopTiming.PhysicsProcess:
                    PhysicsProcess();
                    break;
                case PlayerLoopTiming.Process:
                    Process();
                    break;
                default:
                    break;
            }
#else
            RunCore();
#endif
        }

        void PhysicsProcess() => RunCore();
        void Process() => RunCore();

        [System.Diagnostics.DebuggerHidden]
        void RunCore()
        {
            {
                bool lockTaken = false;
                try
                {
                    gate.Enter(ref lockTaken);
                    if (actionListCount == 0) return;
                    dequing = true;
                }
                finally
                {
                    if (lockTaken) gate.Exit(false);
                }
            }

            for (int i = 0; i < actionListCount; i++)
            {

                var action = actionList[i];
                actionList[i] = null;
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    GD.PrintErr(ex);
                }
            }

            {
                bool lockTaken = false;
                try
                {
                    gate.Enter(ref lockTaken);
                    dequing = false;

                    var swapTempActionList = actionList;

                    actionListCount = waitingListCount;
                    actionList = waitingList;

                    waitingListCount = 0;
                    waitingList = swapTempActionList;
                }
                finally
                {
                    if (lockTaken) gate.Exit(false);
                }
            }
        }
    }
}

