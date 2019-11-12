using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace DeadManSwitch.Benchmarks
{
    [MemoryDiagnoser, ThreadingDiagnoser]
    public class DeadManSwitchBenchmarks
    {
        private readonly DeadManSwitchOptions _deadManSwitchOptions;
        private readonly IDeadManSwitchRunner _deadManSwitchRunner;
        private readonly IInfiniteDeadManSwitchRunner _infiniteDeadManSwitchRunner;

        public DeadManSwitchBenchmarks()
        {
            _deadManSwitchRunner = DeadManSwitchRunner.Create();
            _infiniteDeadManSwitchRunner = InfiniteDeadManSwitchRunner.Create();
            _deadManSwitchOptions = new DeadManSwitchOptions
            {
                Timeout = TimeSpan.FromSeconds(1),
                NumberOfNotificationsToKeep = 5
            };
        }

        [Benchmark]
        public async Task<double> RunAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var benchmarkWorker = new BenchmarkWorker();
            return await _deadManSwitchRunner.RunAsync(benchmarkWorker, _deadManSwitchOptions, cts.Token).ConfigureAwait(false);
        }

        [Benchmark]
        public async Task RunInfinitelyAsync()
        {
            using var cts = new CancellationTokenSource();
            var infiniteBenchmarkWorker = new InfiniteBenchmarkWorker(10, () => cts.Cancel());
            await _infiniteDeadManSwitchRunner.RunAsync(infiniteBenchmarkWorker, _deadManSwitchOptions, cts.Token).ConfigureAwait(false);
        }
    }
}
