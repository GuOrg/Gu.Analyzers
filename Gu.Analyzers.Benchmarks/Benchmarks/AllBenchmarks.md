``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical cores and 8 physical cores
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                                               Method |        Mean |       Error |     StdDev |      Median |   Gen 0 |  Gen 1 | Allocated |
|----------------------------------------------------- |------------:|------------:|-----------:|------------:|--------:|-------:|----------:|
|                  GU0003CtorParameterNamesShouldMatch |   144.41 us |   3.8912 us |  11.412 us |   145.05 us |  0.7324 |      - |    6494 B |
|                       GU0004AssignAllReadOnlyMembers | 5,984.07 us | 168.6079 us | 491.837 us | 5,944.84 us | 46.8750 |      - |  313208 B |
|                                      GU0006UseNameof | 3,346.88 us |  81.4745 us | 229.800 us | 3,344.42 us | 31.2500 |      - |  222162 B |
|                                GU0007PreferInjecting | 5,451.94 us | 119.0403 us | 345.358 us | 5,392.00 us |       - |      - |   23076 B |
|                           GU0008AvoidRelayProperties |   841.30 us |  20.1761 us |  59.173 us |   845.05 us |       - |      - |    7010 B |
|                  GU0009UseNamedParametersForBooleans | 2,839.72 us |  70.1073 us | 200.020 us | 2,809.00 us |       - |      - |     544 B |
|                          GU0011DontIgnoreReturnValue | 2,555.72 us |  56.4490 us | 166.441 us | 2,543.61 us |  3.9063 |      - |   49594 B |
|                                 GU0020SortProperties |    91.75 us |   2.3415 us |   6.830 us |    91.31 us |  0.7324 |      - |    4921 B |
|                    GU0021CalculatedPropertyAllocates |    84.38 us |   2.2878 us |   6.710 us |    83.70 us |       - |      - |     441 B |
|                                     GU0022UseGetOnly | 2,811.45 us |  69.5654 us | 205.115 us | 2,814.36 us | 11.7188 |      - |   92442 B |
|                    GU0050IgnoreEventsWhenSerializing |   395.05 us |  13.8487 us |  40.833 us |   384.86 us |  2.9297 |      - |   20015 B |
|                         GU0051XmlSerializerNotCached |   416.52 us |   9.3123 us |  27.458 us |   414.33 us |  3.9063 | 0.4883 |   27135 B |
|            GU0060EnumMemberValueConflictsWithAnother |    22.92 us |   0.6146 us |   1.812 us |    22.92 us |  0.1831 | 0.0305 |    1350 B |
| GU0070DefaultConstructedValueTypeWithNoUsefulDefault |   561.98 us |  14.2613 us |  41.826 us |   558.57 us |  7.8125 | 0.9766 |   53831 B |
|                            GU0071ForeachImplicitCast |   156.29 us |   8.3341 us |  24.573 us |   168.61 us |       - |      - |     442 B |
|                                 ArgumentListAnalyzer | 2,798.48 us | 112.5444 us | 331.840 us | 2,745.30 us |       - |      - |   17824 B |
|                               ObjectCreationAnalyzer |   445.62 us |  12.4806 us |  36.407 us |   437.49 us |  6.3477 | 0.9766 |   40908 B |
|                                    ParameterAnalyzer | 6,121.88 us | 243.6130 us | 710.630 us | 6,016.49 us | 31.2500 | 7.8125 |  223734 B |
|                             SimpleAssignmentAnalyzer |   811.40 us |  18.5872 us |  54.805 us |   806.80 us |  2.9297 | 0.9766 |   22455 B |
