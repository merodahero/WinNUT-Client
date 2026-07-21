# .NET 8.0 Upgrade Report

## Summary

The upgrade to .NET 8.0 has been **partially completed** for the WinNUT solution. Both projects were successfully converted to SDK-style projects and the target framework was updated. However, the **WinNUT-Client** project requires manual intervention due to a critical dependency issue.

## Project target framework modifications

| Project name                                       | Old Target Framework    | New Target Framework  | Status      | Commits                                                    |
|:---------------------------------------------------|:-----------------------:|:---------------------:|:-----------:|:-----------------------------------------------------------|
| WinNUT-Client_Common\WinNUT-Client_Common.vbproj   | net48                   | net8.0-windows        | ✅ Complete | 314ece05, b0a8c068, e82b4576, bce4ab4f, c67cf433, 0098a28a |
| WinNUT-Client\WinNUT-Client.vbproj                 | net48                   | net8.0-windows        | ⚠️ Blocked  | 69f60abf, d073b538, 421fd1b6, 3a3a0245                     |

## NuGet Packages

| Package Name                           | Old Version          | New Version  | Status      | Commit ID  |
|:---------------------------------------|:--------------------:|:------------:|:-----------:|:-----------|
| AGauge.Classic                         | 2.1.1-prerelease.2   | (Removed)    | ⚠️ Issue    | d073b538   |
| System.Configuration.ConfigurationManager | -                 | 10.0.10      | ✅ Added    | d073b538   |

## Project feature upgrades

### WinNUT-Client_Common\WinNUT-Client_Common.vbproj

**Successfully completed:**
- ✅ Project converted to SDK-style format
- ✅ Target framework upgraded from .NET Framework 4.8 to .NET 8.0-windows
- ✅ Fixed assembly attribute duplication by disabling auto-generation (`GenerateAssemblyInfo=false`)
- ✅ Added System.Windows.Forms import to resolve namespace issues
- ✅ Corrected Application.StartupPath and Application.LocalUserAppDataPath usage
- ✅ Removed legacy assembly references (System.Net.Http, System.Security, System.Windows.Forms)
- ✅ Migrated to PackageReference for Octokit

### WinNUT-Client\WinNUT-Client.vbproj

**Completed:**
- ✅ Project converted to SDK-style format
- ✅ Target framework upgraded from .NET Framework 4.8 to .NET 8.0-windows
- ✅ Fixed assembly attribute duplication by disabling auto-generation (`GenerateAssemblyInfo=false`)
- ✅ Removed legacy assembly references (System, System.Configuration, System.Deployment, System.Drawing, System.Windows.Forms)
- ✅ Added System.Configuration.ConfigurationManager package (v10.0.10)

**⚠️ Requires Manual Intervention:**
- ❌ **AGauge.Classic package incompatibility**: The AGauge.Classic (v2.1.1-prerelease.2) package has been removed as it is not compatible with .NET 8.0 and no supported version exists.
  
  **Impact:** The UPSVarGauge control and all gauge functionality in the main WinNUT form is currently broken with multiple compilation errors.
  
  **Files affected:**
  - `Controls\UPSVarGauge.vb`
  - `Controls\UPSVarGauge.Designer.vb`
  - `WinNUT.Designer.vb`
  - `WinNUT.vb`
  
  **Resolution options:**
  1. **Find an alternative gauge control** - Search for a .NET 8.0-compatible gauge/dial control library (e.g., LiveCharts, ScottPlot, or a custom WinForms control)
  2. **Create a custom gauge implementation** - Implement a basic custom gauge control using GDI+ drawing
  3. **Replace with simpler UI elements** - Replace gauge visualizations with progress bars or numeric labels temporarily

- ❌ **ToastContentBuilder.Show() method issue**: One error related to the Microsoft.Toolkit.Uwp.Notifications package where the `Show()` method is not found on `ToastContentBuilder` (in `ToastPopup.vb`).

## All commits

| Commit ID | Description                                                                                          |
|:----------|:-----------------------------------------------------------------------------------------------------|
| aa6b8151  | Commit upgrade plan                                                                                  |
| 0098a28a  | Added System.Windows.Forms import to resolve namespace issues                                       |
| c67cf433  | Corrected Application usage in WinNUT_Globals.vb                                                     |
| bce4ab4f  | Remove System.Security reference from WinNUT-Client_Common.vbproj                                    |
| 314ece05  | Migrate WinNUT-Client_Common.vbproj to SDK-style project                                             |
| b0a8c068  | Move assembly metadata to project file                                                               |
| e82b4576  | Store final changes for step 'Upgrade WinNUT-Client_Common\WinNUT-Client_Common.vbproj'            |
| 3a3a0245  | Fixed assembly attribute duplication issue for WinNUT-Client                                         |
| 69f60abf  | Refactor WinNUT-client.vbproj to SDK-style and .NET 8                                               |
| d073b538  | Update WinNUT-client.vbproj package references                                                      |
| 421fd1b6  | Move assembly metadata from AssemblyInfo.vb to project file                                          |

## Next steps

### Critical - AGauge.Classic Replacement

You need to decide how to handle the gauge functionality. Here are detailed recommendations:

**Option 1: Use an alternative modern gauge library**
- Research .NET 8.0-compatible gauge controls
- Popular options: LiveCharts2, ScottPlot, OxyPlot, or commercial controls
- Update `UPSVarGauge.vb` to use the new library

**Option 2: Create a custom simple gauge**
- Implement basic circular gauge using `Graphics` API in WinForms
- Override `OnPaint` to draw gauge arc, needle, and scale
- Should be manageable given your current `UPSVarGauge` structure

**Option 3: Temporarily simplify UI**
- Replace gauges with `ProgressBar` or `Label` controls
- Show UPS metrics as text or simple bars
- Can reintroduce gauges later

### Secondary - ToastContentBuilder Issue

The `ToastContentBuilder.Show()` method issue in `ToastPopup.vb` needs investigation:
- Check if Microsoft.Toolkit.Uwp.Notifications v7.1.3 is fully compatible with .NET 8.0
- May need to update to a newer version or use a different API pattern