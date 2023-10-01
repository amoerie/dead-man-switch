using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;
using DeadManSwitch.Tests.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using static DeadManSwitch.Tests.TestHelpers;

namespace DeadManSwitch.Tests
{
    public sealed class TestsForDeadManSwitchRunner : IDisposable
    {
        public TestsForDeadManSwitchRunner(ITestOutputHelper testOutputHelper)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.TestOutput(testOutputHelper,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:w5}] #{ThreadId,-3} {SourceContext} {Message}{NewLine}{Exception}")
                .CreateLogger();
            _loggerFactory = LoggerFactory.Create(builder => { builder.AddSerilog(logger); });
            var loggerFactory = new TestLoggerFactory(_loggerFactory);
            _logger = _loggerFactory.CreateLogger<TestsForDeadManSwitchRunner>();
            _sessionFactory = new CapturingDeadManSwitchSessionFactory(new DeadManSwitchSessionFactory(loggerFactory));
            _runner = new DeadManSwitchRunner(loggerFactory.CreateLogger<DeadManSwitchRunner>(), _sessionFactory);
        }

        public void Dispose()
        {
            _loggerFactory?.Dispose();
        }

        private readonly IDeadManSwitchRunner _runner;
        private readonly ILogger<TestsForDeadManSwitchRunner> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly CapturingDeadManSwitchSessionFactory _sessionFactory;

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
                                .Select(i => Task.Run(() => deadManSwitch.Notify("Notification " + i)));
                            await Task.WhenAll(sendNotifications);
                        }
                    ),
                    Result(Math.PI)
                );

                // Act
                var result = await _runner.RunAsync(worker, options, cts.Token);

                // Assert
                result.Should().Be(Math.PI);
                _sessionFactory.Session.Should().NotBeNull();
                var notifications = _sessionFactory.Session.DeadManSwitchContext.Notifications;
                notifications.Should().HaveCount(3);
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
                await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>();

                // Assert
                e.Should().BeNull();
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
                await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>();
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
                await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>();
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
                await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>();

                // Arrange
                worker = Worker(
                    Work(
                        Notify("Computing PI"),
                        Sleep(TimeSpan.FromSeconds(1))
                    ),
                    Result(Math.PI)
                );

                // Act
                var result = await _runner.RunAsync(worker, options, cts.Token);

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
                await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>();

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
                var result = await _runner.RunAsync(worker, options, cts.Token);

                // Assert
                result.Should().Be(Math.PI);
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
                await _runner.Invoking(m => m.RunAsync(worker, options, cts.Token)).Should().ThrowAsync<OperationCanceledException>();
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

                await Task.Delay(TimeSpan.FromSeconds(0.5));

                cts.Cancel();

                await runTask.Invoking(async task => await task).Should().ThrowAsync<OperationCanceledException>();
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
                var result = await _runner.RunAsync(worker, options, cts.Token);

                // Assert
                result.Should().Be(Math.PI);
            }
        }

        [Fact]
        public async Task ShouldContainNotificationsEvenIfLessThanMaximumNumberWereQueued()
        {
            using (var cts = new CancellationTokenSource())
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(5), NumberOfNotificationsToKeep = 10};
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
                var result = await _runner.RunAsync(worker, options, cts.Token);

                // Assert
                result.Should().Be(Math.PI);
                _sessionFactory.Session.Should().NotBeNull();
                var notifications = _sessionFactory.Session.DeadManSwitchContext.Notifications;
                string[] expected =
                {
                    "Notification 1",
                    "Notification 2",
                    "Notification 3",
                    "Notification 4"
                };
                var actual = notifications.Select(n => n.Content).ToArray();
                actual.Should().BeEquivalentTo(expected);
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
                var result = await _runner.RunAsync(worker, options, cts.Token);

                // Assert
                result.Should().Be(Math.PI);
                _sessionFactory.Session.Should().NotBeNull();
                var notifications = _sessionFactory.Session.DeadManSwitchContext.Notifications;
                string[] expected =
                {
                    "Notification 2",
                    "Notification 3",
                    "Notification 4"
                };
                var actual = notifications.Select(n => n.Content).ToArray();
                actual.Should().BeEquivalentTo(expected);
            }
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
                var result = await _runner.RunAsync(worker, options, cts.Token);

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
                var result = await _runner.RunAsync(worker, options, cts.Token);

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
                var result = await _runner.RunAsync(worker, options, cts.Token);

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
                var result = await _runner.RunAsync(worker, options, cts.Token);

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
                var result = await _runner.RunAsync(worker, options, cts.Token);

                // Assert
                result.Should().Be(Math.PI);
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
                var result = await _runner.RunAsync(worker, options, cts.Token);

                // Assert
                result.Should().Be(Math.PI);
                e.Should().Be(Math.E);
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
                var result = await _runner.RunAsync(worker, options, cts.Token);

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
                var result = await _runner.RunAsync(worker, options, cts.Token);

                // Assert
                result.Should().Be(Math.PI);
            }
        }
    }
}
