# Gu.Analyzers

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Gu.Analyzers.svg)](https://www.nuget.org/packages/Gu.Analyzers/)
[![Build status](https://ci.appveyor.com/api/projects/status/nplt8lc7rhmgdi17/branch/master?svg=true)](https://ci.appveyor.com/project/JohanLarsson/gu-analyzers-qh7oa/branch/master)
[![Join the chat at https://gitter.im/JohanLarsson/Gu.Analyzers](https://badges.gitter.im/JohanLarsson/Gu.Analyzers.svg)](https://gitter.im/JohanLarsson/Gu.Analyzers?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

<!-- start generated table -->
<table>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0001.md">GU0001</a></td>
  <td>Name the arguments.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0002.md">GU0002</a></td>
  <td>The position of a named argument should match.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0003.md">GU0003</a></td>
  <td>Name the parameters to match the assigned members.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0004.md">GU0004</a></td>
  <td>Assign all readonly members.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0005.md">GU0005</a></td>
  <td>Use correct argument positions.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0006.md">GU0006</a></td>
  <td>Use nameof.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0007.md">GU0007</a></td>
  <td>Prefer injecting.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0008.md">GU0008</a></td>
  <td>Avoid relay properties.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0009.md">GU0009</a></td>
  <td>Name the boolean parameter.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0010.md">GU0010</a></td>
  <td>Assigning same value.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0011.md">GU0011</a></td>
  <td>Don't ignore the returnvalue.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0020.md">GU0020</a></td>
  <td>Sort properties.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0021.md">GU0021</a></td>
  <td>Calculated property allocates reference type.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0022.md">GU0022</a></td>
  <td>Use get-only.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0050.md">GU0050</a></td>
  <td>Ignore events when serializing.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0051.md">GU0051</a></td>
  <td>Cache the XmlSerializer.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0060.md">GU0060</a></td>
  <td>Enum member value conflict.</td>
</tr>
<tr>
  <td><a href="https://github.com/JohanLarsson/Gu.Analyzers/blob/master/documentation/GU0070.md">GU0070</a></td>
  <td>Default-constructed value type with no no useful default</td>
</tr>
<table>
<!-- end generated table -->

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
