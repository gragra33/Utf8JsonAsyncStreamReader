using BenchmarkDotNet.Running;

namespace System.Text.Json.Stream.Benchmarks;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Check if we should run the simple performance test
        if (args.Length > 0 && args[0].Equals("--simple", StringComparison.OrdinalIgnoreCase))
        {
            await SimplePerformanceTest.RunAsync();
            return;
        }
        
        // Uncomment the benchmark you want to run:
        
        // Compare old v1.2.0 NuGet version with new optimized version
        BenchmarkRunner.Run<OldVsNewComparisonBenchmark>();
        
        // Run JSON parsing benchmarks
        // BenchmarkRunner.Run<JsonParsingBenchmark>();
        
        // Run WriteToBuffer benchmarks
        // BenchmarkRunner.Run<WriteToBufferStreamBenchmark>();
        
        // Run WriteToBuffer comparison benchmarks
        // BenchmarkRunner.Run<WriteToBufferComparisonBenchmark>();
        
        // Or run all benchmarks
        // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}