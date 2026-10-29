# Pruebas de regresión

Desde la raíz del proyecto, en Windows con el SDK de .NET 8 o posterior:

```powershell
dotnet test BananaCompiler.Tests/BananaCompiler.Tests.csproj
```

- `ScopedSymbolTableTests`: visibilidad, declaración local, asignación a una
  variable visible, valores desconocidos y direcciones alineadas sin solapamientos.
- `ParserSymbolTests`: integración con el lexer y `data.db`, ámbitos de bloques,
  variables de control, condiciones de `do…while`, valores de ejecución y
  compatibilidad con los ejemplos de `Samples/Valid` y los diagnósticos de
  `Samples/InvalidDiagnostics`.
- `DiagnosticRecoveryTests`: cantidades y posiciones precisas, recuperación en
  bloques anidados y en una misma línea, conservación de instrucciones, reversión
  de cambios de instrucciones descartadas, orden de diagnósticos y traza sin duplicados.

Las reglas de recuperación y los códigos están documentados en
[`Docs/Diagnostics.md`](../Docs/Diagnostics.md).

La tabla de símbolos muestra memoria simulada. Los identificadores no declarados
siguen teniendo tipo desconocido: estas correcciones no cambian esa regla del lenguaje.
