using System;
using System.Runtime.CompilerServices;

namespace Fractural.Tasks.Internal;

// optimized version of Standard Queue<T>.
internal class MinimumQueue<T>
{
    private const int MinimumGrow = 4;
    private const int GrowFactor = 200;

    private T[] _array;
    private int _head;
    private int _tail;
    private int _size;

    public MinimumQueue(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _array = new T[capacity];
        _head = _tail = _size = 0;
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return _size; }
    }

    public T Peek()
    {
        if (_size == 0)
            ThrowForEmptyQueue();
        return _array[_head];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(T item)
    {
        if (_size == _array.Length)
        {
            Grow();
        }

        _array[_tail] = item;
        MoveNext(ref _tail);
        _size++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Dequeue()
    {
        if (_size == 0)
            ThrowForEmptyQueue();

        int head = _head;
        T[] array = _array;
        T removed = array[head];
        array[head] = default;
        MoveNext(ref _head);
        _size--;
        return removed;
    }

    private void Grow()
    {
        int newcapacity = (int)((long)_array.Length * (long)GrowFactor / 100);
        if (newcapacity < _array.Length + MinimumGrow)
        {
            newcapacity = _array.Length + MinimumGrow;
        }
        SetCapacity(newcapacity);
    }

    private void SetCapacity(int capacity)
    {
        T[] newarray = new T[capacity];
        if (_size > 0)
        {
            if (_head < _tail)
            {
                Array.Copy(_array, _head, newarray, 0, _size);
            }
            else
            {
                Array.Copy(_array, _head, newarray, 0, _array.Length - _head);
                Array.Copy(_array, 0, newarray, _array.Length - _head, _tail);
            }
        }

        _array = newarray;
        _head = 0;
        _tail = (_size == capacity) ? 0 : _size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MoveNext(ref int index)
    {
        int tmp = index + 1;
        if (tmp == _array.Length)
        {
            tmp = 0;
        }
        index = tmp;
    }

    private void ThrowForEmptyQueue()
    {
        throw new InvalidOperationException("EmptyQueue");
    }
}
