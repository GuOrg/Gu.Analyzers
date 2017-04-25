``` ini

BenchmarkDotNet=v0.10.4, OS=Windows 10.0.14393
Processor=Intel Core i7-3667U CPU 2.00GHz (Ivy Bridge), ProcessorCount=4
Frequency=2435873 Hz, Resolution=410.5304 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 32bit LegacyJIT-v4.6.1637.0
  DefaultJob : Clr 4.0.30319.42000, 32bit LegacyJIT-v4.6.1637.0


```
 |                      Method |        Mean |      Error |     StdDev |  Gen 0 | Allocated |
 |---------------------------- |------------:|-----------:|-----------:|-------:|----------:|
 | GetAnalyzerDiagnosticsAsync | 861.5163 ns | 17.1222 ns | 21.6540 ns | 0.1144 |      0 GB |
