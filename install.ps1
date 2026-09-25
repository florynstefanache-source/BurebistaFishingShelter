param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\TheLongDark')
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $MyInvocation.MyCommand.Path
$src=Join-Path $repo 'release\Mods'
$dst=Join-Path $GameDir 'Mods'
if (!(Test-Path -LiteralPath $GameDir)) { throw "No se encontro The Long Dark: $GameDir" }
if (Get-Process -Name 'tld' -ErrorAction SilentlyContinue) { throw 'Cierra The Long Dark antes de instalar.' }
$names=@('BurebistaFishingShelter.dll','BurebistaFishingShelterEffects.dll','IgluAddon.dll')
foreach ($name in $names) {
    if (!(Test-Path -LiteralPath (Join-Path $src $name))) { throw "Falta el modulo $name" }
}
$backup=Join-Path $GameDir ('BurebistaBackups\before-1.14-'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Force -Path $dst,$backup | Out-Null
foreach ($name in ($names + @('BurebistaFishingShelter'))) {
    $old=Join-Path $dst $name
    if (Test-Path -LiteralPath $old) { Copy-Item -LiteralPath $old -Destination $backup -Recurse }
}
foreach ($item in Get-ChildItem -LiteralPath $src) {
    Copy-Item -LiteralPath $item.FullName -Destination $dst -Recurse -Force
}
Write-Host "Instalado en: $dst"
Write-Host "Copia anterior: $backup"
