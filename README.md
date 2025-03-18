# GDTask ✅

![Deploy](https://github.com/Fractural/GDTask/actions/workflows/deploy.yml/badge.svg)

> [!Note] 
> 
> This branch is for the Godot 4.4 version of the addon. 
> You can download the Godot 3.x version from the 3.x branch.

Adds async/await features in Godot for easier async coding.
Based on code from [Cysharp's UniTask library for Unity](https://github.com/Cysharp/UniTask).

```CSharp
using Fractural.Tasks;

public Test : Node 
{
	[Signal]
	public delegate void MySignalHandler(int number, bool boolean);
	
	public override _Ready() 
	{
		// Running a task from a non-async method.
		Run().Forget();
	}

	public async GDTaskVoid Run() 
	{
		await GDTask.DelayFrame(100);

		// Waiting some amount of time
		// Note that these delays are paused when the game is paused
		await GDTask.Delay(TimeSpan.FromSeconds(10));
		await GDTask.Delay(TimeSpan.FromSeconds(10), PlayerLoopTiming.Process);
		await GDTask.Delay(TimeSpan.FromSeconds(10), PlayerLoopTiming.PhysicsProcess);
		// Waiting some amount of milliseconds
		await GDTask.Delay(1000);
		// Waiting some amount of milliseconds, regardless of whether the game is paused
		await GDTask.Delay(TimeSpan.FromSeconds(10), PlayerLoopTiming.PauseProcess);
		await GDTask.Delay(TimeSpan.FromSeconds(10), PlayerLoopTiming.PausePhysicsProcess);

		// Awaiting for a signal
		WaitAndEmitMySignal(TimeSpan.FromSeconds(2)).Forget();
		var signalResults = await GDTask.ToSignal(this, nameof(MySignal));
		// signalResults = [10, true]

		// Cancellable awaiting a signal
		var cts = new CancellationTokenSource();
		WaitAndEmitMySignal(TimeSpan.FromSeconds(2)).Forget();
		WaitAndCancelToken(TimeSpan.FromSeconds(1), cts).Forget();
		try 
		{
			var signalResults = await GDTask.ToSignal(this, nameof(MySignal), cts.Token);
		}
		catch (OperationCanceledException _)
		{
			GD.Print("Awaiting MySignal cancelled!");
		}

		// Waiting a single frame
		await GDTask.Yield();
		await GDTask.NextFrame();
		await GDTask.WaitForEndOfFrame();

		// Waiting for specific lifetime call
		await GDTask.WaitForPhysicsProcess();

		// Cancellation of a GDTask
		var cts = new CancellationTokenSource();
		CancellableReallyLongTask(cts.Token).Forget();
		await GDTask.Delay(TimeSpan.FromSeconds(3));
		cts.Cancel();

		// Returning a value from a GDTask
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
	
	public async GDTaskVoid WaitAndEmitMySignal(TimeSpan delay)
	{
		await GDTask.Delay(delay);
		EmitSignal(nameof(MySignal), 10, true);
	}

	public async GDTaskVoid WaitAndCancelToken(TimeSpan delay, CancellationTokenSource cts)
	{
		await GDTask.Delay(delay);
		cts.Cancel();
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
