using System;
using System.Collections.Generic;
using Godot;

namespace GDTask.Internal
{
    internal static class GodotEqualityComparer
    {
        public static readonly IEqualityComparer<Vector2> Vector2 = new Vector2EqualityComparer();
        public static readonly IEqualityComparer<Vector3> Vector3 = new Vector3EqualityComparer();
        public static readonly IEqualityComparer<Color> Color = new ColorEqualityComparer();
        public static readonly IEqualityComparer<Rect2> Rect2 = new Rect2EqualityComparer();
        public static readonly IEqualityComparer<AABB> AABB = new AABBEqualityComparer();
        public static readonly IEqualityComparer<Quat> Quat = new QuatEqualityComparer();

        static readonly RuntimeTypeHandle vector2Type = typeof(Vector2).TypeHandle;
        static readonly RuntimeTypeHandle vector3Type = typeof(Vector3).TypeHandle;
        static readonly RuntimeTypeHandle colorType = typeof(Color).TypeHandle;
        static readonly RuntimeTypeHandle rectType = typeof(Rect2).TypeHandle;
        static readonly RuntimeTypeHandle AABBType = typeof(AABB).TypeHandle;
        static readonly RuntimeTypeHandle quaternionType = typeof(Quat).TypeHandle;

        static class Cache<T>
        {
            public static readonly IEqualityComparer<T> Comparer;

            static Cache()
            {
                var comparer = GetDefaultHelper(typeof(T));
                if (comparer == null)
                {
                    Comparer = EqualityComparer<T>.Default;
                }
                else
                {
                    Comparer = (IEqualityComparer<T>)comparer;
                }
            }
        }

        public static IEqualityComparer<T> GetDefault<T>()
        {
            return Cache<T>.Comparer;
        }

        static object GetDefaultHelper(Type type)
        {
            var t = type.TypeHandle;

            if (t.Equals(vector2Type)) return (object)GodotEqualityComparer.Vector2;
            if (t.Equals(vector3Type)) return (object)GodotEqualityComparer.Vector3;
            if (t.Equals(colorType)) return (object)GodotEqualityComparer.Color;
            if (t.Equals(rectType)) return (object)GodotEqualityComparer.Rect2;
            if (t.Equals(AABBType)) return (object)GodotEqualityComparer.AABB;
            if (t.Equals(quaternionType)) return (object)GodotEqualityComparer.Quat;

            return null;
        }

        sealed class Vector2EqualityComparer : IEqualityComparer<Vector2>
        {
            public bool Equals(Vector2 self, Vector2 vector)
            {
                return self.x.Equals(vector.x) && self.y.Equals(vector.y);
            }

            public int GetHashCode(Vector2 obj)
            {
                return obj.x.GetHashCode() ^ obj.y.GetHashCode() << 2;
            }
        }

        sealed class Vector3EqualityComparer : IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 self, Vector3 vector)
            {
                return self.x.Equals(vector.x) && self.y.Equals(vector.y) && self.z.Equals(vector.z);
            }

            public int GetHashCode(Vector3 obj)
            {
                return obj.x.GetHashCode() ^ obj.y.GetHashCode() << 2 ^ obj.z.GetHashCode() >> 2;
            }
        }

        sealed class ColorEqualityComparer : IEqualityComparer<Color>
        {
            public bool Equals(Color self, Color other)
            {
                return self.r.Equals(other.r) && self.g.Equals(other.g) && self.b.Equals(other.b) && self.a.Equals(other.a);
            }

            public int GetHashCode(Color obj)
            {
                return obj.r.GetHashCode() ^ obj.g.GetHashCode() << 2 ^ obj.b.GetHashCode() >> 2 ^ obj.a.GetHashCode() >> 1;
            }
        }

        sealed class Rect2EqualityComparer : IEqualityComparer<Rect2>
        {
            public bool Equals(Rect2 self, Rect2 other)
            {
                return self.Size.Equals(other.Size) && self.Position.Equals(other.Position);
            }

            public int GetHashCode(Rect2 obj)
            {
                return obj.Size.GetHashCode() ^ obj.Position.GetHashCode() << 2;
            }
        }

        sealed class AABBEqualityComparer : IEqualityComparer<AABB>
        {
            public bool Equals(AABB self, AABB vector)
            {
                return self.Position.Equals(vector.Position) && self.Size.Equals(vector.Size);
            }

            public int GetHashCode(AABB obj)
            {
                return obj.Position.GetHashCode() ^ obj.Size.GetHashCode() << 2;
            }
        }

        sealed class QuatEqualityComparer : IEqualityComparer<Quat>
        {
            public bool Equals(Quat self, Quat vector)
            {
                return self.x.Equals(vector.x) && self.y.Equals(vector.y) && self.z.Equals(vector.z) && self.w.Equals(vector.w);
            }

            public int GetHashCode(Quat obj)
            {
                return obj.x.GetHashCode() ^ obj.y.GetHashCode() << 2 ^ obj.z.GetHashCode() >> 2 ^ obj.w.GetHashCode() >> 1;
            }
        }
    }
}
