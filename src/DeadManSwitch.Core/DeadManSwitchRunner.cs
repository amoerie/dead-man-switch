using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using DeadManSwitch.Logging;

namespace DeadManSwitch
{
    public interface IDeadManSwitchRunner
    {
        Task<TResult> RunAsync<TResult>(IDeadManSwitchWorker<TResult> worker, DeadManSwitchOptions options, CancellationToken cancellationToken);
    }

    public class DeadManSwitchRunner : IDeadManSwitchRunner
    {
        private readonly IDeadManSwitchSessionFactory _deadManSwitchSessionFactory;
        private readonly IDeadManSwitchLogger<DeadManSwitchRunner> _logger;

        public DeadManSwitchRunner(IDeadManSwitchLogger<DeadManSwitchRunner> logger,
            IDeadManSwitchSessionFactory deadManSwitchSessionFactory)
        {
            _deadManSwitchSessionFactory = deadManSwitchSessionFactory ?? throw new ArgumentNullException(nameof(deadManSwitchSessionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResult> RunAsync<TResult>(IDeadManSwitchWorker<TResult> worker, DeadManSwitchOptions options,
            CancellationToken cancellationToken)
        {
            if (worker == null) throw new ArgumentNullException(nameof(worker));

            using (var deadManSwitchSession = _deadManSwitchSessionFactory.Create(options))
            using (var watcherCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var workerCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadManSwitchSession.DeadManSwitchContext.CancellationTokenSource.Token))
            {
                _logger.Trace("Running worker {WorkerName} using a dead man's switch", worker.Name);

                var deadManSwitch = deadManSwitchSession.DeadManSwitch;
                var deadManSwitchWatcher = deadManSwitchSession.DeadManSwitchWatcher;

                var workerTask = Task.Run(async () => await worker.WorkAsync(deadManSwitch, workerCTS.Token).ConfigureAwait(false), CancellationToken.None);
                var watcherTask = Task.Run(async () => await deadManSwitchWatcher.WatchAsync(watcherCTS.Token).ConfigureAwait(false), CancellationToken.None);

                var task = await Task.WhenAny(workerTask, watcherTask).ConfigureAwait(false);
                if (task == workerTask)
                {
                    watcherCTS.Cancel();
                    return await workerTask.ConfigureAwait(false);
                }
                
                workerCTS.Cancel();
                throw new OperationCanceledException(workerCTS.Token);
            }
        }
    }
}