using Fractural.Tasks;
using Godot;
using System;
using System.Threading;

namespace Tests.Manual
{
    public partial class Test : Node
    {
        [Signal]
        public delegate void MyEmptySignalEventHandler();
        [Signal]
        public delegate void MyArgSignalEventHandler(int number, bool boolean);

        [Export]
        private bool runTestOnReady;
        [Export]
        private NodePath spritePath;
        [Export]
        private Label pauseLabel;
        public Sprite2D sprite;

        public override void _Ready()
        {
            sprite = GetNode<Sprite2D>(spritePath);
            if (runTestOnReady)
                Run().Forget();
            ProcessMode = ProcessModeEnum.Always;
            pauseLabel.Text = GetTree().Paused ? "Paused" : "Unpaused";
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionReleased("ui_left"))
            {
                Run().Forget();
            }
            else if (@event.IsActionReleased("ui_right"))
            {
                RunPause().Forget();
            }
            else if (@event.IsActionReleased("ui_up"))
            {
                GetTree().Paused = !GetTree().Paused;
                pauseLabel.Text = GetTree().Paused ? "Paused" : "Unpaused";
            }
        }

        private async GDTaskVoid WaitAndEmitMyEmptySignal(TimeSpan delay)
        {
            await GDTask.Delay(delay);
            EmitSignal(nameof(MyEmptySignal));
        }

        private async GDTaskVoid WaitAndEmitMyArgSignal(TimeSpan delay)
        {
            await GDTask.Delay(delay);
            EmitSignal(nameof(MyArgSignal), 10, true);
        }
        private async GDTaskVoid WaitAndCancelToken(TimeSpan delay, CancellationTokenSource cts)
        {
            await GDTask.Delay(delay);
            cts.Cancel();
        }

        private async GDTaskVoid Run()
        {
            GD.Print("Run: Pre delay");
            sprite.Visible = false;
            await GDTask.Delay(TimeSpan.FromSeconds(3));
            sprite.Visible = true;
            GD.Print("Run: Post delay after 3 seconds");

            GD.Print("Run: Await MyEmptySignal");
            WaitAndEmitMyEmptySignal(TimeSpan.FromSeconds(1)).Forget();
            var signalResult = await GDTask.ToSignal(this, nameof(MyEmptySignal));
            GD.Print("Run: Await MyEmptySignal Complete, result: ", Json.Stringify(new Godot.Collections.Array(signalResult)));

            GD.Print("Run: Await MyArgSignal");
            WaitAndEmitMyArgSignal(TimeSpan.FromSeconds(1)).Forget();
            signalResult = await GDTask.ToSignal(this, nameof(MyArgSignal));
            GD.Print("Run: Await MyArgSignal Complete, result: ", Json.Stringify(new Godot.Collections.Array(signalResult)));

            var cts = new CancellationTokenSource();
            GD.Print("Run: Await Cancellable MyEmptySignal");
            WaitAndEmitMyEmptySignal(TimeSpan.FromSeconds(3)).Forget();
            WaitAndCancelToken(TimeSpan.FromSeconds(0.5), cts).Forget();
            try
            {
                signalResult = await GDTask.ToSignal(this, nameof(MyEmptySignal), cts.Token);
                GD.Print("Run: Await Cancellable MyEmptySignal ran with result: ", signalResult);
            }
            catch (OperationCanceledException _)
            {
                GD.Print("Run: Await Cancellable MyEmptySignal Cancelled");
            }

            cts = new CancellationTokenSource();
            GD.Print("Run: Await Cancellable MyArgSignal");
            WaitAndEmitMyArgSignal(TimeSpan.FromSeconds(3)).Forget();
            WaitAndCancelToken(TimeSpan.FromSeconds(0.5), cts).Forget();
            try
            {
                signalResult = await GDTask.ToSignal(this, nameof(MyArgSignal), cts.Token);
                GD.Print("Run: Await Cancellable MyArgSignal ran with result: ", signalResult);
            }
            catch (OperationCanceledException _)
            {
                GD.Print("Run: Await Cancellable MyArgSignal Cancelled");
            }

            GD.Print("Run: Pre RunWithResult");
            string runResult = await RunWithResult();
            GD.Print($"Run: Post got result: {runResult}");

            GD.Print("Run: LongTask started");
            cts = new CancellationTokenSource();

            CancellableReallyLongTask(cts.Token).Forget();

            await GDTask.Delay(TimeSpan.FromSeconds(3));
            cts.Cancel();
            GD.Print("Run: LongTask cancelled");

            await GDTask.WaitForEndOfFrame();
            GD.Print("Run: WaitForEndOfFrame");
            await GDTask.WaitForPhysicsProcess();
            GD.Print("Run: WaitForPhysicsProcess");
            await GDTask.NextFrame();
            GD.Print("Run: NextFrame");
        }

        private async GDTask<string> RunWithResult()
        {
            await GDTask.Delay(TimeSpan.FromSeconds(2));
            return "Hello";
        }

        private async GDTaskVoid CancellableReallyLongTask(CancellationToken cancellationToken)
        {
            int seconds = 10;
            GD.Print($"Run: Starting long task ({seconds} seconds long).");
            for (int i = 0; i < seconds; i++)
            {
                GD.Print($"Run: Working on long task for {i} seconds...");
                await GDTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            }
            GD.Print("Run: Finished long task.");
        }

        private async GDTaskVoid RunPause()
        {
            GD.Print("RunPause: Pre delay");
            sprite.Visible = false;
            await GDTask.Delay(TimeSpan.FromSeconds(3), PlayerLoopTiming.PauseProcess);
            sprite.Visible = true;
            GD.Print("RunPause: Post delay after 3 seconds");

            GD.Print("RunPause: Pre RunWithResult");
            string result = await RunWithResultPause();
            GD.Print($"RunPause: Post got result: {result}");

            GD.Print("RunPause: LongTask started");
            var cts = new CancellationTokenSource();

            CancellableReallyLongTaskPause(cts.Token).Forget();

            await GDTask.Delay(TimeSpan.FromSeconds(3), PlayerLoopTiming.PauseProcess);
            cts.Cancel();
            GD.Print("RunPause: LongTask cancelled");

            await GDTask.Yield(PlayerLoopTiming.PauseProcess);
            GD.Print("RunPause: Yield(PlayerLoopTiming.PauseProcess)");
            await GDTask.Yield(PlayerLoopTiming.PausePhysicsProcess);
            GD.Print("RunPause: Yield(PlayerLoopTiming.PausePhysicsProcess)");
            await GDTask.NextFrame(PlayerLoopTiming.PauseProcess);
            GD.Print("RunPause: NextFrame");
        }

        private async GDTask<string> RunWithResultPause()
        {
            await GDTask.Delay(TimeSpan.FromSeconds(2), PlayerLoopTiming.PauseProcess);
            return "Hello";
        }

        private async GDTaskVoid CancellableReallyLongTaskPause(CancellationToken cancellationToken)
        {
            int seconds = 10;
            GD.Print($"RunPause: Starting long task ({seconds} seconds long).");
            for (int i = 0; i < seconds; i++)
            {
                GD.Print($"RunPause: Working on long task for {i} seconds...");
                await GDTask.Delay(TimeSpan.FromSeconds(1), PlayerLoopTiming.PauseProcess, cancellationToken);
            }
            GD.Print("RunPause: Finished long task.");
        }
    }
}
