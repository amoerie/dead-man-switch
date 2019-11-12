using System;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.AspNetCore.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xunit;

namespace DeadManSwitch.AspNetCore.Tests
{
    public class TestsForDependencyInjection
    {
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
        public async Task ShouldBeAbleToRunCreatedRunner()
        {
            // Arrange
            var serviceProvider = new ServiceCollection()
                .AddLogging(b => b.AddSerilog())
                .AddDeadManSwitch()
                .BuildServiceProvider(); 

            // Act
            var runner = serviceProvider.GetRequiredService<IDeadManSwitchRunner>();

            var worker = new ConfigurableDeadManSwitchWorker<double>((d, t) => Task.CompletedTask, Task.FromResult(Math.PI));
            var result = await runner.RunAsync(worker, new DeadManSwitchOptions(), CancellationToken.None)
                .ConfigureAwait(false);

            // Arrange
            runner.Should().NotBeNull();
            result.Should().Be(Math.PI);
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
                var worker = new ConfigurableDeadManSwitchInfiniteWorker(
                    new Func<IDeadManSwitch, CancellationToken, Task>[]
                    {
                        (d, t) =>
                        {
                            pi = Math.PI;
                            return Task.CompletedTask;
                        },
                        (d, t) =>
                        {
                            cts.Cancel();
                            return Task.CompletedTask;
                        }
                    }
                );
                await runner.RunAsync(worker, new DeadManSwitchOptions(), cts.Token).ConfigureAwait(false);

                // Arrange
                runner.Should().NotBeNull();
                pi.Should().Be(Math.PI);
            }
        }
    }
}