# GDTask ✅

> [!Note]
> 
> This branch is for the Godot 3.x version of the addon. You can download the Godot 4.x version from the main branch.

Adds async/await features in Godot for easier async coding.
Based on code from [Cysharp's UniTask library for Unity](https://github.com/Cysharp/UniTask).

```CSharp
using Fractural.Tasks;

public Test : Node 
{
    public override _Ready() 
    {
        // Running a task from a non-async method
        Run().Forget();
    }

    public async GDTaskVoid Run() 
    {
        await GDTask.DelayFrame(100);

        // waiting some amount of time
        await GDTask.Delay(TimeSpan.FromSeconds(10));

        // Waiting a single frame
        await GDTask.Yield();
        await GDTask.NextFrame();
        await GDTask.WaitForEndOfFrame();

        // Waiting for specific lifetime call
        await GDTask.WaitForPhysicsProcess();

        // Cancellation
        var cts = new CancellationTokenSource();
        CancellableReallyLongTask(cts.Token).Forget();
        await GDTask.Delay(TimeSpan.FromSeconds(3));
        cts.Cancel();

        // Async await with return value
        string result = await RunWithResult();
        return result + " with additional text";
    }

    public async GDTask<string> RunWithResult()
    {
        await GDTask.Delay(TimeSpan.FromSeconds(3));
        return "A result string";
    }

    public async GDTaskVoid ReallyLongTask(CancellationToken cancellationToken)
    {
        GD.Print("Starting long task.");
        await GDTask.Delay(TimeSpan.FromSeconds(1000000), cancellationToken: cancellationToken);
        GD.Print("Finished long task.");
    }
}
```

## Installation

Manual

1. Download the repository
2. Move the `addons/GDTask` folder into `addons/GDTask`

Git Submodules

1. Make sure your project has a git repo initialized
2. Run
   
``` bash
git submodule add -b release https://github.com/fractural/GDTask.git addons/GDTask
```

3. Add `addons/GDTask/Autoload/GDTaskPlayerLoopAutoload` as an autoload
