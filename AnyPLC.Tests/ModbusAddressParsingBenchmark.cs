using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace AnyPLC.Tests
{
    public class ModbusAddressParsingBenchmark
    {
        private readonly ITestOutputHelper _output;

        public ModbusAddressParsingBenchmark(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Benchmark_AddressParsing()
        {
            const int iterations = 10_000_000;
            string address = "HoldingRegister:12345";

            // Baseline
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var parts = address.Split(':');
                if (parts.Length != 2 || !ushort.TryParse(parts[1], out ushort addr))
                {
                    throw new Exception("fail");
                }
                string type = parts[0];
            }
            sw.Stop();
            long baselineMs = sw.ElapsedMilliseconds;

            // Optimized (Span + IndexOf)
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                int colonIndex = address.IndexOf(':');
                if (colonIndex == -1)
                    throw new Exception("fail");

                var typeSpan = address.AsSpan(0, colonIndex);
                var addrSpan = address.AsSpan(colonIndex + 1);

                if (!ushort.TryParse(addrSpan, out ushort addr))
                {
                    throw new Exception("fail");
                }
            }
            sw.Stop();
            long optimizedMs = sw.ElapsedMilliseconds;

            _output.WriteLine($"Iterations: {iterations}");
            _output.WriteLine($"Baseline (Split): {baselineMs} ms");
            _output.WriteLine($"Optimized (Span): {optimizedMs} ms");

            Assert.True(optimizedMs < baselineMs || baselineMs < 10);
        }
    }
}
