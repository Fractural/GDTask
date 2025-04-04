using System;
using System.Runtime.ExceptionServices;

namespace Fractural.Tasks.Internal;

// Bridges for Rx.

internal class EmptyDisposable : IDisposable
{
    public static EmptyDisposable Instance = new();

    private EmptyDisposable() { }

    public void Dispose() { }
}

internal sealed class SingleAssignmentDisposable : IDisposable
{
    private readonly object _gate = new();
    private IDisposable _current;
    private bool _disposed;

    public bool IsDisposed
    {
        get
        {
            lock (_gate)
            {
                return _disposed;
            }
        }
    }

    public IDisposable Disposable
    {
        get { return _current; }
        set
        {
            var old = default(IDisposable);
            bool alreadyDisposed;
            lock (_gate)
            {
                alreadyDisposed = _disposed;
                old = _current;
                if (!alreadyDisposed)
                {
                    if (value is null)
                        return;
                    _current = value;
                }
            }

            if (alreadyDisposed && value is not null)
            {
                value.Dispose();
                return;
            }

            if (old is not null)
                throw new InvalidOperationException("Disposable is already set");
        }
    }

    public void Dispose()
    {
        IDisposable old = null;

        lock (_gate)
        {
            if (!_disposed)
            {
                _disposed = true;
                old = _current;
                _current = null;
            }
        }

        old?.Dispose();
    }
}

internal sealed class AsyncSubject<T> : IObservable<T>, IObserver<T>
{
    private object _observerLock = new();

    private T _lastValue;
    private bool _hasValue;
    private bool _isStopped;
    private bool _isDisposed;
    private Exception _lastError;
    private IObserver<T> _outObserver = EmptyObserver<T>.Instance;

    public T Value
    {
        get
        {
            ThrowIfDisposed();
            if (!_isStopped)
                throw new InvalidOperationException("AsyncSubject is not completed yet");
            if (_lastError is not null)
                ExceptionDispatchInfo.Capture(_lastError).Throw();
            return _lastValue;
        }
    }

    public bool HasObservers
    {
        get { return _outObserver is not EmptyObserver<T> && !_isStopped && !_isDisposed; }
    }

    public bool IsCompleted
    {
        get { return _isStopped; }
    }

    public void OnCompleted()
    {
        IObserver<T> old;
        T value;
        bool hasValue;
        lock (_observerLock)
        {
            ThrowIfDisposed();
            if (_isStopped)
                return;

            old = _outObserver;
            _outObserver = EmptyObserver<T>.Instance;
            _isStopped = true;
            value = _lastValue;
            hasValue = _hasValue;
        }

        if (hasValue)
        {
            old.OnNext(value);
            old.OnCompleted();
        }
        else
        {
            old.OnCompleted();
        }
    }

    public void OnError(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        IObserver<T> old;
        lock (_observerLock)
        {
            ThrowIfDisposed();
            if (_isStopped)
                return;

            old = _outObserver;
            _outObserver = EmptyObserver<T>.Instance;
            _isStopped = true;
            _lastError = error;
        }

        old.OnError(error);
    }

    public void OnNext(T value)
    {
        lock (_observerLock)
        {
            ThrowIfDisposed();
            if (_isStopped)
                return;

            _hasValue = true;
            _lastValue = value;
        }
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        Exception ex = default;
        T value = default;
        var hasValue = false;

        lock (_observerLock)
        {
            ThrowIfDisposed();
            if (!_isStopped)
            {
                if (_outObserver is ListObserver<T> listObserver)
                {
                    _outObserver = listObserver.Add(observer);
                }
                else
                {
                    var current = _outObserver;
                    if (current is EmptyObserver<T>)
                    {
                        _outObserver = observer;
                    }
                    else
                    {
                        _outObserver = new ListObserver<T>(new ImmutableList<IObserver<T>>([current, observer]));
                    }
                }

                return new Subscription(this, observer);
            }

            ex = _lastError;
            value = _lastValue;
            hasValue = _hasValue;
        }

        if (ex is not null)
        {
            observer.OnError(ex);
        }
        else if (hasValue)
        {
            observer.OnNext(value);
            observer.OnCompleted();
        }
        else
        {
            observer.OnCompleted();
        }

        return EmptyDisposable.Instance;
    }

