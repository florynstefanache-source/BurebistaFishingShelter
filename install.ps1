param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\TheLongDark')
$ErrorActionPreference='Stop'; $repo=Split-Path -Parent $MyInvocation.MyCommand.Path; $src=Join-Path $repo 'release\Mods'; $dst=Join-Path $GameDir 'Mods'
if (!(Test-Path $GameDir)) { throw "No se encontró The Long Dark: $GameDir" }; New-Item -ItemType Directory -Force -Path $dst | Out-Null
Copy-Item -Path (Join-Path $src '*') -Destination $dst -Recurse -Force; Write-Host "Instalado en: $dst"
