$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet publish FrameGrabber.csproj -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=none `
        -o dist/release
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
    Get-Item 'dist/release/FrameGrabber.exe' | Select-Object FullName, Length
}
finally {
    Pop-Location
}
