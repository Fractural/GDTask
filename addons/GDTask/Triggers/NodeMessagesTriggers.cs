using System.Threading;
using Godot;

namespace Fractural.Tasks.Triggers
{
    #region PhysicsProcess

    public interface IAsyncPhysicsProcessHandler
    {
        GDTask PhysicsProcessAsync();
    }

    public partial class AsyncTriggerHandler<T> : IAsyncPhysicsProcessHandler
    {
        GDTask IAsyncPhysicsProcessHandler.PhysicsProcessAsync()
        {
            core.Reset();
            return new GDTask((IGDTaskSource)(object)this, core.Version);
        }
    }

    public static partial class AsyncTriggerExtensions
    {
        public static AsyncPhysicsProcessTrigger GetAsyncPhysicsProcessTrigger(this Node node)
        {
            return node.GetOrAddImmediateChild<AsyncPhysicsProcessTrigger>();
        }
    }

    public sealed partial class AsyncPhysicsProcessTrigger : AsyncTriggerBase<AsyncUnit>
    {
        public override void _PhysicsProcess(double delta)
        {
            RaiseEvent(AsyncUnit.Default);
        }

        public IAsyncPhysicsProcessHandler GetPhysicsProcessAsyncHandler()
        {
            return new AsyncTriggerHandler<AsyncUnit>(this, false);
        }

        public IAsyncPhysicsProcessHandler GetPhysicsProcessAsyncHandler(CancellationToken cancellationToken)
        {
            return new AsyncTriggerHandler<AsyncUnit>(this, cancellationToken, false);
        }

        public GDTask PhysicsProcessAsync()
        {
            return ((IAsyncPhysicsProcessHandler)new AsyncTriggerHandler<AsyncUnit>(this, true)).PhysicsProcessAsync();
        }

        public GDTask PhysicsProcessAsync(CancellationToken cancellationToken)
        {
            return ((IAsyncPhysicsProcessHandler)new AsyncTriggerHandler<AsyncUnit>(this, cancellationToken, true)).PhysicsProcessAsync();
        }
    }
    #endregion

    #region Process

    public interface IAsyncProcessHandler
    {
        GDTask ProcessAsync();
    }

    public partial class AsyncTriggerHandler<T> : IAsyncProcessHandler
    {
        GDTask IAsyncProcessHandler.ProcessAsync()
        {
            core.Reset();
            return new GDTask((IGDTaskSource)(object)this, core.Version);
        }
    }

    public static partial class AsyncTriggerExtensions
    {
        public static AsyncProcessTrigger GetAsyncProcessTrigger(this Node node)
        {
            return node.GetOrAddImmediateChild<AsyncProcessTrigger>();
        }
    }

    public sealed partial class AsyncProcessTrigger : AsyncTriggerBase<AsyncUnit>
    {
        public override void _Process(double delta)
        {
            RaiseEvent(AsyncUnit.Default);
        }

        public IAsyncProcessHandler GetProcessAsyncHandler()
        {
            return new AsyncTriggerHandler<AsyncUnit>(this, false);
        }

        public IAsyncProcessHandler GetProcessAsyncHandler(CancellationToken cancellationToken)
        {
            return new AsyncTriggerHandler<AsyncUnit>(this, cancellationToken, false);
        }

        public GDTask ProcessAsync()
        {
            return ((IAsyncProcessHandler)new AsyncTriggerHandler<AsyncUnit>(this, true)).ProcessAsync();
        }

        public GDTask ProcessAsync(CancellationToken cancellationToken)
        {
            return ((IAsyncProcessHandler)new AsyncTriggerHandler<AsyncUnit>(this, cancellationToken, true)).ProcessAsync();
        }
    }
    #endregion

}