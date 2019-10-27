/*using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    public interface IDeadManSwitchTaskInfiniteRunner
    {
        Task<DeadManSwitchTaskInfiniteRunnerResult> RunInfinitelyAsync(IDeadManSwitchInfiniteWorker worker, CancellationToken cancellationToken);
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

        public async Task<DeadManSwitchTaskInfiniteRunnerResult> RunInfinitelyAsync(IDeadManSwitchInfiniteWorker worker, CancellationToken cancellationToken)
        {
            var result = new DeadManSwitchTaskInfiniteRunnerResult();

            _logger.LogTrace("Running worker {WorkerName} infinitely using a dead man's switch", worker.Name);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                var oneTimeResult = await _deadManSwitchTaskOneTimeRunner.RunOneTimeAsync(worker, cancellationToken).ConfigureAwait(false);

                result.Report(oneTimeResult.DeadManSwitchTaskExecutionResult, oneTimeResult.DeadManSwitchResult);

                try
                {
                    await Task.Delay(worker.Delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
            }
            
            _logger.LogTrace("Infinite worker {WorkerName} has been cancelled, these are the results:", worker.Name);
            _logger.LogTrace("Workers that finished gracefully: {TasksThatFinishedGracefully}", result.TasksThatFinishedGracefully);
            _logger.LogTrace("Workers that threw an exception : {TasksThatThrewAnException}", result.TasksThatThrewAnException);
            _logger.LogTrace("Workers that were canceled      : {TasksThatWereCanceled}", result.TasksThatWereCanceled);
            _logger.LogTrace("Dead man switches triggered   : {DeadManSwitchesTriggered}", result.DeadManSwitchesTriggered);
            return result;
        }
    }
}*/