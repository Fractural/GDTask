#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using GDTask.Internal;

namespace GDTask
{
    public static partial class GDTaskExtensions
    {
        /// <summary>
        /// Convert Task[T] -> GDTask[T].
        /// </summary>
        public static GDTask<T> AsGDTask<T>(this Task<T> task, bool useCurrentSynchronizationContext = true)
        {
            var promise = new GDTaskCompletionSource<T>();

            task.ContinueWith((x, state) =>
            {
                var p = (GDTaskCompletionSource<T>)state;

                switch (x.Status)
                {
                    case TaskStatus.Canceled:
                        p.TrySetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        p.TrySetException(x.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                        p.TrySetResult(x.Result);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }, promise, useCurrentSynchronizationContext ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current);

            return promise.Task;
        }

        /// <summary>
        /// Convert Task -> GDTask.
        /// </summary>
        public static GDTask AsGDTask(this Task task, bool useCurrentSynchronizationContext = true)
        {
            var promise = new GDTaskCompletionSource();

            task.ContinueWith((x, state) =>
            {
                var p = (GDTaskCompletionSource)state;

                switch (x.Status)
                {
                    case TaskStatus.Canceled:
                        p.TrySetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        p.TrySetException(x.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                        p.TrySetResult();
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }, promise, useCurrentSynchronizationContext ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current);

            return promise.Task;
        }

        public static Task<T> AsTask<T>(this GDTask<T> task)
        {
            try
            {
                GDTask<T>.Awaiter awaiter;
                try
                {
                    awaiter = task.GetAwaiter();
                }
                catch (Exception ex)
                {
                    return Task.FromException<T>(ex);
                }

                if (awaiter.IsCompleted)
                {
                    try
                    {
                        var result = awaiter.GetResult();
                        return Task.FromResult(result);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromException<T>(ex);
                    }
                }

                var tcs = new TaskCompletionSource<T>();

                awaiter.SourceOnCompleted(state =>
                {
                    using (var tuple = (StateTuple<TaskCompletionSource<T>, GDTask<T>.Awaiter>)state)
                    {
                        var (inTcs, inAwaiter) = tuple;
                        try
                        {
                            var result = inAwaiter.GetResult();
                            inTcs.SetResult(result);
                        }
                        catch (Exception ex)
                        {
                            inTcs.SetException(ex);
                        }
                    }
                }, StateTuple.Create(tcs, awaiter));

                return tcs.Task;
            }
            catch (Exception ex)
            {
                return Task.FromException<T>(ex);
            }
        }

        public static Task AsTask(this GDTask task)
        {
            try
            {
                GDTask.Awaiter awaiter;
                try
                {
                    awaiter = task.GetAwaiter();
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }

                if (awaiter.IsCompleted)
                {
                    try
                    {
                        awaiter.GetResult(); // check token valid on Succeeded
                        return Task.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        return Task.FromException(ex);
                    }
                }

                var tcs = new TaskCompletionSource<object>();

                awaiter.SourceOnCompleted(state =>
                {
                    using (var tuple = (StateTuple<TaskCompletionSource<object>, GDTask.Awaiter>)state)
                    {
                        var (inTcs, inAwaiter) = tuple;
                        try
                        {
                            inAwaiter.GetResult();
                            inTcs.SetResult(null);
                        }
                        catch (Exception ex)
                        {
                            inTcs.SetException(ex);
                        }
                    }
                }, StateTuple.Create(tcs, awaiter));

                return tcs.Task;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        public static AsyncLazy ToAsyncLazy(this GDTask task)
        {
            return new AsyncLazy(task);
        }

        public static AsyncLazy<T> ToAsyncLazy<T>(this GDTask<T> task)
        {
            return new AsyncLazy<T>(task);
        }

        /// <summary>
        /// Ignore task result when cancel raised first.
        /// </summary>
        public static GDTask AttachExternalCancellation(this GDTask task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return GDTask.FromCanceled(cancellationToken);
            }

            if (task.Status.IsCompleted())
            {
                return task;
            }

            return new GDTask(new AttachExternalCancellationSource(task, cancellationToken), 0);
        }

        /// <summary>
        /// Ignore task result when cancel raised first.
        /// </summary>
        public static GDTask<T> AttachExternalCancellation<T>(this GDTask<T> task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return GDTask.FromCanceled<T>(cancellationToken);
            }

            if (task.Status.IsCompleted())
            {
                return task;
            }

            return new GDTask<T>(new AttachExternalCancellationSource<T>(task, cancellationToken), 0);
        }

        sealed class AttachExternalCancellationSource : IGDTaskSource
        {
            static readonly Action<object> cancellationCallbackDelegate = CancellationCallback;

            CancellationToken cancellationToken;
            CancellationTokenRegistration tokenRegistration;
            GDTaskCompletionSourceCore<AsyncUnit> core;

            public AttachExternalCancellationSource(GDTask task, CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
                this.tokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(cancellationCallbackDelegate, this);
                RunTask(task).Forget();
            }

            async GDTaskVoid RunTask(GDTask task)
            {
                try
                {
                    await task;
                    core.TrySetResult(AsyncUnit.Default);
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                }
                finally
                {
                    tokenRegistration.Dispose();
                }
            }

            static void CancellationCallback(object state)
            {
                var self = (AttachExternalCancellationSource)state;
                self.core.TrySetCanceled(self.cancellationToken);
            }

            public void GetResult(short token)
            {
                core.GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }
        }

        sealed class AttachExternalCancellationSource<T> : IGDTaskSource<T>
        {
            static readonly Action<object> cancellationCallbackDelegate = CancellationCallback;

            CancellationToken cancellationToken;
            CancellationTokenRegistration tokenRegistration;
            GDTaskCompletionSourceCore<T> core;

            public AttachExternalCancellationSource(GDTask<T> task, CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
                this.tokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(cancellationCallbackDelegate, this);
                RunTask(task).Forget();
            }

            async GDTaskVoid RunTask(GDTask<T> task)
            {
                try
                {
                    core.TrySetResult(await task);
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                }
                finally
                {
                    tokenRegistration.Dispose();
                }
            }

            static void CancellationCallback(object state)
            {
                var self = (AttachExternalCancellationSource<T>)state;
                self.core.TrySetCanceled(self.cancellationToken);
            }

            void IGDTaskSource.GetResult(short token)
            {
                core.GetResult(token);
            }

            public T GetResult(short token)
            {
                return core.GetResult(token);
            }

            public GDTaskStatus GetStatus(short token)
            {
                return core.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                core.OnCompleted(continuation, state, token);
            }

            public GDTaskStatus UnsafeGetStatus()
            {
                return core.UnsafeGetStatus();
            }
        }

#if UNITY_2018_3_OR_NEWER

        public static IEnumerator ToCoroutine<T>(this GDTask<T> task, Action<T> resultHandler = null, Action<Exception> exceptionHandler = null)
        {
            return new ToCoroutineEnumerator<T>(task, resultHandler, exceptionHandler);
        }

        public static IEnumerator ToCoroutine(this GDTask task, Action<Exception> exceptionHandler = null)
        {
            return new ToCoroutineEnumerator(task, exceptionHandler);
        }

        public static async GDTask Timeout(this GDTask task, TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Update, CancellationTokenSource taskCancellationTokenSource = null)
        {
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckTiming, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            int winArgIndex;
            bool taskResultIsCanceled;
            try
            {
                (winArgIndex, taskResultIsCanceled, _) = await GDTask.WhenAny(task.SuppressCancellationThrow(), timeoutTask);
            }
            catch
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
                throw;
            }

            // timeout
            if (winArgIndex == 1)
            {
                if (taskCancellationTokenSource != null)
                {
                    taskCancellationTokenSource.Cancel();
                    taskCancellationTokenSource.Dispose();
                }

                throw new TimeoutException("Exceed Timeout:" + timeout);
            }
            else
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
            }

            if (taskResultIsCanceled)
            {
                Error.ThrowOperationCanceledException();
            }
        }

        public static async GDTask<T> Timeout<T>(this GDTask<T> task, TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Update, CancellationTokenSource taskCancellationTokenSource = null)
        {
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckTiming, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            int winArgIndex;
            (bool IsCanceled, T Result) taskResult;
            try
            {
                (winArgIndex, taskResult, _) = await GDTask.WhenAny(task.SuppressCancellationThrow(), timeoutTask);
            }
            catch
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
                throw;
            }

            // timeout
            if (winArgIndex == 1)
            {
                if (taskCancellationTokenSource != null)
                {
                    taskCancellationTokenSource.Cancel();
                    taskCancellationTokenSource.Dispose();
                }

                throw new TimeoutException("Exceed Timeout:" + timeout);
            }
            else
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
            }

            if (taskResult.IsCanceled)
            {
                Error.ThrowOperationCanceledException();
            }

            return taskResult.Result;
        }

