# Launch Unity Hub (Store / winget package)
$hub = Get-AppxPackage -Name UnityTechnologies.UnityHub -ErrorAction SilentlyContinue
if (-not $hub) {
    Write-Host "Unity Hub not found. Run: winget install Unity.UnityHub"
    exit 1
}
$uri = "shell:AppsFolder:$($hub.PackageFamilyName)!App"
Start-Process $uri
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Write-Host "Unity Hub started."
Write-Host "Projects -> Add -> $projectRoot"
