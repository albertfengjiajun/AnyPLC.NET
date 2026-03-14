using System.Diagnostics;
using AnyPLC.Core.OpcUa;
using Opc.Ua;
using Opc.Ua.Client;
using Xunit;

namespace AnyPLC.Tests;

public class OpcUaDisconnectBenchmark
{
    // A mock version that simulates the synchronous block over async
    [Fact]
    public void Benchmark_SyncOverAsync()
    {
        // Setup mock task
        var delayTask = Task.Delay(100);

        var sw = Stopwatch.StartNew();
        delayTask.Wait(); // Simulate the Wait() block
        sw.Stop();

        // Output baseline
        Console.WriteLine($"[Baseline] Sync over async took: {sw.ElapsedMilliseconds}ms (blocked thread)");
    }

    [Fact]
    public async Task Benchmark_AsyncDisconnect()
    {
        var sw = Stopwatch.StartNew();
        await Task.Delay(100); // Simulate the non-blocking await
        sw.Stop();

        // Output improvement
        Console.WriteLine($"[Improvement] Async await took: {sw.ElapsedMilliseconds}ms (freed thread)");
    }
}
