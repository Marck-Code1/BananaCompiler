# BananaCompiler

Proyecto de compilador con interfaz gráfica en C# y Windows Forms. Incluye análisis
léxico, sintáctico y semántico, diagnósticos, ejemplos y pruebas de regresión.

## Descargar el proyecto

Con Git instalado:

```sh
git clone https://github.com/Marck-Code1/BananaCompiler.git
cd BananaCompiler
```

En Windows se recomienda clonar con Git en lugar de descargar un ZIP: los archivos
creados por `git clone` normalmente no reciben la "marca de la Web" que Windows
puede aplicar a archivos descargados desde el navegador. Esto no elimina las
restricciones de compatibilidad con Linux descritas más abajo.

El repositorio contiene el código fuente, la solución, los proyectos y `data.db`,
la tabla de transiciones de SQLite necesaria para el lexer. Los paquetes de NuGet
y los archivos de compilación se regeneran localmente; no se incluyen `bin/`,
`obj/`, `.vs/` ni las configuraciones personales de Visual Studio.

## Compatibilidad

**La aplicación actual y sus pruebas requieren Windows.** Usan .NET 8 con destino
`net8.0-windows` y la interfaz está hecha con Windows Forms.

En Linux se puede clonar el repositorio, leer y modificar el código con un editor
como VS Code, y compartir cambios mediante Git. Por el momento no se puede ejecutar
la interfaz gráfica de forma nativa ni ejecutar las pruebas actuales en Linux.
La adaptación a Linux queda pendiente; descargar el código desde GitHub no cambia
esta limitación.

## Compilar y ejecutar en Windows

Requisitos:

- SDK de .NET 8 o una versión posterior compatible, con el runtime de .NET 8.
- Si se utiliza Visual Studio: Visual Studio 2022 actualizado con la carga de
  trabajo **Desarrollo de escritorio de .NET**.
- Conexión a Internet para restaurar los paquetes de NuGet la primera vez.

Abrir `BananaCompiler.sln` en Visual Studio y seleccionar `BananaCompiler` como
proyecto de inicio, o ejecutar desde la raíz del repositorio:

```powershell
dotnet restore BananaCompiler.sln
dotnet build BananaCompiler.sln
dotnet run --project BananaCompiler.csproj
```

`data.db` se copia automáticamente a la carpeta de salida durante la compilación.
No eliminarlo ni sustituirlo por una base de datos vacía.

## Ejecutar las pruebas en Windows

```powershell
dotnet test BananaCompiler.Tests/BananaCompiler.Tests.csproj
```

Más información en [BananaCompiler.Tests/README.md](BananaCompiler.Tests/README.md).

## Colaborar

Antes de empezar una tarea, con los cambios anteriores guardados en un commit:

```sh
git switch main
git pull --ff-only
git switch -c mejora/descripcion-del-cambio
```

Después de modificar el código, revisar los archivos y guardar los avances:

```sh
git status
git diff
git add .
git commit -m "Describe el cambio realizado"
git push -u origin mejora/descripcion-del-cambio
```

Para subir una rama al mismo repositorio, el propietario debe invitar al compañero
desde **Settings → Collaborators** en GitHub y este debe aceptar la invitación.
Después puede abrir un **Pull request** para revisar e integrar sus cambios.
Sin permisos de escritura, puede trabajar en un **fork** y enviar un Pull request
desde allí, si la visibilidad y las políticas del repositorio lo permiten.

## Estructura

- `Lexer/`: analizador léxico y lectura de la tabla de transiciones.
- `Parser/`: análisis sintáctico y semántico, ámbitos y diagnósticos.
- `Models/`: modelos de tokens e identificadores.
- `Form1.cs`, `Form1.Designer.cs`, `Form1.resx`: interfaz de Windows Forms.
- `Samples/`: programas de ejemplo válidos e inválidos.
- `BananaCompiler.Tests/`: pruebas automatizadas.
- `Docs/`: documentación del lenguaje y del análisis.
- `scripts/`: herramientas auxiliares de documentación.
