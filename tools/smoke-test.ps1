param(
    [string]$Executable = "$PSScriptRoot\..\dist\TriggerBot-win-x64\TriggerBot.exe",
    [int]$WaitSeconds = 3
)

$ErrorActionPreference = 'Stop'
$resolved = (Resolve-Path $Executable).Path
Write-Host "Executable: $resolved"

$process = Start-Process -FilePath $resolved -PassThru
try {
    Start-Sleep -Seconds $WaitSeconds
    $process.Refresh()

    if ($process.HasExited) {
        throw "TriggerBot exited during smoke test with code $($process.ExitCode)."
    }

    Write-Host "Process: running (PID $($process.Id))"
    Write-Host "Main window handle: $($process.MainWindowHandle)"
    Write-Host "Main window title: '$($process.MainWindowTitle)'"

    $log = Join-Path $env:LOCALAPPDATA 'TriggerBot\logs\startup.log'
    if (-not (Test-Path $log)) {
        throw "Startup log was not created at $log"
    }

    Write-Host "Startup log: $log"
    Write-Host "Last startup events:"
    Get-Content $log -Tail 8
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
}
