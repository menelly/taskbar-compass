param([string]$Destination = (Join-Path $PSScriptRoot '..'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/win32icon:$PSScriptRoot\AppIcon.ico" "/win32manifest:$PSScriptRoot\app.manifest" "/out:$Destination\TaskbarCompassBar.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "$PSScriptRoot\ReplacementBar.cs" "$PSScriptRoot\PetShop.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath "$Destination\TaskbarCompassBar.exe" -Destination "$Destination\RestoreWindowsTaskbar.exe" -Force
Copy-Item -LiteralPath "$Destination\TaskbarCompassBar.exe" -Destination "$Destination\TaskbarCompass.exe" -Force
Copy-Item -LiteralPath "$PSScriptRoot\AppIcon.ico" -Destination "$Destination\TaskbarCompass.ico" -Force
