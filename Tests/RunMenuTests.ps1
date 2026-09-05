$ErrorActionPreference='Stop'
$stage=Join-Path ([IO.Path]::GetTempPath()) ('TaskbarCompassTests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\Source\ReplacementBar.cs'))
$anchor='Shown+=delegate{Initialize();};'
if(-not $source.Contains($anchor)){throw 'Initialization isolation anchor missing'}
$source=$source.Replace($anchor,'')
$testSource=Join-Path $stage 'ClickSource.cs'
[IO.File]::WriteAllText($testSource,$source)
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $stage 'LauncherClickTests.exe'
& $compiler /nologo /target:winexe /platform:x64 /main:CompassBar.LauncherClickTests "/out:$exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll $testSource (Join-Path $PSScriptRoot 'LauncherClickTests.cs') (Join-Path $PSScriptRoot '..\Source\PetShop.cs')
if($LASTEXITCODE -ne 0){throw 'Click test compilation failed'}
$test=Start-Process -FilePath $exe -WindowStyle Hidden -Wait -PassThru
Get-Content (Join-Path $stage 'launcher-click-validation.txt')
if($test.ExitCode -ne 0){throw 'Click regression failed'}
