using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;

namespace DeadManSwitch
{
    /// <summary>
    /// Provides a convenient static API to run asynchronous workers under the supervision of a dead man's switch 
    /// </summary>
    public static class DeadManSwitchTask
    {
        private static readonly Lazy<IDeadManSwitchRunner> DeadManSwitchRunner 
            = new Lazy<IDeadManSwitchRunner>(global::DeadManSwitch.DeadManSwitchRunner.Create);
        
        /// <summary>
        /// Starts the specified <paramref name="worker"/>, to which it will pass the <see cref="IDeadManSwitch"/> and a cancellation token.
        /// </summary>
        /// <param name="worker">The worker that can works asynchronously and is provided with a dead man switch and a cancellation token</param>
        /// <param name="options">The options that specify how the dead man's switch must behave</param>
        /// <param name="cancellationToken">The cancellation token that is capable of immediately stopping the dead man's switch and the worker.</param>
        /// <typeparam name="TResult">The type of result that the worker produces</typeparam>
        /// <returns>The result that the worker has produced</returns>
        /// <exception cref="OperationCanceledException">When the worked was canceled by the dead man's switch, or when the provided <paramref name="cancellationToken"/> is cancelled while the worker is still busy</exception>
        public static Task<TResult> RunAsync<TResult>(
            Func<IDeadManSwitch, CancellationToken, Task<TResult>> worker,
            DeadManSwitchOptions options,
            CancellationToken cancellationToken)
        {
            return DeadManSwitchRunner.Value.RunAsync(new LambdaDeadManSwitchWorker<TResult>(worker), options, cancellationToken);
        }
    }
}