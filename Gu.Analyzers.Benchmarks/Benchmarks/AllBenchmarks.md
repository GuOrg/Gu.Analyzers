``` ini

BenchmarkDotNet=v0.11.4, OS=Windows 10.0.17134.523 (1803/April2018Update/Redstone4)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical and 8 physical cores
Frequency=3410073 Hz, Resolution=293.2489 ns, Timer=TSC
  [Host]     : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.3260.0
  DefaultJob : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.3260.0


```
|                                               Method |          Mean |       Error |      StdDev |        Median | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|----------------------------------------------------- |--------------:|------------:|------------:|--------------:|------------:|------------:|------------:|--------------------:|
|                                 ArgumentListAnalyzer |  9,726.853 us | 186.5343 us | 199.5896 us |  9,666.655 us |           - |           - |           - |             16384 B |
|                             ClassDeclarationAnalyzer |    622.193 us |  12.2295 us |  21.7380 us |    618.755 us |           - |           - |           - |                   - |
|                                  ConstructorAnalyzer |    432.323 us |   9.6392 us |  28.2702 us |    426.677 us |           - |           - |           - |                   - |
|                                  MethodGroupAnalyzer | 15,890.005 us | 113.8963 us |  95.1086 us | 15,892.035 us |           - |           - |           - |             65536 B |
|                               ObjectCreationAnalyzer |    945.271 us |  18.4327 us |  25.8400 us |    945.141 us |           - |           - |           - |                   - |
|                                    ParameterAnalyzer |  4,114.692 us |  75.9160 us |  71.0119 us |  4,107.830 us |           - |           - |           - |             16384 B |
|                          PropertyDeclarationAnalyzer |    960.345 us |  18.7300 us |  25.6379 us |    962.149 us |           - |           - |           - |                   - |
|                             SimpleAssignmentAnalyzer |  4,033.481 us |  97.9041 us | 140.4111 us |  3,992.437 us |           - |           - |           - |             57344 B |
|                                   TestMethodAnalyzer |  1,204.543 us |  19.2383 us |  21.3833 us |  1,204.666 us |           - |           - |           - |                   - |
|                                      GU0006UseNameof |  2,680.803 us |  53.1731 us |  49.7381 us |  2,675.603 us |           - |           - |           - |             65536 B |
|                                GU0007PreferInjecting | 18,885.391 us | 370.6691 us | 364.0467 us | 18,791.826 us |           - |           - |           - |                   - |
|                  GU0009UseNamedParametersForBooleans | 13,743.362 us | 225.8859 us | 211.2938 us | 13,706.158 us |           - |           - |           - |                   - |
|                          GU0011DoNotIgnoreReturnValue |  9,138.828 us | 149.7263 us | 132.7285 us |  9,095.406 us |           - |           - |           - |             40960 B |
|                                 GU0020SortProperties |    433.710 us |   8.5785 us |  18.2814 us |    431.076 us |           - |           - |           - |                   - |
|                                     GU0022UseGetOnly |      6.111 us |   0.1746 us |   0.4601 us |      6.158 us |           - |           - |           - |                   - |
|                      GU0023StaticMemberOrderAnalyzer |    644.871 us |  12.7972 us |  13.6928 us |    644.561 us |           - |           - |           - |             24576 B |
|                    GU0050IgnoreEventsWhenSerializing |  1,710.749 us |  44.2047 us |  47.2985 us |  1,700.403 us |           - |           - |           - |             98304 B |
|                         GU0051XmlSerializerNotCached |    845.677 us |  16.4014 us |  23.5224 us |    836.639 us |           - |           - |           - |                   - |
|                  GU0052ExceptionShouldBeSerializable |  1,011.823 us |  18.9894 us |  20.3184 us |  1,012.148 us |           - |           - |           - |             57344 B |
|            GU0060EnumMemberValueConflictsWithAnother |     66.336 us |   3.4724 us |  10.1838 us |     62.169 us |           - |           - |           - |                   - |
| GU0070DefaultConstructedValueTypeWithNoUsefulDefault |    787.120 us |  15.7017 us |  23.0154 us |    789.133 us |           - |           - |           - |                   - |
|                            GU0071ForeachImplicitCast |    468.626 us |   9.2637 us |  16.4662 us |    466.852 us |           - |           - |           - |                   - |
|                       GU0072AllTypesShouldBeInternal |    581.106 us |  11.2367 us |  17.1596 us |    585.618 us |           - |           - |           - |                   - |
