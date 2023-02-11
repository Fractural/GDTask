# GDTask ✅

![Deploy](https://github.com/Fractural/GDTask/actions/workflows/deploy.yml/badge.svg) ![Unit Tests](https://github.com/Fractural/GDTask/actions/workflows/tests.yml/badge.svg)

Adds async/await features in Godot for easier async coding.
Based on code from [Cysharp's UniTask library for Unity](https://github.com/Cysharp/UniTask).

```CSharp
using Fractural.Tasks;

async GDTask<string> DemoAsync() 
{
    await GDTask.DelayFrame(100);

    await UniTask.Delay(TimeSpan.FromSeconds(10));

    await GDTask.Yield();
    await GDTask.NextFrame();

    await GDTask.WaitForEndOfFrame();
    await GDTask.WaitForPhysicsProcess();

    return "final value";
}
```