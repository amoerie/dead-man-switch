using System;
using System.Linq;
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
    public class TestsForDeadManSwitchRunner : IDisposable
    {
        private readonly IDeadManSwitchRunner _runner;
        private readonly ILogger<TestsForDeadManSwitchRunner> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly CapturingDeadManSwitchSessionFactory _sessionFactory;

        public TestsForDeadManSwitchRunner(ITestOutputHelper testOutputHelper)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.TestOutput(testOutputHelper, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:w5}] #{ThreadId,-3} {Message}{NewLine}{Exception}")
                .CreateLogger();
            _loggerFactory = LoggerFactory.Create(builder => { builder.AddSerilog(logger); });
            _logger = _loggerFactory.CreateLogger<TestsForDeadManSwitchRunner>();
            _sessionFactory = new CapturingDeadManSwitchSessionFactory(new DeadManSwitchSessionFactory(_logger));
            _runner = new DeadManSwitchRunner(_logger, _sessionFactory);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _loggerFactory?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public class TestsForRunAsync : TestsForDeadManSwitchRunner
        {
            public TestsForRunAsync(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
            {
            }

            [Fact]
            public async Task ShouldLetTaskFinishIfItCompletesImmediately()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var timeout = TimeSpan.FromSeconds(2);
                    var worker = Worker(Work(), Result(Math.PI));
                    var options = new DeadManSwitchOptions {Timeout = timeout};

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                }
            }

            [Fact]
            public async Task ShouldLetTaskFinishIfItCompletesImmediatelyWithDeadManSwitchNotifications()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Notify("Computing PI")
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                }
            }

            [Fact]
            public async Task ShouldLetTaskFinishIfItRunsQuicklyEnoughWithDeadManSwitchNotifications()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Notify("Sleeping for 1 second"),
                            Sleep(TimeSpan.FromSeconds(1))
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                }
            }

            [Fact]
            public async Task ShouldLetTaskFinishIfItRunsQuicklyEnoughWithoutDeadManSwitchNotifications()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Sleep(TimeSpan.FromSeconds(1))
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                }
            }

            [Fact]
            public async Task ShouldLetTaskFinishIfItNotifiesTheDeadManSwitchWithinTheTimeout()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Notify("Sleeping for 1 second"),
                            Sleep(TimeSpan.FromSeconds(1)),
                            Notify("Sleeping for 1 second"),
                            Sleep(TimeSpan.FromSeconds(1)),
                            Notify("Sleeping for 1 second"),
                            Sleep(TimeSpan.FromSeconds(1)),
                            Notify("Computing PI")
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                }
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongToDoSomething()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Notify("Sleeping for 3 seconds"),
                            Sleep(TimeSpan.FromSeconds(3))
                        ),
                        Result(Math.PI)
                    );

                    // Act + Assert
                    await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                }
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongWithoutEverNotifyingTheDeadManSwitch()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Sleep(TimeSpan.FromSeconds(3))
                        ),
                        Result(Math.PI)
                    );

                    // Act + Assert
                    await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                }
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongToDoSomethingAndThenBeAbleToRunAgainAndCompleteImmediately()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Notify("Computing PI"),
                            Sleep(TimeSpan.FromSeconds(3))),
                        Result(Math.PI)
                    );

                    // Act + Assert
                    await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

                    // Arrange
                    worker = Worker(
                        Work(
                            Notify("Computing PI"),
                            Sleep(TimeSpan.FromSeconds(1))
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                }
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongToDoSomethingAndThenBeAbleToRunAgainAndCompleteWithinTimeoutWithNotifications()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Sleep(TimeSpan.FromSeconds(3)),
                            Notify("Computing PI")
                        ),
                        Result(Math.PI)
                    );

                    // Act + Assert
                    await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

                    // Arrange
                    worker = Worker(
                        Work(
                            Notify("Sleeping for 1 second"),
                            Sleep(TimeSpan.FromSeconds(1)),
                            Notify("Sleeping for 1 second"),
                            Sleep(TimeSpan.FromSeconds(1)),
                            Notify("Sleeping for 1 second"),
                            Sleep(TimeSpan.FromSeconds(1)),
                            Notify("Computing PI")
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                }
            }

            [Fact]
            public async Task ShouldCancelTheTaskWhenTheTokenIsCancelled()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Sleep(TimeSpan.FromSeconds(3))
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var runTask = _runner.RunAsync(worker, options, cts.Token);

                    await Task.Delay(TimeSpan.FromSeconds(0.5)).ConfigureAwait(false);

                    cts.Cancel();

                    await runTask.Invoking(async task => await task.ConfigureAwait(false)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                }
            }

            [Fact]
            public async Task ShouldNotCancelTheTaskIfTheDeadManSwitchIsPausedAndTheTaskTakesTooLong()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Pause(),
                            Sleep(TimeSpan.FromSeconds(3))
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                }
            }

            [Fact]
            public async Task ShouldNotCancelTheTaskIfTheDeadManSwitchIsPausedMultipleTimesAndTheTaskTakesTooLong()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Pause(),
                            Sleep(TimeSpan.FromSeconds(3)),
                            Pause(),
                            Pause(),
                            Resume()
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                }
            }

            [Fact]
            public async Task ShouldCompleteEvenIfTheDeadManSwitchIsNeverResumed()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Pause(),
                            Sleep(TimeSpan.FromSeconds(3))
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                }
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongAfterResuming()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    double? e = null;
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                    var worker = Worker(
                        Work(
                            Pause(),
                            Notify("Sleeping 3s"),
                            Sleep(TimeSpan.FromSeconds(3)),
                            Notify("Calculating PI"),
                            Resume(),
                            Notify("Sleeping 3s"),
                            Sleep(TimeSpan.FromSeconds(3)),
                            Notify("Calculating E"),
                            Do(_ => e = Math.E)
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

                    // Assert
                    e.Should().BeNull();
                }
            }

            [Fact]
            public async Task ShouldNotCancelTheTaskIfItTakesTooLongAfterPausingAndResumingAndPausingAgain()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    double? e = null;
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(4)};
                    var worker = Worker(
                        Work(
                            Pause(),
                            Notify("Sleeping 6s"),
                            Sleep(TimeSpan.FromSeconds(6)),
                            Notify("Calculating PI"),
                            Resume(),
                            Pause(),
                            Notify("Sleeping 6s"),
                            Sleep(TimeSpan.FromSeconds(6)),
                            Notify("Calculating E"),
                            Do(_ => e = Math.E)
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                    e.Should().Be(Math.E);
                }
            }

            [Fact]
            public async Task ShouldContainNotificationsRespectingNumberOfNotificationsToKeep()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(5), NumberOfNotificationsToKeep = 3};
                    var worker = Worker(
                        Work(
                            Notify("Notification 1"),
                            Notify("Notification 2"),
                            Notify("Notification 3"),
                            Notify("Notification 4")
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                    _sessionFactory.Session.Should().NotBeNull();
                    var notifications = await _sessionFactory.Session.DeadManSwitchContext.GetNotificationsAsync(cts.Token).ConfigureAwait(false);
                    string[] expected =
                    {
                        "Notification 2",
                        "Notification 3",
                        "Notification 4"
                    };
                    string[] actual = notifications.Select(n => n.Content).ToArray();
                    actual.Should().BeEquivalentTo(expected);
                }
            }

            [Theory]
            [InlineData(100)]
            public async Task ShouldHandleNotificationsInParallel(int numberOfNotifications)
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(5), NumberOfNotificationsToKeep = 3};
                    var worker = Worker(
                        Work(
                            async (deadManSwitch, cancellationToken) =>
                            {
                                var sendNotifications = Enumerable.Range(0, numberOfNotifications)
                                    .AsParallel()
                                    .WithDegreeOfParallelism(100)
                                    .Select(i => deadManSwitch.NotifyAsync("Notification " + i, cancellationToken).AsTask());
                                await Task.WhenAll(sendNotifications).ConfigureAwait(false);
                            }
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    var result = await _runner.RunAsync(worker, options, cts.Token).ConfigureAwait(false);

                    // Assert
                    result.Should().Be(Math.PI);
                    _sessionFactory.Session.Should().NotBeNull();
                    var notifications = await _sessionFactory.Session.DeadManSwitchContext.GetNotificationsAsync(cts.Token).ConfigureAwait(false);
                    notifications.Should().HaveCount(3);
                }
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongSynchronously()
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Arrange
                    var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(5)};
                    var worker = Worker(
                        Work(
                            (deadManSwitch, cancellationToken) =>
                            {
                                Thread.Sleep(6000);
                                return Task.CompletedTask;
                            }
                        ),
                        Result(Math.PI)
                    );

                    // Act
                    await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                }
            }
        }

        #region helper methods

        private static Func<IDeadManSwitch, CancellationToken, Task> Work(params Func<IDeadManSwitch, CancellationToken, Task>[] tasks)
        {
            return async (deadManSwitch, cancellationToken) =>
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);
                foreach (var task in tasks)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);

                    await task(deadManSwitch, cancellationToken).ConfigureAwait(false);
                }
            };
        }

        private static Task<TResult> Result<TResult>(TResult value)
        {
            return Task.FromResult(value);
        }

        private static ConfigurableDeadManSwitchWorker<TResult> Worker<TResult>(Func<IDeadManSwitch, CancellationToken, Task> work, Task<TResult> result)
        {
            return new ConfigurableDeadManSwitchWorker<TResult>(work, result);
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