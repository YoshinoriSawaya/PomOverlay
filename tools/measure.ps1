<#
.SYNOPSIS
    PomOverlay の負荷(CPU・GPU・メモリ)と、合成を担う DWM の負荷を計測する。

.DESCRIPTION
    1. アプリ起動前の DWM の負荷を IdleSec 秒計測する
    2. PomOverlay を起動し、WarmupSec 秒待ってから DurationSec 秒計測する
    3. PomOverlay を終了する

    CPU は「1コア = 100%」換算(タスクマネージャーの値 × 論理コア数)。dwm.exe は保護プロセスで
    Process.TotalProcessorTime を読めないため、WMI のパフォーマンスカウンタ(名前がローカライズされない)を使う。
    GPU は 3D エンジンの使用率を1秒ごとにサンプリングした平均。

    リポジトリ直下で実行する:  pwsh tools/measure.ps1
    事前に Release ビルドしておくこと:  dotnet build PomOverlay/PomOverlay.csproj -c Release
#>
param(
    [string]$ExePath = "PomOverlay/bin/Release/net10.0-windows/PomOverlay.exe",
    [int]$IdleSec = 20,
    [int]$WarmupSec = 10,
    [int]$DurationSec = 30
)

$ErrorActionPreference = "Stop"
$exe = Resolve-Path $ExePath
$cores = [Environment]::ProcessorCount

if (Get-Process PomOverlay -ErrorAction SilentlyContinue) {
    throw "PomOverlay がすでに起動しています。終了してから実行してください。"
}

function Get-CpuRaw([int[]]$ids) {
    $filter = ($ids | ForEach-Object { "IDProcess=$_" }) -join " OR "
    $rows = Get-CimInstance Win32_PerfRawData_PerfProc_Process -Filter $filter
    $map = @{}
    foreach ($r in $rows) { $map[[int]$r.IDProcess] = $r }
    return $map
}

function Get-Gpu3D([int[]]$ids) {
    $engines = Get-CimInstance Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine -Filter "Name LIKE '%engtype_3D'"
    $map = @{}
    foreach ($id in $ids) {
        $map[$id] = ($engines | Where-Object { $_.Name -like "pid_${id}_*" } | Measure-Object UtilizationPercentage -Sum).Sum
    }
    return $map
}

function Measure-Load([int[]]$ids, [int]$sec) {
    $gpuSum = @{}; foreach ($id in $ids) { $gpuSum[$id] = 0.0 }
    $a = Get-CpuRaw $ids
    for ($i = 0; $i -lt $sec; $i++) {
        Start-Sleep -Seconds 1
        $g = Get-Gpu3D $ids
        foreach ($id in $ids) { $gpuSum[$id] += [double]$g[$id] }
    }
    $b = Get-CpuRaw $ids
    $result = @{}
    foreach ($id in $ids) {
        $cpu = ($b[$id].PercentProcessorTime - $a[$id].PercentProcessorTime) / ($b[$id].Timestamp_Sys100NS - $a[$id].Timestamp_Sys100NS) * 100
        $result[$id] = [pscustomobject]@{
            CpuPct = [math]::Round($cpu, 1)
            GpuPct = [math]::Round($gpuSum[$id] / $sec, 1)
        }
    }
    return $result
}

$dwmId = (Get-Process dwm | Select-Object -First 1).Id

Write-Host "DWM のアイドル時を $IdleSec 秒計測中..."
$idle = (Measure-Load @($dwmId) $IdleSec)[$dwmId]

Write-Host "PomOverlay を起動して $WarmupSec 秒待機..."
$app = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru
Start-Sleep -Seconds $WarmupSec

try {
    Write-Host "$DurationSec 秒計測中..."
    $r = Measure-Load @($app.Id, $dwmId) $DurationSec
    $app.Refresh()

    Write-Host ""
    Write-Host ("論理コア数: {0}  (CPU は 1コア = 100% 換算。全体比は ÷{0})" -f $cores)
    Write-Host ("                 CPU      GPU(3D)")
    Write-Host ("PomOverlay     : {0,6}%  {1,6}%" -f $r[$app.Id].CpuPct, $r[$app.Id].GpuPct)
    Write-Host ("DWM 起動前     : {0,6}%  {1,6}%" -f $idle.CpuPct, $idle.GpuPct)
    Write-Host ("DWM 起動中     : {0,6}%  {1,6}%" -f $r[$dwmId].CpuPct, $r[$dwmId].GpuPct)
    Write-Host ("PomOverlay メモリ: WorkingSet {0:N0} MB / Private {1:N0} MB" -f ($app.WorkingSet64 / 1MB), ($app.PrivateMemorySize64 / 1MB))
}
finally {
    if (-not $app.HasExited) { Stop-Process -Id $app.Id -Force }
}
