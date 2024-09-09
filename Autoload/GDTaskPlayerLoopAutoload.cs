using Fractural.Tasks.Internal;
using Godot;
using System;

namespace Fractural.Tasks
{
    public static class GDTaskLoopRunners
    {
        public struct GDTaskLoopRunnerProcess { };
        public struct GDTaskLoopRunnerPhysicsProcess { };
    }

    public enum PlayerLoopTiming
    {
        Process = 0,
        PhysicsProcess = 1,
        PauseProcess = 2,
        PausePhysicsProcess = 3,
    }

    public interface IPlayerLoopItem
    {
        bool MoveNext();
    }

    /// <summary>
    /// Singleton that forwards Godot calls and values to GDTasks.
    /// </summary>
    public partial class GDTaskPlayerLoopAutoload : Node
    {
        public static int MainThreadId => Global.mainThreadId;
        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == Global.mainThreadId;
        public static void AddAction(PlayerLoopTiming timing, IPlayerLoopItem action) => Global.LocalAddAction(timing, action);
        public static void ThrowInvalidLoopTiming(PlayerLoopTiming playerLoopTiming) => throw new InvalidOperationException("Target playerLoopTiming is not injected. Please check PlayerLoopHelper.Initialize. PlayerLoopTiming:" + playerLoopTiming);
        public static void AddContinuation(PlayerLoopTiming timing, Action continuation) => Global.LocalAddContinuation(timing, continuation);

        public void LocalAddAction(PlayerLoopTiming timing, IPlayerLoopItem action)
        {
            var runner = runners[(int)timing];
            if (runner == null)
            {
                ThrowInvalidLoopTiming(timing);
            }
            runner.AddAction(action);
        }

        // NOTE: Continuation means a asynchronous task invoked by another task after the other task finishes.
        public void LocalAddContinuation(PlayerLoopTiming timing, Action continuation)
        {
            var q = yielders[(int)timing];
            if (q == null)
            {
                ThrowInvalidLoopTiming(timing);
            }
            q.Enqueue(continuation);
        }

        public static GDTaskPlayerLoopAutoload Global
        {
            get
            {
                if (s_Global != null) return s_Global;

                var newInstance = new GDTaskPlayerLoopAutoload();
                newInstance.Initialize();
                var currentScene = ((SceneTree)Engine.GetMainLoop()).CurrentScene;
                currentScene.AddChild(newInstance);
                currentScene.MoveChild(newInstance, 0);
                newInstance.Name = "GDTaskPlayerLoopAutoload";
                s_Global = newInstance;

                return s_Global;
            }
        }
        public double DeltaTime => GetProcessDeltaTime();
        public double PhysicsDeltaTime => GetPhysicsProcessDeltaTime();

        private static GDTaskPlayerLoopAutoload s_Global;
        private int mainThreadId;
        private ContinuationQueue[] yielders;
        private PlayerLoopRunner[] runners;
        private ProcessListener processListener;

        public override void _EnterTree()
        {
            if (s_Global == null)
            {
                Initialize();
                s_Global = this;
                return;
            }
            QueueFree();
        }

        private void Initialize()
        {
            ProcessMode = ProcessModeEnum.Pausable;
            mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            yielders = new[] {
                new ContinuationQueue(PlayerLoopTiming.Process),
                new ContinuationQueue(PlayerLoopTiming.PhysicsProcess),
                new ContinuationQueue(PlayerLoopTiming.PauseProcess),
                new ContinuationQueue(PlayerLoopTiming.PausePhysicsProcess),
            };
            runners = new[] {
                new PlayerLoopRunner(PlayerLoopTiming.Process),
                new PlayerLoopRunner(PlayerLoopTiming.PhysicsProcess),
                new PlayerLoopRunner(PlayerLoopTiming.PauseProcess),
                new PlayerLoopRunner(PlayerLoopTiming.PausePhysicsProcess),
            };
            processListener = new ProcessListener();
            AddChild(processListener);
            processListener.ProcessMode = ProcessModeEnum.Always;
            processListener.OnProcess += PauseProcess;
            processListener.OnPhysicsProcess += PausePhysicsProcess;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (Global == this)
                    s_Global = null;
                if (yielders != null)
                {
                    foreach (var yielder in yielders)
                        yielder.Clear();
                    foreach (var runner in runners)
                        runner.Clear();
                }
            }
        }

        public override void _Process(double delta)
        {
            yielders[(int)PlayerLoopTiming.Process].Run();
            runners[(int)PlayerLoopTiming.Process].Run();
        }

        public override void _PhysicsProcess(double delta)
        {
            yielders[(int)PlayerLoopTiming.PhysicsProcess].Run();
            runners[(int)PlayerLoopTiming.PhysicsProcess].Run();
        }

        private void PauseProcess(double delta)
        {
            yielders[(int)PlayerLoopTiming.PauseProcess].Run();
            runners[(int)PlayerLoopTiming.PauseProcess].Run();
        }

        private void PausePhysicsProcess(double delta)
        {
            yielders[(int)PlayerLoopTiming.PausePhysicsProcess].Run();
            runners[(int)PlayerLoopTiming.PausePhysicsProcess].Run();
        }
    }
}

