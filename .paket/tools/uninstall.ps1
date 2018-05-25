param($installPath, $toolsPath, $package, $project)

function Remove-AnalyzerReferences($folderPath)
{
    # Write-Host 'Folder '$folderPath
    if (Test-Path $folderPath)
    {
        foreach ($dllPath in Get-ChildItem -Path "$folderPath\*.dll" -Exclude *.resources.dll)
        {
            # Write-Host 'File '$dllPath.FullName
            $project.Object.AnalyzerReferences.Remove($dllPath.FullName)
        }
    }
}

$analyzersPath = Join-Path (Split-Path -Path $toolsPath -Parent) "analyzers\dotnet"
Remove-AnalyzerReferences($analyzersPath)

if($project.Type -eq "C#")
{
    $csAnalyzersPath = Join-Path $analyzersPath "cs"
    Remove-AnalyzerReferences($csAnalyzersPath)
}
if($project.Type -eq "VB.NET")
{
    $vbAnalyzersPath = Join-Path $analyzersPath "vb"
    Remove-AnalyzerReferences($vbAnalyzersPath)
}
