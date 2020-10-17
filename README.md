# Gu.Analyzers

This is a collection of warnings and refactorings with no real plan/scope. Made a package of it for the event someone finds something useful in it.

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Gu.Analyzers.svg)](https://www.nuget.org/packages/Gu.Analyzers/)
[![Build status](https://ci.appveyor.com/api/projects/status/nplt8lc7rhmgdi17/branch/master?svg=true)](https://ci.appveyor.com/project/JohanLarsson/gu-analyzers-qh7oa/branch/master)
[![Build Status](https://dev.azure.com/guorg/Gu.Analyzers/_apis/build/status/GuOrg.Gu.Analyzers?branchName=master)](https://dev.azure.com/guorg/Gu.Analyzers/_build/latest?definitionId=1&branchName=master)
[![Join the chat at https://gitter.im/JohanLarsson/Gu.Analyzers](https://badges.gitter.im/JohanLarsson/Gu.Analyzers.svg)](https://gitter.im/JohanLarsson/Gu.Analyzers?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

| Id       | Title
| :--      | :--
| [GU0001](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0001.md)| Name the arguments.
| [GU0002](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0002.md)| The position of a named argument should match.
| [GU0003](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0003.md)| Name the parameter to match the assigned member.
| [GU0004](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0004.md)| Assign all readonly members.
| [GU0005](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0005.md)| Use correct argument positions.
| [GU0006](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0006.md)| Use nameof.
| [GU0007](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0007.md)| Prefer injecting.
| [GU0008](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0008.md)| Avoid relay properties.
| [GU0009](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0009.md)| Name the boolean parameter.
| [GU0010](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0010.md)| Assigning same value.
| [GU0011](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0011.md)| Don't ignore the return value.
| [GU0012](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0012.md)| Check if parameter is null.
| [GU0013](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0013.md)| Throw for correct parameter.
| [GU0014](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0014.md)| Prefer using parameter.
| [GU0015](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0015.md)| Don't assign same more than once.
| [GU0016](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0016.md)| Prefer lambda.
| [GU0017](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0017.md)| Don't use discarded.
| [GU0020](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0020.md)| Sort properties.
| [GU0021](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0021.md)| Calculated property allocates reference type.
| [GU0022](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0022.md)| Use get-only.
| [GU0023](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0023.md)| Static members that initialize with other static members depend on document order.
| [GU0024](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0024.md)| Seal type with default member.
| [GU0025](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0025.md)| Seal type with overridden equality.
| [GU0050](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0050.md)| Ignore events when serializing.
| [GU0051](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0051.md)| Cache the XmlSerializer.
| [GU0052](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0052.md)| Mark exception with [Serializable].
| [GU0060](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0060.md)| Enum member value conflict.
| [GU0061](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0061.md)| Enum member value out of range.
| [GU0070](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0070.md)| Default-constructed value type with no useful default
| [GU0071](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0071.md)| Implicit casting done by the foreach
| [GU0072](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0072.md)| All types should be internal.
| [GU0073](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0073.md)| Member of non-public type should be internal.
| [GU0074](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0074.md)| Prefer pattern.
| [GU0075](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0075.md)| Prefer return nullable.
| [GU0076](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0076.md)| Merge pattern.
| [GU0077](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0077.md)| Prefer is null.
| [GU0080](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0080.md)| Parameter count does not match attribute.
| [GU0081](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0081.md)| TestCase does not match parameters.
| [GU0082](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0082.md)| TestCase is identical.
| [GU0083](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0083.md)| TestCase Arguments Mismatch Method Parameters
| [GU0084](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0084.md)| Assert exception message.
| [GU0090](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0090.md)| Don't throw NotImplementedException.
| [GU0100](https://github.com/GuOrg/Gu.Analyzers/blob/master/documentation/GU0100.md)| Wrong cref type.

## Using Gu.Analyzers

The preferable way to use the analyzers is to add the nuget package [Gu.Analyzers](https://www.nuget.org/packages/Gu.Analyzers/)
to the project(s).

The severity of individual rules may be configured using [rule set files](https://msdn.microsoft.com/en-us/library/dd264996.aspx)
in Visual Studio 2015.

## Installation

Gu.Analyzers can be installed using [Paket](https://fsprojects.github.io/Paket/) or the NuGet command line or the NuGet Package Manager in Visual Studio 2015.


**Install using the command line:**
```bash
Install-Package Gu.Analyzers
```

## Updating

The ruleset editor does not handle changes IDs well, if things get out of sync you can try:

1) Close visual studio.
2) Edit the ProjectName.rulset file and remove the Gu.Analyzers element.
3) Start visual studio and add back the desired configuration.

Above is not ideal, sorry about this. Not sure this is our bug.


## Current status

Early alpha, names and IDs may change.
