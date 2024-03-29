#### 2.0.2
* BUGFIX: Don't warn when indexing.

#### 2.0.1
* GU0019: Warn on IEnumerable&lt;struct&gt;.FirstOrDefault()
* GU0026: Warn on array[1..] as it allocates

#### 2.0.0
* BREAKING: For VS2022+ now.
* BUGFIX: AD0001 -&gt; Could not load file or assembly

#### 1.8.5
* BUGFIX: Handle roslyn reporting different error.

#### 1.8.2
* BUGFIX: GU0011 should not warn on void returning.

#### 1.8.0
* FEATURE: Name mocks

#### 1.5.5
* FEATURE: GU0007 handle singletons.
* BUGFIX GU0012: ignore out parameters.

#### 1.5.4
* FEATURE: Member of internal class should be internal, analyzer + fix.

#### 1.5.3
* BUGFIX: NRE in exception analyzer.

#### 1.5.0
* FEATURE: SplitStringRefactoring
* FEATURE: MakeStaticFix

#### 1.4.0
* FEATURE: GU0017 Don't use discarded

#### 1.3.2
* BUGFIX: Garbage docs for SA1611.
* Garbage docs for type parameter.

#### 1.3.0
* Generate useless docs for SA1614. 
* Fix: create parameter when adding testcase arg.

#### 1.2.18
* FEATURE: New analyzer GU0024.

#### 1.2.13
* BUGFIX: Static member order.

#### 1.2.13
* FEATURE: Code fix for GU0009.

#### 1.2.10
* BUGFIXES: Don't nag on fluent moq.

#### 1.2.9
* FEATURE: New analyzer: check that exceptions are [Serializable].
* BUGFIX: GU0082 handle enums.

#### 1.2.6
* FEATURE: New analyzer: check testcase attribute arguments & parameter types.

#### 1.2.5
* FEATURE: New analyzers for checking NUnit attribute usage.

#### 1.2.1
* BREAKING: Only VS2017+ from now on.
* Bugfixes.

#### 1.2.0
* NEW ANALYZER: GU0071

#### 1.0.0
* BREAKING: Moved dispoable analyzers to https://www.nuget.org/packages/IDisposableAnalyzers/