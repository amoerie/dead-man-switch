using BenchmarkDotNet.Running;

namespace DeadManSwitch.Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<DeadManSwitchBenchmarks>();
        }
    }
}