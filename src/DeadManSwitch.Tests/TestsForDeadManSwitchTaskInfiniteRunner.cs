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
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DeadManSwitch.Tests
{
    public sealed class TestsForDeadManSwitchTaskInfiniteRunner : IDisposable
    {
        private readonly ILogger _logger;
        private readonly DeadManSwitchTaskInfiniteRunner _runner;
        private readonly CancellationTokenSource _cts;
        private ILoggerFactory _loggerFactory;

        public TestsForDeadManSwitchTaskInfiniteRunner(ITestOutputHelper testOutputHelper)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.TestOutput(testOutputHelper, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level,-9:w9}] {Message}{NewLine}{Exception}")
                .CreateLogger();
            _loggerFactory = LoggerFactory.Create(builder => { builder.AddSerilog(logger, dispose: true); });
            _logger = _loggerFactory.CreateLogger<TestsForDeadManSwitchTaskInfiniteRunner>();
            _runner = new DeadManSwitchTaskInfiniteRunner(
                _logger, 
                new DeadManSwitchTaskOneTimeRunner(
                    _logger, 
                    new DeadManSwitchSessionFactory(_logger, 10), 
                    new DeadManSwitchWorkerScheduler(_logger)
                )
            );
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
            var timeout = TimeSpan.FromSeconds(2);
            var delay = TimeSpan.FromSeconds(1);
            Func<IDeadManSwitch, Task>[] actions =
            {
                deadManSwitch =>
                {
                    pi = Math.PI;
                    return Task.CompletedTask;
                },
                deadManSwitch =>
                {
                    _cts.Cancel();
                    return Task.CompletedTask;
                }
            };
            var task = new ConfigurableDeadManSwitchInfiniteWorker(timeout, delay, actions);
    
            // Act
            var results = await _runner.RunInfinitelyAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().Be(Math.PI);
            results.Should().BeEquivalentTo(new DeadManSwitchTaskInfiniteRunnerResult
            {
                DeadManSwitchesTriggered = 0,
                TasksThatFinishedGracefully = 1,
                TasksThatWereCanceled = 1,
                TasksThatThrewAnException = 0
            });
        }

        [Fact]
        public async Task ShouldRunTaskMultipleTimesUntilStopped()
        {
            // Arrange
            List<double> pies = new List<double>();
            var timeout = TimeSpan.FromSeconds(2);
            var delay = TimeSpan.FromSeconds(1);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Do(() => pies.Add(Math.PI)),
                Do(() => pies.Add(Math.PI)),
                Do(() => pies.Add(Math.PI)),
                Do(() => _cts.Cancel())
            };
            var task = new ConfigurableDeadManSwitchInfiniteWorker(timeout, delay, actions);

            // Act
            var results = await _runner.RunInfinitelyAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pies.Should().HaveCount(3);
            pies.Should().AllBeEquivalentTo(Math.PI);
            results.Should().BeEquivalentTo(new DeadManSwitchTaskInfiniteRunnerResult
            {
                DeadManSwitchesTriggered = 0,
                TasksThatFinishedGracefully = 3,
                TasksThatWereCanceled = 1,
                TasksThatThrewAnException = 0
            });
        }

        [Fact]
        public async Task ShouldRunTaskAgainAfterStoppingItTheFirstTime()
        {
            // Arrange
            double? pi = null;
            double? e = null;
            var timeout = TimeSpan.FromSeconds(2);
            var delay = TimeSpan.FromSeconds(1);
            Func<IDeadManSwitch, Task>[] actions =
            {
                async deadManSwitch =>
                {
                    await Notify("Going to sleep for 5 seconds")(deadManSwitch).ConfigureAwait(false);
                    _logger.LogInformation("In loop 1, this should be aborted");
                    await Sleep(TimeSpan.FromSeconds(5))(deadManSwitch).ConfigureAwait(false);
                    _logger.LogInformation("In loop 1, thread was not aborted!!!");
                    e = Math.E;
                },
                deadManSwitch =>
                {
                    _logger.LogInformation("In loop 2, assigning Math.PI");
                    pi = Math.PI;
                    return Task.CompletedTask;
                },
                deadManSwitch =>
                {
                    _logger.LogInformation("In loop 3, stopping task");
                    _cts.Cancel();
                    return Task.CompletedTask;
                }
            };
            var task = new ConfigurableDeadManSwitchInfiniteWorker(timeout, delay, actions);

            // Act
            var results = await _runner.RunInfinitelyAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            e.Should().BeNull();
            pi.Should().Be(Math.PI);
            results.Should().BeEquivalentTo(new DeadManSwitchTaskInfiniteRunnerResult
            {
                DeadManSwitchesTriggered = 1,
                TasksThatFinishedGracefully = 1,
                TasksThatWereCanceled = 2,
                TasksThatThrewAnException = 0
            });
        }

        [Fact]
        public async Task ShouldNotStopTaskIfTaskIsNotifyingTheDeadManSwitch()
        {
            // Arrange
            double? pi = null;
            double? e = null;
            var timeout = TimeSpan.FromSeconds(2);
            var delay = TimeSpan.FromSeconds(1);
            Func<IDeadManSwitch, Task>[] actions =
            {
                async deadManSwitch =>
                {
                    await Notify("Going to sleep for 1 second")(deadManSwitch).ConfigureAwait(false);
                    await Sleep(TimeSpan.FromSeconds(1))(deadManSwitch).ConfigureAwait(false);
                    await Notify("Going to sleep for 1 second")(deadManSwitch).ConfigureAwait(false);
                    await Sleep(TimeSpan.FromSeconds(1))(deadManSwitch).ConfigureAwait(false);
                    await Notify("Going to sleep for 1 second")(deadManSwitch).ConfigureAwait(false);
                    await Sleep(TimeSpan.FromSeconds(1))(deadManSwitch).ConfigureAwait(false);
                    e = Math.E;
                },
                deadManSwitch =>
                {
                    pi = Math.PI;
                    return Task.CompletedTask;
                },
                deadManSwitch =>
                {
                    _cts.Cancel();
                    return Task.CompletedTask;
                }
            };
            var task = new ConfigurableDeadManSwitchInfiniteWorker(timeout, delay, actions);

            // Act
            var results = await _runner.RunInfinitelyAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            e.Should().Be(Math.E);
            pi.Should().Be(Math.PI);
            results.Should().BeEquivalentTo(new DeadManSwitchTaskInfiniteRunnerResult
            {
                DeadManSwitchesTriggered = 0,
                TasksThatFinishedGracefully = 2,
                TasksThatWereCanceled = 1,
                TasksThatThrewAnException = 0
            });
        }
        
        [Fact]
        public async Task ShouldCancelTheTaskWhenTokenIsCancelled()
        {
            // Arrange
            double? pi = null;
            var timeout = TimeSpan.FromSeconds(5);
            var delay = TimeSpan.FromSeconds(1);
            Func<IDeadManSwitch, Task>[] actions =
            {
                async deadManSwitch =>
                {
                    await Sleep(TimeSpan.FromSeconds(2))(deadManSwitch).ConfigureAwait(false);
                    pi = Math.PI;
                }
            };
            var task = new ConfigurableDeadManSwitchInfiniteWorker(timeout, delay, actions);

            // Act
            var run = _runner.RunInfinitelyAsync(task, _cts.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
            
            _cts.Cancel();

            var results = await run.ConfigureAwait(false);

            // Assert
            pi.Should().BeNull();
            
            results.Should().BeEquivalentTo(new DeadManSwitchTaskInfiniteRunnerResult
            {
                DeadManSwitchesTriggered = 0,
                TasksThatFinishedGracefully = 0,
                TasksThatWereCanceled = 1,
                TasksThatThrewAnException = 0
            });
        }

        [Fact]
        public async Task ShouldCancelTheTaskWhenTasksTimeOutAndWhenCancelling()
        {
            // Arrange
            double? pi = null;
            var timeout = TimeSpan.FromSeconds(3);
            var delay = TimeSpan.FromSeconds(1);
            Func<IDeadManSwitch, Task>[] actions =
            {
                async deadManSwitch =>
                {
                    await Sleep(TimeSpan.FromSeconds(4))(deadManSwitch).ConfigureAwait(false);
                    pi = Math.PI;
                },
                deadManSwitch =>
                {
                    _cts.Cancel();
                    return Task.CompletedTask;
                }
            };
            var task = new ConfigurableDeadManSwitchInfiniteWorker(timeout, delay, actions);

            // Act
            var run = _runner.RunInfinitelyAsync(task, _cts.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
            _cts.Cancel();
            var results = await run.ConfigureAwait(false);

            // Assert
            pi.Should().BeNull();
            
            results.Should().BeEquivalentTo(new DeadManSwitchTaskInfiniteRunnerResult
            {
                DeadManSwitchesTriggered = 0,
                TasksThatFinishedGracefully = 0,
                TasksThatWereCanceled = 1,
                TasksThatThrewAnException = 0
            });
        }
        
        [Fact]
        public async Task ShouldNotCancelTheTaskIfTheDeadManSwitchIsPausedAndTheTaskTakesTooLong()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            var deadManSwitchDelay = TimeSpan.FromMilliseconds(100);
            Func<IDeadManSwitch, Task>[] actions =
            {
                async deadManSwitch =>
                {
                    await Pause()(deadManSwitch).ConfigureAwait(false);
                    await Sleep(TimeSpan.FromSeconds(3))(deadManSwitch).ConfigureAwait(false);
                    await Do(() => pi = Math.PI)(deadManSwitch).ConfigureAwait(false);
                    await Resume()(deadManSwitch).ConfigureAwait(false);
                },
                Do(() => _cts.Cancel())
            };
            var task = new ConfigurableDeadManSwitchInfiniteWorker(deadManSwitchTimeout, deadManSwitchDelay, actions);
            
            // Act
            var results = await _runner.RunInfinitelyAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().NotBeNull();
            results.Should().BeEquivalentTo(new DeadManSwitchTaskInfiniteRunnerResult
            {
                DeadManSwitchesTriggered = 0,
                TasksThatFinishedGracefully = 1,
                TasksThatWereCanceled = 1,
                TasksThatThrewAnException = 0
            });
        }
        
        [Fact]
        public async Task ShouldCancelTheTaskIfTheDeadManSwitchWasPausedAndResumedAndTheTaskTakesTooLong()
        {
            // Arrange
            double? pi = null, e = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            var deadManSwitchDelay = TimeSpan.FromMilliseconds(100);
            Func<IDeadManSwitch, Task>[] actions =
            {
                async deadManSwitch =>
                {
                    await Pause()(deadManSwitch).ConfigureAwait(false);
                    await Sleep(TimeSpan.FromSeconds(3))(deadManSwitch).ConfigureAwait(false);
                    await Do(() => pi = Math.PI)(deadManSwitch).ConfigureAwait(false);
                    await Resume()(deadManSwitch).ConfigureAwait(false);
                    await Sleep(TimeSpan.FromSeconds(3))(deadManSwitch).ConfigureAwait(false);
                    await Do(() => e = Math.E)(deadManSwitch).ConfigureAwait(false);
                },
                Do(() => _cts.Cancel())
            };
            var task = new ConfigurableDeadManSwitchInfiniteWorker(deadManSwitchTimeout, deadManSwitchDelay, actions);
            
            // Act
            var results = await _runner.RunInfinitelyAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().NotBeNull();
            e.Should().BeNull();
            results.Should().BeEquivalentTo(new DeadManSwitchTaskInfiniteRunnerResult
            {
                DeadManSwitchesTriggered = 1,
                TasksThatFinishedGracefully = 0,
                TasksThatWereCanceled = 2,
                TasksThatThrewAnException = 0
            });
        }
        
        #region helper methods

        private static Func<IDeadManSwitch, ValueTask> Notify(string message)
        {
            return deadManSwitch => deadManSwitch.NotifyAsync(message);
        }

        private static Func<IDeadManSwitch, Task> Sleep(TimeSpan duration)
        {
            return deadManSwitch => Task.Delay(duration, deadManSwitch.CancellationToken);
        }

        private static Func<IDeadManSwitch, Task> Do(Action action)
        {
            return deadManSwitch =>
            {
                action();
                return Task.CompletedTask;
            };
        }

        private static Func<IDeadManSwitch, ValueTask> Pause()
        {
            return deadManSwitch => deadManSwitch.SuspendAsync();
        }

        private static Func<IDeadManSwitch, ValueTask> Resume()
        {
            return deadManSwitch => deadManSwitch.ResumeAsync();
        }

        #endregion
    }
}