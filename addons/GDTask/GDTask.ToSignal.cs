using System.Threading;
using Godot;

namespace Fractural.Tasks;

public partial struct GDTask
{
    public static async GDTask<Variant[]> ToSignal(GodotObject self, StringName signal)
    {
        return await self.ToSignal(self, signal);
    }

    public static async GDTask<Variant[]> ToSignal(GodotObject self, StringName signal, CancellationToken ct)
    {
        var tcs = new GDTaskCompletionSource<Variant[]>();
        ct.Register(() => tcs.TrySetCanceled(ct));
        Create(async () =>
            {
                var result = await self.ToSignal(self, signal);
                tcs.TrySetResult(result);
            })
            .Forget();
        return await tcs.Task;
    }
}
