``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 7 SP1 (6.1.7601)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0


```
 |                                    Method |        Mean |      Error |     StdDev |      Median |   Gen 0 |  Gen 1 | Allocated |
 |------------------------------------------ |------------:|-----------:|-----------:|------------:|--------:|-------:|----------:|
 |                       GU0001NameArguments | 1,749.48 us |  37.454 us | 107.463 us | 1,752.80 us |       - |      - |   12016 B |
 |        GU0002NamedArgumentPositionMatches | 1,800.94 us |  37.251 us | 107.479 us | 1,796.27 us |       - |      - |     928 B |
 |       GU0003CtorParameterNamesShouldMatch |   146.87 us |   4.194 us |  12.365 us |   147.51 us |  0.7324 | 0.2441 |    6510 B |
 |            GU0004AssignAllReadOnlyMembers | 2,642.84 us |  89.076 us | 262.643 us | 2,638.92 us | 11.7188 |      - |  103746 B |
 |         GU0005ExceptionArgumentsPositions |   183.50 us |   4.353 us |  12.628 us |   183.48 us |       - |      - |      42 B |
 |                           GU0006UseNameof | 2,639.51 us |  68.803 us | 199.610 us | 2,624.60 us | 31.2500 |      - |  212033 B |
 |                     GU0007PreferInjecting | 6,749.91 us | 177.702 us | 523.958 us | 6,654.21 us |       - |      - |   30140 B |
 |                GU0008AvoidRelayProperties | 4,661.51 us | 106.766 us | 311.441 us | 4,654.01 us |  7.8125 |      - |  105537 B |
 |       GU0009UseNamedParametersForBooleans | 2,257.17 us |  52.912 us | 152.663 us | 2,240.46 us |       - |      - |     160 B |
 |                GU0010DoNotAssignSameValue |   497.58 us |  12.313 us |  35.721 us |   494.91 us |       - |      - |    1008 B |
 |               GU0011DontIgnoreReturnValue | 5,538.10 us | 220.870 us | 651.240 us | 5,418.40 us | 78.1250 |      - |  535878 B |
 |                      GU0020SortProperties |    83.31 us |   2.221 us |   6.514 us |    82.16 us |  0.6104 |      - |    4392 B |
 |         GU0021CalculatedPropertyAllocates |    64.05 us |   1.858 us |   5.331 us |    62.97 us |       - |      - |      40 B |
 |                          GU0022UseGetOnly | 5,736.53 us | 153.064 us | 448.909 us | 5,699.72 us | 15.6250 |      - |  148609 B |
 |         GU0050IgnoreEventsWhenSerializing |   287.93 us |   7.878 us |  23.228 us |   285.13 us |  2.4414 |      - |   16624 B |
 |              GU0051XmlSerializerNotCached |   171.41 us |   4.151 us |  12.173 us |   168.71 us |       - |      - |      42 B |
 | GU0060EnumMemberValueConflictsWithAnother |    24.11 us |   1.292 us |   3.809 us |    22.98 us |  0.1221 | 0.0305 |     950 B |
