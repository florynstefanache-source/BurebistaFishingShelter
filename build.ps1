param([string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\TheLongDark',[string]$Configuration='Release')
$ErrorActionPreference='Stop'; $repo=Split-Path -Parent $MyInvocation.MyCommand.Path; $out=Join-Path $repo 'artifacts'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$projects = @(
  (Join-Path $repo 'src\BurebistaFishingShelter\BurebistaFishingShelter.csproj'),
  (Join-Path $repo 'src\BurebistaFishingShelterEffects\BurebistaFishingShelterEffects.csproj'),
  (Join-Path $repo 'src\IgluAddon\IgluAddon.csproj')
)
foreach ($project in $projects) {
  dotnet build $project -c $Configuration -p:TLDPath=$GameDir -p:OutputPath=$out
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
Write-Host "Compilación terminada: $out"
