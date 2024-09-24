$chromium_path = Get-ItemPropertyValue -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vivaldi.exe" -Name "(default)"
$project_dir = Split-Path $script:MyInvocation.MyCommand.Path
$project_name = Split-Path -Leaf $project_dir 
$source_dir = "$project_dir\src"
$output_dir = "$project_dir\bin"

New-Item -Path $output_dir -ItemType Directory -Force > $null

Write-Output "Packing extension"
# Private keys cannot be shared between multiple extensions, because Chromium will think they are all the same extension and they will overwrite each other when you install them.
# You must generate a new key for each extension by using Pack Extension in chrome://extensions in Developer Mode.
Start-Process -FilePath $chromium_path -ArgumentList "--pack-extension=`"$source_dir`" --pack-extension-key=`"$project_dir\PackExtensionPrivateKey.pem`"" -Wait

Move-Item -Path "$project_dir\src.crx" -Destination "$output_dir\RemoteControl.crx" -Force