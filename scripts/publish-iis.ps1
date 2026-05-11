param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "",
    [switch]$SkipFrontendBuild
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$clientDir = Join-Path $root.Path "DragChain.Client"
$apiDir = Join-Path $root.Path "DragChain.API"
$distDir = Join-Path $clientDir "dist"
$wwwrootDir = Join-Path $apiDir "wwwroot"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $root.Path "publish\iis-dragchain"
}

$publishDir = [System.IO.Path]::GetFullPath($OutputPath)

Write-Host "发布配置: $Configuration"
Write-Host "发布目录: $publishDir"

if (-not $SkipFrontendBuild) {
    Write-Host "构建前端..."
    Push-Location $clientDir
    try {
        npm run build
    }
    finally {
        Pop-Location
    }
}

if (-not (Test-Path -LiteralPath $distDir)) {
    throw "前端构建目录不存在: $distDir"
}

Write-Host "复制前端文件到后端 wwwroot..."
if (Test-Path -LiteralPath $wwwrootDir) {
    Remove-Item -LiteralPath $wwwrootDir -Recurse -Force
}
New-Item -ItemType Directory -Path $wwwrootDir | Out-Null
Get-ChildItem -LiteralPath $distDir -Force | Copy-Item -Destination $wwwrootDir -Recurse -Force

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
    (Join-Path $publishDir "publish-run.log"), `
    (Join-Path $publishDir "publish-run.err.log") `
    -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "IIS 发布文件已生成:"
Write-Host $publishDir
Write-Host ""
Write-Host "部署提示:"
Write-Host "1. IIS 站点物理路径指向该目录。"
Write-Host "2. 应用程序池设为“无托管代码”。"
Write-Host "3. 对外端口由 IIS 站点绑定决定。"
