using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DeadManSwitch.Tests
{
    public sealed class TestsForDeadManSwitchTaskOneTimeRunner : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly IDeadManSwitchTaskOneTimeRunner _oneTimeRunner;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public TestsForDeadManSwitchTaskOneTimeRunner(ITestOutputHelper testOutputHelper)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.TestOutput(testOutputHelper, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level,-9:w9}] {Message}{NewLine}{Exception}")
                .CreateLogger();
            _loggerFactory = LoggerFactory.Create(builder => { builder.AddSerilog(logger, dispose: true); });
            _logger = _loggerFactory.CreateLogger<TestsForDeadManSwitchTaskInfiniteRunner>();
            _cts = new CancellationTokenSource();
            _oneTimeRunner = new DeadManSwitchTaskOneTimeRunner(
                _logger,
                new DeadManSwitchFactory(_logger, 10),
                new DeadManSwitchTaskExecutor(_logger)
            );
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
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Do(_ => pi = Math.PI)
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().Be(Math.PI);
        }

        [Fact]
        public async Task ShouldLetTaskFinishIfItCompletesImmediatelyWithDeadManSwitchNotifications()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Notify("Computing PI"),
                Do(_ => pi = Math.PI)
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().Be(Math.PI);
        }

        [Fact]
        public async Task ShouldLetTaskFinishIfItRunsQuicklyEnoughWithDeadManSwitchNotifications()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Notify("Sleeping for 1 second"),
                Sleep(TimeSpan.FromSeconds(1)),
                Notify("Computing PI"),
                Do(_ => pi = Math.PI)
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().Be(Math.PI);
        }

        [Fact]
        public async Task ShouldLetTaskFinishIfItRunsQuicklyEnoughWithoutDeadManSwitchNotifications()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Sleep(TimeSpan.FromSeconds(1)),
                Do(_ => pi = Math.PI)
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().Be(Math.PI);
        }

        [Fact]
        public async Task ShouldLetTaskFinishIfItNotifiesTheDeadManSwitchWithinTheTimeout()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Notify("Sleeping for 1 second"),
                Sleep(TimeSpan.FromSeconds(1)),
                Notify("Sleeping for 1 second"),
                Sleep(TimeSpan.FromSeconds(1)),
                Notify("Sleeping for 1 second"),
                Sleep(TimeSpan.FromSeconds(1)),
                Notify("Computing PI"),
                Do(_ => pi = Math.PI)
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().Be(Math.PI);
        }

        [Fact]
        public async Task ShouldCancelTheTaskIfItTakesTooLongToDoSomething()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Notify("Sleeping for 3 seconds"),
                Sleep(TimeSpan.FromSeconds(3)),
                Notify("Computing PI"),
                Do(_ => pi = Math.PI)
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().BeNull();
        }

        [Fact]
        public async Task ShouldCancelTheTaskIfItTakesTooLongWithoutEverNotifyingTheDeadManSwitch()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Sleep(TimeSpan.FromSeconds(3)),
                Do(_ => pi = Math.PI)
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().BeNull();
        }

        [Fact]
        public async Task ShouldCancelTheTaskIfItTakesTooLongToDoSomethingAndThenBeAbleToRunAgainAndCompleteImmediately()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Sleep(TimeSpan.FromSeconds(3)),
                Notify("Computing PI"),
                Do(_ => pi = Math.PI)
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().BeNull();

            // Arrange
            Func<IDeadManSwitch, Task>[] nextActions =
            {
                Sleep(TimeSpan.FromSeconds(1)),
                async deadManSwitch =>
                {
                    await deadManSwitch.NotifyAsync("Computing pi").ConfigureAwait(false);
                    pi = Math.PI;
                }
            };
            task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, nextActions);

            // Act
            await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().Be(Math.PI);
        }

        [Fact]
        public async Task ShouldCancelTheTaskIfItTakesTooLongToDoSomethingAndThenBeAbleToRunAgainAndCompleteWithinTimeoutWithNotifications()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Sleep(TimeSpan.FromSeconds(3)),
                async deadManSwitch =>
                {
                    await deadManSwitch.NotifyAsync("Computing pi").ConfigureAwait(false);
                    pi = Math.PI;
                }
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().BeNull();

            // Arrange
            Func<IDeadManSwitch, Task>[] nextActions =
            {
                Sleep(TimeSpan.FromSeconds(1)),
                Notify("Sleeping for 1 second"),
                Sleep(TimeSpan.FromSeconds(1)),
                Notify("Sleeping for 1 second"),
                Sleep(TimeSpan.FromSeconds(1)),
                Notify("Sleeping for 1 second"),
                async deadManSwitch =>
                {
                    await deadManSwitch.NotifyAsync("Computing pi").ConfigureAwait(false);
                    pi = Math.PI;
                }
            };
            task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, nextActions);

            // Act
            await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().Be(Math.PI);
        }

        [Fact]
        public async Task ShouldCancelTheTaskWhenTheTokenIsCancelled()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Sleep(TimeSpan.FromSeconds(3)),
                Do(_ => pi = Math.PI)
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            var runTask = _oneTimeRunner.RunOneTimeAsync(task, _cts.Token);

            await Task.Delay(TimeSpan.FromSeconds(0.5)).ConfigureAwait(false);
            _cts.Cancel();

            var runResult = await runTask.ConfigureAwait(false);

            // Assert
            pi.Should().BeNull();
            runResult.DeadManSwitchTaskExecutionResult.Should().Be(DeadManSwitchTaskExecutionResult.TaskWasCancelled);
            runResult.DeadManSwitchResult.Should().Be(DeadManSwitchResult.DeadManSwitchWasNotTriggered);
        }

        [Fact]
        public async Task ShouldNotCancelTheTaskIfTheDeadManSwitchIsPausedAndTheTaskTakesTooLong()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Pause(),
                Sleep(TimeSpan.FromSeconds(3)),
                Do(_ => pi = Math.PI),
                Resume()
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            var runResult = await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().NotBeNull();
            runResult.DeadManSwitchTaskExecutionResult.Should().Be(DeadManSwitchTaskExecutionResult.TaskFinishedGracefully);
            runResult.DeadManSwitchResult.Should().Be(DeadManSwitchResult.DeadManSwitchWasNotTriggered);
        }

        [Fact]
        public async Task ShouldNotCancelTheTaskIfTheDeadManSwitchIsPausedMultipleTimesAndTheTaskTakesTooLong()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Pause(),
                Sleep(TimeSpan.FromSeconds(3)),
                Pause(),
                Do(_ => pi = Math.PI),
                Pause(),
                Resume()
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            var runResult = await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().NotBeNull();
            runResult.DeadManSwitchTaskExecutionResult.Should().Be(DeadManSwitchTaskExecutionResult.TaskFinishedGracefully);
            runResult.DeadManSwitchResult.Should().Be(DeadManSwitchResult.DeadManSwitchWasNotTriggered);
        }

        [Fact]
        public async Task ShouldCompleteEvenIfTheDeadManSwitchIsNeverResumed()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Pause(),
                Sleep(TimeSpan.FromSeconds(3)),
                Do(_ => pi = Math.PI),
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            var runResult = await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().NotBeNull();
            runResult.DeadManSwitchTaskExecutionResult.Should().Be(DeadManSwitchTaskExecutionResult.TaskFinishedGracefully);
            runResult.DeadManSwitchResult.Should().Be(DeadManSwitchResult.DeadManSwitchWasNotTriggered);
        }

        [Fact]
        public async Task ShouldCancelTheTaskIfItTakesTooLongAfterResuming()
        {
            // Arrange
            double? pi = null;
            double? e = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(2);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Pause(),
                Notify("Sleeping 3s"),
                Sleep(TimeSpan.FromSeconds(3)),
                Notify("Calculating PI"),
                Do(_ => pi = Math.PI),
                Resume(),
                Notify("Sleeping 3s"),
                Sleep(TimeSpan.FromSeconds(3)),
                Notify("Calculating E"),
                Do(_ => e = Math.E)
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            var runResult = await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().NotBeNull();
            e.Should().BeNull();
            runResult.DeadManSwitchTaskExecutionResult.Should().Be(DeadManSwitchTaskExecutionResult.TaskWasCancelled);
            runResult.DeadManSwitchResult.Should().Be(DeadManSwitchResult.DeadManSwitchWasTriggered);
        }

        [Fact]
        public async Task ShouldNotCancelTheTaskIfItTakesTooLongAfterPausingAndResumingAndPausingAgain()
        {
            // Arrange
            double? pi = null;
            double? e = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(5);
            Func<IDeadManSwitch, Task>[] actions =
            {
                Pause(),
                Notify("Sleeping 6s"),
                Sleep(TimeSpan.FromSeconds(6)),
                Notify("Calculating PI"),
                Do(_ => pi = Math.PI),
                Resume(),
                Pause(),
                Notify("Sleeping 6s"),
                Sleep(TimeSpan.FromSeconds(6)),
                Notify("Calculating E"),
                Do(_ => e = Math.E)
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            var runResult = await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().NotBeNull();
            e.Should().NotBeNull();
            runResult.DeadManSwitchTaskExecutionResult.Should().Be(DeadManSwitchTaskExecutionResult.TaskFinishedGracefully);
            runResult.DeadManSwitchResult.Should().Be(DeadManSwitchResult.DeadManSwitchWasNotTriggered);
        }

        [Fact]
        public async Task ShouldContainNotificationsRespectingNumberOfNotificationsToKeep()
        {
            // Arrange
            var runner = new DeadManSwitchTaskOneTimeRunner(
                _logger,
                new DeadManSwitchFactory(_logger, 3),
                new DeadManSwitchTaskExecutor(_logger)
            );
            var deadManSwitchTimeout = TimeSpan.FromSeconds(5);
            List<DeadManSwitchNotification> capturedNotifications = null;
            Func<IDeadManSwitch, Task>[] actions =
            {
                Notify("Notification 1"),
                Notify("Notification 2"),
                Notify("Notification 3"),
                Notify("Notification 4"),
                Do(deadManSwitch => { capturedNotifications = deadManSwitch.Notifications.ToList(); })
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            var runResult = await runner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            runResult.DeadManSwitchTaskExecutionResult.Should().Be(DeadManSwitchTaskExecutionResult.TaskFinishedGracefully);
            runResult.DeadManSwitchResult.Should().Be(DeadManSwitchResult.DeadManSwitchWasNotTriggered);
            string[] expected =
            {
                "Notification 2",
                "Notification 3",
                "Notification 4"
            };
            string[] actual = capturedNotifications.Select(n => n.Content).ToArray();
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public async Task ShouldHandleNotificationsInParallel()
        {
            // Arrange
            var runner = new DeadManSwitchTaskOneTimeRunner(
                _logger,
                new DeadManSwitchFactory(_logger, 3),
                new DeadManSwitchTaskExecutor(_logger)
            );
            var deadManSwitchTimeout = TimeSpan.FromSeconds(5);
            List<DeadManSwitchNotification> capturedNotifications = null;
            Func<IDeadManSwitch, Task>[] actions =
            {
                async deadManSwitch =>
                {
                    var sendNotifications = Enumerable.Range(0, 5000)
                        .AsParallel()
                        .WithDegreeOfParallelism(100)
                        .Select(i => deadManSwitch.NotifyAsync("Notification " + i).AsTask());
                    await Task.WhenAll(sendNotifications).ConfigureAwait(false);
                },
                Do(deadManSwitch => { capturedNotifications = deadManSwitch.Notifications.ToList(); })
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            var runResult = await runner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            runResult.DeadManSwitchTaskExecutionResult.Should().Be(DeadManSwitchTaskExecutionResult.TaskFinishedGracefully);
            runResult.DeadManSwitchResult.Should().Be(DeadManSwitchResult.DeadManSwitchWasNotTriggered);
            capturedNotifications.Should().HaveCount(3);
        }

        [Fact]
        public async Task ShouldCancelTheTaskIfItTakesTooLongSynchronously()
        {
            // Arrange
            double? pi = null;
            var deadManSwitchTimeout = TimeSpan.FromSeconds(5);
            Func<IDeadManSwitch, Task>[] actions =
            {
                deadManSwitch =>
                {
                    Thread.Sleep(6000);
                    return Task.CompletedTask;
                },
                Do(_ => pi = Math.PI),
            };
            var task = new ConfigurableDeadManSwitchTask(deadManSwitchTimeout, actions);

            // Act
            var runResult = await _oneTimeRunner.RunOneTimeAsync(task, _cts.Token).ConfigureAwait(false);

            // Assert
            pi.Should().BeNull();
            runResult.DeadManSwitchTaskExecutionResult.Should().Be(DeadManSwitchTaskExecutionResult.TaskWasCancelled);
            runResult.DeadManSwitchResult.Should().Be(DeadManSwitchResult.DeadManSwitchWasTriggered);
        }

        #region helper methods

        private static Func<IDeadManSwitch, Task> Notify(string notification)
        {
            return deadManSwitch => deadManSwitch.NotifyAsync(notification).AsTask();
        }

        private static Func<IDeadManSwitch, Task> Sleep(TimeSpan duration)
        {
            return deadManSwitch => Task.Delay(duration, deadManSwitch.CancellationToken);
        }

        private static Func<IDeadManSwitch, Task> Do(Action<IDeadManSwitch> action)
        {
            return deadManSwitch =>
            {
                action(deadManSwitch);
                return Task.CompletedTask;
            };
        }

        private static Func<IDeadManSwitch, Task> Pause()
        {
            return deadManSwitch => deadManSwitch.PauseAsync().AsTask();
        }

        private static Func<IDeadManSwitch, Task> Resume()
        {
            return deadManSwitch => deadManSwitch.ResumeAsync().AsTask();
        }

        #endregion
    }
}