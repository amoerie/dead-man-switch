using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    public interface IDeadManSwitchTaskInfiniteRunner
    {
        Task<DeadManSwitchTaskInfiniteRunnerResult> RunInfinitelyAsync(IDeadManSwitchInfiniteTask task, CancellationToken cancellationToken);
    }

    public class DeadManSwitchTaskInfiniteRunner : IDeadManSwitchTaskInfiniteRunner
    {
        private readonly IDeadManSwitchTaskOneTimeRunner _deadManSwitchTaskOneTimeRunner;
        private readonly ILogger _logger;

        public DeadManSwitchTaskInfiniteRunner(ILogger logger,
            IDeadManSwitchTaskOneTimeRunner deadManSwitchTaskOneTimeRunner)
        {
            _deadManSwitchTaskOneTimeRunner = deadManSwitchTaskOneTimeRunner ?? throw new ArgumentNullException(nameof(deadManSwitchTaskOneTimeRunner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DeadManSwitchTaskInfiniteRunnerResult> RunInfinitelyAsync(IDeadManSwitchInfiniteTask task, CancellationToken cancellationToken)
        {
            var result = new DeadManSwitchTaskInfiniteRunnerResult();

            _logger.LogTrace($"Running task {task.Name} infinitely using a dead man switch");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                var oneTimeResult = await _deadManSwitchTaskOneTimeRunner.RunOneTimeAsync(task, cancellationToken).ConfigureAwait(false);

                result.Report(oneTimeResult.DeadManSwitchTaskExecutionResult, oneTimeResult.DeadManSwitchResult);

                try
                {
                    await Task.Delay(task.Delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
            }
            
            _logger.LogTrace($"Stopping infinite task {task.Name}, with the following results: {result}");

            return result;
        }
    }

    public class DeadManSwitchTaskInfiniteRunnerResult
    {
        public int TasksThatFinishedGracefully { get; set; }
        public int TasksThatThrewAnException { get; set; }
        public int TasksThatWereCanceled { get; set; }
        public int DeadManSwitchesTriggered { get; set; }

        public void Report(DeadManSwitchTaskExecutionResult executionResult, DeadManSwitchResult switchResult)
        {
            switch (executionResult)
            {
                case DeadManSwitchTaskExecutionResult.TaskFinishedGracefully:
                    TasksThatFinishedGracefully++;
                    break;
                case DeadManSwitchTaskExecutionResult.TaskThrewAnException:
                    TasksThatThrewAnException++;
                    break;
                case DeadManSwitchTaskExecutionResult.TaskWasCancelled:
                    TasksThatWereCanceled++;
                    break;
            }

            switch (switchResult)
            {    
                case DeadManSwitchResult.DeadManSwitchWasTriggered:
                    DeadManSwitchesTriggered++;
                    break;
            }
        }

        public override string ToString()
        {
            return $"{nameof(TasksThatFinishedGracefully)}: {TasksThatFinishedGracefully}, " +
                   $"{nameof(TasksThatThrewAnException)}: {TasksThatThrewAnException}, " +
                   $"{nameof(TasksThatWereCanceled)}: {TasksThatWereCanceled}, " +
                   $"{nameof(DeadManSwitchesTriggered)}: {DeadManSwitchesTriggered}";
        }
    }
}