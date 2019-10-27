using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    public interface IDeadManSwitchManager
    {
        Task<TResult> RunAsync<TResult>(IDeadManSwitchWorker<TResult> deadManSwitchWorker, DeadManSwitchOptions deadManSwitchOptions, CancellationToken cancellationToken);
    }

    public class DeadManSwitchManager : IDeadManSwitchManager
    {
        private readonly IDeadManSwitchSessionFactory _deadManSwitchSessionFactory;
        private readonly ILogger _logger;

        public DeadManSwitchManager(ILogger logger,
            IDeadManSwitchSessionFactory deadManSwitchSessionFactory)
        {
            _deadManSwitchSessionFactory = deadManSwitchSessionFactory ?? throw new ArgumentNullException(nameof(deadManSwitchSessionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResult> RunAsync<TResult>(IDeadManSwitchWorker<TResult> deadManSwitchWorker, DeadManSwitchOptions deadManSwitchOptions,
            CancellationToken cancellationToken)
        {
            if (deadManSwitchWorker == null) throw new ArgumentNullException(nameof(deadManSwitchWorker));

            using (var deadManSwitchSession = _deadManSwitchSessionFactory.Create(deadManSwitchOptions))
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadManSwitchSession.DeadManSwitchContext.CancellationTokenSource.Token))
            {
                _logger.LogTrace("Running worker {WorkerName} using a dead man's switch", deadManSwitchWorker.Name);

                var deadManSwitch = deadManSwitchSession.DeadManSwitch;
                var deadManSwitchWatcher = deadManSwitchSession.DeadManSwitchWatcher;

                var workerTask = Task.Run(async () => await deadManSwitchWorker.WorkAsync(deadManSwitch, cts.Token).ConfigureAwait(false), cts.Token);
                var watcherTask = Task.Run(async () => await deadManSwitchWatcher.WatchAsync(cts.Token).ConfigureAwait(false), cts.Token);

                var worker = await Task.WhenAny(workerTask, watcherTask).ConfigureAwait(false);
                if (worker == workerTask)
                {
                    return await workerTask.ConfigureAwait(false);
                }

                throw new OperationCanceledException(cts.Token);
            }
        }
    }
}