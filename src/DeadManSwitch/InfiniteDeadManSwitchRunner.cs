using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    public interface IInfiniteDeadManSwitchRunner
    {
        Task RunAsync(IInfiniteDeadManSwitchWorker worker, DeadManSwitchOptions options, CancellationToken cancellationToken);
    }

    public class InfiniteDeadManSwitchRunner : IInfiniteDeadManSwitchRunner
    {
        private readonly IDeadManSwitchSessionFactory _deadManSwitchSessionFactory;
        private readonly ILogger _logger;

        public InfiniteDeadManSwitchRunner(ILogger logger,
            IDeadManSwitchSessionFactory deadManSwitchSessionFactory)
        {
            _deadManSwitchSessionFactory = deadManSwitchSessionFactory ?? throw new ArgumentNullException(nameof(deadManSwitchSessionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync(IInfiniteDeadManSwitchWorker worker, DeadManSwitchOptions options, CancellationToken cancellationToken)
        {
            if (worker == null) throw new ArgumentNullException(nameof(worker));

            using (var deadManSwitchSession = _deadManSwitchSessionFactory.Create(options))
            using (var watcherCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var deadManSwitch = deadManSwitchSession.DeadManSwitch;
                var deadManSwitchWatcher = deadManSwitchSession.DeadManSwitchWatcher;
                var watcherTask = Task.Run(async () => await deadManSwitchWatcher.WatchAsync(watcherCTS.Token).ConfigureAwait(false), watcherCTS.Token);

                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var workerCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadManSwitchSession.DeadManSwitchContext.CancellationTokenSource.Token))
                    {
                        _logger.LogTrace("Running worker {WorkerName} using a dead man's switch", worker.Name);

                        var workerTask = Task.Run(async () => await worker.WorkAsync(deadManSwitch, workerCTS.Token).ConfigureAwait(false), workerCTS.Token);

                        var task = await Task.WhenAny(workerTask, watcherTask).ConfigureAwait(false);
                        if (task == watcherTask)
                        {
                            workerCTS.Cancel();
                            watcherTask = Task.Run(async () => await deadManSwitchWatcher.WatchAsync(watcherCTS.Token).ConfigureAwait(false), watcherCTS.Token);
                        }
                        else
                        {
                            await workerTask.ConfigureAwait(false);
                            await deadManSwitch.NotifyAsync("Worker task completed gracefully", cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }
        }
    }
}