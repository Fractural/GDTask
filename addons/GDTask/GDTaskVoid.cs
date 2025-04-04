using System.Runtime.CompilerServices;
using Fractural.Tasks.CompilerServices;

namespace Fractural.Tasks;

[AsyncMethodBuilder(typeof(AsyncGDTaskVoidMethodBuilder))]
public readonly struct GDTaskVoid
{
    public void Forget() { }
}
