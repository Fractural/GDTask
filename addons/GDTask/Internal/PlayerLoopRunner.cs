using System;
using Godot;

namespace Fractural.Tasks.Internal;

internal sealed class PlayerLoopRunner
{
    private const int InitialSize = 16;

    private readonly PlayerLoopTiming _timing;
    private readonly object _runningAndQueueLock = new();
    private readonly object _arrayLock = new();
    private readonly Action<Exception> _unhandledExceptionCallback;

    private int _tail = 0;
    private bool _running = false;
    private IPlayerLoopItem[] _loopItems = new IPlayerLoopItem[InitialSize];
    private MinimumQueue<IPlayerLoopItem> _waitQueue = new(InitialSize);

    public PlayerLoopRunner(PlayerLoopTiming timing)
    {
        _unhandledExceptionCallback = ex => GD.PrintErr(ex);
        _timing = timing;
    }

    public void AddAction(IPlayerLoopItem item)
    {
        lock (_runningAndQueueLock)
        {
            if (_running)
            {
                _waitQueue.Enqueue(item);
                return;
            }
        }

        lock (_arrayLock)
        {
            // Ensure Capacity
            if (_loopItems.Length == _tail)
            {
                Array.Resize(ref _loopItems, checked(_tail * 2));
            }
            _loopItems[_tail++] = item;
        }
    }

    public int Clear()
    {
        lock (_arrayLock)
        {
            var rest = 0;

            for (var index = 0; index < _loopItems.Length; index++)
            {
                if (_loopItems[index] != null)
                {
                    rest++;
                }

                _loopItems[index] = null;
            }

            _tail = 0;
            return rest;
        }
    }

    // delegate entrypoint.
    public void Run()
    {
        // for debugging, create named stacktrace.
#if DEBUG
        switch (_timing)
        {
            case PlayerLoopTiming.PhysicsProcess:
                PhysicsProcess();
                break;
            case PlayerLoopTiming.Process:
                Process();
                break;
            case PlayerLoopTiming.PausePhysicsProcess:
                PausePhysicsProcess();
                break;
            case PlayerLoopTiming.PauseProcess:
                PauseProcess();
                break;
        }
#else
        RunCore();
#endif
    }

    void PhysicsProcess() => RunCore();

    void Process() => RunCore();

    void PausePhysicsProcess() => RunCore();

    void PauseProcess() => RunCore();

    [System.Diagnostics.DebuggerHidden]
    private void RunCore()
    {
        lock (_runningAndQueueLock)
        {
            _running = true;
        }

        lock (_arrayLock)
        {
            var j = _tail - 1;

            for (int i = 0; i < _loopItems.Length; i++)
            {
                var action = _loopItems[i];
                if (action != null)
                {
                    try
                    {
                        if (!action.MoveNext())
                        {
                            _loopItems[i] = null;
                        }
                        else
                        {
                            continue; // next i
                        }
                    }
                    catch (Exception ex)
                    {
                        _loopItems[i] = null;
                        try
                        {
                            _unhandledExceptionCallback(ex);
                        }
                        catch { }
                    }
                }

                // find null, loop from tail
                while (i < j)
                {
                    var fromTail = _loopItems[j];
                    if (fromTail != null)
                    {
                        try
                        {
                            if (!fromTail.MoveNext())
                            {
                                _loopItems[j] = null;
                                j--;
                                continue; // next j
                            }
                            else
                            {
                                // swap
                                _loopItems[i] = fromTail;
                                _loopItems[j] = null;
                                j--;
                                goto NEXT_LOOP; // next i
                            }
                        }
                        catch (Exception ex)
                        {
                            _loopItems[j] = null;
                            j--;
                            try
                            {
                                _unhandledExceptionCallback(ex);
                            }
                            catch { }
                            continue; // next j
                        }
                    }
                    else
                    {
                        j--;
                    }
                }

                _tail = i; // loop end
                break; // LOOP END

                NEXT_LOOP:
                continue;
            }

            lock (_runningAndQueueLock)
            {
                _running = false;
                while (_waitQueue.Count != 0)
                {
                    if (_loopItems.Length == _tail)
                    {
                        Array.Resize(ref _loopItems, checked(_tail * 2));
                    }
                    _loopItems[_tail++] = _waitQueue.Dequeue();
                }
            }
        }
    }
}
