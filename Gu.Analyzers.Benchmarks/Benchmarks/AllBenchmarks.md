``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.685 (2004/?/20H1)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical and 8 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT
  DefaultJob : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT


```
|                                               Method |         Mean |       Error |      StdDev |       Median | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------------------------- |-------------:|------------:|------------:|-------------:|------:|------:|------:|----------:|
|                                     ArgumentAnalyzer | 1,239.489 μs |  22.0772 μs |  43.0600 μs | 1,234.700 μs |     - |     - |     - |         - |
|                                 ArgumentListAnalyzer | 2,460.835 μs |  48.2614 μs |  49.5609 μs | 2,442.500 μs |     - |     - |     - |   57344 B |
|                             ClassDeclarationAnalyzer |   438.950 μs |   7.4700 μs |   6.6220 μs |   436.400 μs |     - |     - |     - |         - |
|                                  ConstructorAnalyzer |   442.590 μs |   8.7814 μs |  20.8699 μs |   435.900 μs |     - |     - |     - |   16384 B |
|                                    ExceptionAnalyzer |   101.106 μs |   1.9595 μs |   2.0123 μs |   101.200 μs |     - |     - |     - |         - |
|                               IdentifierNameAnalyzer | 2,808.051 μs |  55.1631 μs | 112.6835 μs | 2,773.000 μs |     - |     - |     - |         - |
|                                  MethodGroupAnalyzer | 4,024.531 μs |  77.0268 μs |  75.6506 μs | 3,991.200 μs |     - |     - |     - |  167368 B |
|                               ObjectCreationAnalyzer |   336.907 μs |   6.6482 μs |  12.3229 μs |   335.400 μs |     - |     - |     - |         - |
|                                    ParameterAnalyzer | 3,268.500 μs |  65.0749 μs | 145.5493 μs | 3,222.650 μs |     - |     - |     - |   47304 B |
|                          PropertyDeclarationAnalyzer |   292.040 μs |   5.6111 μs |  13.4438 μs |   290.400 μs |     - |     - |     - |         - |
|                             SimpleAssignmentAnalyzer | 6,325.541 μs | 118.1670 μs | 121.3488 μs | 6,339.700 μs |     - |     - |     - |  140640 B |
|                      StringLiteralExpressionAnalyzer | 3,186.350 μs |  62.0486 μs |  71.4553 μs | 3,191.800 μs |     - |     - |     - |  385024 B |
|                                   TestMethodAnalyzer |   128.223 μs |   1.2399 μs |   1.0353 μs |   128.200 μs |     - |     - |     - |         - |
|                                GU0007PreferInjecting | 2,518.271 μs |  46.5340 μs |  41.2512 μs | 2,513.750 μs |     - |     - |     - |   40960 B |
|                         GU0011DoNotIgnoreReturnValue | 1,557.504 μs |  29.5147 μs |  42.3291 μs | 1,550.400 μs |     - |     - |     - |   73728 B |
|                                 GU0020SortProperties |    84.492 μs |   1.1553 μs |   0.9648 μs |    84.400 μs |     - |     - |     - |         - |
|                                     GU0022UseGetOnly |     8.382 μs |   0.2733 μs |   0.7295 μs |     8.500 μs |     - |     - |     - |         - |
|                      GU0023StaticMemberOrderAnalyzer | 8,817.146 μs | 109.1388 μs |  91.1358 μs | 8,778.100 μs |     - |     - |     - |  601672 B |
|                    GU0050IgnoreEventsWhenSerializing |   454.833 μs |   9.0032 μs |  18.5933 μs |   457.250 μs |     - |     - |     - |   32768 B |
|                         GU0051XmlSerializerNotCached |   191.933 μs |   3.1008 μs |   2.4209 μs |   191.650 μs |     - |     - |     - |         - |
|                  GU0052ExceptionShouldBeSerializable |   368.490 μs |   7.3662 μs |  19.0146 μs |   359.200 μs |     - |     - |     - |   16384 B |
|            GU0060EnumMemberValueConflictsWithAnother |    57.639 μs |   1.1141 μs |   1.5978 μs |    57.250 μs |     - |     - |     - |         - |
|                      GU0061EnumMemberValueOutOfRange |    13.424 μs |   0.2628 μs |   0.4169 μs |    13.300 μs |     - |     - |     - |         - |
| GU0070DefaultConstructedValueTypeWithNoUsefulDefault |   148.179 μs |   3.5666 μs |  10.0011 μs |   144.100 μs |     - |     - |     - |         - |
|                            GU0071ForeachImplicitCast |    71.662 μs |   1.3824 μs |   1.1544 μs |    71.700 μs |     - |     - |     - |         - |
|                       GU0072AllTypesShouldBeInternal |   119.275 μs |   2.2737 μs |   1.7751 μs |   119.550 μs |     - |     - |     - |         - |
|                         GU0073MemberShouldBeInternal |   268.331 μs |   5.3260 μs |  13.3619 μs |   260.700 μs |     - |     - |     - |         - |
