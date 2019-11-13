using System.Threading;
using System.Threading.Tasks;

namespace DeadManSwitch
{
    /// <summary>
    ///     Represents a worker that should be cancelled automatically if it does not pull the dead man's switch in a timely fashion.
    ///     An <see cref="IInfiniteDeadManSwitchWorker" /> will be called repeatedly after each execution
    /// </summary>
    public interface IInfiniteDeadManSwitchWorker
    {
        /// <summary>
        ///     The name of this worker
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Executes a worker using a dead man's switch. If the switch is not notified in a periodical, timely manner, the
        ///     work will be cancelled using the cancellation token.
        /// </summary>
        /// <param name="deadManSwitch">The dead man's switch that should be notified every x seconds.</param>
        /// <param name="cancellationToken">The cancellation token that will be marked as cancelled when the dead man's switch is not notified in a timely fashion</param>
        Task WorkAsync(IDeadManSwitch deadManSwitch, CancellationToken cancellationToken);
    }
}