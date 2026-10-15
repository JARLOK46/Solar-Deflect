param(
    [string]$OutputDirectory = "$PSScriptRoot\..\dist\TriggerBot-win-x64"
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\TriggerBot.App\TriggerBot.App.csproj'
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
$tempOutput = Join-Path ([System.IO.Path]::GetTempPath()) ("TriggerBot-publish-" + [guid]::NewGuid().ToString('N'))

try {
    Write-Host "Publishing TriggerBot (win-x64, self-contained, single-file)..."
    dotnet publish $project -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $tempOutput

    $publishedExe = Join-Path $tempOutput 'TriggerBot.exe'
    if (-not (Test-Path $publishedExe)) {
        throw "Publish did not produce TriggerBot.exe."
    }

    # Keep one stable delivery directory and one file only. Build intermediates
    # stay in the OS temp directory and never accumulate under dist.
    if (Test-Path $resolvedOutput) {
        Remove-Item $resolvedOutput -Recurse -Force
    }
    New-Item -ItemType Directory -Path $resolvedOutput -Force | Out-Null
    Copy-Item $publishedExe (Join-Path $resolvedOutput 'TriggerBot.exe') -Force

    Write-Host "Ready: $resolvedOutput\TriggerBot.exe"
}
finally {
    if (Test-Path $tempOutput) {
        Remove-Item $tempOutput -Recurse -Force -ErrorAction SilentlyContinue
    }
}
