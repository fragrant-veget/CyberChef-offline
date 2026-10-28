# CyberChef Offline - build script
#   Requires only Windows built-in .NET Framework compiler (csc.exe). No SDK, no NuGet.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\build.ps1
#   powershell -ExecutionPolicy Bypass -File .\build.ps1 -Target launcher
#   powershell -ExecutionPolicy Bypass -File .\build.ps1 -Target package -CyberChefDir .\out\CyberChef -Version 11.5.0
#
# Targets:
#   launcher : compile src\CyberChef_launcher.cs  -> bin\CyberChef_launcher.exe
#   package  : launcher + copy CyberChefDir -> release\<name>\ + strip redundant .gz/.br + zip
#   all      : launcher + package (package requires -CyberChefDir)
#
param(
    [ValidateSet('launcher', 'package', 'all')]
    [string]$Target = 'launcher',

    # Folder containing the extracted official CyberChef standalone build
    # (must contain CyberChef_*.html, assets\, modules\, images\).
    [string]$CyberChefDir = '',

    # CyberChef version used in the output folder / zip name.
    [string]$Version = '11.5.0',

    # Skip the launcher compile if you already have bin\CyberChef_launcher.exe.
    [switch]$ReuseLauncher
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$src = Join-Path $root 'src\CyberChef_launcher.cs'
$icon = Join-Path $root 'src\CyberChef_launcher.ico'
$bin = Join-Path $root 'bin'
$exe = Join-Path $bin 'CyberChef_launcher.exe'
$releaseRoot = Join-Path $root 'release'
$pkgName = "CyberChef_v${Version}_offline_win64"
$stage = Join-Path $releaseRoot $pkgName
$zipPath = Join-Path $releaseRoot "$pkgName.zip"

function Write-Step([string]$text) {
    Write-Host ''
    Write-Host "==> $text" -ForegroundColor Cyan
}

function Build-Launcher {
    Write-Step 'Compiling launcher'
    if (-not (Test-Path $csc)) { throw "csc.exe not found: $csc" }
    if (-not (Test-Path $src)) { throw "source not found: $src" }
    New-Item -ItemType Directory -Force -Path $bin | Out-Null
    $iconArg = @()
    if (Test-Path $icon) {
        $iconArg = @("/win32icon:$icon")
        Write-Host ("    icon: {0}" -f $icon)
    }
    & $csc /nologo /target:exe /platform:anycpu /optimize+ /codepage:65001 /warn:4 "/out:$exe" $iconArg $src
    if ($LASTEXITCODE -ne 0) { throw "csc failed with exit code $LASTEXITCODE" }
    $size = (Get-Item $exe).Length
    $hash = (Get-FileHash $exe -Algorithm SHA256).Hash
    Write-Host ("    {0}  ({1:N0} bytes)" -f $exe, $size)
    Write-Host "    SHA256: $hash"
}

# Remove web-server-only pre-compressed copies (*.gz / *.br) but keep tesseract
# language data, which is only shipped compressed.
function Remove-RedundantCompressedCopies([string]$dir) {
    Write-Step 'Stripping redundant *.gz / *.br'
    $files = Get-ChildItem $dir -Recurse -File |
        Where-Object { $_.Name -match '\.(gz|br)$' -and $_.Name -notlike '*.traineddata.gz' }
    $mb = 0
    foreach ($f in $files) {
        $mb += $f.Length
        Remove-Item -LiteralPath $f.FullName -Force
    }
    Write-Host ("    removed {0} files, {1:N1} MB" -f $files.Count, ($mb / 1MB))
}

function New-Package {
    if (-not $CyberChefDir) { throw 'package target requires -CyberChefDir <path to extracted official build>' }
    if (-not (Test-Path $CyberChefDir)) { throw "CyberChefDir not found: $CyberChefDir" }
    if (-not (Get-ChildItem $CyberChefDir -Filter 'CyberChef_*.html' -File)) {
        throw "no CyberChef_*.html found in $CyberChefDir"
    }

    if (-not $ReuseLauncher) { Build-Launcher }
    if (-not (Test-Path $exe)) { throw "launcher missing: $exe (run -Target launcher first)" }

    Write-Step "Staging $pkgName"
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    Copy-Item (Join-Path $CyberChefDir '*') $stage -Recurse -Force

    Remove-RedundantCompressedCopies $stage

    Copy-Item $exe $stage -Force
    Copy-Item (Join-Path $root 'packaging\README.txt') $stage -Force -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $root 'LICENSE-CyberChef.txt') $stage -Force -ErrorAction SilentlyContinue

    # Recompute checksums in the user facing README.txt
    $readme = Join-Path $stage 'README.txt'
    if (Test-Path $readme) {
        $launcherHash = (Get-FileHash $exe -Algorithm SHA256).Hash
        (Get-Content $readme -Raw).Replace('__LAUNCHER_SHA256__', $launcherHash) |
            Set-Content $readme -Encoding UTF8
    }

    Write-Step "Zipping $zipPath"
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    $zipStream = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::Create)
    $archive = New-Object System.IO.Compression.ZipArchive($zipStream, [System.IO.Compression.ZipArchiveMode]::Create)
    $rootLen = $stage.Length + 1
    try {
        foreach ($f in (Get-ChildItem $stage -Recurse -File)) {
            # forward slashes: portable across Windows / Linux / macOS unzip tools
            $entryName = $pkgName + '/' + $f.FullName.Substring($rootLen).Replace('\', '/')
            $entry = $archive.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)
            $es = $entry.Open()
            $fs = [System.IO.File]::OpenRead($f.FullName)
            try { $fs.CopyTo($es) } finally { $fs.Dispose(); $es.Dispose() }
        }
    }
    finally {
        $archive.Dispose()
        $zipStream.Dispose()
    }

    $zipSize = (Get-Item $zipPath).Length
    $zipHash = (Get-FileHash $zipPath -Algorithm SHA256).Hash
    Write-Host ("    {0}  ({1:N2} MB)" -f $zipPath, ($zipSize / 1MB))
    Write-Host "    SHA256: $zipHash"
    Write-Step 'Done'
}

switch ($Target) {
    'launcher' { Build-Launcher }
    'package'  { New-Package }
    'all'      { Build-Launcher; New-Package }
}
