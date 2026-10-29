# BananaCompiler

Proyecto de compilador con interfaz gráfica en C# y Windows Forms. Incluye análisis
léxico, sintáctico y semántico, diagnósticos, ejemplos y pruebas de regresión.

## Compatibilidad

- **Windows:** la aplicación y las pruebas usan .NET 8 con destino
  `net8.0-windows`. La interfaz depende de Windows Forms y Scintilla5.NET.
- **Linux:** el código se puede editar, pero la interfaz gráfica no se puede
  ejecutar de forma nativa. Las pruebas actuales también requieren Windows porque
  referencian el proyecto de la aplicación.

El lexer y el parser están organizados en carpetas independientes de la interfaz,
pero todavía no existe un proyecto de biblioteca multiplataforma. La adaptación a
Linux queda pendiente.

## Estructura del proyecto

| Ruta | Contenido |
| --- | --- |
| `BananaCompiler.sln` | Solución con la aplicación y el proyecto de pruebas. |
| `BananaCompiler.csproj` | Configuración de la aplicación y dependencias de NuGet. |
| `Program.cs` | Punto de entrada de la aplicación. |
| `Form1.cs`, `Form1.Designer.cs`, `Form1.resx` | Interfaz de Windows Forms. |
| `Lexer/` | Analizador léxico y lectura de la tabla de transiciones. |
| `Parser/` | Análisis sintáctico y semántico, ámbitos y diagnósticos. |
| `Models/` | Modelos de tokens e identificadores. |
| `Properties/` | Recursos de la aplicación. |
| `data.db` | Tabla de transiciones de SQLite utilizada por el lexer. |
| `Samples/` | Programas de ejemplo válidos e inválidos. |
| `BananaCompiler.Tests/` | Pruebas automatizadas con xUnit. |
| `Docs/` | Documentación de la gramática, diagnósticos y árbol sintáctico. |
| `scripts/` | Herramientas auxiliares de documentación. |

## Ejecución en Visual Studio

Utilizar Visual Studio 2022 con la carga de trabajo **Desarrollo de escritorio de
.NET** y soporte para .NET 8.

1. Abrir `BananaCompiler.sln`.
2. Esperar a que se restauren los paquetes de NuGet.
3. Establecer `BananaCompiler` como proyecto de inicio.
4. Ejecutar con **F5** o **Ctrl+F5** para iniciar sin depuración.

`data.db` es necesaria para el análisis léxico y se copia automáticamente a la
carpeta de salida durante la compilación. Los paquetes de NuGet se restauran
localmente; `bin/`, `obj/` y la configuración personal del IDE no se versionan.

## Pruebas

En Visual Studio, abrir **Prueba → Explorador de pruebas** y seleccionar
**Ejecutar todas las pruebas**. Las pruebas están en `BananaCompiler.Tests`.

Más información en [BananaCompiler.Tests/README.md](BananaCompiler.Tests/README.md).
