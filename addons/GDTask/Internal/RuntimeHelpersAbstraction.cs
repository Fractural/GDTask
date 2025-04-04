using System;
using Godot;

namespace Fractural.Tasks.Internal;

internal static class RuntimeHelpersAbstraction
{
    // If we can use RuntimeHelpers.IsReferenceOrContainsReferences(.NET Core 2.0), use it.
    public static bool IsWellKnownNoReferenceContainsType<T>()
    {
        return WellKnownNoReferenceContainsType<T>.IsWellKnownType;
    }

    private static bool WellKnownNoReferenceContainsTypeInitialize(Type t)
    {
        // The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
        if (t.IsPrimitive)
            return true;

        if (t.IsEnum)
            return true;
        if (t == typeof(DateTime))
            return true;
        if (t == typeof(DateTimeOffset))
            return true;
        if (t == typeof(Guid))
            return true;
        if (t == typeof(decimal))
            return true;

        // unwrap nullable
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return WellKnownNoReferenceContainsTypeInitialize(t.GetGenericArguments()[0]);
        }

        // or add other wellknown types(Vector, etc...) here
        if (t == typeof(Vector2))
            return true;
        if (t == typeof(Vector3))
            return true;
        if (t == typeof(Color))
            return true;
        if (t == typeof(Rect2))
            return true;
        if (t == typeof(Aabb))
            return true;
        if (t == typeof(Quaternion))
            return true;

        return false;
    }

    private static class WellKnownNoReferenceContainsType<T>
    {
        public static readonly bool IsWellKnownType;

        static WellKnownNoReferenceContainsType()
        {
            IsWellKnownType = WellKnownNoReferenceContainsTypeInitialize(typeof(T));
        }
    }
}
