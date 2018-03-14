``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical cores and 8 physical cores
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                                               Method |        Mean |       Error |     StdDev |      Median |   Gen 0 |  Gen 1 | Allocated |
|----------------------------------------------------- |------------:|------------:|-----------:|------------:|--------:|-------:|----------:|
|                                      GU0006UseNameof | 3,154.79 us |  62.8416 us | 173.084 us | 3,113.94 us | 46.8750 |      - |  318995 B |
|                                GU0007PreferInjecting | 5,070.94 us | 100.7315 us | 221.108 us | 5,071.66 us |       - |      - |   21045 B |
|                           GU0008AvoidRelayProperties |   739.19 us |  14.6156 us |  33.582 us |   730.53 us |       - |      - |    7007 B |
|                  GU0009UseNamedParametersForBooleans | 3,084.43 us | 224.8630 us | 663.014 us | 2,651.06 us |       - |      - |     544 B |
|                          GU0011DontIgnoreReturnValue | 2,251.18 us |  44.9386 us | 129.658 us | 2,237.04 us |  3.9063 |      - |   48122 B |
|                                 GU0020SortProperties |    81.64 us |   1.6112 us |   3.065 us |    81.65 us |  0.8545 |      - |    5945 B |
|                    GU0021CalculatedPropertyAllocates |    76.25 us |   1.5187 us |   3.237 us |    76.02 us |       - |      - |     441 B |
|                                     GU0022UseGetOnly | 2,603.51 us |  54.4459 us | 160.535 us | 2,576.37 us | 11.7188 |      - |   91962 B |
|                    GU0050IgnoreEventsWhenSerializing |   388.63 us |  22.8561 us |  67.392 us |   356.68 us |  2.9297 |      - |   20739 B |
|                         GU0051XmlSerializerNotCached |   365.72 us |   7.2773 us |  15.820 us |   364.52 us |  3.9063 | 0.4883 |   27839 B |
|            GU0060EnumMemberValueConflictsWithAnother |    21.39 us |   0.5526 us |   1.612 us |    20.88 us |  0.1831 | 0.0305 |    1350 B |
| GU0070DefaultConstructedValueTypeWithNoUsefulDefault |   536.33 us |  11.1039 us |  32.214 us |   526.87 us |  7.8125 | 0.9766 |   55239 B |
|                            GU0071ForeachImplicitCast |   100.86 us |   2.0147 us |   5.481 us |    99.84 us |       - |      - |     441 B |
|                                 ArgumentListAnalyzer | 2,389.74 us |  47.4793 us | 100.150 us | 2,386.50 us |       - |      - |   18048 B |
|                                  ConstructorAnalyzer | 5,705.98 us | 176.2743 us | 519.749 us | 5,632.14 us | 39.0625 |      - |  295799 B |
|                               ObjectCreationAnalyzer |   430.46 us |   8.5982 us |  20.766 us |   426.80 us |  6.3477 | 0.9766 |   42172 B |
|                                    ParameterAnalyzer | 5,586.77 us | 149.5943 us | 438.734 us | 5,495.79 us | 31.2500 | 7.8125 |  224054 B |
|                             SimpleAssignmentAnalyzer |   777.37 us |  18.0857 us |  52.181 us |   772.29 us |  2.9297 | 0.9766 |   21959 B |
