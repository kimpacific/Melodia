# Builds Melodia and packages it into an installer.
# Run from this folder:   .\build.ps1            (or  .\build.ps1 -Version 1.2.0)
# Needs: .NET 10 SDK, Inno Setup 6.6+ (iscc.exe)
param(
    [string]$Version = "1.0.0",
    # Build even if the legal text / publisher still contain [PLACEHOLDER] values (testing only)
    [switch]$AllowPlaceholders,
    # Path to the folder that contains Melodia.csproj
    [string]$Project = "..\Melodia"
)
$ErrorActionPreference = "Stop"

# Don't ship an installer whose terms still say [YOUR NAME]
$placeholders = @()
if (Select-String -Path .\docs\LICENSE_AND_PRIVACY.txt -Pattern '\[(YOUR NAME|COUNTRY|YOUR EMAIL|PLACEHOLDER)' -Quiet) { $placeholders += "docs\LICENSE_AND_PRIVACY.txt still has [PLACEHOLDER] values" }
if (Select-String -Path .\Melodia.iss -Pattern 'MyAppPublisher "YOUR NAME"' -Quiet) { $placeholders += "Melodia.iss: set MyAppPublisher to your name" }
if ($placeholders.Count -gt 0) {
    $placeholders | ForEach-Object { Write-Host "   ! $_" -ForegroundColor Yellow }
    if (-not $AllowPlaceholders) { throw "Fix the items above (or re-run with -AllowPlaceholders for a test build)." }
}

Write-Host "==> Cleaning old output" -ForegroundColor DarkYellow
Remove-Item -Recurse -Force .\publish, .\output -ErrorAction SilentlyContinue

Write-Host "==> Publishing Melodia $Version (self-contained, win-x64)" -ForegroundColor DarkYellow
dotnet publish "$Project\Melodia.csproj" -c Release -r win-x64 --self-contained true `
    -p:PublishReadyToRun=true -p:Version=$Version -o .\publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

if (-not (Test-Path .\publish\Melodia.exe)) { throw "Melodia.exe missing from .\publish" }

# Safety check: the audio engine must be in the output or the app will start silent.
if (-not (Get-ChildItem .\publish -Recurse -Filter libvlc.dll -ErrorAction SilentlyContinue)) {
    throw "libvlc not found in .\publish - the installer would ship an app without audio."
}

Write-Host "==> Compiling installer" -ForegroundColor DarkYellow
$iscc = (Get-Command iscc -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    $roots = @("$env:ProgramFiles", "${env:ProgramFiles(x86)}", "$env:LOCALAPPDATA\Programs")
    $iscc = $roots |
        ForEach-Object { Get-ChildItem -Path $_ -Directory -Filter "Inno Setup*" -ErrorAction SilentlyContinue } |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName "ISCC.exe" } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
}
if (-not $iscc) { throw "Inno Setup (ISCC.exe) not found. Install Inno Setup 6.6 or newer." }
Write-Host "Using Inno Setup: $iscc" -ForegroundColor DarkGray
& $iscc "/DMyAppVersion=$Version" .\Melodia.iss
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compile failed" }

Write-Host ""
Write-Host "Tip: to measure launch time on your PC, run the installed app with MELODIA_STARTUP_LOG=1 and read %AppData%\Melodia\startup.log" -ForegroundColor DarkGray
Write-Host "==> Done: output\Melodia-Setup-$Version.exe" -ForegroundColor Green
Get-FileHash ".\output\Melodia-Setup-$Version.exe" -Algorithm SHA256
