# PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#
# Packs a patched VTK 9.7.1 Release build into the SDK zip that vtkControl.csproj downloads when no local VTK is
# found. Build VTK as described in README.md (with patches\*.patch applied), then run
#
#   .\Package-VtkSdk.ps1 -VtkBuildDir <vtk build> -OutputDir <folder>
#
# and upload the zip as the asset of the GitHub release named by VtkNativeSdkUrl in vtkControl\vtkControl.csproj.
# A new SDK (another VTK version or patch) gets a new SdkName, so a stale extracted SDK is never reused.
param(
    [Parameter(Mandatory = $true)][string]$VtkBuildDir,
    [Parameter(Mandatory = $true)][string]$OutputDir,
    [string]$SdkName = "vtk-9.7.1-pmx1-win-x64",
    [string]$CMake = "cmake"
)
$ErrorActionPreference = "Stop"

$stageDir = Join-Path $OutputDir $SdkName
$zipPath = Join-Path $OutputDir "$SdkName.zip"
if (Test-Path $stageDir) { Remove-Item -Recurse -Force $stageDir }
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
New-Item -ItemType Directory -Force $stageDir | Out-Null

& $CMake --install $VtkBuildDir --config Release --prefix $stageDir
if ($LASTEXITCODE -ne 0) { throw "cmake --install failed with exit code $LASTEXITCODE" }
if (-not (Test-Path (Join-Path $stageDir "lib\cmake\vtk-9.7\vtk-config.cmake"))) {
    throw "The installed tree has no lib\cmake\vtk-9.7\vtk-config.cmake"
}

$patchDir = Join-Path $stageDir "patches"
New-Item -ItemType Directory -Force $patchDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot "patches\*.patch") $patchDir
Set-Content -Encoding ascii (Join-Path $stageDir "README.txt") @"
$SdkName
VTK 9.7.1 (BSD-3-Clause, https://vtk.org), shared, x64, Release, built with the patches in the patches folder.
Installed with cmake --install for building PrePoMax vtkNative; see README.md of PrePoMax.
"@

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stageDir, $zipPath,
    [System.IO.Compression.CompressionLevel]::Optimal, $false)
$size = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "Wrote $zipPath ($size MB)"
