# Burebista Fishing Shelter

**Versión:** 1.13 sobre base 0.8  
**Estado:** Estable  
**Probado con:** The Long Dark 2.55 (IL2CPP) y MelonLoader 0.7.2

Refugio de pesca colocable para The Long Dark. Incluye los tres DLL (`BurebistaFishingShelter`, `BurebistaFishingShelterEffects` e `IgluAddon`) y los recursos exactos de la versión instalada y confirmada como estable.

## Instalación

1. Instala MelonLoader y ejecuta el juego una vez.
2. Copia el contenido de `release` en la carpeta principal de The Long Dark, conservando `Mods`.
3. También puedes ejecutar `install.bat`. Para otra ubicación: `.\install.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\TheLongDark'`.

## Compilar

Necesitas .NET SDK 6.0 o posterior y una instalación local del juego con los ensamblados IL2CPP generados por MelonLoader.

```powershell
.\build.ps1
# Otra biblioteca de Steam:
.\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\TheLongDark'
```

La compilación se guarda en `artifacts` y no sobrescribe el mod instalado. Las DLL de The Long Dark, Unity, MelonLoader e Il2CppInterop no se distribuyen en este repositorio.

Los binarios de `release` son las copias canónicas probadas que estaban instaladas en MelonLoader. Compilar el código genera binarios nuevos en `artifacts` sin reemplazarlas.

## Estructura

- `src`: código fuente C# independiente.
- `release`: archivos listos para instalar.
- `build.ps1` / `build.bat`: compilación local.
- `install.ps1` / `install.bat`: instalación del release incluido.

## Licencia

Código fuente bajo MIT. Los recursos y binarios de terceros conservan sus licencias. The Long Dark es propiedad de Hinterland Studio. Mod de aficionados no oficial y no afiliado a Hinterland.
