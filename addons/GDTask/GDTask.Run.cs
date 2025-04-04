using System;
using System.Threading;

namespace Fractural.Tasks;

public partial struct GDTask
{
    #region OBSOLETE_RUN

    [Obsolete(
        "GDTask.Run is similar as Task.Run, it uses ThreadPool. For equivalent behaviour, use GDTask.RunOnThreadPool instead. If you don't want to use ThreadPool, you can use GDTask.Void(async void) or GDTask.Create(async GDTask) too."
    )]
    public static GDTask Run(Action action, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        return RunOnThreadPool(action, configureAwait, cancellationToken);
    }

    [Obsolete(
        "GDTask.Run is similar as Task.Run, it uses ThreadPool. For equivalent behaviour, use GDTask.RunOnThreadPool instead. If you don't want to use ThreadPool, you can use GDTask.Void(async void) or GDTask.Create(async GDTask) too."
    )]
    public static GDTask Run(Action<object> action, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        return RunOnThreadPool(action, state, configureAwait, cancellationToken);
    }

    [Obsolete(
        "GDTask.Run is similar as Task.Run, it uses ThreadPool. For equivalent behaviour, use GDTask.RunOnThreadPool instead. If you don't want to use ThreadPool, you can use GDTask.Void(async void) or GDTask.Create(async GDTask) too."
    )]
    public static GDTask Run(Func<GDTask> action, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        return RunOnThreadPool(action, configureAwait, cancellationToken);
    }

    [Obsolete(
        "GDTask.Run is similar as Task.Run, it uses ThreadPool. For equivalent behaviour, use GDTask.RunOnThreadPool instead. If you don't want to use ThreadPool, you can use GDTask.Void(async void) or GDTask.Create(async GDTask) too."
    )]
    public static GDTask Run(Func<object, GDTask> action, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        return RunOnThreadPool(action, state, configureAwait, cancellationToken);
    }

    [Obsolete(
        "GDTask.Run is similar as Task.Run, it uses ThreadPool. For equivalent behaviour, use GDTask.RunOnThreadPool instead. If you don't want to use ThreadPool, you can use GDTask.Void(async void) or GDTask.Create(async GDTask) too."
    )]
    public static GDTask<T> Run<T>(Func<T> func, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        return RunOnThreadPool(func, configureAwait, cancellationToken);
    }

    [Obsolete(
        "GDTask.Run is similar as Task.Run, it uses ThreadPool. For equivalent behaviour, use GDTask.RunOnThreadPool instead. If you don't want to use ThreadPool, you can use GDTask.Void(async void) or GDTask.Create(async GDTask) too."
    )]
    public static GDTask<T> Run<T>(Func<GDTask<T>> func, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        return RunOnThreadPool(func, configureAwait, cancellationToken);
    }

    [Obsolete(
        "GDTask.Run is similar as Task.Run, it uses ThreadPool. For equivalent behaviour, use GDTask.RunOnThreadPool instead. If you don't want to use ThreadPool, you can use GDTask.Void(async void) or GDTask.Create(async GDTask) too."
    )]
    public static GDTask<T> Run<T>(Func<object, T> func, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        return RunOnThreadPool(func, state, configureAwait, cancellationToken);
    }

    [Obsolete(
        "GDTask.Run is similar as Task.Run, it uses ThreadPool. For equivalent behaviour, use GDTask.RunOnThreadPool instead. If you don't want to use ThreadPool, you can use GDTask.Void(async void) or GDTask.Create(async GDTask) too."
    )]
    public static GDTask<T> Run<T>(
        Func<object, GDTask<T>> func,
        object state,
        bool configureAwait = true,
        CancellationToken cancellationToken = default
    )
    {
        return RunOnThreadPool(func, state, configureAwait, cancellationToken);
    }

    #endregion

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async GDTask RunOnThreadPool(Action action, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await GDTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                action();
            }
            finally
            {
                await GDTask.Yield();
            }
        }
        else
        {
            action();
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async GDTask RunOnThreadPool(
        Action<object> action,
        object state,
        bool configureAwait = true,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await GDTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                action(state);
            }
            finally
            {
                await GDTask.Yield();
            }
        }
        else
        {
            action(state);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async GDTask RunOnThreadPool(Func<GDTask> action, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await GDTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                await action();
            }
            finally
            {
                await GDTask.Yield();
            }
        }
        else
        {
            await action();
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async GDTask RunOnThreadPool(
        Func<object, GDTask> action,
        object state,
        bool configureAwait = true,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await GDTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                await action(state);
            }
            finally
            {
                await GDTask.Yield();
            }
        }
        else
        {
            await action(state);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async GDTask<T> RunOnThreadPool<T>(Func<T> func, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await GDTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                return func();
            }
            finally
            {
                await GDTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            return func();
        }
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async GDTask<T> RunOnThreadPool<T>(Func<GDTask<T>> func, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await GDTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                return await func();
            }
            finally
            {
                cancellationToken.ThrowIfCancellationRequested();
                await GDTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            var result = await func();
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async GDTask<T> RunOnThreadPool<T>(
        Func<object, T> func,
        object state,
        bool configureAwait = true,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await GDTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                return func(state);
            }
            finally
            {
                await GDTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            return func(state);
        }
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async GDTask<T> RunOnThreadPool<T>(
        Func<object, GDTask<T>> func,
        object state,
        bool configureAwait = true,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await GDTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                return await func(state);
            }
            finally
            {
                cancellationToken.ThrowIfCancellationRequested();
                await GDTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            var result = await func(state);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
    }
}
