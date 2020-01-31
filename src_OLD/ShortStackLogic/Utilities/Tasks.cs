namespace Microsoft.Tools.Productivity.ShortStack.Utilities
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Tasks
    {
        /// <summary>
        /// Allows performing long-running operations using a back-ground thread.
        /// </summary>
        /// <typeparam name="T">The return type of the long-running function.</typeparam>
        /// <param name="func">The function that will perform the long running operation.</param>
        /// <param name="cancellationToken">The cancellation token to be used by the long running operation.</param>
        /// <returns>Returns a task that can be "await"ed on.</returns>
        /// <remarks>
        /// This version of the generic allows for the function to be marked "async" and use the "await" keyword.
        /// </remarks>
        public static Task<T> PerformLongRunningOperation<T>(Func<Task<T>> func, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();

            new Thread(new ThreadStart(async () =>
            {
                T result;
                try
                {
                    result = await func();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        taskCompletionSource.SetCanceled();
                    }
                    else
                    {
                        taskCompletionSource.SetResult(result);
                    }
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            })).Start();

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Allows performing long-running operations using a back-ground thread.
        /// </summary>
        /// <typeparam name="T">The return type of the long-running function.</typeparam>
        /// <param name="func">The function that will perform the long running operation.</param>
        /// <param name="cancellationToken">The cancellation token to be used by the long running operation.</param>
        /// <returns>Returns a task that can be "await"ed on.</returns>
        /// <remarks>
        /// This version of the generic allows for functions that are not marked "async" and do not use "await".
        /// </remarks>
        public static Task<T> PerformLongRunningOperation<T>(Func<T> func, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();

            new Thread(new ThreadStart(() =>
            {
                T result;
                try
                {
                    result = func();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        taskCompletionSource.SetCanceled();
                    }
                    else
                    {
                        taskCompletionSource.SetResult(result);
                    }
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            })).Start();

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Allows performing long-running operations using a back-ground thread.
        /// </summary>
        /// <typeparam name="T">The return type of the long-running function.</typeparam>
        /// <param name="action">The function that will perform the long running operation.</param>
        /// <param name="cancellationToken">The cancellation token to be used by the long running operation.</param>
        /// <returns>Returns a task that can be "await"ed on.</returns>
        /// <remarks>
        /// This version of the generic allows for functions that are not marked "async" and do not use "await".
        /// </remarks>
        public static Task PerformLongRunningOperation(Action action, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            new Thread(new ThreadStart(() =>
            {
                try
                {
                    action();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        taskCompletionSource.SetCanceled();
                    }
                    else
                    {
                        taskCompletionSource.SetResult(null);
                    }
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            })).Start();

            return taskCompletionSource.Task;
        }
    }
}
