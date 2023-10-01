using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DeadManSwitch.Tests.TestHelpers;

namespace DeadManSwitch.Tests
{
    public class TestsForInfiniteDeadManSwitchTask
    {
        [Fact]
        public async Task ShouldBeAbleToRunInlineWorker()
        {
            using (var cts = new CancellationTokenSource())
            {
                // Arrange
                double? pi = null;
                var worker = WorkItems(
                    Work(
                        Do(_ => pi = Math.PI),
                        Notify("Test")
                    ),
                    Work(
                        Do(_ => cts.Cancel())
                    )
                );

                // Act
                await InfiniteDeadManSwitchTask.RunAsync(worker, DeadManSwitchOptions.Default, cts.Token);

                // Arrange
                pi.Should().Be(Math.PI);
            }
        }
    }
}
