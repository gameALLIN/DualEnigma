# ============================================================
# build-win.ps1 — DualEnigma Windows 客户端命令行打包
# 用法:  .\build-win.ps1
# 说明:  调用 Unity batchmode 执行 BuildTool.BuildWindowsCI
#        (自动: 打 AssetBundle → 打 exe → 输出到 Client/Builds/Windows)
# 注意:  打包前请关闭正在使用该工程的 Unity 编辑器（工程目录锁）
# ============================================================

param(
    # Unity 编辑器路径（默认本机安装位置，可用参数覆盖）
    [string]$UnityExe = "D:\32970\Game\Unity\unity_editor\2022.3.62f3c1\Editor\Unity.exe",

    # 工程目录（脚本所在目录 = Client/）
    [string]$ProjectPath = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $UnityExe)) {
    Write-Host "[错误] 未找到 Unity 编辑器: $UnityExe" -ForegroundColor Red
    Write-Host "       用法: .\build-win.ps1 -UnityExe 'C:\path\to\Unity.exe'"
    exit 1
}

$logFile = Join-Path $ProjectPath "build-win.log"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " DualEnigma Windows 客户端打包" -ForegroundColor Cyan
Write-Host " 工程: $ProjectPath"
Write-Host " Unity: $UnityExe"
Write-Host " 日志:  $logFile"
Write-Host "========================================" -ForegroundColor Cyan

$sw = [System.Diagnostics.Stopwatch]::StartNew()

& $UnityExe -batchmode -quit `
    -projectPath "$ProjectPath" `
    -executeMethod DualEnigma.Editor.BuildTool.BuildWindowsCI `
    -logFile "$logFile"

$exitCode = $LASTEXITCODE
$sw.Stop()

if ($exitCode -eq 0) {
    Write-Host ""
    Write-Host "[成功] 打包完成 ($($sw.Elapsed.ToString('mm\:ss')))" -ForegroundColor Green
    Write-Host " 产物: $ProjectPath\Builds\Windows\DualEnigma.exe" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[失败] Unity 退出码: $exitCode，最近错误:" -ForegroundColor Red
    Select-String -Path $logFile -Pattern "error CS|BuildTool.*失败|Aborting batchmode" |
        Select-Object -Last 10 | ForEach-Object { Write-Host "  $($_.Line)" -ForegroundColor Yellow }
    Write-Host " 完整日志: $logFile" -ForegroundColor Yellow
}

exit $exitCode
