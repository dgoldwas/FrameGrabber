param(
    [string]$FfmpegExe,
    [string]$FfprobeExe
)

$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
if (-not $FfmpegExe) {
    $FfmpegExe = (Get-Command ffmpeg.exe -ErrorAction Stop).Source
}
if (-not $FfprobeExe) {
    $FfprobeExe = (Get-Command ffprobe.exe -ErrorAction Stop).Source
}
foreach ($tool in @($FfmpegExe, $FfprobeExe)) {
    if (-not (Test-Path -LiteralPath $tool -PathType Leaf)) {
        throw "Tool not found: $tool"
    }
}

dotnet clean (Join-Path $project 'FrameGrabber.csproj') -c Release -r win-x64 -v:quiet
if ($LASTEXITCODE -ne 0) { throw "dotnet clean failed with exit code $LASTEXITCODE" }

$staging = Join-Path $project 'obj\portable-tools'
New-Item -ItemType Directory -Path $staging -Force | Out-Null
Copy-Item -LiteralPath $FfmpegExe -Destination (Join-Path $staging 'ffmpeg.exe') -Force
Copy-Item -LiteralPath $FfprobeExe -Destination (Join-Path $staging 'ffprobe.exe') -Force

Push-Location $project
try {
    dotnet publish FrameGrabber.csproj -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=none `
        -p:FfmpegExe=obj/portable-tools/ffmpeg.exe `
        -p:FfprobeExe=obj/portable-tools/ffprobe.exe `
        -o dist/portable
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
    Get-Item 'dist/portable/FrameGrabber.exe' | Select-Object FullName, Length
}
finally {
    Pop-Location
}
