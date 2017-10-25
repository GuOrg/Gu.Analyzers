``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 7 SP1 (6.1.7601)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0


```
 |                                    Method |        Mean |       Error |       StdDev |      Median |   Gen 0 |  Gen 1 | Allocated |
 |------------------------------------------ |------------:|------------:|-------------:|------------:|--------:|-------:|----------:|
 |                       GU0001NameArguments | 1,812.74 us |  42.3929 us |   122.989 us | 1,803.85 us |       - |      - |   13076 B |
 |        GU0002NamedArgumentPositionMatches | 1,823.10 us |  46.0031 us |   134.193 us | 1,813.12 us |       - |      - |     912 B |
 |       GU0003CtorParameterNamesShouldMatch |   149.76 us |   7.8503 us |    23.147 us |   149.91 us |  0.8545 | 0.1221 |    5701 B |
 |            GU0004AssignAllReadOnlyMembers | 4,827.96 us | 164.4667 us |   482.352 us | 4,788.67 us | 31.2500 |      - |  239492 B |
 |         GU0005ExceptionArgumentsPositions |   476.67 us |  11.6285 us |    33.551 us |   472.63 us |  6.8359 | 0.9766 |   48937 B |
 |                           GU0006UseNameof | 2,636.39 us |  72.2371 us |   208.421 us | 2,602.59 us | 31.2500 |      - |  212351 B |
 |                     GU0007PreferInjecting | 9,013.11 us | 423.3321 us | 1,248.204 us | 9,863.43 us |       - |      - |   29754 B |
 |                GU0008AvoidRelayProperties | 4,879.62 us | 146.6065 us |   432.273 us | 4,856.19 us |  7.8125 |      - |  104256 B |
 |       GU0009UseNamedParametersForBooleans | 2,297.13 us |  78.1819 us |   223.057 us | 2,248.63 us |       - |      - |     160 B |
 |                GU0010DoNotAssignSameValue |   522.02 us |  10.4265 us |    20.823 us |   520.05 us |       - |      - |    1004 B |
 |               GU0011DontIgnoreReturnValue | 2,323.98 us |  51.5901 us |   148.849 us | 2,319.95 us |  3.9063 |      - |   45952 B |
 |                      GU0020SortProperties |    84.28 us |   2.0833 us |     6.044 us |    83.47 us |  0.6104 |      - |    4392 B |
 |         GU0021CalculatedPropertyAllocates |    62.49 us |   1.2374 us |     2.965 us |    62.50 us |       - |      - |      41 B |
 |                          GU0022UseGetOnly | 6,145.66 us | 146.4460 us |   429.501 us | 6,057.26 us | 15.6250 |      - |  148609 B |
 |         GU0050IgnoreEventsWhenSerializing |   304.72 us |   8.0486 us |    23.732 us |   300.88 us |  2.4414 |      - |   16944 B |
 |              GU0051XmlSerializerNotCached |   329.76 us |   8.3656 us |    24.535 us |   332.59 us |  2.9297 | 0.4883 |   21540 B |
 | GU0060EnumMemberValueConflictsWithAnother |    22.85 us |   0.5669 us |     1.654 us |    22.95 us |  0.1221 | 0.0305 |     950 B |