    public void Dispose()
    {
        lock (_observerLock)
        {
            _isDisposed = true;
            _outObserver = DisposedObserver<T>.Instance;
            _lastError = null;
            _lastValue = default;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    private class Subscription : IDisposable
    {
        private readonly object _gate = new();
        private AsyncSubject<T> _parent;
        private IObserver<T> _unsubscribeTarget;

        public Subscription(AsyncSubject<T> parent, IObserver<T> unsubscribeTarget)
        {
            _parent = parent;
            _unsubscribeTarget = unsubscribeTarget;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_parent is not null)
                {
                    lock (_parent._observerLock)
                    {
                        if (_parent._outObserver is ListObserver<T> listObserver)
                        {
                            _parent._outObserver = listObserver.Remove(_unsubscribeTarget);
                        }
                        else
                        {
                            _parent._outObserver = EmptyObserver<T>.Instance;
                        }

                        _unsubscribeTarget = null;
                        _parent = null;
                    }
                }
            }
        }
    }
}

internal class ListObserver<T> : IObserver<T>
{
    private readonly ImmutableList<IObserver<T>> _observers;

    public ListObserver(ImmutableList<IObserver<T>> observers)
    {
        _observers = observers;
    }

    public void OnCompleted()
    {
        var targetObservers = _observers.Data;
        for (int i = 0; i < targetObservers.Length; i++)
        {
            targetObservers[i].OnCompleted();
        }
    }

    public void OnError(Exception error)
    {
        var targetObservers = _observers.Data;
        for (int i = 0; i < targetObservers.Length; i++)
        {
            targetObservers[i].OnError(error);
        }
    }

    public void OnNext(T value)
    {
        var targetObservers = _observers.Data;
        for (int i = 0; i < targetObservers.Length; i++)
        {
            targetObservers[i].OnNext(value);
        }
    }

    internal IObserver<T> Add(IObserver<T> observer)
    {
        return new ListObserver<T>(_observers.Add(observer));
    }

    internal IObserver<T> Remove(IObserver<T> observer)
    {
        var i = Array.IndexOf(_observers.Data, observer);
        if (i < 0)
            return this;

        if (_observers.Data.Length == 2)
        {
            return _observers.Data[1 - i];
        }
        else
        {
            return new ListObserver<T>(_observers.Remove(observer));
        }
    }
}

internal class EmptyObserver<T> : IObserver<T>
{
    public static readonly EmptyObserver<T> Instance = new();

    private EmptyObserver() { }

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    public void OnNext(T value) { }
}

internal class ThrowObserver<T> : IObserver<T>
{
    public static readonly ThrowObserver<T> Instance = new();

    private ThrowObserver() { }

    public void OnCompleted() { }

    public void OnError(Exception error)
    {
        ExceptionDispatchInfo.Capture(error).Throw();
    }

    public void OnNext(T value) { }
}

internal class DisposedObserver<T> : IObserver<T>
{
    public static readonly DisposedObserver<T> Instance = new DisposedObserver<T>();

    private DisposedObserver() { }

    public void OnCompleted()
    {
        throw new ObjectDisposedException(string.Empty);
    }

    public void OnError(Exception error)
    {
        throw new ObjectDisposedException(string.Empty);
    }

    public void OnNext(T value)
    {
        throw new ObjectDisposedException(string.Empty);
    }
}

internal class ImmutableList<T>
{
    public static readonly ImmutableList<T> Empty = new();

    private T[] _data;

    public T[] Data
    {
        get { return _data; }
    }

    private ImmutableList()
    {
        _data = [];
    }

    public ImmutableList(T[] data)
    {
        _data = data;
    }

    public ImmutableList<T> Add(T value)
    {
        var newData = new T[_data.Length + 1];
        Array.Copy(_data, newData, _data.Length);
        newData[_data.Length] = value;
        return new ImmutableList<T>(newData);
    }

    public ImmutableList<T> Remove(T value)
    {
        var i = IndexOf(value);
        if (i < 0)
            return this;

        var length = _data.Length;
        if (length == 1)
            return Empty;

        var newData = new T[length - 1];

        Array.Copy(_data, 0, newData, 0, i);
        Array.Copy(_data, i + 1, newData, i, length - i - 1);

        return new ImmutableList<T>(newData);
    }

    public int IndexOf(T value)
    {
        for (var i = 0; i < _data.Length; ++i)
        {
            // ImmutableList only use for IObserver(no worry for boxed)
            if (object.Equals(_data[i], value))
                return i;
        }
        return -1;
    }
}
