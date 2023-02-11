using System;

namespace Fractural.Tasks
{
    public abstract class MoveNextSource : IGDTaskSource<bool>
    {
        protected GDTaskCompletionSourceCore<bool> completionSource;

        public bool GetResult(short token)
        {
            return completionSource.GetResult(token);
        }

        public GDTaskStatus GetStatus(short token)
        {
            return completionSource.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            completionSource.OnCompleted(continuation, state, token);
        }

        public GDTaskStatus UnsafeGetStatus()
        {
            return completionSource.UnsafeGetStatus();
        }

        void IGDTaskSource.GetResult(short token)
        {
            completionSource.GetResult(token);
        }

        protected bool TryGetResult<T>(GDTask<T>.Awaiter awaiter, out T result)
        {
            try
            {
                result = awaiter.GetResult();
                return true;
            }
            catch (Exception ex)
            {
                completionSource.TrySetException(ex);
                result = default;
                return false;
            }
        }

        protected bool TryGetResult(GDTask.Awaiter awaiter)
        {
            try
            {
                awaiter.GetResult();
                return true;
            }
            catch (Exception ex)
            {
                completionSource.TrySetException(ex);
                return false;
            }
        }
    }
}
