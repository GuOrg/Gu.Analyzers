``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 7 SP1 (6.1.7601)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0


```
 |                  Method |     Mean |    Error |   StdDev |   Median |  Gen 0 |  Gen 1 | Allocated |
 |------------------------ |---------:|---------:|---------:|---------:|-------:|-------:|----------:|
 | RunOnGuAnalyzersProject | 128.7 us | 4.629 us | 13.65 us | 122.5 us | 0.7324 | 0.2441 |   6.36 KB |