        /// <summary>
        /// Timeout with suppress OperationCanceledException. Returns (bool, IsCacneled).
        /// </summary>
        public static async GDTask<bool> TimeoutWithoutException(this GDTask task, TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Update, CancellationTokenSource taskCancellationTokenSource = null)
        {
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckTiming, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            int winArgIndex;
            bool taskResultIsCanceled;
            try
            {
                (winArgIndex, taskResultIsCanceled, _) = await GDTask.WhenAny(task.SuppressCancellationThrow(), timeoutTask);
            }
            catch
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
                return true;
            }

            // timeout
            if (winArgIndex == 1)
            {
                if (taskCancellationTokenSource != null)
                {
                    taskCancellationTokenSource.Cancel();
                    taskCancellationTokenSource.Dispose();
                }

                return true;
            }
            else
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
            }

            if (taskResultIsCanceled)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Timeout with suppress OperationCanceledException. Returns (bool IsTimeout, T Result).
        /// </summary>
        public static async GDTask<(bool IsTimeout, T Result)> TimeoutWithoutException<T>(this GDTask<T> task, TimeSpan timeout, DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming timeoutCheckTiming = PlayerLoopTiming.Update, CancellationTokenSource taskCancellationTokenSource = null)
        {
            var delayCancellationTokenSource = new CancellationTokenSource();
            var timeoutTask = GDTask.Delay(timeout, delayType, timeoutCheckTiming, delayCancellationTokenSource.Token).SuppressCancellationThrow();

            int winArgIndex;
            (bool IsCanceled, T Result) taskResult;
            try
            {
                (winArgIndex, taskResult, _) = await GDTask.WhenAny(task.SuppressCancellationThrow(), timeoutTask);
            }
            catch
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
                return (true, default);
            }

