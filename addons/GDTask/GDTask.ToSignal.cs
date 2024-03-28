using Godot;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Fractural.Tasks
{
    /// <summary>
    /// A cancellable signal awaiter that wraps the Godot <see cref="SignalAwaiter"/>. Using ToSignal 
    /// with an additional <see cref="CancellationToken"/> as parameter automatically returns this awaiter. 
    /// See <see cref="GodotObjectExtensions.ToSignal(GodotObject, GodotObject, StringName, CancellationToken)"/>.
    /// 
    /// Originally from <see href="https://github.com/altamkp/GodotEx/blob/master/src/GodotEx.Async/src/Core/CancellableSignalAwaiter.cs">GodotEx</see>
    /// </summary>
    public class CancellableSignalAwaiter : IAwaiter<Variant[]>, INotifyCompletion, IAwaitable<Variant[]>
    {
        private readonly SignalAwaiter _signalAwaiter;
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenRegistration _cancellationTokenRegistration;

        private Action? _continuation;
        private bool _isCancelled;

        /// <summary>
        /// Creates a new <see cref="CancellableSignalAwaiter"/> that wraps the Godot <see cref="SignalAwaiter"/>.
        /// </summary>
        /// <param name="signalAwaiter">Godot <see cref="SignalAwaiter"/>.</param>
        /// <param name="cancellationToken">Cancellation token for cancellation request.</param>
        public CancellableSignalAwaiter(SignalAwaiter signalAwaiter, CancellationToken cancellationToken)
        {
            _signalAwaiter = signalAwaiter;
            _cancellationToken = cancellationToken;
            _cancellationTokenRegistration = _cancellationToken.Register(() =>
            {
                _cancellationTokenRegistration.Dispose();
                _isCancelled = true;
                OnAwaiterCompleted();
            });
        }

        /// <summary>
        /// Completion status of the awaiter. True if canceled or if signal is emitted.
        /// </summary>
        public bool IsCompleted => _isCancelled || _signalAwaiter.IsCompleted;

        /// <summary>
        /// Registers action delegate upon completion.
        /// </summary>
        /// <param name="continuation">Action delegate up completion.</param>
        public void OnCompleted(Action continuation)
        {
            _continuation = continuation;
            _signalAwaiter.OnCompleted(OnAwaiterCompleted);
        }

        /// <summary>
        /// Returns current awaiter as <see cref="IAwaiter"/> that can be used with the await keyword.
        /// </summary>
        /// <returns>Current awaiter as <see cref="IAwaiter"/>.</returns>
        public IAwaiter<Variant[]> GetAwaiter() => this;

        /// <summary>
        /// Returns result upon completion.
        /// </summary>
        /// <returns>Result upon completion.</returns>
        public Variant[] GetResult() => _signalAwaiter.GetResult();

        private void OnAwaiterCompleted()
        {
            var continuation = _continuation;
            _continuation = null;
            continuation?.Invoke();
        }
    }

    public partial struct GDTask
    {
        public static async GDTask<Variant[]> ToSignal(GodotObject self, string signal)
        {
            return await self.ToSignal(self, signal);
        }

        public static async GDTask<Variant[]> ToSignal(GodotObject self, string signal, CancellationToken ct)
        {
            var cancellableSignalAwaiter = new CancellableSignalAwaiter(self.ToSignal(self, signal), ct);
            return await cancellableSignalAwaiter;
        }
    }
}
