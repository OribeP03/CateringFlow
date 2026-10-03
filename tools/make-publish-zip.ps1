param(
    [string]$Source = "$env:TEMP\cf-publish-tc",
    [string]$ZipPath = "$PSScriptRoot\..\publish-tc.zip"
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Resolve paths
$sourceFull = (Resolve-Path $Source).Path
$zipFull = [System.IO.Path]::GetFullPath($ZipPath)
if (Test-Path $zipFull) { Remove-Item $zipFull -Force }

# Never ship user uploads, source maps, or pre-compressed duplicates on shared hosting.
$excludeDirs = @(
    'wwwroot\uploads',
    'runtimes\linux-x64', 'runtimes\linux-arm64', 'runtimes\linux-musl-x64',
    'runtimes\osx-x64', 'runtimes\osx-arm64', 'runtimes\unix',
    'runtimes\win-arm64', 'runtimes\win-x86'
)
$excludeFiles = @('cateringflow.pdb')

$zip = [System.IO.Compression.ZipFile]::Open($zipFull, [System.IO.Compression.ZipArchiveMode]::Create)
$count = 0
$skipped = 0

foreach ($file in Get-ChildItem -Path $sourceFull -Recurse -File) {
    $rel = $file.FullName.Substring($sourceFull.Length).TrimStart('\')

    $skip = $false
    foreach ($dir in $excludeDirs) {
        if ($rel.StartsWith($dir + '\', [System.StringComparison]::OrdinalIgnoreCase)) { $skip = $true; break }
    }
    if (-not $skip -and ($excludeFiles -contains $rel)) { $skip = $true }
    if (-not $skip -and ($rel.EndsWith('.map') -or $rel.EndsWith('.br') -or $rel.EndsWith('.gz'))) { $skip = $true }
    if ($skip) { $skipped++; continue }

    # ZIP spec requires forward slashes, otherwise some unzip tools create odd filenames.
    $entryName = $rel.Replace('\', '/')
    $entry = $zip.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)
    $entry.LastWriteTime = $file.LastWriteTime
    $input = [System.IO.File]::OpenRead($file.FullName)
    $output = $entry.Open()
    $input.CopyTo($output)
    $output.Dispose()
    $input.Dispose()
    $count++
}

$zip.Dispose()
Write-Output "entries: $count (skipped $skipped)"
Write-Output ("size MB: " + [math]::Round((Get-Item $zipFull).Length / 1MB, 1))
Write-Output "zip: $zipFull"