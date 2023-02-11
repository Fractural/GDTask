using Fractural.Tasks;
using Godot;
using System;

namespace Tests.Manual
{
    public class Test : Node2D
    {
        [Export]
        private NodePath spritePath;
        public Sprite sprite;

        public override void _Ready()
        {
            sprite = GetNode<Sprite>(spritePath);
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

            await GDTask.WaitForEndOfFrame();
            GD.Print("WaitForEndOfFrame");
            await GDTask.WaitForPhysicsProcess();
            GD.Print("WaitForPhysicsProcess");
            await GDTask.NextFrame();
            GD.Print("NextFrame");
            await GDTask.WaitForPhysicsProcess();
            GD.Print("WaitForPhysicsProcess");
        }

        private async GDTask<string> RunWithResult()
        {
            await GDTask.Delay(TimeSpan.FromSeconds(2));
            return "Hello";
        }
    }
}
