using System;
using System.Diagnostics;

namespace Fractural.Tasks.Internal;

internal readonly struct ValueStopwatch
{
    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    private readonly long _startTimestamp;

    public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());

    ValueStopwatch(long startTimestamp)
    {
        _startTimestamp = startTimestamp;
    }

    public TimeSpan Elapsed => TimeSpan.FromTicks(ElapsedTicks);

    public bool IsInvalid => _startTimestamp == 0;

    public long ElapsedTicks
    {
        get
        {
            if (_startTimestamp == 0)
            {
                throw new InvalidOperationException("Detected invalid initialization(use 'default'), only to create from StartNew().");
            }

            var delta = Stopwatch.GetTimestamp() - _startTimestamp;
            return (long)(delta * TimestampToTicks);
        }
    }
}
