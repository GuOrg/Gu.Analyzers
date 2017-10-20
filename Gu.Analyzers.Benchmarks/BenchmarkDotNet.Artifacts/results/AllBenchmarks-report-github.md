``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i7-3667U CPU 2.00GHz (Ivy Bridge), ProcessorCount=4
Frequency=2435876 Hz, Resolution=410.5299 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2115.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2115.0


```
 |                                    Method |         Mean |         Error |        StdDev |       Median |    Gen 0 |  Gen 1 | Allocated |
 |------------------------------------------ |-------------:|--------------:|--------------:|-------------:|---------:|-------:|----------:|
 |       GU0009UseNamedParametersForBooleans |  2,898.65 us |    47.0396 us |    44.0009 us |  2,900.51 us |        - |      - |      64 B |
 |                          GU0022UseGetOnly |  8,977.33 us |   360.2198 us |   385.4311 us |  8,841.25 us |  62.5000 |      - |  142724 B |
 |                       GU0001NameArguments |  2,594.63 us |    49.4209 us |    43.8104 us |  2,581.80 us |   3.9063 |      - |   14016 B |
 |        GU0002NamedArgumentPositionMatches |  4,640.53 us |   183.6851 us |   518.0870 us |  4,421.33 us |  85.9375 |      - |  194376 B |
 |       GU0003CtorParameterNamesShouldMatch |    277.84 us |     5.3113 us |     5.4543 us |    278.48 us |   3.4180 |      - |    7568 B |
 |            GU0004AssignAllReadOnlyMembers |  3,756.64 us |    85.0428 us |    71.0146 us |  3,732.41 us |  46.8750 |      - |  104741 B |
 |         GU0005ExceptionArgumentsPositions |    669.61 us |     8.2206 us |     7.6895 us |    668.58 us |   0.9766 |      - |    2136 B |
 |                           GU0006UseNameof | 23,896.74 us |   223.5292 us |   186.6570 us | 23,915.85 us | 687.5000 |      - | 1471324 B |
 |                     GU0007PreferInjecting | 64,722.29 us | 1,291.5028 us | 2,295.6450 us | 64,589.42 us | 812.5000 |      - | 1734963 B |
 |                GU0008AvoidRelayProperties |  8,641.51 us |   166.8044 us |   156.0289 us |  8,625.33 us |  78.1250 |      - |  185605 B |
 |                GU0010DoNotAssignSameValue |    457.56 us |    10.6137 us |    29.9361 us |    443.64 us |        - |      - |    1008 B |
 |               GU0011DontIgnoreReturnValue |  9,424.53 us |   191.1486 us |   372.8207 us |  9,356.38 us | 187.5000 |      - |  413581 B |
 |                      GU0020SortProperties |    247.22 us |     2.6868 us |     2.5132 us |    247.03 us |   4.3945 |      - |   10024 B |
 |         GU0021CalculatedPropertyAllocates |    101.71 us |     2.3217 us |     6.5861 us |    100.44 us |        - |      - |      41 B |
 |         GU0050IgnoreEventsWhenSerializing |    681.60 us |    13.5586 us |    21.1091 us |    684.57 us |  13.6719 |      - |   30025 B |
 | GU0060EnumMemberValueConflictsWithAnother |     38.23 us |     0.4834 us |     0.4521 us |     38.15 us |   0.4272 | 0.0610 |    1046 B |
 |              GU0051XmlSerializerNotCached |  2,136.31 us |    43.7001 us |   108.0157 us |  2,102.33 us |  93.7500 |      - |  200778 B |
