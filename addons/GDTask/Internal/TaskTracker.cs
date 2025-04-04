using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Fractural.Tasks.Internal;

namespace Fractural.Tasks;

// public for add user custom.

public static class TaskTracker
{
    // TODO: Work on task tracker after getting tasks functioning
#if DEBUG

    private static int trackingId = 0;

    public const string EnableAutoReloadKey = "GDTaskTrackerWindow_EnableAutoReloadKey";
    public const string EnableTrackingKey = "GDTaskTrackerWindow_EnableTrackingKey";
    public const string EnableStackTraceKey = "GDTaskTrackerWindow_EnableStackTraceKey";

    public static class EditorEnableState
    {
        private static bool _enableAutoReload;
        public static bool EnableAutoReload
        {
            get { return _enableAutoReload; }
            set
            {
                _enableAutoReload = value;
                //UnityEditor.EditorPrefs.SetBool(EnableAutoReloadKey, value);
            }
        }

        private static bool _enableTracking;
        public static bool EnableTracking
        {
            get { return _enableTracking; }
            set
            {
                _enableTracking = value;
                //UnityEditor.EditorPrefs.SetBool(EnableTrackingKey, value);
            }
        }

        private static bool _enableStackTrace;
        public static bool EnableStackTrace
        {
            get { return _enableStackTrace; }
            set
            {
                _enableStackTrace = value;
                //UnityEditor.EditorPrefs.SetBool(EnableStackTraceKey, value);
            }
        }
    }
#endif

    private static bool _dirty;

    private static List<KeyValuePair<IGDTaskSource, (string formattedType, int trackingId, DateTime addTime, string stackTrace)>> listPool = [];

    private static readonly WeakDictionary<IGDTaskSource, (string formattedType, int trackingId, DateTime addTime, string stackTrace)> tracking =
        new();

    [Conditional("DEBUG")]
    public static void TrackActiveTask(IGDTaskSource task, int skipFrame)
    {
#if DEBUG
        _dirty = true;
        if (!EditorEnableState.EnableTracking)
            return;
        var stackTrace = EditorEnableState.EnableStackTrace ? new StackTrace(skipFrame, true).CleanupAsyncStackTrace() : string.Empty;

        string typeName;
        if (EditorEnableState.EnableStackTrace)
        {
            var sb = new StringBuilder();
            TypeBeautify(task.GetType(), sb);
            typeName = sb.ToString();
        }
        else
        {
            typeName = task.GetType().Name;
        }
        tracking.TryAdd(task, (typeName, Interlocked.Increment(ref trackingId), DateTime.UtcNow, stackTrace));
#endif
    }

    [Conditional("DEBUG")]
    public static void RemoveTracking(IGDTaskSource task)
    {
#if DEBUG
        _dirty = true;
        if (!EditorEnableState.EnableTracking)
            return;
        var success = tracking.TryRemove(task);
#endif
    }

    public static bool CheckAndResetDirty()
    {
        var current = _dirty;
        _dirty = false;
        return current;
    }

    /// <summary>(trackingId, awaiterType, awaiterStatus, createdTime, stackTrace)</summary>
    public static void ForEachActiveTask(Action<int, string, GDTaskStatus, DateTime, string> action)
    {
        lock (listPool)
        {
            var count = tracking.ToList(ref listPool, clear: false);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    action(
                        listPool[i].Value.trackingId,
                        listPool[i].Value.formattedType,
                        listPool[i].Key.UnsafeGetStatus(),
                        listPool[i].Value.addTime,
                        listPool[i].Value.stackTrace
                    );
                    listPool[i] = default;
                }
            }
            catch
            {
                listPool.Clear();
                throw;
            }
        }
    }

    private static void TypeBeautify(Type type, StringBuilder sb)
    {
        if (type.IsNested)
        {
            // TypeBeautify(type.DeclaringType, sb);
            sb.Append(type.DeclaringType.Name.ToString());
            sb.Append(".");
        }

        if (type.IsGenericType)
        {
            var genericsStart = type.Name.IndexOf("`");
            if (genericsStart != -1)
            {
                sb.Append(type.Name.Substring(0, genericsStart));
            }
            else
            {
                sb.Append(type.Name);
            }
            sb.Append("<");
            var first = true;
            foreach (var item in type.GetGenericArguments())
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;
                TypeBeautify(item, sb);
            }
            sb.Append(">");
        }
        else
        {
            sb.Append(type.Name);
        }
    }
}