            // timeout
            if (winArgIndex == 1)
            {
                if (taskCancellationTokenSource != null)
                {
                    taskCancellationTokenSource.Cancel();
                    taskCancellationTokenSource.Dispose();
                }

                return (true, default);
            }
            else
            {
                delayCancellationTokenSource.Cancel();
                delayCancellationTokenSource.Dispose();
            }

            if (taskResult.IsCanceled)
            {
                return (true, default);
            }

            return (false, taskResult.Result);
        }

#endif

        public static void Forget(this GDTask task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    GDTaskScheduler.PublishUnobservedTaskException(ex);
                }
            }
            else
            {
                awaiter.SourceOnCompleted(state =>
                {
                    using (var t = (StateTuple<GDTask.Awaiter>)state)
                    {
                        try
                        {
                            t.Item1.GetResult();
                        }
                        catch (Exception ex)
                        {
                            GDTaskScheduler.PublishUnobservedTaskException(ex);
                        }
                    }
                }, StateTuple.Create(awaiter));
            }
        }

        public static void Forget(this GDTask task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread = true)
        {
            if (exceptionHandler == null)
            {
                Forget(task);
            }
            else
            {
                ForgetCoreWithCatch(task, exceptionHandler, handleExceptionOnMainThread).Forget();
            }
        }

        static async GDTaskVoid ForgetCoreWithCatch(GDTask task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                try
                {
                    if (handleExceptionOnMainThread)
                    {
#if UNITY_2018_3_OR_NEWER
                        await GDTask.SwitchToMainThread();
#endif
                    }
                    exceptionHandler(ex);
                }
                catch (Exception ex2)
                {
                    GDTaskScheduler.PublishUnobservedTaskException(ex2);
                }
            }
        }

        public static void Forget<T>(this GDTask<T> task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    GDTaskScheduler.PublishUnobservedTaskException(ex);
                }
            }
            else
            {
                awaiter.SourceOnCompleted(state =>
                {
                    using (var t = (StateTuple<GDTask<T>.Awaiter>)state)
                    {
                        try
                        {
                            t.Item1.GetResult();
                        }
                        catch (Exception ex)
                        {
                            GDTaskScheduler.PublishUnobservedTaskException(ex);
                        }
                    }
                }, StateTuple.Create(awaiter));
            }
        }

        public static void Forget<T>(this GDTask<T> task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread = true)
        {
            if (exceptionHandler == null)
            {
                task.Forget();
            }
            else
            {
                ForgetCoreWithCatch(task, exceptionHandler, handleExceptionOnMainThread).Forget();
            }
        }

        static async GDTaskVoid ForgetCoreWithCatch<T>(GDTask<T> task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                try
                {
                    if (handleExceptionOnMainThread)
                    {
#if UNITY_2018_3_OR_NEWER
                        await GDTask.SwitchToMainThread();
#endif
                    }
                    exceptionHandler(ex);
                }
                catch (Exception ex2)
                {
                    GDTaskScheduler.PublishUnobservedTaskException(ex2);
                }
            }
        }

        public static async GDTask ContinueWith<T>(this GDTask<T> task, Action<T> continuationFunction)
        {
            continuationFunction(await task);
        }

        public static async GDTask ContinueWith<T>(this GDTask<T> task, Func<T, GDTask> continuationFunction)
        {
            await continuationFunction(await task);
        }

        public static async GDTask<TR> ContinueWith<T, TR>(this GDTask<T> task, Func<T, TR> continuationFunction)
        {
            return continuationFunction(await task);
        }

        public static async GDTask<TR> ContinueWith<T, TR>(this GDTask<T> task, Func<T, GDTask<TR>> continuationFunction)
        {
            return await continuationFunction(await task);
        }

        public static async GDTask ContinueWith(this GDTask task, Action continuationFunction)
        {
            await task;
            continuationFunction();
        }

        public static async GDTask ContinueWith(this GDTask task, Func<GDTask> continuationFunction)
        {
            await task;
            await continuationFunction();
        }

        public static async GDTask<T> ContinueWith<T>(this GDTask task, Func<T> continuationFunction)
        {
            await task;
            return continuationFunction();
        }

        public static async GDTask<T> ContinueWith<T>(this GDTask task, Func<GDTask<T>> continuationFunction)
        {
            await task;
            return await continuationFunction();
        }

        public static async GDTask<T> Unwrap<T>(this GDTask<GDTask<T>> task)
        {
            return await await task;
        }

        public static async GDTask Unwrap(this GDTask<GDTask> task)
        {
            await await task;
        }

        public static async GDTask<T> Unwrap<T>(this Task<GDTask<T>> task)
        {
            return await await task;
        }

        public static async GDTask<T> Unwrap<T>(this Task<GDTask<T>> task, bool continueOnCapturedContext)
        {
            return await await task.ConfigureAwait(continueOnCapturedContext);
        }

        public static async GDTask Unwrap(this Task<GDTask> task)
        {
            await await task;
        }

        public static async GDTask Unwrap(this Task<GDTask> task, bool continueOnCapturedContext)
        {
            await await task.ConfigureAwait(continueOnCapturedContext);
        }

        public static async GDTask<T> Unwrap<T>(this GDTask<Task<T>> task)
        {
            return await await task;
        }

        public static async GDTask<T> Unwrap<T>(this GDTask<Task<T>> task, bool continueOnCapturedContext)
        {
            return await (await task).ConfigureAwait(continueOnCapturedContext);
        }

        public static async GDTask Unwrap(this GDTask<Task> task)
        {
            await await task;
        }

        public static async GDTask Unwrap(this GDTask<Task> task, bool continueOnCapturedContext)
        {
            await (await task).ConfigureAwait(continueOnCapturedContext);
        }

