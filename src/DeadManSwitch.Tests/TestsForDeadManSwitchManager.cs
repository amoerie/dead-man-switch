﻿using System;
using System.Collections.Generic;
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
                .WriteTo.TestOutput(testOutputHelper, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level,-9:w9}] {Message}{NewLine}{Exception}")
                .CreateLogger();
            _loggerFactory = LoggerFactory.Create(builder => { builder.AddSerilog(logger); });
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
                var worker = Worker(Tasks(), Result(Math.PI));
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
                var worker = Worker(
                    Tasks(
                        Notify("Computing PI")
                    ),
                    Result(Math.PI)
                );

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
                var worker = Worker(
                    Tasks(
                        Notify("Sleeping for 1 second"),
                        Sleep(TimeSpan.FromSeconds(1))
                    ),
                    Result(Math.PI)
                );

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
                var worker = Worker(
                    Tasks(
                        Sleep(TimeSpan.FromSeconds(1))
                    ),
                    Result(Math.PI)
                );

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
                var worker = Worker(
                    Tasks(
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
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongToDoSomething()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var worker = Worker(
                    Tasks(
                        Notify("Sleeping for 3 seconds"),
                        Sleep(TimeSpan.FromSeconds(3))
                    ),
                    Result(Math.PI)
                );

                // Act + Assert
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongWithoutEverNotifyingTheDeadManSwitch()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var worker = Worker(
                    Tasks(
                        Sleep(TimeSpan.FromSeconds(3))
                    ),
                    Result(Math.PI)
                );

                // Act + Assert
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            }

            [Fact]
            public async Task ShouldCancelTheTaskIfItTakesTooLongToDoSomethingAndThenBeAbleToRunAgainAndCompleteImmediately()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var worker = Worker(
                    Tasks(
                        Notify("Computing PI"),
                        Sleep(TimeSpan.FromSeconds(3))),
                    Result(Math.PI)
                );

                // Act + Assert
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

                // Arrange
                worker = Worker(
                    Tasks(
                        Notify("Computing PI"),
                        Sleep(TimeSpan.FromSeconds(1))
                    ),
                    Result(Math.PI)
                );

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
                var worker = Worker(
                    Tasks(
                        Sleep(TimeSpan.FromSeconds(3)),
                        Notify("Computing PI")
                    ),
                    Result(Math.PI)
                );

                // Act + Assert
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

                // Arrange
                worker = Worker(
                    Tasks(
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
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
            }

            [Fact]
            public async Task ShouldCancelTheTaskWhenTheTokenIsCancelled()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(2)};
                var worker = Worker(
                    Tasks(
                        Sleep(TimeSpan.FromSeconds(3))
                    ),
                    Result(Math.PI)
                );

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
                var worker = Worker(
                    Tasks(
                        Pause(),
                        Sleep(TimeSpan.FromSeconds(3))
                    ),
                    Result(Math.PI)
                );

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
                var worker = Worker(
                    Tasks(
                        Pause(),
                        Sleep(TimeSpan.FromSeconds(3)),
                        Pause(),
                        Pause(),
                        Resume()
                    ),
                    Result(Math.PI)
                );

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
                var worker = Worker(
                    Tasks(
                        Pause(),
                        Sleep(TimeSpan.FromSeconds(3))
                    ),
                    Result(Math.PI)
                );

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
                var worker = Worker(
                    Tasks(
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
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                
                // Assert
                e.Should().BeNull();
            }

            [Fact]
            public async Task ShouldNotCancelTheTaskIfItTakesTooLongAfterPausingAndResumingAndPausingAgain()
            {
                // Arrange
                double? e = null;
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(4)};
                var worker = Worker(
                    Tasks(
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
                var result = await _manager.RunAsync(worker, options, _cts.Token).ConfigureAwait(false);

                // Assert
                result.Should().Be(Math.PI);
                e.Should().Be(Math.E);
            }

            [Fact]
            public async Task ShouldContainNotificationsRespectingNumberOfNotificationsToKeep()
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(5), NumberOfNotificationsToKeep = 3};
                var worker = Worker(
                    Tasks(
                        Notify("Notification 1"),
                        Notify("Notification 2"),
                        Notify("Notification 3"),
                        Notify("Notification 4")
                    ),
                    Result(Math.PI)
                );

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

            [Theory]
            [InlineData(100)]
            public async Task ShouldHandleNotificationsInParallel(int numberOfNotifications)
            {
                // Arrange
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(5), NumberOfNotificationsToKeep = 3};
                var worker = Worker(
                    Tasks(
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
                var options = new DeadManSwitchOptions {Timeout = TimeSpan.FromSeconds(5)};
                var worker = Worker(
                    Tasks(
                        (deadManSwitch, cancellationToken) =>
                        {
                            Thread.Sleep(6000);
                            return Task.CompletedTask;
                        }
                    ),
                    Result(Math.PI)
                );

                // Act
                await _manager.Invoking(m => m.RunAsync(worker, options, _cts.Token)).Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            }
        }

        #region helper methods

        private static IEnumerable<Func<IDeadManSwitch, CancellationToken, Task>> Tasks(params Func<IDeadManSwitch, CancellationToken, Task>[] tasks)
        {
            return tasks;
        }

        private static Task<TResult> Result<TResult>(TResult value)
        {
            return Task.FromResult(value);
        }

        private static ConfigurableDeadManSwitchWorker<TResult> Worker<TResult>(IEnumerable<Func<IDeadManSwitch, CancellationToken, Task>> tasks, Task<TResult> result)
        {
            return new ConfigurableDeadManSwitchWorker<TResult>(tasks, result);
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