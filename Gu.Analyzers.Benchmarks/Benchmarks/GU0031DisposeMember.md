``` ini

BenchmarkDotNet=v0.10.4, OS=Windows 10.0.14393
Processor=Intel Core i7-3667U CPU 2.00GHz (Ivy Bridge), ProcessorCount=4
Frequency=2435873 Hz, Resolution=410.5304 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 32bit LegacyJIT-v4.6.1637.0
  DefaultJob : Clr 4.0.30319.42000, 32bit LegacyJIT-v4.6.1637.0


```
 |                      Method |        Mean |      Error |     StdDev |      Median |  Gen 0 | Allocated |
 |---------------------------- |------------:|-----------:|-----------:|------------:|-------:|----------:|
 | GetAnalyzerDiagnosticsAsync | 910.8131 ns | 33.2809 ns | 91.6653 ns | 878.2130 ns | 0.0947 |      0 GB |
