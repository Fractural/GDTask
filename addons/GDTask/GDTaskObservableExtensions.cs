using System;
using System.Threading;
using Fractural.Tasks.Internal;

namespace Fractural.Tasks;

public static class GDTaskObservableExtensions
{
    public static GDTask<T> ToGDTask<T>(this IObservable<T> source, bool useFirstValue = false, CancellationToken cancellationToken = default)
    {
        var promise = new GDTaskCompletionSource<T>();
        var disposable = new SingleAssignmentDisposable();

        IObserver<T> observer = useFirstValue
            ? new FirstValueToGDTaskObserver<T>(promise, disposable, cancellationToken)
            : new ToGDTaskObserver<T>(promise, disposable, cancellationToken);

        try
        {
            disposable.Disposable = source.Subscribe(observer);
        }
        catch (Exception ex)
        {
            promise.TrySetException(ex);
        }

        return promise.Task;
    }

    public static IObservable<T> ToObservable<T>(this GDTask<T> task)
    {
        if (task.Status.IsCompleted())
        {
            try
            {
                return new ReturnObservable<T>(task.GetAwaiter().GetResult());
            }
            catch (Exception ex)
            {
                return new ThrowObservable<T>(ex);
            }
        }

        var subject = new AsyncSubject<T>();
        Fire(subject, task).Forget();
        return subject;
    }

    /// <summary>
    /// Ideally returns IObservabl[Unit] is best but GDTask does not have Unit so return AsyncUnit instead.
    /// </summary>
    public static IObservable<AsyncUnit> ToObservable(this GDTask task)
    {
        if (task.Status.IsCompleted())
        {
            try
            {
                task.GetAwaiter().GetResult();
                return new ReturnObservable<AsyncUnit>(AsyncUnit.Default);
            }
            catch (Exception ex)
            {
                return new ThrowObservable<AsyncUnit>(ex);
            }
        }

        var subject = new AsyncSubject<AsyncUnit>();
        Fire(subject, task).Forget();
        return subject;
    }

    private static async GDTaskVoid Fire<T>(AsyncSubject<T> subject, GDTask<T> task)
    {
        T value;
        try
        {
            value = await task;
        }
        catch (Exception ex)
        {
            subject.OnError(ex);
            return;
        }

        subject.OnNext(value);
        subject.OnCompleted();
    }

    private static async GDTaskVoid Fire(AsyncSubject<AsyncUnit> subject, GDTask task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            subject.OnError(ex);
            return;
        }

        subject.OnNext(AsyncUnit.Default);
        subject.OnCompleted();
    }

    private class ToGDTaskObserver<T> : IObserver<T>
    {
        private static readonly Action<object> callback = OnCanceled;

        private readonly GDTaskCompletionSource<T> _promise;
        private readonly SingleAssignmentDisposable _disposable;
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenRegistration _registration;

        private bool _hasValue;
        private T _latestValue;

        public ToGDTaskObserver(GDTaskCompletionSource<T> promise, SingleAssignmentDisposable disposable, CancellationToken cancellationToken)
        {
            _promise = promise;
            _disposable = disposable;
            _cancellationToken = cancellationToken;

            if (_cancellationToken.CanBeCanceled)
            {
                _registration = _cancellationToken.RegisterWithoutCaptureExecutionContext(callback, this);
            }
        }

        private static void OnCanceled(object state)
        {
            var self = (ToGDTaskObserver<T>)state;
            self._disposable.Dispose();
            self._promise.TrySetCanceled(self._cancellationToken);
        }

        public void OnNext(T value)
        {
            _hasValue = true;
            _latestValue = value;
        }

        public void OnError(Exception error)
        {
            try
            {
                _promise.TrySetException(error);
            }
            finally
            {
                _registration.Dispose();
                _disposable.Dispose();
            }
        }

        public void OnCompleted()
        {
            try
            {
                if (_hasValue)
                {
                    _promise.TrySetResult(_latestValue);
                }
                else
                {
                    _promise.TrySetException(new InvalidOperationException("Sequence has no elements"));
                }
            }
            finally
            {
                _registration.Dispose();
                _disposable.Dispose();
            }
        }
    }

    private class FirstValueToGDTaskObserver<T> : IObserver<T>
    {
        private static readonly Action<object> _callback = OnCanceled;

        private readonly GDTaskCompletionSource<T> _promise;
        private readonly SingleAssignmentDisposable _disposable;
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenRegistration _registration;

        private bool _hasValue;

        public FirstValueToGDTaskObserver(
            GDTaskCompletionSource<T> promise,
            SingleAssignmentDisposable disposable,
            CancellationToken cancellationToken
        )
        {
            _promise = promise;
            _disposable = disposable;
            _cancellationToken = cancellationToken;

            if (_cancellationToken.CanBeCanceled)
            {
                _registration = _cancellationToken.RegisterWithoutCaptureExecutionContext(_callback, this);
            }
        }

        private static void OnCanceled(object state)
        {
            var self = (FirstValueToGDTaskObserver<T>)state;
            self._disposable.Dispose();
            self._promise.TrySetCanceled(self._cancellationToken);
        }

        public void OnNext(T value)
        {
            _hasValue = true;
            try
            {
                _promise.TrySetResult(value);
            }
            finally
            {
                _registration.Dispose();
                _disposable.Dispose();
            }
        }

        public void OnError(Exception error)
        {
            try
            {
                _promise.TrySetException(error);
            }
            finally
            {
                _registration.Dispose();
                _disposable.Dispose();
            }
        }

        public void OnCompleted()
        {
            try
            {
                if (!_hasValue)
                {
                    _promise.TrySetException(new InvalidOperationException("Sequence has no elements"));
                }
            }
            finally
            {
                _registration.Dispose();
                _disposable.Dispose();
            }
        }
    }

    private class ReturnObservable<T> : IObservable<T>
    {
        private readonly T _value;

        public ReturnObservable(T value)
        {
            _value = value;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(_value);
            observer.OnCompleted();
            return EmptyDisposable.Instance;
        }
    }

    private class ThrowObservable<T> : IObservable<T>
    {
        private readonly Exception _value;

        public ThrowObservable(Exception value)
        {
            _value = value;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnError(_value);
            return EmptyDisposable.Instance;
        }
    }
}
