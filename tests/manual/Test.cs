using Fractural.Tasks;
using Godot;
using System;
using System.Threading;

namespace Tests.Manual
{
    public partial class Test : Node2D
    {
        [Export]
        private bool runTestOnReady;
        [Export]
        private NodePath spritePath;
        public Sprite2D sprite;

        public override void _Ready()
        {
            sprite = GetNode<Sprite2D>(spritePath);
            if (runTestOnReady)
                Run().Forget();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionReleased("ui_select"))
            {
                Run().Forget();
            }
        }

        private async GDTaskVoid Run()
        {
            GD.Print("Pre delay");
            sprite.Visible = false;
            await GDTask.Delay(TimeSpan.FromSeconds(3));
            sprite.Visible = true;
            GD.Print("Post delay after 3 seconds");

            GD.Print("Pre RunWithResult");
            string result = await RunWithResult();
            GD.Print($"Post got result: {result}");

            GD.Print("LongTask started");
            var cts = new CancellationTokenSource();

            CancellableReallyLongTask(cts.Token).Forget();

            await GDTask.Delay(TimeSpan.FromSeconds(3));
            cts.Cancel();
            GD.Print("LongTask cancelled");

            await GDTask.WaitForEndOfFrame();
            GD.Print("WaitForEndOfFrame");
            await GDTask.WaitForPhysicsProcess();
            GD.Print("WaitForPhysicsProcess");
            await GDTask.NextFrame();
            GD.Print("NextFrame");
        }

        private async GDTask<string> RunWithResult()
        {
            await GDTask.Delay(TimeSpan.FromSeconds(2));
            return "Hello";
        }

        private async GDTaskVoid CancellableReallyLongTask(CancellationToken cancellationToken)
        {
            int seconds = 10;
            GD.Print($"Starting long task ({seconds} seconds long).");
            for (int i = 0; i < seconds; i++)
            {
                GD.Print($"Working on long task for {i} seconds...");
                await GDTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            }
            GD.Print("Finished long task.");
        }
    }
}
