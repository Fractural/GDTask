using System;
using System.Collections.Generic;
using Fractural.Tasks.Internal;

namespace Fractural.Tasks;

/// <summary>
/// Lightweight IProgress[T] factory.
/// </summary>
public static class Progress
{
    public static IProgress<T> Create<T>(Action<T> handler)
    {
        if (handler is null)
            return NullProgress<T>.Instance;
        return new AnonymousProgress<T>(handler);
    }

    public static IProgress<T> CreateOnlyValueChanged<T>(Action<T> handler, IEqualityComparer<T> comparer = null)
    {
        if (handler is null)
            return NullProgress<T>.Instance;
        return new OnlyValueChangedProgress<T>(handler, comparer ?? GodotEqualityComparer.GetDefault<T>());
    }

    private sealed class NullProgress<T> : IProgress<T>
    {
        public static readonly IProgress<T> Instance = new NullProgress<T>();

        private NullProgress() { }

        public void Report(T value) { }
    }

    private sealed class AnonymousProgress<T> : IProgress<T>
    {
        private readonly Action<T> action;

        public AnonymousProgress(Action<T> action)
        {
            this.action = action;
        }

        public void Report(T value)
        {
            action(value);
        }
    }

    private sealed class OnlyValueChangedProgress<T> : IProgress<T>
    {
        private readonly Action<T> _action;
        private readonly IEqualityComparer<T> _comparer;
        private bool _isFirstCall;
        private T _latestValue;

        public OnlyValueChangedProgress(Action<T> action, IEqualityComparer<T> comparer)
        {
            _action = action;
            _comparer = comparer;
            _isFirstCall = true;
        }

        public void Report(T value)
        {
            if (_isFirstCall)
            {
                _isFirstCall = false;
            }
            else if (_comparer.Equals(value, _latestValue))
            {
                return;
            }

            _latestValue = value;
            _action(value);
        }
    }
}
