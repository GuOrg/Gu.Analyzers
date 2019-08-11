``` ini

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.17134.885 (1803/April2018Update/Redstone4)
Intel Core i7-7500U CPU 2.70GHz (Kaby Lake), 1 CPU, 4 logical and 2 physical cores
Frequency=2835936 Hz, Resolution=352.6173 ns, Timer=TSC
  [Host]     : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3416.0
  DefaultJob : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3416.0


```
|                                               Method |         Mean |      Error |     StdDev |       Median | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------------------------- |-------------:|-----------:|-----------:|-------------:|------:|------:|------:|----------:|
|                                     ArgumentAnalyzer |   642.351 us |  8.1435 us |  7.6174 us |   639.295 us |     - |     - |     - |         - |
|                                 ArgumentListAnalyzer | 1,399.575 us | 26.7499 us | 29.7325 us | 1,398.480 us |     - |     - |     - |   40960 B |
|                             ClassDeclarationAnalyzer |   144.321 us |  2.2198 us |  1.9678 us |   143.868 us |     - |     - |     - |         - |
|                                  ConstructorAnalyzer |   332.974 us |  8.1018 us |  8.3200 us |   330.050 us |     - |     - |     - |         - |
|                                    ExceptionAnalyzer |    51.401 us |  1.2686 us |  1.0594 us |    51.130 us |     - |     - |     - |         - |
|                               IdentifierNameAnalyzer | 1,420.505 us | 13.1342 us | 10.9677 us | 1,420.695 us |     - |     - |     - |         - |
|                                  MethodGroupAnalyzer | 2,497.792 us | 60.6062 us | 67.3637 us | 2,475.373 us |     - |     - |     - |  122880 B |
|                               ObjectCreationAnalyzer |   249.300 us |  3.1515 us |  2.7937 us |   249.477 us |     - |     - |     - |         - |
|                                    ParameterAnalyzer | 1,344.123 us | 18.3343 us | 15.3100 us | 1,338.535 us |     - |     - |     - |   24576 B |
|                          PropertyDeclarationAnalyzer |   639.695 us | 11.8395 us | 11.0747 us |   637.885 us |     - |     - |     - |   16384 B |
|                             SimpleAssignmentAnalyzer | 3,464.792 us | 41.8836 us | 37.1288 us | 3,452.123 us |     - |     - |     - |  120640 B |
|                      StringLiteralExpressionAnalyzer | 1,295.570 us | 13.2953 us | 11.1022 us | 1,293.753 us |     - |     - |     - |  131072 B |
|                                   TestMethodAnalyzer |    77.901 us |  1.5270 us |  1.2751 us |    77.576 us |     - |     - |     - |         - |
|                                GU0007PreferInjecting | 1,560.684 us | 14.3301 us | 12.7033 us | 1,557.158 us |     - |     - |     - |   16384 B |
|                         GU0011DoNotIgnoreReturnValue | 1,042.906 us |  9.4557 us |  7.8960 us | 1,044.100 us |     - |     - |     - |   81920 B |
|                                 GU0020SortProperties |    79.420 us |  1.3808 us |  1.1530 us |    78.986 us |     - |     - |     - |         - |
|                                     GU0022UseGetOnly |     5.945 us |  0.1746 us |  0.4751 us |     5.994 us |     - |     - |     - |         - |
|                      GU0023StaticMemberOrderAnalyzer | 2,137.390 us | 42.4081 us | 58.0487 us | 2,131.042 us |     - |     - |     - |  132896 B |
|                    GU0050IgnoreEventsWhenSerializing |   367.496 us |  7.3310 us | 18.7921 us |   362.138 us |     - |     - |     - |   49152 B |
|                         GU0051XmlSerializerNotCached |   132.209 us |  2.6073 us |  5.9908 us |   130.116 us |     - |     - |     - |         - |
|                  GU0052ExceptionShouldBeSerializable |   277.634 us |  6.4828 us | 11.0083 us |   275.041 us |     - |     - |     - |   40960 B |
|            GU0060EnumMemberValueConflictsWithAnother |    55.307 us |  1.1018 us |  0.9200 us |    55.361 us |     - |     - |     - |         - |
|                      GU0061EnumMemberValueOutOfRange |    14.520 us |  0.3888 us |  1.0511 us |    14.457 us |     - |     - |     - |         - |
| GU0070DefaultConstructedValueTypeWithNoUsefulDefault |   121.901 us |  2.3902 us |  3.3507 us |   120.948 us |     - |     - |     - |         - |
|                            GU0071ForeachImplicitCast |    46.328 us |  0.9222 us |  1.6151 us |    46.545 us |     - |     - |     - |         - |
|                       GU0072AllTypesShouldBeInternal |    67.187 us |  2.0515 us |  1.7131 us |    66.645 us |     - |     - |     - |         - |
|                         GU0073MemberShouldBeInternal |   194.368 us |  4.7635 us |  4.2228 us |   193.411 us |     - |     - |     - |         - |
