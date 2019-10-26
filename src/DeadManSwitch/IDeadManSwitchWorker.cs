using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch
{
    /// <summary>
    /// Represents a worker that should be cancelled automatically if it does not notify the dead man's switch in a timely fashion
    /// </summary>
    /// <typeparam name="TResult">The type of result this worker produces</typeparam>
    public interface IDeadManSwitchWorker<TResult>
    {
        /// <summary>
        /// The name of this worker
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes a task using a dead man's switch. If the switch is not notified in a periodical, timely manner, the
        /// work will be cancelled using the cancellation token.
        /// </summary>
        /// <param name="deadManSwitch">The dead man's switch that should be notified every x seconds.</param>
        /// <param name="cancellationToken">The cancellation token that will be marked as cancelled when the dead man's switch is not notified in a timely fashion</param>
        /// <returns><see cref="TResult"/></returns>
        Task<TResult> WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken);
    }
}