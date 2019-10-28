using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace DeadManSwitch.Tests
{
    public sealed class TestsForInfiniteDeadManSwitchRunner : IDisposable
    {
        private readonly ILogger<TestsForInfiniteDeadManSwitchRunner> _logger;
        private readonly InfiniteDeadManSwitchRunner _runner;
        private readonly CancellationTokenSource _cts;
        private readonly ILoggerFactory _loggerFactory;
        private readonly CapturingDeadManSwitchSessionFactory _sessionFactory;

        public TestsForInfiniteDeadManSwitchRunner(ITestOutputHelper testOutputHelper)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.TestOutput(testOutputHelper, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level,-9:w9}] {Message}{NewLine}{Exception}")
                .CreateLogger();
            _loggerFactory = LoggerFactory.Create(builder => { builder.AddSerilog(logger, dispose: true); });
            _logger = _loggerFactory.CreateLogger<TestsForInfiniteDeadManSwitchRunner>();
            _sessionFactory = new CapturingDeadManSwitchSessionFactory(new DeadManSwitchSessionFactory(_logger));
            _runner = new InfiniteDeadManSwitchRunner(_logger, _sessionFactory);
            _cts = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
            _cts.Dispose();
        }

        [Fact]
        public async Task ShouldLetTaskFinishIfItCompletesImmediately()
        {
            // Arrange
            double? pi = null;
            var options = new DeadManSwitchOptions { Timeout = TimeSpan.FromSeconds(2) };
            var workItems = WorkItems(
                Work(Do(_ => pi = Math.PI)),
                Work(Do(_ => _cts.Cancel()))
            );
            var worker = Worker(workItems);
    
            // Act
            await _runner.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().Be(Math.PI);
        }

        [Fact]
        public async Task ShouldRunTaskMultipleTimesUntilStopped()
        {
            // Arrange
            List<double> pies = new List<double>();
            var options = new DeadManSwitchOptions { Timeout = TimeSpan.FromSeconds(2) };
            var workItems = WorkItems(
                Work(Do(_ => pies.Add(Math.PI))),
                Work(Do(_ => pies.Add(Math.PI))),
                Work(Do(_ => pies.Add(Math.PI))),
                Work(Do(_ => _cts.Cancel()))
            );
            var worker = Worker(workItems);

            // Act
            await _runner.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

            // Assert
            pies.Should().HaveCount(3);
            pies.Should().AllBeEquivalentTo(Math.PI);
        }

        [Fact]
        public async Task ShouldRunTaskAgainAfterStoppingItTheFirstTime()
        {
            // Arrange
            double? pi = null;
            double? e = null;
            var options = new DeadManSwitchOptions { Timeout = TimeSpan.FromSeconds(2) };
            var workItems = WorkItems(
                Work(
                    Notify("Going to sleep for 5 seconds"),
                    Do(_ => _logger.LogInformation("In loop 1, this should be cancelled")),
                    Sleep(TimeSpan.FromSeconds(5)),
                    Do(_ => _logger.LogInformation("In loop 1, task was not aborted!!!")),
                    Do(_ => e = Math.E)
                ),
                Work(
                    Do(_ => _logger.LogInformation("In loop 2, assigning Math.PI")),
                    Do(_ => pi = Math.PI)
                ),
                Work(
                    Do(_ => _logger.LogInformation("In loop 3, stopping worker")),
                    Do(_ => _cts.Cancel())
                )
            );
            var worker = Worker(workItems);

            // Act
            await _runner.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

            // Assert
            e.Should().BeNull();
            pi.Should().Be(Math.PI);
        }

        [Fact]
        public async Task ShouldNotStopTaskIfTaskIsNotifyingTheDeadManSwitch()
        {
            // Arrange
            double? pi = null;
            double? e = null;
            var options = new DeadManSwitchOptions { Timeout = TimeSpan.FromSeconds(2) };
            var workItems = WorkItems(
                Work(
                    Notify("Going to sleep for 1 second"),
                    Sleep(TimeSpan.FromSeconds(1)),
                    Notify("Going to sleep for 1 second"),
                    Sleep(TimeSpan.FromSeconds(1)),
                    Notify("Going to sleep for 1 second"),
                    Sleep(TimeSpan.FromSeconds(1)),
                    Do(_ => e = Math.E)
                ),
                Work(
                    Do(_ => pi = Math.PI)
                ),
                Work(
                    Do(_ => _cts.Cancel())
                )
            );
            var worker = Worker(workItems);

            // Act
            await _runner.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

            // Assert
            e.Should().Be(Math.E);
            pi.Should().Be(Math.PI);
        }
        
        [Fact]
        public async Task ShouldCancelTheTaskWhenTokenIsCancelled()
        {
            // Arrange
            double? pi = null;
            var options = new DeadManSwitchOptions { Timeout = TimeSpan.FromSeconds(5) };
            var workItems = WorkItems(
                Work(
                    Sleep(TimeSpan.FromSeconds(2)),
                    Do(_ => pi = Math.PI)
                )
            );
            var worker = Worker(workItems);

            // Act
            var run = _runner.RunAsync(worker, options, _cts.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
            
            _cts.Cancel();

            await run.ConfigureAwait(false);

            // Assert
            pi.Should().BeNull();
        }

        [Fact]
        public async Task ShouldCancelTheTaskWhenTasksTimeOutAndWhenCancelling()
        {
            // Arrange
            double? pi = null;
            var options = new DeadManSwitchOptions { Timeout = TimeSpan.FromSeconds(3) };
            var workItems = WorkItems(
                Work(
                    Sleep(TimeSpan.FromSeconds(4)),
                    Do(_ => pi = Math.PI)
                ),
                Work(
                    Do(_ => _cts.Cancel())
                )
            );
            var worker = Worker(workItems);

            // Act
            var run = _runner.RunAsync(worker, options, _cts.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
            _cts.Cancel();
            await run.ConfigureAwait(false);

            // Assert
            pi.Should().BeNull();
        }
        
        [Fact]
        public async Task ShouldNotCancelTheTaskIfTheDeadManSwitchIsPausedAndTheTaskTakesTooLong()
        {
            // Arrange
            double? pi = null;
            var options = new DeadManSwitchOptions { Timeout = TimeSpan.FromSeconds(2) }; 
            var workItems = WorkItems(
                Work(
                    Pause(),
                    Sleep(TimeSpan.FromSeconds(3)),
                    Do(_ => pi = Math.PI),
                    Resume()
                ),
                Work(
                    Do(_ => _cts.Cancel())
                )
            );
            var worker = new ConfigurableDeadManSwitchInfiniteWorker(workItems);
            
            // Act
            await _runner.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().NotBeNull();
        }
        
        [Fact]
        public async Task ShouldCancelTheTaskIfTheDeadManSwitchWasPausedAndResumedAndTheTaskTakesTooLong()
        {
            // Arrange
            double? pi = null, e = null;
            var options = new DeadManSwitchOptions { Timeout = TimeSpan.FromSeconds(2) }; 
            var workItems = WorkItems(
                Work(
                    Pause(),
                    Sleep(TimeSpan.FromSeconds(3)),
                    Do(_ => pi = Math.PI),
                    Resume(),
                    Sleep(TimeSpan.FromSeconds(3)),
                    Do(_ => e = Math.E)
                ),
                Work(
                    Do(_ => _cts.Cancel())
                )
            );
            var worker = new ConfigurableDeadManSwitchInfiniteWorker(workItems);
            
            // Act
            await _runner.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().NotBeNull();
            e.Should().BeNull();
        }
        
        #region helper methods

        private static IEnumerable<Func<IDeadManSwitch, CancellationToken, Task>> WorkItems(params Func<IDeadManSwitch, CancellationToken, Task>[] workItems)
        {
            return workItems;
        }
        
        private static Func<IDeadManSwitch, CancellationToken, Task> Work(params Func<IDeadManSwitch, CancellationToken, Task>[] workItems)
        {
            return async (deadManSwitch, cancellationToken) =>
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);
                foreach (var task in workItems)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);

                    await task(deadManSwitch, cancellationToken).ConfigureAwait(false);
                }
            };
        }

        private static ConfigurableDeadManSwitchInfiniteWorker Worker(IEnumerable<Func<IDeadManSwitch, CancellationToken, Task>> workItems)
        {
            return new ConfigurableDeadManSwitchInfiniteWorker(workItems);
        }

        private static Func<IDeadManSwitch, CancellationToken, Task> Notify(string notification)
        {
            return async (deadManSwitch, cancellationToken) => { await deadManSwitch.NotifyAsync(notification, cancellationToken).ConfigureAwait(false); };
        }

        private static Func<IDeadManSwitch, CancellationToken, Task> Sleep(TimeSpan duration)
        {
            return async (deadManSwitch, cancellationToken) => { await Task.Delay(duration, cancellationToken).ConfigureAwait(false); };
        }

        private static Func<IDeadManSwitch, CancellationToken, Task> Do(Action<IDeadManSwitch> action)
        {
            return (deadManSwitch, cancellationToken) =>
            {
                action(deadManSwitch);
                return Task.CompletedTask;
            };
        }

        private static Func<IDeadManSwitch, CancellationToken, Task> Pause()
        {
            return (deadManSwitch, cancellationToken) => deadManSwitch.SuspendAsync(cancellationToken).AsTask();
        }

        private static Func<IDeadManSwitch, CancellationToken, Task> Resume()
        {
            return (deadManSwitch, cancellationToken) => deadManSwitch.ResumeAsync(cancellationToken).AsTask();
        }

        #endregion
    }
}