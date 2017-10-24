``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 7 SP1 (6.1.7601)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0


```
 |                                    Method |         Mean |         Error |        StdDev |       Median |    Gen 0 |  Gen 1 | Allocated |
 |------------------------------------------ |-------------:|--------------:|--------------:|-------------:|---------:|-------:|----------:|
 |                       GU0001NameArguments |  1,721.35 us |    34.7436 us |     95.110 us |  1,719.87 us |        - |      - |   11728 B |
 |        GU0002NamedArgumentPositionMatches |  2,518.71 us |    56.5631 us |    165.890 us |  2,524.69 us |  27.3438 |      - |  186050 B |
 |       GU0003CtorParameterNamesShouldMatch |    137.54 us |     4.3758 us |     12.764 us |    135.79 us |   0.7324 | 0.2441 |    6510 B |
 |            GU0004AssignAllReadOnlyMembers |  2,670.69 us |    75.6582 us |    223.080 us |  2,637.29 us |  15.6250 |      - |  104739 B |
 |         GU0005ExceptionArgumentsPositions |    348.02 us |     7.9839 us |     23.289 us |    345.48 us |        - |      - |     692 B |
 |                           GU0006UseNameof | 54,638.96 us | 3,525.0473 us | 10,393.681 us | 49,226.67 us | 812.5000 |      - | 5348734 B |
 |                     GU0007PreferInjecting | 37,791.62 us | 1,345.0969 us |  3,966.049 us | 37,524.44 us | 312.5000 |      - | 2126360 B |
 |                GU0008AvoidRelayProperties |  4,678.71 us |   121.4725 us |    352.414 us |  4,626.31 us |   7.8125 |      - |  105537 B |
 |       GU0009UseNamedParametersForBooleans |  2,020.21 us |    48.8660 us |    142.544 us |  1,977.40 us |        - |      - |     160 B |
 |                GU0010DoNotAssignSameValue |    496.87 us |    10.4368 us |     30.773 us |    494.01 us |        - |      - |    1008 B |
 |               GU0011DontIgnoreReturnValue |  4,779.84 us |   161.7515 us |    476.928 us |  4,578.06 us |  70.3125 |      - |  491270 B |
 |                      GU0020SortProperties |     83.68 us |     1.8023 us |      5.257 us |     83.07 us |   0.6104 |      - |    4392 B |
 |         GU0021CalculatedPropertyAllocates |     61.61 us |     1.2846 us |      3.788 us |     61.54 us |        - |      - |      41 B |
 |                          GU0022UseGetOnly |  5,943.50 us |   131.9130 us |    388.948 us |  5,908.48 us |  15.6250 |      - |  148609 B |
 |         GU0050IgnoreEventsWhenSerializing |    302.73 us |     7.7728 us |     22.674 us |    299.39 us |   2.4414 |      - |   16664 B |
 |              GU0051XmlSerializerNotCached |  1,093.16 us |    37.6978 us |    111.153 us |  1,065.50 us |  28.3203 |      - |  182315 B |
 | GU0060EnumMemberValueConflictsWithAnother |     19.52 us |     0.4614 us |      1.324 us |     19.07 us |   0.1221 | 0.0305 |     950 B |
