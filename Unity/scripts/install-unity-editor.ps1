# Silent install Unity 2022.3 LTS editor (official installer).
# Approve UAC when prompted.

$ErrorActionPreference = "Stop"
$changeset = "96770f904ca7"
$version = "2022.3.62f3"
$url = "https://download.unity3d.com/download_unity/$changeset/Windows64EditorInstaller/UnitySetup64-$version.exe"
$installer = Join-Path $env:TEMP "UnitySetup64-$version.exe"

if (-not (Test-Path $installer)) {
    Write-Host "Downloading Unity $version ..."
    $ProgressPreference = "SilentlyContinue"
    Invoke-WebRequest -Uri $url -OutFile $installer -UseBasicParsing
}

Write-Host "Installing (may take 10-25 minutes) ..."
$p = Start-Process -FilePath $installer -ArgumentList "/S" -Wait -PassThru -Verb RunAs
Write-Host "Installer exit code: $($p.ExitCode)"

$unityExe = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
if (Test-Path -LiteralPath $unityExe) {
    Write-Host "OK: $unityExe"
} else {
    Write-Warning "Unity.exe not found. Check Unity Hub -> Installs."
}
