param(
    [string]$Source = "$env:TEMP\cf-publish-tc",
    [string]$ZipPath = "$PSScriptRoot\..\publish-tc.zip",
    [string]$ProductionSettings = "$PSScriptRoot\appsettings.production.json",
    [bool]$EnableStdoutLogging = $true
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Resolve paths
$sourceFull = (Resolve-Path $Source).Path
$zipFull = [System.IO.Path]::GetFullPath($ZipPath)

# The production appsettings (live DB password) lives outside git and is injected here,
# so the repository copy of appsettings.json never contains the real credentials.
if (-not (Test-Path $ProductionSettings)) {
    throw "Missing $ProductionSettings - copy the production appsettings.json there first."
}
$productionJson = Get-Content $ProductionSettings -Raw
$productionConfig = $productionJson | ConvertFrom-Json
if (-not $productionConfig.ConnectionStrings.DefaultConnection) {
    throw "appsettings.production.json has no ConnectionStrings:DefaultConnection."
}
if ($productionConfig.ConnectionStrings.DefaultConnection -match 'localhost|\\SQLEXPRESS') {
    throw "appsettings.production.json still points at a local SQL Server."
}

if (Test-Path $zipFull) { Remove-Item $zipFull -Force }

# Never ship user uploads, source maps, or pre-compressed duplicates on shared hosting.
$excludeDirs = @(
    'wwwroot\uploads',
    'runtimes\linux-x64', 'runtimes\linux-arm64', 'runtimes\linux-musl-x64',
    'runtimes\osx-x64', 'runtimes\osx-arm64', 'runtimes\unix',
    'runtimes\win-arm64', 'runtimes\win-x86',
    'logs'
)
$excludeFiles = @('cateringflow.pdb')

$zip = [System.IO.Compression.ZipFile]::Open($zipFull, [System.IO.Compression.ZipArchiveMode]::Create)
$count = 0
$skipped = 0

function Add-TextEntry {
    param($Archive, [string]$Name, [string]$Text)
    $entry = $Archive.CreateEntry($Name, [System.IO.Compression.CompressionLevel]::Optimal)
    $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($Text)
    $stream = $entry.Open()
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Dispose()
}

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

    if ($rel -eq 'appsettings.json') {
        Add-TextEntry $zip 'appsettings.json' $productionJson
        Write-Output "injected production appsettings.json (local SQL credentials never leave this machine)"
        $count++
        continue
    }

    if ($rel -eq 'web.config') {
        $webConfig = Get-Content $file.FullName -Raw
        if ($EnableStdoutLogging) {
            if ($webConfig -notmatch 'stdoutLogEnabled="false"') {
                throw "web.config no longer contains stdoutLogEnabled=`"false`" - check it manually."
            }
            $webConfig = $webConfig -replace 'stdoutLogEnabled="false"', 'stdoutLogEnabled="true"'
            Write-Output "web.config: stdoutLogEnabled=true (HTTP 500.30 diagnostics)"
        }
        Add-TextEntry $zip 'web.config' $webConfig
        $count++
        continue
    }

    $entry = $zip.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)
    $entry.LastWriteTime = $file.LastWriteTime
    $input = [System.IO.File]::OpenRead($file.FullName)
    $output = $entry.Open()
    $input.CopyTo($output)
    $output.Dispose()
    $input.Dispose()
    $count++
}

# The ASP.NET Core Module cannot create this folder itself and startup logging
# silently fails when it is missing, so ship an empty logs directory.
$zip.CreateEntry('logs/') | Out-Null
if ($EnableStdoutLogging) {
    Add-TextEntry $zip 'logs/README.txt' "This folder holds ASP.NET Core stdout logs used to diagnose startup errors.`r`nAfter the site starts, set stdoutLogEnabled=`"false`" in web.config and delete this folder.`r`n"
}

$zip.Dispose()
Write-Output "entries: $count (skipped $skipped)"
Write-Output ("size MB: " + [math]::Round((Get-Item $zipFull).Length / 1MB, 1))
Write-Output "zip: $zipFull"