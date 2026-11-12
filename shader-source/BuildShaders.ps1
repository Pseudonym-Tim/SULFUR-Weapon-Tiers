param([Parameter(Mandatory=$true)][string]$UnityEditor)

$ErrorActionPreference = 'Stop'

if(!(Test-Path -LiteralPath $UnityEditor))
{
    throw 'Pass -UnityEditor with the path to Unity.exe!'
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$buildRoot = Join-Path $PSScriptRoot '.shader-build'
New-Item -ItemType Directory -Force -Path (Join-Path $buildRoot 'Assets/Editor'),(Join-Path $buildRoot 'Packages'),(Join-Path $buildRoot 'ProjectSettings') | Out-Null
Copy-Item -Path (Join-Path $PSScriptRoot 'Assets/*.shader') -Destination (Join-Path $buildRoot 'Assets') -Force
Copy-Item -Path (Join-Path $PSScriptRoot 'Assets/*.cginc') -Destination (Join-Path $buildRoot 'Assets') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Assets/Editor/BuildGlintShaders.cs') -Destination (Join-Path $buildRoot 'Assets/Editor/BuildGlintShaders.cs') -Force
'{"dependencies":{"com.unity.modules.assetbundle":"1.0.0"}}' | Set-Content -LiteralPath (Join-Path $buildRoot 'Packages/manifest.json')
'm_EditorVersion: 6000.3.22f1' | Set-Content -LiteralPath (Join-Path $buildRoot 'ProjectSettings/ProjectVersion.txt')
$logPath = Join-Path $buildRoot 'build.log'
$arguments = @('-batchmode','-nographics','-quit','-projectPath',('"{0}"' -f $buildRoot),'-executeMethod','BuildGlintShaders.Build','-logFile',('"{0}"' -f $logPath))
$buildProcess = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait

if($buildProcess.ExitCode -ne 0)
{
    throw "Unity shader build failed! See $logPath"
}

$bundle = Join-Path $buildRoot 'BundleOutput/weapon-tier-glint.bundle'

if(!(Test-Path -LiteralPath $bundle))
{
    throw "No bundle was produced! See $logPath"
}

Copy-Item -LiteralPath $bundle -Destination (Join-Path $projectRoot 'Weapon Tiers/Assets/weapon-tier-glint.bundle') -Force
Write-Host 'Shader bundle updated! Rebuild SulfurWeaponTiers.csproj to embed!'