#if UNITY_2018_3_OR_NEWER

        sealed class ToCoroutineEnumerator : IEnumerator
        {
            bool completed;
            GDTask task;
            Action<Exception> exceptionHandler = null;
            bool isStarted = false;
            ExceptionDispatchInfo exception;

            public ToCoroutineEnumerator(GDTask task, Action<Exception> exceptionHandler)
            {
                completed = false;
                this.exceptionHandler = exceptionHandler;
                this.task = task;
            }

            async GDTaskVoid RunTask(GDTask task)
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null)
                    {
                        exceptionHandler(ex);
                    }
                    else
                    {
                        this.exception = ExceptionDispatchInfo.Capture(ex);
                    }
                }
                finally
                {
                    completed = true;
                }
            }

            public object Current => null;

            public bool MoveNext()
            {
                if (!isStarted)
                {
                    isStarted = true;
                    RunTask(task).Forget();
                }

                if (exception != null)
                {
                    exception.Throw();
                    return false;
                }

                return !completed;
            }

            void IEnumerator.Reset()
            {
            }
        }

        sealed class ToCoroutineEnumerator<T> : IEnumerator
        {
            bool completed;
            Action<T> resultHandler = null;
            Action<Exception> exceptionHandler = null;
            bool isStarted = false;
            GDTask<T> task;
            object current = null;
            ExceptionDispatchInfo exception;

            public ToCoroutineEnumerator(GDTask<T> task, Action<T> resultHandler, Action<Exception> exceptionHandler)
            {
                completed = false;
                this.task = task;
                this.resultHandler = resultHandler;
                this.exceptionHandler = exceptionHandler;
            }

            async GDTaskVoid RunTask(GDTask<T> task)
            {
                try
                {
                    var value = await task;
                    current = value; // boxed if T is struct...
                    if (resultHandler != null)
                    {
                        resultHandler(value);
                    }
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null)
                    {
                        exceptionHandler(ex);
                    }
                    else
                    {
                        this.exception = ExceptionDispatchInfo.Capture(ex);
                    }
                }
                finally
                {
                    completed = true;
                }
            }

            public object Current => current;

            public bool MoveNext()
            {
                if (!isStarted)
                {
                    isStarted = true;
                    RunTask(task).Forget();
                }

                if (exception != null)
                {
                    exception.Throw();
                    return false;
                }

                return !completed;
            }

            void IEnumerator.Reset()
            {
            }
        }

#endif
    }
}

