using System;
using System.Threading;

namespace Fractural.Tasks;

// CancellationTokenSource itself can not reuse but CancelAfter(Timeout.InfiniteTimeSpan) allows reuse if did not reach timeout.
// Similar discussion:
// https://github.com/dotnet/runtime/issues/4694
// https://github.com/dotnet/runtime/issues/48492
// This TimeoutController emulate similar implementation, using CancelAfterSlim; to achieve zero allocation timeout.

public sealed class TimeoutController : IDisposable
{
    private static readonly Action<object> CancelCancellationTokenSourceStateDelegate = new(CancelCancellationTokenSourceState);

    private static void CancelCancellationTokenSourceState(object state)
    {
        var cts = (CancellationTokenSource)state;
        cts.Cancel();
    }

    private CancellationTokenSource _timeoutSource;
    private CancellationTokenSource _linkedSource;
    private PlayerLoopTimer _timer;
    private bool _isDisposed;

    private readonly DelayType _delayType;
    private readonly PlayerLoopTiming _delayTiming;
    private readonly CancellationTokenSource _originalLinkCancellationTokenSource;

    public TimeoutController(DelayType delayType = DelayType.DeltaTime, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process)
    {
        _timeoutSource = new CancellationTokenSource();
        _originalLinkCancellationTokenSource = null;
        _linkedSource = null;
        _delayType = delayType;
        _delayTiming = delayTiming;
    }

    public TimeoutController(
        CancellationTokenSource linkCancellationTokenSource,
        DelayType delayType = DelayType.DeltaTime,
        PlayerLoopTiming delayTiming = PlayerLoopTiming.Process
    )
    {
        _timeoutSource = new CancellationTokenSource();
        _originalLinkCancellationTokenSource = linkCancellationTokenSource;
        _linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_timeoutSource.Token, linkCancellationTokenSource.Token);
        _delayType = delayType;
        _delayTiming = delayTiming;
    }

    public CancellationToken Timeout(int millisecondsTimeout)
    {
        return Timeout(TimeSpan.FromMilliseconds(millisecondsTimeout));
    }

    public CancellationToken Timeout(TimeSpan timeout)
    {
        if (_originalLinkCancellationTokenSource is { IsCancellationRequested: true })
        {
            return _originalLinkCancellationTokenSource.Token;
        }

        // Timeouted, create new source and timer.
        if (_timeoutSource.IsCancellationRequested)
        {
            _timeoutSource.Dispose();
            _timeoutSource = new CancellationTokenSource();
            if (_linkedSource != null)
            {
                _linkedSource.Cancel();
                _linkedSource.Dispose();
                _linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_timeoutSource.Token, _originalLinkCancellationTokenSource.Token);
            }

            _timer?.Dispose();
            _timer = null;
        }

        var useSource = _linkedSource ?? _timeoutSource;
        var token = useSource.Token;
        if (_timer is null)
        {
            // Timer complete => timeoutSource.Cancel() -> linkedSource will be canceled.
            // (linked)token is canceled => stop timer
            _timer = PlayerLoopTimer.StartNew(
                timeout,
                false,
                _delayType,
                _delayTiming,
                token,
                CancelCancellationTokenSourceStateDelegate,
                _timeoutSource
            );
        }
        else
        {
            _timer.Restart(timeout);
        }

        return token;
    }

    public bool IsTimeout()
    {
        return _timeoutSource.IsCancellationRequested;
    }

    public void Reset()
    {
        _timer?.Stop();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        try
        {
            // stop timer.
            _timer?.Dispose();

            // cancel and dispose.
            _timeoutSource.Cancel();
            _timeoutSource.Dispose();
            if (_linkedSource is not null)
            {
                _linkedSource.Cancel();
                _linkedSource.Dispose();
            }
        }
        finally
        {
            _isDisposed = true;
        }
    }
}
