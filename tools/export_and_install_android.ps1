param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$GodotExe = 'D:\code_workspace\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe',
    [ValidateSet('debug', 'release')]
    [string]$BuildType = 'debug',
    [string]$ExportPreset = 'Android',
    [string]$ApkPath,
    [string]$PackageName = 'com.example.cursedblood',
    [string]$ActivityName = 'com.godot.game.GodotAppLauncher',
    [string]$DeviceSerial,
    [int]$ExportTimeoutSeconds = 1800,
    [int]$ShutdownGraceSeconds = 5,
    [bool]$KillAdbBeforeExport = $true,
    [switch]$SkipDotnetBuild,
    [switch]$SkipInstall,
    [switch]$SkipLaunch
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)

    Write-Host "[CursedBlood] $Message"
}

function Get-CommandSource {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandName,
        [switch]$Optional
    )

    $resolvedCommand = Get-Command -Name $CommandName -ErrorAction SilentlyContinue
    if ($null -ne $resolvedCommand) {
        return $resolvedCommand.Source
    }

    if ($Optional) {
        return $null
    }

    throw "Required command '$CommandName' was not found in PATH."
}

function Get-FileState {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return [pscustomobject]@{
            Exists = $false
            Length = 0L
            LastWriteTime = [datetime]::MinValue
        }
    }

    $item = Get-Item -LiteralPath $Path
    return [pscustomobject]@{
        Exists = $true
        Length = [int64]$item.Length
        LastWriteTime = $item.LastWriteTime
    }
}

function Read-TextTail {
    param(
        [string]$Path,
        [int]$LineCount = 80
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    try {
        $lines = Get-Content -LiteralPath $Path -Tail $LineCount -ErrorAction Stop
        return [string]::Join([Environment]::NewLine, $lines)
    }
    catch {
        return ''
    }
}

function Remove-AnsiEscapeSequences {
    param([string]$Text)

    if ([string]::IsNullOrEmpty($Text)) {
        return ''
    }

    $ansiPattern = [string][char]27 + '\[[0-9;?]*[ -/]*[@-~]'
    return [regex]::Replace($Text, $ansiPattern, '')
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [string]$WorkingDirectory
    )

    if ($WorkingDirectory) {
        Push-Location -LiteralPath $WorkingDirectory
    }

    try {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
        }
    }
    finally {
        if ($WorkingDirectory) {
            Pop-Location
        }
    }
}

function Get-AdbArgumentPrefix {
    param([string]$Serial)

    if ([string]::IsNullOrWhiteSpace($Serial)) {
        return @()
    }

    return @('-s', $Serial)
}

if ([string]::IsNullOrWhiteSpace($ApkPath)) {
    $ApkPath = Join-Path -Path $ProjectRoot -ChildPath 'CursedBlood.apk'
}

$projectFile = Join-Path -Path $ProjectRoot -ChildPath 'CursedBlood.csproj'
$tempRoot = 'D:\temp\CursedBlood'
$stdoutLogPath = Join-Path -Path $tempRoot -ChildPath 'godot_android_export_stdout.log'
$stderrLogPath = Join-Path -Path $tempRoot -ChildPath 'godot_android_export_stderr.log'

if (-not (Test-Path -LiteralPath $ProjectRoot)) {
    throw "Project root does not exist: $ProjectRoot"
}

if (-not (Test-Path -LiteralPath $GodotExe)) {
    throw "Godot executable was not found: $GodotExe"
}

if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "Project file was not found: $projectFile"
}

New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
if (Test-Path -LiteralPath $stdoutLogPath) {
    Remove-Item -LiteralPath $stdoutLogPath -Force
}
if (Test-Path -LiteralPath $stderrLogPath) {
    Remove-Item -LiteralPath $stderrLogPath -Force
}

$dotnetCommand = Get-CommandSource -CommandName 'dotnet'
$adbCommand = $null
if (-not $SkipInstall -or $KillAdbBeforeExport) {
    $adbCommand = Get-CommandSource -CommandName 'adb' -Optional
}
$apksignerCommand = Get-CommandSource -CommandName 'apksigner' -Optional

if (-not $SkipDotnetBuild) {
    Write-Step 'Running dotnet build.'
    Invoke-NativeCommand -FilePath $dotnetCommand -Arguments @('build', $projectFile) -WorkingDirectory $ProjectRoot
}

