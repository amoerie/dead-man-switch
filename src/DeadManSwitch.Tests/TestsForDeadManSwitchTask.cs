using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DeadManSwitch.Tests
{
    public class TestsForDeadManSwitchTask
    {
        [Fact]
        public async Task ShouldBeAbleToRunCreatedRunner()
        {
            // Arrange
            Func<IDeadManSwitch, CancellationToken, Task<double>> worker = (deadManSwitch, cancellationToken) =>
            {
                deadManSwitch.Notify("Test");
                return Task.FromResult(Math.PI);
            };

            // Act
            var result = await DeadManSwitchTask.RunAsync(worker, DeadManSwitchOptions.Default, CancellationToken.None);

            // Arrange
            result.Should().Be(Math.PI);
        }
    }
}
