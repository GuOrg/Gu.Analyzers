``` ini

BenchmarkDotNet=v0.10.4, OS=Windows 10.0.14393
Processor=Intel Core i7-3667U CPU 2.00GHz (Ivy Bridge), ProcessorCount=4
Frequency=2435873 Hz, Resolution=410.5304 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 32bit LegacyJIT-v4.6.1637.0
  DefaultJob : Clr 4.0.30319.42000, 32bit LegacyJIT-v4.6.1637.0


```
 |                      Method |        Mean |      Error |     StdDev |  Gen 0 | Allocated |
 |---------------------------- |------------:|-----------:|-----------:|-------:|----------:|
 | GetAnalyzerDiagnosticsAsync | 839.8876 ns | 16.3972 ns | 14.5357 ns | 0.1184 |      0 GB |
