using System;
using System.Threading;

namespace Fractural.Tasks.Internal;

// Same interface as System.Buffers.ArrayPool<T> but only provides Shared.

internal sealed class ArrayPool<T>
{
    // Same size as System.Buffers.DefaultArrayPool<T>
    private const int DefaultMaxNumberOfArraysPerBucket = 50;

    private static readonly T[] EmptyArray = [];

    public static readonly ArrayPool<T> Shared = new();

    private readonly MinimumQueue<T[]>[] _buckets;
    private readonly SpinLock[] _locks;

    private ArrayPool()
    {
        // see: GetQueueIndex
        _buckets = new MinimumQueue<T[]>[18];
        _locks = new SpinLock[18];
        for (int i = 0; i < _buckets.Length; i++)
        {
            _buckets[i] = new MinimumQueue<T[]>(4);
            _locks[i] = new SpinLock(false);
        }
    }

    public T[] Rent(int minimumLength)
    {
        if (minimumLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumLength));
        }
        else if (minimumLength == 0)
        {
            return EmptyArray;
        }

        var size = CalculateSize(minimumLength);
        var index = GetQueueIndex(size);
        if (index != -1)
        {
            var q = _buckets[index];
            var lockTaken = false;
            try
            {
                _locks[index].Enter(ref lockTaken);

                if (q.Count != 0)
                {
                    return q.Dequeue();
                }
            }
            finally
            {
                if (lockTaken)
                    _locks[index].Exit(false);
            }
        }

        return new T[size];
    }

    public void Return(T[] array, bool clearArray = false)
    {
        if (array is null || array.Length == 0)
        {
            return;
        }

        var index = GetQueueIndex(array.Length);
        if (index != -1)
        {
            if (clearArray)
            {
                Array.Clear(array, 0, array.Length);
            }

            var q = _buckets[index];
            var lockTaken = false;

            try
            {
                _locks[index].Enter(ref lockTaken);

                if (q.Count > DefaultMaxNumberOfArraysPerBucket)
                {
                    return;
                }

                q.Enqueue(array);
            }
            finally
            {
                if (lockTaken)
                    _locks[index].Exit(false);
            }
        }
    }

    private static int CalculateSize(int size)
    {
        size--;
        size |= size >> 1;
        size |= size >> 2;
        size |= size >> 4;
        size |= size >> 8;
        size |= size >> 16;
        size += 1;

        if (size < 8)
        {
            size = 8;
        }

        return size;
    }

    private static int GetQueueIndex(int size)
    {
        return size switch
        {
            8 => 0,
            16 => 1,
            32 => 2,
            64 => 3,
            128 => 4,
            256 => 5,
            512 => 6,
            1024 => 7,
            2048 => 8,
            4096 => 9,
            8192 => 10,
            16384 => 11,
            32768 => 12,
            65536 => 13,
            131072 => 14,
            262144 => 15,
            524288 => 16,
            1048576 => 17, // max array length
            _ => -1,
        };
    }
}
