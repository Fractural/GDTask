using System;
using System.Threading;
using Godot;

namespace Fractural.Tasks;

public interface ITriggerHandler<T>
{
    void OnNext(T value);
    void OnError(Exception ex);
    void OnCompleted();
    void OnCanceled(CancellationToken cancellationToken);

    // set/get from TriggerEvent<T>
    ITriggerHandler<T> Previous { get; set; }
    ITriggerHandler<T> Next { get; set; }
}

// be careful to use, itself is struct.
public struct TriggerEvent<T>
{
    private ITriggerHandler<T> _head; // head.prev is last
    private ITriggerHandler<T> _iteratingHead;

    private bool _preserveRemoveSelf;
    private ITriggerHandler<T> _iteratingNode;

    private void LogError(Exception ex)
    {
        GD.PrintErr(ex);
    }

    public void SetResult(T value)
    {
        if (_iteratingNode is not null)
        {
            throw new InvalidOperationException("Can not trigger itself in iterating.");
        }

        var head = _head;
        while (head is not null)
        {
            _iteratingNode = head;

            try
            {
                head.OnNext(value);
            }
            catch (Exception ex)
            {
                LogError(ex);
                Remove(head);
            }

            if (_preserveRemoveSelf)
            {
                _preserveRemoveSelf = false;
                _iteratingNode = null;
                var next = head.Next;
                Remove(head);
                head = next;
            }
            else
            {
                head = head.Next;
            }
        }

        _iteratingNode = null;
        if (_iteratingHead is not null)
        {
            Add(_iteratingHead);
            _iteratingHead = null;
        }
    }

    public void SetCanceled(CancellationToken cancellationToken)
    {
        if (_iteratingNode is not null)
        {
            throw new InvalidOperationException("Can not trigger itself in iterating.");
        }

        var head = _head;
        while (head is not null)
        {
            _iteratingNode = head;
            try
            {
                head.OnCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            _preserveRemoveSelf = false;
            _iteratingNode = null;
            var next = head.Next;
            Remove(head);
            head = next;
        }

        _iteratingNode = null;
        if (_iteratingHead is not null)
        {
            Add(_iteratingHead);
            _iteratingHead = null;
        }
    }

    public void SetCompleted()
    {
        if (_iteratingNode is not null)
        {
            throw new InvalidOperationException("Can not trigger itself in iterating.");
        }

        var head = _head;
        while (head is not null)
        {
            _iteratingNode = head;
            try
            {
                head.OnCompleted();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            _preserveRemoveSelf = false;
            _iteratingNode = null;
            var next = head.Next;
            Remove(head);
            head = next;
        }

        _iteratingNode = null;
        if (_iteratingHead is not null)
        {
            Add(_iteratingHead);
            _iteratingHead = null;
        }
    }

    public void SetError(Exception exception)
    {
        if (_iteratingNode is not null)
        {
            throw new InvalidOperationException("Can not trigger itself in iterating.");
        }

        var head = _head;
        while (head is not null)
        {
            _iteratingNode = head;
            try
            {
                head.OnError(exception);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            _preserveRemoveSelf = false;
            _iteratingNode = null;
            var next = head.Next;
            Remove(head);
            head = next;
        }

        _iteratingNode = null;
        if (_iteratingHead is not null)
        {
            Add(_iteratingHead);
            _iteratingHead = null;
        }
    }

    public void Add(ITriggerHandler<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        // zero node.
        if (_head is null)
        {
            _head = handler;
            return;
        }

        if (_iteratingNode is not null)
        {
            if (_iteratingHead is null)
            {
                _iteratingHead = handler;
                return;
            }

            var last = _iteratingHead.Previous;
            if (last is null)
            {
                // single node.
                _iteratingHead.Previous = handler;
                _iteratingHead.Next = handler;
                handler.Previous = _iteratingHead;
            }
            else
            {
                // multi node
                _iteratingHead.Previous = handler;
                last.Next = handler;
                handler.Previous = last;
            }
        }
        else
        {
            var last = _head.Previous;
            if (last is null)
            {
                // single node.
                _head.Previous = handler;
                _head.Next = handler;
                handler.Previous = _head;
            }
            else
            {
                // multi node
                _head.Previous = handler;
                last.Next = handler;
                handler.Previous = last;
            }
        }
    }

    public void Remove(ITriggerHandler<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (_iteratingNode is not null && _iteratingNode == handler)
        {
            // if remove self, reserve remove self after invoke completed.
            _preserveRemoveSelf = true;
        }
        else
        {
            var previous = handler.Previous;
            var next = handler.Next;

            if (next is not null)
            {
                next.Previous = previous;
            }

            if (handler == _head)
            {
                _head = next;
            }
            else if (handler == _iteratingHead)
            {
                _iteratingHead = next;
            }
            else
            {
                // when handler is head, prev indicate last so don't use it.
                if (previous is not null)
                {
                    previous.Next = next;
                }
            }

            if (_head is not null)
            {
                if (_head.Previous == handler)
                {
                    if (previous != _head)
                    {
                        _head.Previous = previous;
                    }
                    else
                    {
                        _head.Previous = null;
                    }
                }
            }

            if (_iteratingHead is not null)
            {
                if (_iteratingHead.Previous == handler)
                {
                    if (previous != _iteratingHead.Previous)
                    {
                        _iteratingHead.Previous = previous;
                    }
                    else
                    {
                        _iteratingHead.Previous = null;
                    }
                }
            }

            handler.Previous = null;
            handler.Next = null;
        }
    }
}
