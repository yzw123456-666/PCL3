<#
    PCL3 一键构建脚本
    用法：
        powershell -ExecutionPolicy Bypass -File 构建.ps1            # 内测构建（内测版本号 +1）
        powershell -ExecutionPolicy Bypass -File 构建.ps1 正式版      # 正式构建（正式版本名 +1，改用 Release 配置）

    版本名规则：Plain Craft Launcher 3.<正式版本名>.<内测版本名>
        · 正式版本名：默认 1.0，只在执行「正式版」构建时 +1
        · 内测版本名：每次打包自动 +1
#>

param(
    [string]$Mode = ''
)

$Release = ($Mode -eq '正式版')

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$pcl3 = Join-Path $root 'pcl3'
$outDir = Join-Path $root 'pcl3-成品'
$modBase = Join-Path $pcl3 'Plain Craft Launcher 2\Modules\Base\ModBase.vb'
$proj = Join-Path $pcl3 'Plain Craft Launcher 2\Plain Craft Launcher 2.vbproj'
$roslyn = Join-Path $root '工具\roslyn'
$msbuild = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe'
$ref48 = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.netframework.referenceassemblies.net48\1.0.3\build'
$dotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
$enc = New-Object System.Text.UTF8Encoding($false)

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

# ---------------------------------------------------------------- 1. 版本号
function Get-Version {
    $t = [IO.File]::ReadAllText($modBase, $enc)
    [pscustomobject]@{
        Formal  = [regex]::Match($t, 'VersionFormalName As String = "([^"]+)"').Groups[1].Value
        Internal = [regex]::Match($t, 'VersionInternalName As String = "([^"]+)"').Groups[1].Value
        Text    = $t
    }
}
function Set-Version($formal, $internal) {
    $t = Get-Version
    $t = $t.Text -replace 'VersionFormalName As String = "[^"]+"', ('VersionFormalName As String = "' + $formal + '"')
    $t = $t -replace 'VersionInternalName As String = "[^"]+"', ('VersionInternalName As String = "' + $internal + '"')
    [IO.File]::WriteAllText($modBase, $t, $enc)
}

$v = Get-Version
$origFormal, $origInternal = $v.Formal, $v.Internal
if ($Release) {
    $parts = $v.Formal -split '\.'
    $major = [int]$parts[0] + 1
    $minor = if ($parts.Length -gt 1) { $parts[1] } else { '0' }
    $newFormal = "$major.$minor"
} else {
    $newFormal = $v.Formal
}
$newInternal = [int]$v.Internal + 1
# 构建失败时回滚版本号，避免失败的构建也消耗版本号
function Rollback-Version {
    Set-Version $origFormal $origInternal
    Write-Host '[提示] 已回滚版本号（构建未成功，不消耗版本号）' -ForegroundColor Yellow
}
Set-Version $newFormal $newInternal
$versionName = "Plain Craft Launcher 3.$newFormal.$newInternal"
Write-Step "版本：$versionName（正式版本名 $newFormal，内测版本名 $newInternal）"

# ---------------------------------------------------------------- 2. 环境检查
foreach ($p in @($roslyn, $msbuild, $ref48, $dotnet)) {
    if (-not (Test-Path $p)) { Write-Host "[错误] 缺少构建依赖：$p" -ForegroundColor Red; Rollback-Version; exit 1 }
}

# ---------------------------------------------------------------- 3. 构建 C# 依赖
Write-Step '构建 PCLCS 与 MeloongCore ...'
& $dotnet build (Join-Path $pcl3 'PCLCS\PCLCS.csproj') -c Release -v minimal
if ($LASTEXITCODE -ne 0) { Write-Host '[错误] PCLCS 构建失败' -ForegroundColor Red; Rollback-Version; exit 1 }

# ---------------------------------------------------------------- 4. 构建主程序
$config = if ($Release) { 'Release' } else { 'Snapshot' }
Write-Step "构建 PCL3 主程序（$config）..."
& $msbuild $proj `
    "/p:Configuration=$config" `
    "/p:TargetFrameworkRootPath=$ref48" `
    '/p:BuildProjectReferences=false' `
    "/p:VbcToolPath=$roslyn" `
    '/p:VbcToolExe=vbc.exe' `
    '/p:UseHostCompilerIfAvailable=false' `
    '/v:minimal' '/nologo'
if ($LASTEXITCODE -ne 0) { Write-Host '[错误] 主程序构建失败' -ForegroundColor Red; Rollback-Version; exit 1 }

# ---------------------------------------------------------------- 5. 输出成品
$src = Join-Path $pcl3 "Plain Craft Launcher 2\bin\Plain Craft Launcher 3.exe"
if (-not (Test-Path $src)) { Write-Host "[错误] 未找到构建产物：$src" -ForegroundColor Red; Rollback-Version; exit 1 }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$dst = Join-Path $outDir "$versionName.exe"
Copy-Item -LiteralPath $src -Destination $dst -Force
# 删除旧成品，只保留本次构建（旧文件被占用时仅提示，不中断）
Get-ChildItem -Path $outDir -Filter 'Plain Craft Launcher 3.*.exe' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -ne $dst } |
    ForEach-Object {
        try {
            Remove-Item -LiteralPath $_.FullName -Force -ErrorAction Stop
        } catch {
            Write-Host "[提示] 旧成品被占用，未能删除（关闭正在运行的旧版后，下次构建会自动清理）：$($_.Name)" -ForegroundColor Yellow
        }
    }
Write-Step "成品已输出：$dst（旧成品已清理）" -ForegroundColor Green
Get-Item $dst | Select-Object Name, Length, LastWriteTime | Format-List
