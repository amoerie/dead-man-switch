using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using Microsoft.Extensions.Logging;

namespace DeadManSwitch
{
    public interface IDeadManSwitchTaskOneTimeRunner
    {
        Task<DeadManSwitchTaskOneTimeRunnerResult> RunOneTimeAsync(IDeadManSwitchWorker<> deadManSwitchWorker, CancellationToken cancellationToken);
    }
    
    public class DeadManSwitchTaskOneTimeRunner : IDeadManSwitchTaskOneTimeRunner
    {
        private readonly IDeadManSwitchSessionFactory _deadManSwitchSessionFactory;
        private readonly IDeadManSwitchWorkerScheduler _deadManSwitchWorkerScheduler;
        private readonly ILogger _logger;

        public DeadManSwitchTaskOneTimeRunner(ILogger logger,
            IDeadManSwitchSessionFactory deadManSwitchSessionFactory,
            IDeadManSwitchWorkerScheduler deadManSwitchWorkerScheduler)
        {
            _deadManSwitchSessionFactory = deadManSwitchSessionFactory ?? throw new ArgumentNullException(nameof(deadManSwitchSessionFactory));
            _deadManSwitchWorkerScheduler = deadManSwitchWorkerScheduler ?? throw new ArgumentNullException(nameof(deadManSwitchWorkerScheduler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DeadManSwitchTaskOneTimeRunnerResult> RunOneTimeAsync(IDeadManSwitchWorker<> deadManSwitchWorker, CancellationToken cancellationToken)
        {
            if (deadManSwitchWorker == null) throw new ArgumentNullException(nameof(deadManSwitchWorker));
            
            using(var deadManSwitch = _deadManSwitchSessionFactory.Create(deadManSwitchWorker.Timeout, cancellationToken))
            using(var deadManSwitchCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                _logger.LogTrace("Running task {WorkerName} using a dead man's switch", deadManSwitchWorker.Name);

                var deadManSwitchTaskExecution = Task.Run(() => _deadManSwitchWorkerScheduler.ExecuteAsync(deadManSwitchWorker, deadManSwitch), cancellationToken);
                var deadManSwitchRunTask = deadManSwitch.RunAsync(deadManSwitchCTS.Token);
                
                var deadManSwitchTaskExecutionResult = await deadManSwitchTaskExecution.ConfigureAwait(false);
                
                deadManSwitchCTS.Cancel();
                
                var deadManSwitchTaskResult = await deadManSwitchRunTask.ConfigureAwait(false);
                var result = new DeadManSwitchTaskOneTimeRunnerResult(deadManSwitchTaskExecutionResult, deadManSwitchTaskResult);
                
                _logger.LogTrace("Worker {WorkerName} is finished with result {TaskResult} and dead man's switch result {DeadManSwitchResult}",
                    deadManSwitchWorker.Name, result.DeadManSwitchTaskExecutionResult, result.DeadManSwitchResult);

                return result;
            }
        }
    }
}