if ($KillAdbBeforeExport -and $null -ne $adbCommand) {
    Write-Step 'Stopping adb before export.'
    try {
        & $adbCommand 'kill-server' | Out-Null
    }
    catch {
        Write-Warning "adb kill-server reported an error: $($_.Exception.Message)"
    }

    Get-Process -Name 'adb' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

$initialApkState = Get-FileState -Path $ApkPath
$exportSwitch = if ($BuildType -eq 'release') { '--export-release' } else { '--export-debug' }
$exportArguments = @(
    '--headless',
    '--path', $ProjectRoot,
    $exportSwitch,
    $ExportPreset,
    $ApkPath
)

Write-Step 'Starting Godot Android export.'
$exportProcess = Start-Process -FilePath $GodotExe -ArgumentList $exportArguments -WorkingDirectory $ProjectRoot -PassThru -NoNewWindow -RedirectStandardOutput $stdoutLogPath -RedirectStandardError $stderrLogPath
$timeoutAt = (Get-Date).AddSeconds($ExportTimeoutSeconds)
$doneMarkerSeen = $false
$exportSucceeded = $false

while ($true) {
    $exportProcess.Refresh()
    $stdoutTail = Read-TextTail -Path $stdoutLogPath
    $stderrTail = Read-TextTail -Path $stderrLogPath
    $normalizedStdoutTail = Remove-AnsiEscapeSequences -Text $stdoutTail

    if (-not $doneMarkerSeen -and $normalizedStdoutTail.Contains('[ DONE ] export')) {
        $doneMarkerSeen = $true
        Write-Step 'Detected export completion marker in Godot output.'
    }

    $currentApkState = Get-FileState -Path $ApkPath
    if ($doneMarkerSeen -and $currentApkState.Exists -and $currentApkState.Length -gt 0) {
        $exportSucceeded = $true
        break
    }

    if ($exportProcess.HasExited) {
        break
    }

    if ((Get-Date) -ge $timeoutAt) {
        break
    }

    Start-Sleep -Seconds 1
}

$stdoutContent = if (Test-Path -LiteralPath $stdoutLogPath) { Get-Content -LiteralPath $stdoutLogPath -Raw } else { '' }
$stderrContent = if (Test-Path -LiteralPath $stderrLogPath) { Get-Content -LiteralPath $stderrLogPath -Raw } else { '' }
$normalizedStdoutContent = Remove-AnsiEscapeSequences -Text $stdoutContent
$finalApkState = Get-FileState -Path $ApkPath

if (-not $doneMarkerSeen -and $normalizedStdoutContent.Contains('[ DONE ] export')) {
    $doneMarkerSeen = $true
}

if (-not $exportSucceeded -and $doneMarkerSeen -and $finalApkState.Exists -and $finalApkState.Length -gt 0) {
    $exportSucceeded = $true
}

if ($exportSucceeded -and -not $exportProcess.HasExited) {
    Write-Step "Export finished. Waiting $ShutdownGraceSeconds second(s) for a clean Godot shutdown."
    $shutdownDeadline = (Get-Date).AddSeconds($ShutdownGraceSeconds)
    while (-not $exportProcess.HasExited -and (Get-Date) -lt $shutdownDeadline) {
        Start-Sleep -Milliseconds 500
        $exportProcess.Refresh()
    }

    if (-not $exportProcess.HasExited) {
        Write-Warning 'Detected the known Godot Android export shutdown hang after a successful export. Terminating the stuck Godot process.'
        Stop-Process -Id $exportProcess.Id -Force
        $exportProcess.WaitForExit()
    }
}

if (-not $exportSucceeded) {
    if (-not $exportProcess.HasExited) {
        Stop-Process -Id $exportProcess.Id -Force
        $exportProcess.WaitForExit()
    }

    Write-Host '----- Godot stdout tail -----'
    Write-Host (Read-TextTail -Path $stdoutLogPath -LineCount 120)
    Write-Host '----- Godot stderr tail -----'
    Write-Host (Read-TextTail -Path $stderrLogPath -LineCount 120)
    throw 'Android export did not reach a successful completion state.'
}

if ($stderrContent -match 'shutdown_adb_on_exit') {
    Write-Warning 'Godot printed the known shutdown_adb_on_exit teardown error. The wrapper treated export as successful because the export-complete marker and APK output were both confirmed.'
}

if ($apksignerCommand) {
    Write-Step 'Verifying APK signature with apksigner.'
    Invoke-NativeCommand -FilePath $apksignerCommand -Arguments @('verify', '-v', $ApkPath) -WorkingDirectory $ProjectRoot
}
else {
    Write-Warning 'apksigner was not found in PATH. Skipping signature verification.'
}

$selectedDeviceSerial = $DeviceSerial
if (-not $SkipInstall) {
    if ($null -eq $adbCommand) {
        throw 'adb was not found in PATH. Re-run with -SkipInstall or add adb to PATH.'
    }

    Write-Step 'Querying connected Android devices.'
    $deviceOutput = & $adbCommand 'devices'
    if ($LASTEXITCODE -ne 0) {
        throw 'adb devices failed.'
    }

    $connectedDevices = @(
        $deviceOutput |
            Select-String -Pattern '^([^\s]+)\s+device$' |
            ForEach-Object { $_.Matches[0].Groups[1].Value }
    )

    if (-not $selectedDeviceSerial) {
        if ($connectedDevices.Count -eq 0) {
            throw 'No Android device is connected. Re-run with -SkipInstall or connect a device.'
        }

        if ($connectedDevices.Count -gt 1) {
            throw 'Multiple Android devices are connected. Re-run with -DeviceSerial to select one.'
        }

        $selectedDeviceSerial = $connectedDevices[0]
    }
    elseif ($connectedDevices -notcontains $selectedDeviceSerial) {
        throw "Requested device serial '$selectedDeviceSerial' is not connected."
    }

    $adbPrefix = Get-AdbArgumentPrefix -Serial $selectedDeviceSerial
    Write-Step "Installing APK to device $selectedDeviceSerial."
    Invoke-NativeCommand -FilePath $adbCommand -Arguments ($adbPrefix + @('install', '-r', $ApkPath)) -WorkingDirectory $ProjectRoot

    if (-not $SkipLaunch) {
        Write-Step "Launching $PackageName/$ActivityName on device $selectedDeviceSerial."
        Invoke-NativeCommand -FilePath $adbCommand -Arguments ($adbPrefix + @('shell', 'am', 'start', '-n', "$PackageName/$ActivityName")) -WorkingDirectory $ProjectRoot
    }
}

$apkUpdated = $initialApkState.LastWriteTime -ne $finalApkState.LastWriteTime -or $initialApkState.Length -ne $finalApkState.Length
Write-Step 'Android export workflow completed successfully.'
Write-Step "APK path: $ApkPath"
Write-Step "APK size: $($finalApkState.Length) bytes"
Write-Step "APK updated: $apkUpdated"
Write-Step "APK timestamp: $($finalApkState.LastWriteTime.ToString('yyyy/MM/dd HH:mm:ss'))"
Write-Step "stdout log: $stdoutLogPath"
Write-Step "stderr log: $stderrLogPath"