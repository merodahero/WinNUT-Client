param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$setupDirectory = $PSScriptRoot
$solutionDirectory = Split-Path -Parent $setupDirectory
$appProject = Join-Path $solutionDirectory 'WinNUT-Avalonia\WinNUT-Avalonia.csproj'
$publishDirectory = Join-Path $solutionDirectory 'WinNUT-Avalonia\bin\Publish\win-x64'
$installerProject = Join-Path $setupDirectory 'WinNUT-Avalonia.Setup.wixproj'

dotnet publish $appProject --configuration $Configuration -p:PublishProfile=win-x64-self-contained
dotnet build $installerProject --configuration $Configuration "-p:PublishDir=$publishDirectory"
