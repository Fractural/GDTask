using System;
using System.Runtime.CompilerServices;

namespace Fractural.Tasks.Internal;

internal static class Error
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowArgumentNullException<T>(T value, string paramName)
        where T : class
    {
        if (value is null)
            ThrowArgumentNullExceptionCore(paramName);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowArgumentNullExceptionCore(string paramName)
    {
        throw new ArgumentNullException(paramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Exception ArgumentOutOfRange(string paramName)
    {
        return new ArgumentOutOfRangeException(paramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Exception NoElements()
    {
        return new InvalidOperationException("Source sequence doesn't contain any elements.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Exception MoreThanOneElement()
    {
        return new InvalidOperationException("Source sequence contains more than one element.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentException<T>(string message)
    {
        throw new ArgumentException(message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowNotYetCompleted()
    {
        throw new InvalidOperationException("Not yet completed.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ThrowNotYetCompleted<T>()
    {
        throw new InvalidOperationException("Not yet completed.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowWhenContinuationIsAlreadyRegistered<T>(T continuationField)
        where T : class
    {
        if (continuationField != null)
            ThrowInvalidOperationExceptionCore("continuation is already registered.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidOperationExceptionCore(string message)
    {
        throw new InvalidOperationException(message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowOperationCanceledException()
    {
        throw new OperationCanceledException();
    }
}
