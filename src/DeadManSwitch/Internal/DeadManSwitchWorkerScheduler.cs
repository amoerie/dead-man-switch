using System;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    internal interface IDeadManSwitchWorkerScheduler
    {
        ValueTask<TResult> ScheduleAsync<TResult>(IDeadManSwitchWorker<TResult> deadManSwitchWorker, IDeadManSwitchSession deadManSwitchSession);
    }

    internal class DeadManSwitchWorkerScheduler : IDeadManSwitchWorkerScheduler
    {
        private readonly ILogger _logger;

        public DeadManSwitchWorkerScheduler(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<TResult> ScheduleAsync<TResult>(IDeadManSwitchWorker<TResult> deadManSwitchWorker, IDeadManSwitchSession deadManSwitchSession)
        {
            if (deadManSwitchWorker == null) throw new ArgumentNullException(nameof(deadManSwitchWorker));

            var deadManSwitchContext = deadManSwitchSession.DeadManSwitchContext;
            var deadManSwitch = deadManSwitchSession.DeadManSwitch;
            var cancellationToken = deadManSwitchContext.CancellationTokenSource.Token;

            _logger.LogDebug("Starting worker {WorkerName}", deadManSwitchWorker.Name);
            var workerTask = deadManSwitchWorker.WorkAsync(deadManSwitch, cancellationToken);
            if (!cancellationToken.CanBeCanceled)
            {
                var result = await workerTask.ConfigureAwait(false);
                _logger.LogDebug("Worker {WorkerName} finished gracefully", deadManSwitchWorker.Name);
                return result;
            }

            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            var cancellationTaskCompletionSource = new TaskCompletionSource<DeadManSwitchTaskExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancellationTask = cancellationTaskCompletionSource.Task;
            using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
            {
                _logger.LogTrace("Waiting for worker {WorkerName} to finish or be canceled", deadManSwitchWorker.Name);
                var task = await Task.WhenAny(workerTask, cancellationTask).ConfigureAwait(false);

                // worker task finished before it was cancelled
                if (task == workerTask)
                    return await workerTask.ConfigureAwait(false);

                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
}