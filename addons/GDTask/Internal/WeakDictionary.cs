using System;
using System.Collections.Generic;
using System.Threading;

namespace Fractural.Tasks.Internal;

// Add, Remove, Enumerate with sweep. All operations are thread safe(in spinlock).
internal class WeakDictionary<TKey, TValue>
    where TKey : class
{
    private Entry[] _buckets;
    private int _size;
    private SpinLock _gate; // mutable struct(not readonly)

    private readonly float _loadFactor;
    private readonly IEqualityComparer<TKey> _keyEqualityComparer;

    public WeakDictionary(int capacity = 4, float loadFactor = 0.75f, IEqualityComparer<TKey> keyComparer = null)
    {
        var tableSize = CalculateCapacity(capacity, loadFactor);
        _buckets = new Entry[tableSize];
        _loadFactor = loadFactor;
        _gate = new SpinLock(false);
        _keyEqualityComparer = keyComparer ?? EqualityComparer<TKey>.Default;
    }

    public bool TryAdd(TKey key, TValue value)
    {
        bool lockTaken = false;
        try
        {
            _gate.Enter(ref lockTaken);
            return TryAddInternal(key, value);
        }
        finally
        {
            if (lockTaken)
                _gate.Exit(false);
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        bool lockTaken = false;
        try
        {
            _gate.Enter(ref lockTaken);
            if (TryGetEntry(key, out _, out var entry))
            {
                value = entry.Value;
                return true;
            }

            value = default;
            return false;
        }
        finally
        {
            if (lockTaken)
                _gate.Exit(false);
        }
    }

    public bool TryRemove(TKey key)
    {
        bool lockTaken = false;
        try
        {
            _gate.Enter(ref lockTaken);
            if (TryGetEntry(key, out var hashIndex, out var entry))
            {
                Remove(hashIndex, entry);
                return true;
            }

            return false;
        }
        finally
        {
            if (lockTaken)
                _gate.Exit(false);
        }
    }

    private bool TryAddInternal(TKey key, TValue value)
    {
        var nextCapacity = CalculateCapacity(_size + 1, _loadFactor);

        TRY_ADD_AGAIN:
        if (_buckets.Length < nextCapacity)
        {
            // rehash
            var nextBucket = new Entry[nextCapacity];
            for (int i = 0; i < _buckets.Length; i++)
            {
                var e = _buckets[i];
                while (e is not null)
                {
                    AddToBuckets(nextBucket, key, e.Value, e.Hash);
                    e = e.Next;
                }
            }

            _buckets = nextBucket;
            goto TRY_ADD_AGAIN;
        }
        else
        {
            // add entry
            var successAdd = AddToBuckets(_buckets, key, value, _keyEqualityComparer.GetHashCode(key));
            if (successAdd)
                _size++;
            return successAdd;
        }
    }

    private bool AddToBuckets(Entry[] targetBuckets, TKey newKey, TValue value, int keyHash)
    {
        var h = keyHash;
        var hashIndex = h & (targetBuckets.Length - 1);

        TRY_ADD_AGAIN:
        if (targetBuckets[hashIndex] is null)
        {
            targetBuckets[hashIndex] = new Entry
            {
                Key = new WeakReference<TKey>(newKey, false),
                Value = value,
                Hash = h
            };

            return true;
        }
        else
        {
            // add to last.
            var entry = targetBuckets[hashIndex];
            while (entry is not null)
            {
                if (entry.Key.TryGetTarget(out var target))
                {
                    if (_keyEqualityComparer.Equals(newKey, target))
                    {
                        return false; // duplicate
                    }
                }
                else
                {
                    Remove(hashIndex, entry);
                    if (targetBuckets[hashIndex] is null)
                        goto TRY_ADD_AGAIN; // add new entry
                }

                if (entry.Next is not null)
                {
                    entry = entry.Next;
                }
                else
                {
                    // found last
                    entry.Next = new Entry
                    {
                        Key = new WeakReference<TKey>(newKey, false),
                        Value = value,
                        Hash = h
                    };
                    entry.Next.Prev = entry;
                }
            }

            return false;
        }
    }

    private bool TryGetEntry(TKey key, out int hashIndex, out Entry entry)
    {
        var table = _buckets;
        var hash = _keyEqualityComparer.GetHashCode(key);
        hashIndex = hash & table.Length - 1;
        entry = table[hashIndex];

        while (entry != null)
        {
            if (entry.Key.TryGetTarget(out var target))
            {
                if (_keyEqualityComparer.Equals(key, target))
                {
                    return true;
                }
            }
            else
            {
                // sweap
                Remove(hashIndex, entry);
            }

            entry = entry.Next;
        }

        return false;
    }

    private void Remove(int hashIndex, Entry entry)
    {
        if (entry.Prev is null && entry.Next is null)
        {
            _buckets[hashIndex] = null;
        }
        else
        {
            if (entry.Prev is null)
            {
                _buckets[hashIndex] = entry.Next;
            }
            if (entry.Prev is not null)
            {
                entry.Prev.Next = entry.Next;
            }
            if (entry.Next is not null)
            {
                entry.Next.Prev = entry.Prev;
            }
        }
        _size--;
    }

    public List<KeyValuePair<TKey, TValue>> ToList()
    {
        var list = new List<KeyValuePair<TKey, TValue>>(_size);
        ToList(ref list, false);
        return list;
    }

    // avoid allocate everytime.
    public int ToList(ref List<KeyValuePair<TKey, TValue>> list, bool clear = true)
    {
        if (clear)
        {
            list.Clear();
        }

        var listIndex = 0;

        bool lockTaken = false;
        try
        {
            for (int i = 0; i < _buckets.Length; i++)
            {
                var entry = _buckets[i];
                while (entry != null)
                {
                    if (entry.Key.TryGetTarget(out var target))
                    {
                        var item = new KeyValuePair<TKey, TValue>(target, entry.Value);
                        if (listIndex < list.Count)
                        {
                            list[listIndex++] = item;
                        }
                        else
                        {
                            list.Add(item);
                            listIndex++;
                        }
                    }
                    else
                    {
                        // sweap
                        Remove(i, entry);
                    }

                    entry = entry.Next;
                }
            }
        }
        finally
        {
            if (lockTaken)
                _gate.Exit(false);
        }

        return listIndex;
    }

    private static int CalculateCapacity(int collectionSize, float loadFactor)
    {
        var size = (int)(((float)collectionSize) / loadFactor);

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

    private class Entry
    {
        public WeakReference<TKey> Key;
        public TValue Value;
        public int Hash;
        public Entry Prev;
        public Entry Next;

        // debug only
        public override string ToString()
        {
            if (Key.TryGetTarget(out var target))
            {
                return target + "(" + Count() + ")";
            }
            else
            {
                return "(Dead)";
            }
        }

        private int Count()
        {
            var count = 1;
            var n = this;
            while (n.Next is not null)
            {
                count++;
                n = n.Next;
            }
            return count;
        }
    }
}
