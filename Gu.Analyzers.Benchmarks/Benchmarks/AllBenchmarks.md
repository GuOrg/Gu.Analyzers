``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 7 SP1 (6.1.7601)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0


```
 |                                    Method |        Mean |       Error |     StdDev |      Median |   Gen 0 |  Gen 1 | Allocated |
 |------------------------------------------ |------------:|------------:|-----------:|------------:|--------:|-------:|----------:|
 |                       GU0001NameArguments | 2,508.40 us |   1.7381 us |   2.493 us | 2,507.52 us |       - |      - |   11728 B |
 |        GU0002NamedArgumentPositionMatches | 2,493.22 us |  63.3161 us | 186.689 us | 2,457.08 us | 27.3438 |      - |  185026 B |
 |       GU0003CtorParameterNamesShouldMatch |   140.88 us |   3.9939 us |  11.776 us |   139.73 us |  0.7324 | 0.2441 |    6510 B |
 |            GU0004AssignAllReadOnlyMembers | 2,579.32 us |  55.2618 us | 162.941 us | 2,578.97 us | 15.6250 |      - |  104739 B |
 |         GU0005ExceptionArgumentsPositions |   345.92 us |   6.8229 us |  10.822 us |   345.40 us |       - |      - |     692 B |
 |                           GU0006UseNameof | 2,655.36 us |  58.6237 us | 171.933 us | 2,664.66 us | 31.2500 |      - |  211833 B |
 |                     GU0007PreferInjecting | 7,064.98 us | 178.3777 us | 514.660 us | 7,034.30 us |       - |      - |   30150 B |
 |                GU0008AvoidRelayProperties | 4,663.66 us |  93.0221 us | 256.210 us | 4,681.26 us |  7.8125 |      - |  105537 B |
 |       GU0009UseNamedParametersForBooleans | 2,141.26 us |  51.0187 us | 145.559 us | 2,124.95 us |       - |      - |     160 B |
 |                GU0010DoNotAssignSameValue |   505.42 us |  11.7541 us |  34.287 us |   501.28 us |       - |      - |    1004 B |
 |               GU0011DontIgnoreReturnValue | 4,995.49 us | 140.5587 us | 410.016 us | 4,954.92 us | 78.1250 |      - |  507654 B |
 |                      GU0020SortProperties |    83.85 us |   1.6824 us |   4.934 us |    83.68 us |  0.6104 |      - |    4392 B |
 |         GU0021CalculatedPropertyAllocates |    62.97 us |   1.2485 us |   3.397 us |    63.02 us |       - |      - |      41 B |
 |                          GU0022UseGetOnly | 5,734.08 us | 126.3002 us | 364.405 us | 5,738.06 us | 15.6250 |      - |  148609 B |
 |         GU0050IgnoreEventsWhenSerializing |   346.15 us |  23.9144 us |  70.512 us |   305.15 us |  2.4414 |      - |   16664 B |
 |              GU0051XmlSerializerNotCached | 1,152.37 us |  35.0444 us | 102.779 us | 1,140.56 us | 27.3438 |      - |  182323 B |
 | GU0060EnumMemberValueConflictsWithAnother |    21.74 us |   0.5504 us |   1.597 us |    21.55 us |  0.1221 | 0.0305 |     950 B |
