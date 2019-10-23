using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    public interface IDeadManSwitchTaskOneTimeRunner
    {
        Task<DeadManSwitchTaskOneTimeRunnerResult> RunOneTimeAsync(IDeadManSwitchTask task, CancellationToken cancellationToken);
    }
    
    public class DeadManSwitchTaskOneTimeRunner : IDeadManSwitchTaskOneTimeRunner
    {
        private readonly IDeadManSwitchFactory _deadManSwitchFactory;
        private readonly IDeadManSwitchTaskExecutor _deadManSwitchTaskExecutor;
        private readonly ILogger _logger;

        public DeadManSwitchTaskOneTimeRunner(ILogger logger,
            IDeadManSwitchFactory deadManSwitchFactory,
            IDeadManSwitchTaskExecutor deadManSwitchTaskExecutor)
        {
            _deadManSwitchFactory = deadManSwitchFactory ?? throw new ArgumentNullException(nameof(deadManSwitchFactory));
            _deadManSwitchTaskExecutor = deadManSwitchTaskExecutor ?? throw new ArgumentNullException(nameof(deadManSwitchTaskExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DeadManSwitchTaskOneTimeRunnerResult> RunOneTimeAsync(IDeadManSwitchTask task, CancellationToken cancellationToken)
        {
            using(var deadManSwitch = _deadManSwitchFactory.Create(task.Timeout, cancellationToken))
            using(var deadManSwitchCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                _logger.LogTrace("Running task {TaskName} using a dead man switch", task.Name);

                var deadManSwitchTaskExecution = Task.Run(() => _deadManSwitchTaskExecutor.ExecuteAsync(task, deadManSwitch), cancellationToken);
                var deadManSwitchTask = deadManSwitch.RunAsync(deadManSwitchCTS.Token);
                
                var deadManSwitchTaskExecutionResult = await deadManSwitchTaskExecution.ConfigureAwait(false);
                
                deadManSwitchCTS.Cancel();
                
                var deadManSwitchTaskResult = await deadManSwitchTask.ConfigureAwait(false);
                var result = new DeadManSwitchTaskOneTimeRunnerResult(deadManSwitchTaskExecutionResult, deadManSwitchTaskResult);
                
                _logger.LogTrace("Task {TaskName} is finished with result {TaskResult} and dead man switch result {DeadManSwitchResult}",
                    task.Name, result.DeadManSwitchTaskExecutionResult, result.DeadManSwitchResult);

                return result;
            }
        }
    }
}