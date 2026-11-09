param(
  [string]$Compiler = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
  # Installs the new Setup.exe into Program Files after building it.
  [switch]$Install,
  # Publishes the Setup.exe as GitHub release v<version>, which installed copies offer as an update.
  [switch]$Release
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $Compiler)) { throw "Inno Setup compiler not found: $Compiler" }
if ($Release -and (& git -C $PSScriptRoot status --porcelain)) { throw 'Commit your changes before releasing, so the release tag matches the build.' }

Push-Location $PSScriptRoot
try {
  $version = (dotnet msbuild FrameGrabber.csproj -getProperty:Version).Trim()
  if ($LASTEXITCODE -ne 0 -or $version -notmatch '^\d{4}\.\d{2}\.\d{2}(\.\d+)?$') { throw "Unexpected version '$version'." }
  $payload = Join-Path $PSScriptRoot 'obj\installer-payload'
  dotnet restore FrameGrabber.csproj -r win-x64 -v:quiet
  if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE" }
  dotnet clean FrameGrabber.csproj -c Release -r win-x64 -v:quiet
  if ($LASTEXITCODE -ne 0) { throw "dotnet clean failed with exit code $LASTEXITCODE" }
  if (Test-Path -LiteralPath $payload) { Remove-Item -LiteralPath $payload -Recurse -Force }
  # The installer carries FrameGrabber only; FFmpeg comes from the user's machine or winget.
  dotnet publish FrameGrabber.csproj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o $payload
  if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
  & $Compiler "/DAppVersion=$version" "/DPayloadRoot=$payload" (Join-Path $PSScriptRoot 'installer\FrameGrabber.iss')
  if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
}
finally {
  Pop-Location
}
$setup = Join-Path $PSScriptRoot "artifacts\FrameGrabber-$version-Setup.exe"
Write-Output "Built $setup (SHA-256 $((Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash))."

if ($Install) {
  # Setup elevates through UAC and closes a running FrameGrabber itself.
  $process = Start-Process -FilePath $setup -ArgumentList '/SILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/CLOSEAPPLICATIONS' -Verb RunAs -Wait -PassThru
  if ($process.ExitCode -ne 0) { throw "Setup exited with code $($process.ExitCode)." }
  $installed = Join-Path $env:ProgramFiles 'FrameGrabber\FrameGrabber.exe'
  if ((Get-Item -LiteralPath $installed).VersionInfo.ProductVersion -ne $version) { throw "Installed FrameGrabber.exe does not report version $version." }
  Write-Output "Installed FrameGrabber $version to $(Split-Path $installed)."
}

if ($Release) {
  $tag = "v$version"
  $notes = "FrameGrabber $version. Install with the Setup.exe below; installed copies offer this release from the update button in the status bar. FFmpeg is not included; FrameGrabber offers to install it with winget."
  # Tag the commit that was built, not whatever the default branch points at.
  $commit = (& git -C $PSScriptRoot rev-parse HEAD).Trim()
  & gh release create $tag $setup --repo dgoldwas/FrameGrabber --target $commit --title "FrameGrabber $version" --notes $notes
  if ($LASTEXITCODE -ne 0) { throw "Creating GitHub release $tag failed." }
  Write-Output "Released $tag."
}
