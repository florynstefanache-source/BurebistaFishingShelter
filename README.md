# Burebista Fishing Shelter — v1.15.1

Refugio de pesca para The Long Dark, basado en el mod v1.13 de Burebista. Incluye saco, mesa, bastidor y nuevo almacén opcional de 500 kg.

## Descargas

- [Actualización v1.15.1](https://github.com/florynstefanache-source/BurebistaFishingShelter/raw/refs/heads/main/downloads/BurebistaFishingShelter-v1.15.1-Actualizacion.zip): tres DLL para una instalación existente.
- [Proyecto completo](https://github.com/florynstefanache-source/BurebistaFishingShelter/archive/refs/heads/main.zip): código y carpeta `release/Mods` con todos los modelos y texturas.

## Requisitos

- The Long Dark para Windows, IL2CPP. Compilación comprobada con las bibliotecas locales de la versión 2.55 y MelonLoader 0.7.2.
- Las tres DLL de esta versión y los modelos/texturas de `Mods/BurebistaFishingShelter/iceland`.
- **ModData.dll para el almacén**. Instálalo si todavía no lo tienes. No se incluye ni se sustituye en este paquete. Las otras mejoras no requieren esta dependencia nueva.
- No necesitas Unity ni .NET SDK para jugar. El SDK es necesario solo para compilar el proyecto.

## Instalación y actualización

1. Guarda y cierra el juego. Haz una copia de la partida y de la carpeta del mod.
2. Copia las tres DLL de `Mods` del ZIP Actualizacion a `TheLongDark/Mods`, sustituyendo las anteriores.
3. Conserva `BurebistaFishingShelter/iceland`, `states-v2`, `shelter-v1.state` y los datos de ModData. No borres la carpeta del mod.
4. Para una instalación completa, usa la carpeta `release/Mods` del ZIP Proyecto, que incluye los modelos y las texturas.
5. Comprueba que ModData.dll está instalado. Abre el juego y espera a que termine la carga.

Ruta habitual: `C:/Program Files (x86)/Steam/steamapps/common/TheLongDark`.

Los tres módulos son `BurebistaFishingShelter.dll`, `BurebistaFishingShelterEffects.dll` e `IgluAddon.dll`. No necesitas reconstruir la mesa o el bastidor existentes.

## Construcciones y materiales

Los materiales deben estar en tu inventario. Las mejoras no aparecen gratis; se construyen por separado dentro del iglú.

| Construcción | Tecla | Materiales |
|---|---|---|
| Iglú | F8 | 20 palos, 5 telas y 2 pieles de ciervo curadas |
| Mesa | Ctrl+J | 6 maderas recuperadas, 4 chatarras y 2 telas |
| Bastidor | Ctrl+K | 12 palos, 4 tripas curadas y 2 telas |
| Almacén de 500 kg | Ctrl+U | 12 maderas recuperadas, 6 chatarras, 4 telas y 4 tripas curadas |

## Teclas de uso

| Tecla | Acción |
|---|---|
| T | Abrir el menú de la mesa construida, dentro del iglú |
| G | Colocar materiales frescos cercanos en el bastidor |
| U | Abrir el almacén construido, a menos de 3 m |
| B | Descansar en el saco, cerca de él |
| E | Abrir o cerrar la puerta, cerca del centro |
| F9 | Mostrar u ocultar las instrucciones en pantalla |
| F8 | Construir o recolocar el iglú |
| F7 | Cambiar la variante |
| F5 / F6 | Ajustar el tamaño del iglú |
| F10 sin modificadores | Desmontar el iglú |

T, G y U se usan sin Ctrl, Alt ni Mayús. Ctrl+F10 no desmonta el iglú, para conservar el control del ahumador. En algunos teclados hay que usar Fn para las teclas F.

## Mesa y bastidor

La mesa mide hasta 90 cm de altura y conserva las texturas y el crafteo normal de TLD. El bastidor mantiene el tamaño aprobado en v1.14.8 y la tecla G. Ninguno crece al ampliar el iglú; se reducen para caber en iglús pequeños.

Para usar el bastidor, suelta desde tu inventario pieles o tripas frescas y retoños verdes de abedul/arce cerca de él. Acércate y pulsa G. Tiene cuatro sitios para pieles/tripas y dos para retoños. Selecciona los objetos para recogerlos. Se conserva su tamaño y progreso de curado; las pieles grandes pueden solaparse visualmente. No se puede recolocar, redimensionar ni desmontar el iglú con objetos en el bastidor.

## Almacén nuevo

Estantería detrás de la mesa, hacia la pared, de unos 1,8 m de ancho y 1,9 m de alto, con madera del juego y adornos visuales de comida, herramientas, linterna y equipo. Los adornos no son objetos recogibles ni añaden inventario gratis. Si un modelo no está disponible en esa versión del juego, se omite ese adorno y se anota en el registro.

Pulsa Ctrl+U para construirlo y U para abrir la interfaz normal de contenedores. Capacidad de 500 kg, sin filtro adicional de tipos: acepta los alimentos y el equipo que permita guardar TLD en un contenedor normal. Mantiene las reglas normales del juego, incluido el deterioro; no es una nevera ni un congelador.

El almacén empieza vacío. Su contenido y su construcción se guardan con ModData por partida y región al guardar el juego. No se escriben los objetos en el archivo independiente del refugio. Guarda después de construirlo y después de cambiar su contenido. Conserva ModData y sus datos al actualizar o hacer copias de seguridad.

Debes vaciarlo antes de mover, redimensionar o desmontar el iglú. Al desmontar se devuelve la mitad de sus materiales. Si falla la lectura de sus datos, se bloquea el almacén para evitar sustituir contenido por un almacén vacío.

## Comprobación de esta versión

Los tres módulos compilan sin errores. Pasan 60 comprobaciones de estado, restauración, selección del bastidor y formato del almacén, además de las comprobaciones del cargador B3D. Estas pruebas no ejecutan Unity ni la interfaz nativa de contenedores.

La v1.15.1 ha sido confirmada como funcional por el usuario. Las pruebas automatizadas no verifican todos los casos de capacidad, guardado/recarga y cambio de región. Antes de utilizarlo para equipo valioso, prueba depositar un objeto, guardar, salir al menú, cargar y retirarlo. Si hay un fallo, conserva la partida y los datos y revisa `MelonLoader/Latest.log`.

## Compilar

Ejecuta `build.ps1` con .NET SDK y las bibliotecas generadas por MelonLoader en la instalación del juego. `-GameDir` permite elegir otra ruta. El ZIP Proyecto incluye el código y las pruebas. ModData se detecta al ejecutar el juego; no hace falta referenciarlo para compilar.

## Corrección v1.15.1

Corrige el error NullReferenceException en Container.BeginContainerClose al pulsar Atrás o Escape en el almacén. El cierre sin animación solo se aplica al almacén del iglú; los demás contenedores conservan su comportamiento. Mantiene contenido, capacidad, recetas, muebles y teclas. Cierra el juego e instala las tres DLL sin borrar ModData ni estados. No necesitas reconstruir el almacén. Compilación verificada; El usuario ha confirmado que esta versión funciona en su instalación.
