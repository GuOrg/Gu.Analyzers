``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical cores and 8 physical cores
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                                               Method |        Mean |       Error |     StdDev |      Median |   Gen 0 |  Gen 1 | Allocated |
|----------------------------------------------------- |------------:|------------:|-----------:|------------:|--------:|-------:|----------:|
|                  GU0003CtorParameterNamesShouldMatch |   134.38 us |   4.7350 us |  13.961 us |   130.94 us |  0.7324 |      - |    6494 B |
|                       GU0004AssignAllReadOnlyMembers | 5,776.48 us | 195.5185 us | 576.491 us | 5,807.08 us | 39.0625 |      - |  294967 B |
|                                      GU0006UseNameof | 3,182.99 us | 101.7849 us | 295.296 us | 3,119.21 us | 35.1563 |      - |  244115 B |
|                                GU0007PreferInjecting | 6,716.12 us | 147.7002 us | 433.179 us | 6,719.78 us |       - |      - |   25676 B |
|                           GU0008AvoidRelayProperties |   832.69 us |  24.2274 us |  71.435 us |   835.68 us |       - |      - |    7011 B |
|                  GU0009UseNamedParametersForBooleans | 2,673.04 us |  85.8559 us | 250.446 us | 2,635.28 us |       - |      - |     544 B |
|                          GU0011DontIgnoreReturnValue | 2,676.20 us |  75.3900 us | 219.916 us | 2,648.44 us |  3.9063 |      - |   47866 B |
|                                 GU0020SortProperties |    81.48 us |   1.9146 us |   5.524 us |    81.47 us |  0.7324 |      - |    4921 B |
|                    GU0021CalculatedPropertyAllocates |    68.36 us |   1.3583 us |   3.602 us |    68.04 us |       - |      - |     441 B |
|                                     GU0022UseGetOnly | 3,314.39 us | 143.7837 us | 423.949 us | 3,185.89 us | 11.7188 |      - |   99738 B |
|                    GU0050IgnoreEventsWhenSerializing |   400.00 us |  11.7736 us |  34.715 us |   392.16 us |  2.4414 |      - |   19051 B |
|                         GU0051XmlSerializerNotCached |   376.78 us |  10.9225 us |  31.339 us |   375.41 us |  3.9063 | 0.4883 |   26576 B |
|            GU0060EnumMemberValueConflictsWithAnother |    22.10 us |   0.7050 us |   2.068 us |    22.34 us |  0.1831 | 0.0305 |    1350 B |
| GU0070DefaultConstructedValueTypeWithNoUsefulDefault |   537.51 us |  16.1837 us |  46.434 us |   527.50 us |  7.8125 | 0.9766 |   52711 B |
|                            GU0071ForeachImplicitCast |   111.05 us |   2.9413 us |   8.486 us |   109.12 us |       - |      - |     441 B |
|                                 ArgumentListAnalyzer | 2,393.02 us |  70.9957 us | 205.971 us | 2,380.82 us |       - |      - |   17216 B |
|                               ObjectCreationAnalyzer |   441.57 us |  12.0281 us |  34.896 us |   436.60 us |  6.3477 | 0.9766 |   40908 B |
|                             SimpleAssignmentAnalyzer |   607.06 us |  15.5954 us |  45.739 us |   609.61 us |  2.9297 | 0.9766 |   21039 B |
