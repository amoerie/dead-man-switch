﻿using System;
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
    public class TestsForDeadManSwitchManager : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly IDeadManSwitchManager _manager;
        private readonly ILogger<TestsForDeadManSwitchManager> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly CapturingDeadManSwitchSessionFactory _sessionFactory;

        public TestsForDeadManSwitchManager(ITestOutputHelper testOutputHelper)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.TestOutput(testOutputHelper, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level,-9:w9}] {Message}{NewLine}{Exception}")
                .CreateLogger();
            _loggerFactory = LoggerFactory.Create(builder => { builder.AddSerilog(logger, dispose: true); });
            _logger = _loggerFactory.CreateLogger<TestsForDeadManSwitchManager>();
            _cts = new CancellationTokenSource();
            _sessionFactory = new CapturingDeadManSwitchSessionFactory(new DeadManSwitchSessionFactory(_logger));
            _manager = new DeadManSwitchManager(_logger, _sessionFactory);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Dispose();
                _loggerFactory?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public class TestsForRunAsync : TestsForDeadManSwitchManager
        {
            public TestsForRunAsync(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
            {
            }

            [Fact]
            public async Task ShouldLetTaskFinishIfItCompletesImmediately()
            {
                // Arrange
                var timeout = TimeSpan.FromSeconds(2);
                var actions = Actions<double>()(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);
                var options = new DeadManSwitchOptions {Timeout = timeout};

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldLetTaskFinishIfItCompletesImmediatelyWithDeadManSwitchNotifications()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(Notify("Computing PI"))(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldLetTaskFinishIfItRunsQuicklyEnoughWithDeadManSwitchNotifications()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                    Notify("Sleeping for 1 second"),
                    Sleep(TimeSpan.FromSeconds(1)),
                    Notify("Computing PI")
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldLetTaskFinishIfItRunsQuicklyEnoughWithoutDeadManSwitchNotifications()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                    Sleep(TimeSpan.FromSeconds(1))
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldLetTaskFinishIfItNotifiesTheDeadManSwitchWithinTheTimeout()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                    Notify("Sleeping for 1 second"),
                    Sleep(TimeSpan.FromSeconds(1)),
                    Notify("Sleeping for 1 second"),
                    Sleep(TimeSpan.FromSeconds(1)),
                    Notify("Sleeping for 1 second"),
                    Sleep(TimeSpan.FromSeconds(1)),
                    Notify("Computing PI")
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongToDoSomething()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                    Notify("Sleeping for 3 seconds"),
                    Sleep(TimeSpan.FromSeconds(3)),
                    Notify("Computing PI")
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act + Assert
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongWithoutEverNotifyingTheDeadManSwitch()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                    Sleep(TimeSpan.FromSeconds(3))
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act + Assert
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongToDoSomethingAndThenBeAbleToRunAgainAndCompleteImmediately()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                    Sleep(TimeSpan.FromSeconds(3)),
                    Notify("Computing PI")
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act + Assert
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

                // Arrange
                var nextActions = Actions<double>(
                    Sleep(TimeSpan.FromSeconds(1)),
                    Notify("Computing PI")
                )(Math.PI);
                worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongToDoSomethingAndThenBeAbleToRunAgainAndCompleteWithinTimeoutWithNotifications()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                        Sleep(TimeSpan.FromSeconds(3)),
                        Notify("Computing PI"))
                    (Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act + Assert
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

                // Arrange
                var nextActions = Actions<double>(
                        Notify("Sleeping for 1 second"),
                        Sleep(TimeSpan.FromSeconds(1)),
                        Notify("Sleeping for 1 second"),
                        Sleep(TimeSpan.FromSeconds(1)),
                        Notify("Sleeping for 1 second"),
                        Sleep(TimeSpan.FromSeconds(1)),
                        Notify("Computing PI"))
                    (Math.PI);
                worker = new ConfigurableDeadManSwitchWorker<double>(nextActions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldCancelTheTaskWhenTheTokenIsCancelled()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                    Sleep(TimeSpan.FromSeconds(3))
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var runTask = _manager.RunAsync(worker, options, _cts.Token);

                await Task.Delay(TimeSpan.FromSeconds(0.5)).ConfigureAwait(false);
                
                _cts.Cancel();

                await runTask.Invoking(async task => await task.ConfigureAwait(false)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            }

            [Fact]
            public async Task ShouldNotCancelTheTaskIfTheDeadManSwitchIsPausedAndTheTaskTakesTooLong()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                    Pause(),
                    Sleep(TimeSpan.FromSeconds(3)),
                    Resume()
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldNotCancelTheTaskIfTheDeadManSwitchIsPausedMultipleTimesAndTheTaskTakesTooLong()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                    Pause(),
                    Sleep(TimeSpan.FromSeconds(3)),
                    Pause(),
                    Pause(),
                    Resume()
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldCompleteEvenIfTheDeadManSwitchIsNeverResumed()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                    Pause(),
                    Sleep(TimeSpan.FromSeconds(3))
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongAfterResuming()
            {
                // Arrange
                double? e = null;
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var actions = Actions<double>(
                    Pause(),
                    Notify("Sleeping 3s"),
                    Sleep(TimeSpan.FromSeconds(3)),
                    Notify("Calculating PI"),
                    Resume(),
                    Notify("Sleeping 3s"),
                    Sleep(TimeSpan.FromSeconds(3)),
                    Notify("Calculating E"),
                    Do(_ => e = Math.E)
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act + Assert
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            }

            [Fact]
            public async Task ShouldNotCancelTheTaskIfItTakesTooLongAfterPausingAndResumingAndPausingAgain()
            {
                // Arrange
                double? e = null;
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(5)};
                var actions = Actions<double>(
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
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
                e.Should().Be(Math.E);
            }

            [Fact]
            public async Task ShouldContainNotificationsRespectingNumberOfNotificationsToKeep()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(5), NumberOfNotificationsToKeep = 3 };
                var actions = Actions<double>(
                    Notify("Notification 1"),
                    Notify("Notification 2"),
                    Notify("Notification 3"),
                    Notify("Notification 4")
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
                _sessionFactory.Session.Should().NotBeNull();
                var notifications = await _sessionFactory.Session.DeadManSwitchContext.GetNotificationsAsync(_cts.Token).ConfigureAwait(false);
                string[] expected =
                {
                    "Notification 2",
                    "Notification 3",
                    "Notification 4"
                };
                string[] actual = notifications.Select(n => n.Content).ToArray();
                actual.Should().BeEquivalentTo(expected);
            }

            [Fact]
            public async Task ShouldHandleNotificationsInParallel()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(5), NumberOfNotificationsToKeep = 3 };
                var actions = Actions<double>(
                    async (deadManSwitch, cancellationToken) =>
                    {
                        var sendNotifications = Enumerable.Range(0, 5000)
                            .AsParallel()
                            .WithDegreeOfParallelism(100)
                            .Select(i => deadManSwitch.NotifyAsync("Notification " + i, cancellationToken).AsTask());
                        await Task.WhenAll(sendNotifications).ConfigureAwait(false);
                    }
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
                _sessionFactory.Session.Should().NotBeNull();
                var notifications = await _sessionFactory.Session.DeadManSwitchContext.GetNotificationsAsync(_cts.Token).ConfigureAwait(false);
                notifications.Should().HaveCount(3);
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongSynchronously()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(5) };
                var actions = Actions<double>(
                    (deadManSwitch, cancellationToken) =>
                    {
                        Thread.Sleep(6000);
                        return Task.CompletedTask;
                    }
                )(Math.PI);
                var worker = new ConfigurableDeadManSwitchWorker<double>(actions);

                // Act
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            }
        }

        #region helper methods

        private static Func<TResult, Func<IDeadManSwitch, CancellationToken, Task<TResult>>> Actions<TResult>(params Func<IDeadManSwitch, CancellationToken, Task>[] actions)
        {
            return result => async (deadManSwitch, cancellationToken) =>
            {
                if(cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);
                foreach (var action in actions)
                {
                    if(cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);
                    
                    await action(deadManSwitch, cancellationToken).ConfigureAwait(false);
                }

                return result;
            };
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