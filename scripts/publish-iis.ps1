param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "",
    [switch]$SkipFrontendBuild,
    [switch]$IncludeDragChainDb,
    [string]$DragChainDbPath = "",
    [switch]$IncludeSensorDb,
    [string]$SensorDbPath = ""
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$clientDir = Join-Path $root.Path "DragChain.Client"
$apiDir = Join-Path $root.Path "DragChain.API"
$wwwrootDir = Join-Path $apiDir "wwwroot"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $root.Path "publish\iis-selection-software"
}

$publishDir = [System.IO.Path]::GetFullPath($OutputPath)

Write-Host "发布配置: $Configuration"
Write-Host "发布目录: $publishDir"
Write-Host "打包线槽/拖链数据库: $IncludeDragChainDb"
Write-Host "打包感应器数据库: $IncludeSensorDb"

if (-not $SkipFrontendBuild) {
    Write-Host "清理旧前端输出..."
    if (Test-Path -LiteralPath $wwwrootDir) {
        Remove-Item -LiteralPath $wwwrootDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $wwwrootDir | Out-Null

    Write-Host "构建前端..."
    Push-Location $clientDir
    try {
        npm run build
    }
    finally {
        Pop-Location
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $wwwrootDir "index.html"))) {
    throw "前端构建输出不存在: $wwwrootDir"
}

Write-Host "前端文件已输出到后端 wwwroot: $wwwrootDir"

Write-Host "清理旧发布目录..."
if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

Write-Host "发布后端和静态前端..."
dotnet publish (Join-Path $apiDir "DragChain.API.csproj") -c $Configuration -o $publishDir

Write-Host "移除发布包中的运行期数据库和日志..."
Remove-Item -LiteralPath `
    (Join-Path $publishDir "dragchain.db"), `
    (Join-Path $publishDir "dragchain.db-shm"), `
    (Join-Path $publishDir "dragchain.db-wal"), `
    (Join-Path $publishDir "sensor.db"), `
    (Join-Path $publishDir "sensor.db-shm"), `
    (Join-Path $publishDir "sensor.db-wal"), `
    (Join-Path $publishDir "publish-run.log"), `
    (Join-Path $publishDir "publish-run.err.log") `
    -Force -ErrorAction SilentlyContinue

function Copy-RuntimeDatabase {
    param(
        [string]$DatabaseName,
        [string]$ConfiguredPath
    )

    $sourcePath = $ConfiguredPath
    if ([string]::IsNullOrWhiteSpace($sourcePath)) {
        $sourcePath = Join-Path $apiDir $DatabaseName
    }

    $sourcePath = [System.IO.Path]::GetFullPath($sourcePath)
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "数据库文件不存在: $sourcePath"
    }

    $targetPath = Join-Path $publishDir $DatabaseName
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
    Write-Host "已复制数据库: $sourcePath -> $targetPath"
}

if ($IncludeDragChainDb) {
    Copy-RuntimeDatabase -DatabaseName "dragchain.db" -ConfiguredPath $DragChainDbPath
}

if ($IncludeSensorDb) {
    Copy-RuntimeDatabase -DatabaseName "sensor.db" -ConfiguredPath $SensorDbPath
}

Write-Host ""
Write-Host "IIS 发布文件已生成:"
Write-Host $publishDir
Write-Host ""
Write-Host "部署提示:"
Write-Host "1. IIS 站点物理路径指向该目录。"
Write-Host "2. 应用程序池设为“无托管代码”。"
Write-Host "3. 对外端口由 IIS 站点绑定决定。"
Write-Host "4. 默认不打包运行期数据库，避免覆盖生产数据。"
Write-Host "5. 如首次部署需要带库，可加 -IncludeDragChainDb/-IncludeSensorDb，并用 *DbPath 指定来源。"
Write-Host "6. 生产启动前必须安全设置 DRAGCHAIN_AUTH_SIGNING_KEY，值应为至少 32 字节的高熵随机秘密。"
Write-Host "7. 新建空 sensor.db 首次启动必须设置至少 12 个字符且包含字母、数字和符号的 DRAGCHAIN_BOOTSTRAP_ADMIN_PASSWORD；可选设置 DRAGCHAIN_BOOTSTRAP_ADMIN_EMPLOYEE_NO。"
Write-Host "8. 旧库若含历史固定账号且没有已启用的非旧版超级管理员，也必须设置该强密码完成一次性轮换，否则应用会拒绝启动。"
Write-Host "9. 请通过 IIS 应用程序池环境变量等安全方式注入上述值；本脚本不会生成、保存或输出任何秘密。"
