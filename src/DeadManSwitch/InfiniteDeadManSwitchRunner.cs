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
                var deadManSwitchContext = deadManSwitchSession.DeadManSwitchContext;
                var watcherTask = Task.Factory.StartNew(() => deadManSwitchWatcher.WatchAsync(watcherCTS.Token), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var workerCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadManSwitchContext.CancellationTokenSource.Token))
                    {
                        _logger.LogTrace("Running worker {WorkerName} using a dead man's switch", worker.Name);

                        var workerTask = Task.Run(() => worker.WorkAsync(deadManSwitch, workerCTS.Token), CancellationToken.None);

                        try
                        {
                            await workerTask.ConfigureAwait(false);
                            
                            _logger.LogDebug("Worker {WorkerName} completed gracefully", worker.Name);
                            
                            await deadManSwitch.NotifyAsync("Worker task completed gracefully", CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogWarning("Worker {WorkerName} was canceled");
                            
                            // Restart watcher
                            await watcherTask.ConfigureAwait(false);
                            
                            await deadManSwitch.NotifyAsync("Worker task was canceled", CancellationToken.None).ConfigureAwait(false);
                            
                            watcherTask = Task.Factory.StartNew(() => deadManSwitchWatcher.WatchAsync(watcherCTS.Token), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "Worker {WorkerName} threw an exception", exception);
                            
                            await deadManSwitch.NotifyAsync("Worker task threw an exception", CancellationToken.None).ConfigureAwait(false);
                        }
                    }
                }

                _logger.LogInformation("Infinite runner was canceled. Cleaning up.");

                watcherCTS.Cancel();
                await watcherTask.ConfigureAwait(false);
            }
        }
    }
}