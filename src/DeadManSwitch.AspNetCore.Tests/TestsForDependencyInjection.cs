using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.AspNetCore.DependencyInjection;
using DeadManSwitch.Internal;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xunit;

namespace DeadManSwitch.AspNetCore.Tests
{
    public class TestsForDependencyInjection
    {
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

        private static IInfiniteDeadManSwitchWorker InfiniteWorker(IEnumerable<Func<IDeadManSwitch, CancellationToken, Task>> workItems)
        {
            var iterationIndex = 0;
            var iterations = workItems.ToList();
            return new LambdaInfiniteDeadManSwitchWorker(async (deadManSwitch, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (iterationIndex < iterations.Count)
                {
                    var iteration = iterations[iterationIndex];
                    iterationIndex++;
                    await iteration(deadManSwitch, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new Exception("No more actions");
                }
            });
        }

        private static IDeadManSwitchWorker<TResult> Worker<TResult>(Func<IDeadManSwitch, CancellationToken, Task> work, Task<TResult> result)
        {
            return new LambdaDeadManSwitchWorker<TResult>(async (deadManSwitch, cancellationToken) =>
            {
                await work(deadManSwitch, cancellationToken).ConfigureAwait(false);

                return await result.ConfigureAwait(false);
            });
        }

        private static Func<IDeadManSwitch, CancellationToken, Task> Notify(string notification)
        {
            return (deadManSwitch, cancellationToken) => Task.Run(() => deadManSwitch.Notify(notification), cancellationToken);
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
            return (deadManSwitch, cancellationToken) => Task.Run(() => deadManSwitch.Suspend(), cancellationToken);
        }

        private static Func<IDeadManSwitch, CancellationToken, Task> Resume()
        {
            return (deadManSwitch, cancellationToken) => Task.Run(() => deadManSwitch.Resume(), cancellationToken);
        }

        private static Task<TResult> Result<TResult>(TResult value)
        {
            return Task.FromResult(value);
        }

        [Fact]
        public void ShouldBeAbleToCreateADeadManSwitchRunner()
        {
            // Arrange
            var serviceProvider = new ServiceCollection()
                .AddLogging(b => b.AddSerilog())
                .AddDeadManSwitch()
                .BuildServiceProvider();

            // Act
            var runner = serviceProvider.GetRequiredService<IDeadManSwitchRunner>();

            // Arrange
            runner.Should().NotBeNull();
        }

        [Fact]
        public void ShouldBeAbleToCreateAnInfiniteDeadManSwitchRunner()
        {
            // Arrange
            var serviceProvider = new ServiceCollection()
                .AddLogging(b => b.AddSerilog())
                .AddDeadManSwitch()
                .BuildServiceProvider();

            // Act
            var runner = serviceProvider.GetRequiredService<IInfiniteDeadManSwitchRunner>();

            // Arrange
            runner.Should().NotBeNull();
        }

        [Fact]
        public async Task ShouldBeAbleToRunCreatedInfiniteRunner()
        {
            using (var cts = new CancellationTokenSource())
            {
                // Arrange
                var serviceProvider = new ServiceCollection()
                    .AddLogging(b => b.AddSerilog())
                    .AddDeadManSwitch()
                    .BuildServiceProvider();

                // Act
                var runner = serviceProvider.GetRequiredService<IInfiniteDeadManSwitchRunner>();

                double? pi = null;
                var worker = InfiniteWorker(
                    WorkItems(
                        Work(
                            Do(_ => pi = Math.PI),
                            Notify("Test")
                        ),
                        Work(
                            Do(_ => cts.Cancel())
                        )
                    )
                );
                await runner.RunAsync(worker, new DeadManSwitchOptions(), cts.Token).ConfigureAwait(false);

                // Arrange
                runner.Should().NotBeNull();
                pi.Should().Be(Math.PI);
            }
        }

        [Fact]
        public async Task ShouldBeAbleToRunCreatedRunner()
        {
            // Arrange
            var serviceProvider = new ServiceCollection()
                .AddLogging(b => b.AddSerilog())
                .AddDeadManSwitch()
                .BuildServiceProvider();

            // Act
            var runner = serviceProvider.GetRequiredService<IDeadManSwitchRunner>();

            var worker = Worker(
                Work(
                    Notify("Test")
                ),
                Result(Math.PI)
            );
            var result = await runner.RunAsync(worker, new DeadManSwitchOptions(), CancellationToken.None)
                .ConfigureAwait(false);

            // Arrange
            runner.Should().NotBeNull();
            result.Should().Be(Math.PI);
        }
    }
}