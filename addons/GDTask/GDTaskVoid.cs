#pragma warning disable CS1591
#pragma warning disable CS0436

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Fractural.Tasks.CompilerServices;

namespace Fractural.Tasks
{
    [AsyncMethodBuilder(typeof(AsyncGDTaskVoidMethodBuilder))]
    public readonly struct GDTaskVoid
    {
        public void Forget()
        {
        }
    }
}

