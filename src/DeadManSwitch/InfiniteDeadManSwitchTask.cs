using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;

namespace DeadManSwitch
{
    /// <summary>
    /// Provides a convenient static API to run infinite asynchronous workers under the supervision of a dead man's switch 
    /// </summary>
    public static class InfiniteDeadManSwitchTask
    {
        private static readonly Lazy<IInfiniteDeadManSwitchRunner> InfiniteDeadManSwitchRunner
            = new Lazy<IInfiniteDeadManSwitchRunner>(global::DeadManSwitch.InfiniteDeadManSwitchRunner.Create);

        /// <summary>
        /// Starts the specified <paramref name="worker"/>, to which it will pass a <see cref="IDeadManSwitch"/> and a cancellation token.
        /// </summary>
        /// <param name="worker">The worker that can perform work asynchronously</param>
        /// <param name="options">The options that specify how the dead man's switch must behave</param>
        /// <param name="cancellationToken">The cancellation token that is capable of immediately stopping the dead man's switch and the worker.</param>
        /// <returns>A task that will complete when the provided <paramref name="cancellationToken"/> is cancelled.</returns>
        /// <exception cref="Exception">When the worker throws an exception, this will not be caught</exception>
        public static Task RunAsync(Func<IDeadManSwitch, CancellationToken, Task> worker, DeadManSwitchOptions options, CancellationToken cancellationToken)
        {
            return InfiniteDeadManSwitchRunner.Value.RunAsync(new LambdaInfiniteDeadManSwitchWorker(worker), options, cancellationToken);
        }
    }
}