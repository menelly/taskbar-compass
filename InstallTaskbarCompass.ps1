param([string]$SourceDirectory = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$install = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\TaskbarCompass'
$start = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\Taskbar Compass'
$desktop = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Taskbar Compass.lnk'
$files = @('TaskbarCompass.exe','RestoreWindowsTaskbar.exe','TaskbarCompass.ico','README.md')
foreach($name in $files){if(-not(Test-Path -LiteralPath (Join-Path $SourceDirectory $name))){throw "Missing application file: $name"}}
New-Item -ItemType Directory -Force -Path $install,$start | Out-Null
foreach($name in $files){Copy-Item -LiteralPath (Join-Path $SourceDirectory $name) -Destination (Join-Path $install $name) -Force}
$shell = New-Object -ComObject WScript.Shell
$links = @(
 @{Path=$desktop;Target='TaskbarCompass.exe';Description='Taskbar Compass - choose an edge and Continue to apply your taskbar'},
 @{Path=(Join-Path $start 'Taskbar Compass.lnk');Target='TaskbarCompass.exe';Description='Taskbar Compass - setup and taskbar controls'},
 @{Path=(Join-Path $start 'Restore Windows Taskbar.lnk');Target='RestoreWindowsTaskbar.exe';Description='Restore the native Windows taskbar and stop Taskbar Compass'}
)
foreach($item in $links){
 $link=$shell.CreateShortcut($item.Path)
 $link.TargetPath=Join-Path $install $item.Target
 $link.Arguments=''
 $link.WorkingDirectory=$install
 $link.Description=$item.Description
 $link.IconLocation=(Join-Path $install 'TaskbarCompass.ico')+',0'
 $link.WindowStyle=1
 $link.Save()
}
foreach($item in $links){
 $link=$shell.CreateShortcut($item.Path)
 if($link.TargetPath -ne (Join-Path $install $item.Target)){throw "Shortcut target verification failed: $($item.Path)"}
 if(-not(Test-Path -LiteralPath $link.TargetPath)){throw "Shortcut executable missing: $($item.Path)"}
 if($link.Arguments -ne ''){throw 'Unexpected shortcut arguments'}
 [pscustomobject]@{Shortcut=$item.Path;Target=$link.TargetPath;Icon=$link.IconLocation}
}
