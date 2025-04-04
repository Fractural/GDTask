using System;
using System.Collections.Generic;
using System.Threading;
using Fractural.Tasks.Internal;

namespace Fractural.Tasks;

public partial struct GDTask
{
    public static GDTask<(bool hasResultLeft, T result)> WhenAny<T>(GDTask<T> leftTask, GDTask rightTask)
    {
        return new GDTask<(bool, T)>(new WhenAnyLRPromise<T>(leftTask, rightTask), 0);
    }

    public static GDTask<(int winArgumentIndex, T result)> WhenAny<T>(params GDTask<T>[] tasks)
    {
        return new GDTask<(int, T)>(new WhenAnyPromise<T>(tasks, tasks.Length), 0);
    }

    public static GDTask<(int winArgumentIndex, T result)> WhenAny<T>(IEnumerable<GDTask<T>> tasks)
    {
        using var span = ArrayPoolUtil.Materialize(tasks);
        return new GDTask<(int, T)>(new WhenAnyPromise<T>(span.Array, span.Length), 0);
    }

    /// <summary>Return value is winArgumentIndex</summary>
    public static GDTask<int> WhenAny(params GDTask[] tasks)
    {
        return new GDTask<int>(new WhenAnyPromise(tasks, tasks.Length), 0);
    }

    /// <summary>Return value is winArgumentIndex</summary>
    public static GDTask<int> WhenAny(IEnumerable<GDTask> tasks)
    {
        using var span = ArrayPoolUtil.Materialize(tasks);
        return new GDTask<int>(new WhenAnyPromise(span.Array, span.Length), 0);
    }

    private sealed class WhenAnyLRPromise<T> : IGDTaskSource<(bool, T)>
    {
        private int _completedCount;
        private GDTaskCompletionSourceCore<(bool, T)> _core;

        public WhenAnyLRPromise(GDTask<T> leftTask, GDTask rightTask)
        {
            TaskTracker.TrackActiveTask(this, 3);

            {
                GDTask<T>.Awaiter awaiter;
                try
                {
                    awaiter = leftTask.GetAwaiter();
                }
                catch (Exception ex)
                {
                    _core.TrySetException(ex);
                    goto RIGHT;
                }

                if (awaiter.IsCompleted)
                {
                    TryLeftInvokeContinuation(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(
                        state =>
                        {
                            using var t = (StateTuple<WhenAnyLRPromise<T>, GDTask<T>.Awaiter>)state;
                            TryLeftInvokeContinuation(t.Item1, t.Item2);
                        },
                        StateTuple.Create(this, awaiter)
                    );
                }
            }
            RIGHT:
            {
                GDTask.Awaiter awaiter;
                try
                {
                    awaiter = rightTask.GetAwaiter();
                }
                catch (Exception ex)
                {
                    _core.TrySetException(ex);
                    return;
                }

                if (awaiter.IsCompleted)
                {
                    TryRightInvokeContinuation(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(
                        state =>
                        {
                            using var t = (StateTuple<WhenAnyLRPromise<T>, GDTask.Awaiter>)state;
                            TryRightInvokeContinuation(t.Item1, t.Item2);
                        },
                        StateTuple.Create(this, awaiter)
                    );
                }
            }
        }

        static void TryLeftInvokeContinuation(WhenAnyLRPromise<T> self, in GDTask<T>.Awaiter awaiter)
        {
            T result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self._core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self._completedCount) == 1)
            {
                self._core.TrySetResult((true, result));
            }
        }

        static void TryRightInvokeContinuation(WhenAnyLRPromise<T> self, in GDTask.Awaiter awaiter)
        {
            try
            {
                awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self._core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self._completedCount) == 1)
            {
                self._core.TrySetResult((false, default));
            }
        }

        public (bool, T) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return _core.GetResult(token);
        }

        public GDTaskStatus GetStatus(short token)
        {
            return _core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompleted(continuation, state, token);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _core.UnsafeGetStatus();
        }

        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    private sealed class WhenAnyPromise<T> : IGDTaskSource<(int, T)>
    {
        private int _completedCount;
        private GDTaskCompletionSourceCore<(int, T)> _core;

        public WhenAnyPromise(GDTask<T>[] tasks, int tasksLength)
        {
            if (tasksLength == 0)
            {
                throw new ArgumentException("The tasks argument contains no tasks.");
            }

            TaskTracker.TrackActiveTask(this, 3);

            for (int i = 0; i < tasksLength; i++)
            {
                GDTask<T>.Awaiter awaiter;
                try
                {
                    awaiter = tasks[i].GetAwaiter();
                }
                catch (Exception ex)
                {
                    _core.TrySetException(ex);
                    continue; // consume others.
                }

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuation(this, awaiter, i);
                }
                else
                {
                    awaiter.SourceOnCompleted(
                        state =>
                        {
                            using var t = (StateTuple<WhenAnyPromise<T>, GDTask<T>.Awaiter, int>)state;
                            TryInvokeContinuation(t.Item1, t.Item2, t.Item3);
                        },
                        StateTuple.Create(this, awaiter, i)
                    );
                }
            }
        }

        private static void TryInvokeContinuation(WhenAnyPromise<T> self, in GDTask<T>.Awaiter awaiter, int i)
        {
            T result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self._core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self._completedCount) == 1)
            {
                self._core.TrySetResult((i, result));
            }
        }

        public (int, T) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return _core.GetResult(token);
        }

        public GDTaskStatus GetStatus(short token)
        {
            return _core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompleted(continuation, state, token);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _core.UnsafeGetStatus();
        }

        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    private sealed class WhenAnyPromise : IGDTaskSource<int>
    {
        private int _completedCount;
        private GDTaskCompletionSourceCore<int> _core;

        public WhenAnyPromise(GDTask[] tasks, int tasksLength)
        {
            if (tasksLength is 0)
            {
                throw new ArgumentException("The tasks argument contains no tasks.");
            }

            TaskTracker.TrackActiveTask(this, 3);

            for (int i = 0; i < tasksLength; i++)
            {
                GDTask.Awaiter awaiter;
                try
                {
                    awaiter = tasks[i].GetAwaiter();
                }
                catch (Exception ex)
                {
                    _core.TrySetException(ex);
                    continue; // consume others.
                }

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuation(this, awaiter, i);
                }
                else
                {
                    awaiter.SourceOnCompleted(
                        state =>
                        {
                            using var t = (StateTuple<WhenAnyPromise, GDTask.Awaiter, int>)state;
                            TryInvokeContinuation(t.Item1, t.Item2, t.Item3);
                        },
                        StateTuple.Create(this, awaiter, i)
                    );
                }
            }
        }

        private static void TryInvokeContinuation(WhenAnyPromise self, in GDTask.Awaiter awaiter, int i)
        {
            try
            {
                awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self._core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self._completedCount) == 1)
            {
                self._core.TrySetResult(i);
            }
        }

        public int GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return _core.GetResult(token);
        }

        public GDTaskStatus GetStatus(short token)
        {
            return _core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompleted(continuation, state, token);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return _core.UnsafeGetStatus();
        }

        void IGDTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }
}
