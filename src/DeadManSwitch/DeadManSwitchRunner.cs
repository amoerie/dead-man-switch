using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using DeadManSwitch.Logging;

namespace DeadManSwitch
{
    /// <summary>
    /// The entry point to run dead man's switch workers.  
    /// </summary>
    public interface IDeadManSwitchRunner
    {
        /// <summary>
        /// Starts the specified <paramref name="worker"/>, to which it will pass the <see cref="IDeadManSwitch"/> and a cancellation token.
        /// </summary>
        /// <param name="worker">The worker that can perform work asynchronously</param>
        /// <param name="options">The options that specify how the dead man's switch must behave</param>
        /// <param name="cancellationToken">The cancellation token that is capable of immediately stopping the dead man's switch and the worker.</param>
        /// <typeparam name="TResult">The type of result that the worker produces</typeparam>
        /// <returns>The result that the worker has produced</returns>
        /// <exception cref="OperationCanceledException">When the worked was canceled by the dead man's switch, or when the provided <paramref name="cancellationToken"/> is cancelled while the worker is still busy</exception>
        Task<TResult> RunAsync<TResult>(IDeadManSwitchWorker<TResult> worker, DeadManSwitchOptions options, CancellationToken cancellationToken);
    }

    /// <inheritdoc />
    public class DeadManSwitchRunner : IDeadManSwitchRunner
    {
        private readonly IDeadManSwitchSessionFactory _deadManSwitchSessionFactory;
        private readonly IDeadManSwitchLogger<DeadManSwitchRunner> _logger;

        /// <summary>
        /// Creates a new instance of a <see cref="DeadManSwitchRunner"/>
        /// </summary>
        /// <param name="logger">The logger that will be used for diagnostic log messages</param>
        /// <param name="deadManSwitchSessionFactory">The session factory that is capable of starting a new dead man's switch session</param>
        internal DeadManSwitchRunner(IDeadManSwitchLogger<DeadManSwitchRunner> logger,
            IDeadManSwitchSessionFactory deadManSwitchSessionFactory)
        {
            _deadManSwitchSessionFactory = deadManSwitchSessionFactory ?? throw new ArgumentNullException(nameof(deadManSwitchSessionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new <see cref="IDeadManSwitchRunner"/> that is capable of running <see cref="IDeadManSwitchWorker{TResult}"/>
        /// </summary>
        /// <returns>A new <see cref="IDeadManSwitchRunner"/> that is capable of running <see cref="IDeadManSwitchWorker{TResult}"/></returns>
        public static IDeadManSwitchRunner Create()
        {
            return Create(new SilentDeadManSwitchLoggerFactory());
        }

        /// <summary>
        /// Creates a new <see cref="IDeadManSwitchRunner"/> that is capable of running <see cref="IDeadManSwitchWorker{TResult}"/>
        /// </summary>
        /// <param name="loggerFactory">The factory that is capable of creating loggers</param>
        /// <returns>A new <see cref="IDeadManSwitchRunner"/> that is capable of running <see cref="IDeadManSwitchWorker{TResult}"/></returns>
        public static IDeadManSwitchRunner Create(IDeadManSwitchLoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            return new DeadManSwitchRunner(
                loggerFactory.CreateLogger<DeadManSwitchRunner>(),
                new DeadManSwitchSessionFactory(loggerFactory));
        }
        
        /// <inheritdoc />
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