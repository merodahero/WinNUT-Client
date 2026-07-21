# .NET 8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Upgrade WinNUT-Client_Common\WinNUT-Client_Common.vbproj
4. Upgrade WinNUT-Client\WinNUT-Client.vbproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                        | Current Version      | New Version | Description                                           |
|:------------------------------------|:--------------------:|:-----------:|:------------------------------------------------------|
| AGauge.Classic                      | 2.1.1-prerelease.2   |             | Incompatible with .NET 8.0, no supported version found|

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### WinNUT-Client_Common\WinNUT-Client_Common.vbproj modifications

Project properties changes:
  - Project file needs to be converted to SDK-style format
  - Target framework should be changed from `net48` to `net8.0-windows`

Other changes:
  - Assess project structure and dependencies after SDK-style conversion

#### WinNUT-Client\WinNUT-Client.vbproj modifications

Project properties changes:
  - Project file needs to be converted to SDK-style format
  - Target framework should be changed from `net48` to `net8.0-windows`

NuGet packages changes:
  - AGauge.Classic (2.1.1-prerelease.2) is incompatible with .NET 8.0 - no supported version found. This package will need to be removed or replaced with an alternative solution.

Other changes:
  - Assess project structure and dependencies after SDK-style conversion
  - Find alternative solution for AGauge.Classic functionality