# 基本設定
$baseRoot = "C:\Users\yutok\Documents\AmongUsModManager\Release"
$projectFile = ".\AmongUsModManager.csproj"
$appName = "AmongUsModManager"
$appVersion = "1.4.5.3" # ここを書き換えるだけでファイル名に反映されます

# 既存の出力先をクリア
if (Test-Path $baseRoot) { Remove-Item -Recurse -Force $baseRoot }

$rids = @("win-x64", "win-x86", "win-arm64")

foreach ($rid in $rids) {
    # アーキテクチャ判別（ファイル名用とビルド用）
    $archShort = $rid.Replace("win-", "") # "x64", "x86", "arm64"
    $archTarget = if ($rid -eq "win-x86") { "x86" } elseif ($rid -eq "win-arm64") { "arm64" } else { "x64" }

    # --- 1. 単一ファイル版 (SingleFile) のビルド ---
    Write-Host "--- Building Single-File for $rid ---" -ForegroundColor Cyan
    $singleFileDir = Join-Path $baseRoot "$rid\SingleFile"
    
    dotnet publish $projectFile -c Release `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:PlatformTarget=$archTarget `
        -p:WindowsPackageType=None `
        -p:IncludeAllContentForSelfExtract=true `
        -o $singleFileDir

    # ファイル名をリネーム (例: AmongUsModManager.exe -> AmongUsModManager-1.4.5.3-x64.exe)
    $oldExe = Join-Path $singleFileDir "$appName.exe"
    $newExeName = "$appName-$appVersion-$archShort.exe"
    $newExePath = Join-Path $singleFileDir $newExeName
    
    if (Test-Path $oldExe) {
        Rename-Item -Path $oldExe -NewName $newExeName
        Write-Host "Renamed to: $newExeName" -ForegroundColor Green
    }

    # --- 2. 通常版 (Normal) のビルド ---
    Write-Host "--- Building Normal-Files for $rid ---" -ForegroundColor Yellow
    $normalFileDir = Join-Path $baseRoot "$rid\Normal"

    dotnet publish $projectFile -c Release `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=false `
        -p:PlatformTarget=$archTarget `
        -p:WindowsPackageType=None `
        -o $normalFileDir

    if ($lastExitCode -ne 0) {
        Write-Error "Build failed for $rid"
    }
}

Write-Host "`n--- All builds completed! ---" -ForegroundColor Green
Write-Host "Check the folder: $baseRoot"