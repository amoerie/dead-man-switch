using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using DeadManSwitch.Logging;

namespace DeadManSwitch
{
    /// <summary>
    ///     The entry point to keep running a dead man's switch worker infinitely.
    /// </summary>
    public interface IInfiniteDeadManSwitchRunner
    {
        /// <summary>
        ///     Starts the specified <paramref name="worker" />, to which it will pass a <see cref="IDeadManSwitch" /> and a cancellation token.
        /// </summary>
        /// <param name="worker">The worker that can perform work asynchronously</param>
        /// <param name="options">The options that specify how the dead man's switch must behave</param>
        /// <param name="cancellationToken">The cancellation token that is capable of immediately stopping the dead man's switch and the worker.</param>
        /// <returns>A task that will complete when the provided <paramref name="cancellationToken" /> is cancelled.</returns>
        /// <exception cref="Exception">When the worker throws an exception, this will not be caught</exception>
        Task RunAsync(IInfiniteDeadManSwitchWorker worker, DeadManSwitchOptions options, CancellationToken cancellationToken);
    }

    /// <inheritdoc />
    public class InfiniteDeadManSwitchRunner : IInfiniteDeadManSwitchRunner
    {
        private readonly IDeadManSwitchSessionFactory _deadManSwitchSessionFactory;
        private readonly IDeadManSwitchLogger<InfiniteDeadManSwitchRunner> _logger;

        /// <summary>
        ///     Creates a new instance of a <see cref="InfiniteDeadManSwitchRunner" />
        /// </summary>
        /// <param name="logger">The logger that will be used for diagnostic log messages</param>
        /// <param name="deadManSwitchSessionFactory">The session factory that is capable of starting a new dead man's switch session</param>
        internal InfiniteDeadManSwitchRunner(IDeadManSwitchLogger<InfiniteDeadManSwitchRunner> logger,
            IDeadManSwitchSessionFactory deadManSwitchSessionFactory)
        {
            _deadManSwitchSessionFactory = deadManSwitchSessionFactory ?? throw new ArgumentNullException(nameof(deadManSwitchSessionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task RunAsync(IInfiniteDeadManSwitchWorker worker, DeadManSwitchOptions options, CancellationToken cancellationToken)
        {
            if (worker == null) throw new ArgumentNullException(nameof(worker));

            _logger.Trace("Starting infinite worker loop for {WorkerName} using a dead man's switch", worker.Name);

            using (var deadManSwitchSession = _deadManSwitchSessionFactory.Create(options))
            using (var watcherCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var deadManSwitch = deadManSwitchSession.DeadManSwitch;
                var deadManSwitchWatcher = deadManSwitchSession.DeadManSwitchWatcher;
                var deadManSwitchContext = deadManSwitchSession.DeadManSwitchContext;
                var watcherTask = Task.Factory.StartNew(() => deadManSwitchWatcher.WatchAsync(watcherCTS.Token), CancellationToken.None, TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
                var iteration = 1;
                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var workerCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadManSwitchContext.CancellationToken))
                    {
                        _logger.Trace("Beginning work iteration {Iteration} of infinite worker {WorkerName} using a dead man's switch", iteration, worker.Name);

                        var workerTask = Task.Run(() => worker.WorkAsync(deadManSwitch, workerCTS.Token), CancellationToken.None);

                        try
                        {
                            await workerTask.ConfigureAwait(false);

                            _logger.Debug("Worker {WorkerName} completed gracefully", worker.Name);

                            deadManSwitch.Notify("Worker task completed gracefully");
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.Warning("Worker {WorkerName} was canceled", worker.Name);

                            // Restart watcher
                            await watcherTask.ConfigureAwait(false);

                            deadManSwitch.Notify("Worker task was canceled");

                            watcherTask = Task.Factory.StartNew(() => deadManSwitchWatcher.WatchAsync(watcherCTS.Token), CancellationToken.None, TaskCreationOptions.LongRunning,
                                TaskScheduler.Default);
                        }
                    }

                    iteration++;
                }

                _logger.Information("Cancellation requested, cleaning up infinite worker loop for {WorkerName}", worker.Name);

                watcherCTS.Cancel();
                await watcherTask.ConfigureAwait(false);
            }

            _logger.Trace("Infinite worker loop for {WorkerName} has stopped", worker.Name);
        }

        /// <summary>
        ///     Creates a new <see cref="IInfiniteDeadManSwitchRunner" /> that is capable of running <see cref="IInfiniteDeadManSwitchRunner" />
        /// </summary>
        /// <returns>A new <see cref="IInfiniteDeadManSwitchRunner" /> that is capable of running <see cref="IInfiniteDeadManSwitchWorker" /></returns>
        public static IInfiniteDeadManSwitchRunner Create()
        {
            return Create(new SilentDeadManSwitchLoggerFactory());
        }

        /// <summary>
        ///     Creates a new <see cref="IInfiniteDeadManSwitchRunner" /> that is capable of running <see cref="IInfiniteDeadManSwitchRunner" />
        /// </summary>
        /// <param name="loggerFactory">The factory that is capable of creating loggers</param>
        /// <returns>A new <see cref="IInfiniteDeadManSwitchRunner" /> that is capable of running <see cref="IInfiniteDeadManSwitchWorker" /></returns>
        public static IInfiniteDeadManSwitchRunner Create(IDeadManSwitchLoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            return new InfiniteDeadManSwitchRunner(loggerFactory.CreateLogger<InfiniteDeadManSwitchRunner>(), new DeadManSwitchSessionFactory(loggerFactory));
        }
    }
}