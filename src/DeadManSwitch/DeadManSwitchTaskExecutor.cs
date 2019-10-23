using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    public interface IDeadManSwitchTaskExecutor
    {
        Task<DeadManSwitchTaskExecutionResult> ExecuteAsync(IDeadManSwitchTask task, IDeadManSwitch deadManSwitch);
    }

    public class DeadManSwitchTaskExecutor : IDeadManSwitchTaskExecutor
    {
        private readonly ILogger _logger;

        public DeadManSwitchTaskExecutor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<DeadManSwitchTaskExecutionResult> ExecuteAsync(IDeadManSwitchTask task, IDeadManSwitch deadManSwitch)
        {
            var logger = _logger;
            var cancellationToken = deadManSwitch.CancellationToken;
            try
            {
                logger.LogDebug("Starting task {TaskName}", task.Name);
                var execute = task.ExecuteAsync(deadManSwitch);
                if (!deadManSwitch.CancellationToken.CanBeCanceled)
                {
                    await execute.ConfigureAwait(false);
                    logger.LogDebug("Task {TaskName} finished gracefully", task.Name);
                    return DeadManSwitchTaskExecutionResult.TaskFinishedGracefully;
                }

                if (cancellationToken.IsCancellationRequested)
                    await Task.FromCanceled<DeadManSwitchStatus>(cancellationToken).ConfigureAwait(false);

                var cancellationTaskCompletionSource = new TaskCompletionSource<DeadManSwitchTaskExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                var cancellationTask = cancellationTaskCompletionSource.Task;
                using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
                {
                    logger.LogTrace("Waiting for task {TaskName} to finish or be canceled", task.Name);
                    var winner = await Task.WhenAny(execute, cancellationTask).ConfigureAwait(false);
                    await winner.ConfigureAwait(false);
                    return DeadManSwitchTaskExecutionResult.TaskFinishedGracefully;
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Task {TaskName} was canceled", task.Name);
                return DeadManSwitchTaskExecutionResult.TaskWasCancelled;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Task {TaskName} threw an exception", task.Name);
                return DeadManSwitchTaskExecutionResult.TaskThrewAnException;
            }
        }
    }
}