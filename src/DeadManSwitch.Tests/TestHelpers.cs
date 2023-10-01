using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeadManSwitch.Internal;

namespace DeadManSwitch.Tests
{
    public static class TestHelpers
    {
        public static IInfiniteDeadManSwitchWorker InfiniteWorker(Func<IDeadManSwitch, CancellationToken, Task> worker)
        {
            return new LambdaInfiniteDeadManSwitchWorker(worker);
        }

        public static Func<IDeadManSwitch, CancellationToken, Task> WorkItems(params Func<IDeadManSwitch, CancellationToken, Task>[] workItems)
        {
            var iterationIndex = 0;
            var iterations = workItems.ToList();
            return async (deadManSwitch, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (iterationIndex < iterations.Count)
                {
                    var iteration = iterations[iterationIndex];
                    iterationIndex++;
                    await iteration(deadManSwitch, cancellationToken);
                }
                else
                {
                    throw new Exception("No more actions");
                }
            };
        }

        public static IDeadManSwitchWorker<TResult> Worker<TResult>(Func<IDeadManSwitch, CancellationToken, Task> work, Task<TResult> result)
        {
            return new LambdaDeadManSwitchWorker<TResult>(async (deadManSwitch, cancellationToken) =>
            {
                await work(deadManSwitch, cancellationToken);

                return await result;
            });
        }

        public static Func<IDeadManSwitch, CancellationToken, Task> Work(params Func<IDeadManSwitch, CancellationToken, Task>[] tasks)
        {
            return async (deadManSwitch, cancellationToken) =>
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);
                foreach (var task in tasks)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);

                    await task(deadManSwitch, cancellationToken);
                }
            };
        }

        public static Task<TResult> Result<TResult>(TResult value)
        {
            return Task.FromResult(value);
        }

        public static Func<IDeadManSwitch, CancellationToken, Task> Notify(string notification)
        {
            return (deadManSwitch, cancellationToken) => Task.Run(() => deadManSwitch.Notify(notification), cancellationToken);
        }

        public static Func<IDeadManSwitch, CancellationToken, Task> Sleep(TimeSpan duration)
        {
            return async (deadManSwitch, cancellationToken) => { await Task.Delay(duration, cancellationToken); };
        }

        public static Func<IDeadManSwitch, CancellationToken, Task> Do(Action<IDeadManSwitch> action)
        {
            return (deadManSwitch, cancellationToken) =>
            {
                action(deadManSwitch);
                return Task.CompletedTask;
            };
        }

        public static Func<IDeadManSwitch, CancellationToken, Task> Pause()
        {
            return (deadManSwitch, cancellationToken) => Task.Run(deadManSwitch.Suspend, cancellationToken);
        }

        public static Func<IDeadManSwitch, CancellationToken, Task> Resume()
        {
            return (deadManSwitch, cancellationToken) => Task.Run(deadManSwitch.Resume, cancellationToken);
        }
    }
}
