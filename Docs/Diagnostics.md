# Diagnósticos y recuperación de errores

## Resultado y presentación

`ParseResult.Errors` y `ParseResult.SemanticErrors` siguen disponibles por
separado. `ParseResult.Diagnostics` combina ambos grupos y los ordena por
línea, columna y categoría. Cada diagnóstico incluye:

- `Kind`: sintáctico o semántico.
- `Code`: identificador estable del problema.
- `Token`: ubicación principal.
- `Message`: texto para mostrar al usuario.
- `RelatedToken`: apertura relacionada, cuando falta un delimitador o un cierre.

La interfaz muestra `Línea:Columna`, categoría, código y mensaje. Los errores
de condiciones señalan el inicio de la expresión, no la palabra `if` o `while`.
Los errores de operadores señalan el operador e indican los tipos recibidos.
Los mensajes de `for` y `move` distinguen límites y argumentos.

## Códigos principales

| Código | Significado |
| --- | --- |
| `SYN_EXPECTED_TOKEN` | Falta un token requerido por la producción |
| `SYN_MISSING_EXPRESSION` | Falta una expresión después de `=`, `write`, etc. |
| `SYN_MISSING_OPERAND` | Falta un operando después de un operador |
| `SYN_MISSING_TOKEN` | Se recuperó un delimitador obligatorio ausente |
| `SYN_MISSING_CLOSER` | Falta el cierre de un bloque antes de un cierre externo o EOF |
| `SYN_MISMATCHED_CLOSER` | Un bloque se cierra con una palabra incorrecta |
| `SYN_UNEXPECTED_CLOSER` | Cierre sin bloque correspondiente |
| `SYN_DUPLICATE_ELSE` | Segunda rama `else` del mismo `if` |
| `SYN_CHAINED_COMPARISON` | Comparaciones encadenadas no admitidas por la gramática |
| `SYN_UNEXPECTED_TOKEN` | Token que no puede continuar una instrucción |
| `SEM_TYPE_MISMATCH` | Tipos incompatibles para un operador |
| `SEM_CONDITION_TYPE` | Condición conocida que no es booleana |
| `SEM_ARGUMENT_TYPE` | Argumento o límite conocido que no es numérico |
| `SEM_BREAK_OUTSIDE_LOOP` | `break` fuera de un ciclo |

## Recuperación

### Expresiones e instrucciones

La sincronización no consume un token que ya inicia una instrucción válida.
Reconoce palabras de inicio (`write`, `read`, `if`, etc.) y un identificador
seguido de `=`, incluso en la misma línea. Por ejemplo:

```banana
start
  x = 1 +
  y = 2
  write y
end
```

Se informa un operando faltante en `+`. `y = 2` y `write y` se conservan;
`y` no se interpreta como operando de `x`.

Los saltos de línea **siguen siendo espacios en blanco** para la gramática:
una expresión válida puede continuar en otra línea. No se añadió un terminador
de instrucciones nuevo.

### Bloques

El parser mantiene los bloques abiertos y sus cierres. Un cierre que pertenece
a un bloque externo no se consume dentro del bloque incompleto. Se informa el
cierre faltante, se inserta un token sintético y se deja al padre leer su cierre.

```banana
start
  while true do
    if true then
      x = 1
  endwhile
  y = 2
end
```

Solo se informa el `endif` faltante. Se conservan el `while` y el `if` en el
árbol; `y` pertenece al ámbito global, no al bloque incompleto.

Se pueden insertar `start`, `then` y `do` cuando el siguiente token ya inicia
una instrucción o cierra un bloque. También se puede recuperar `)` ante un
límite claro de expresión. La inserción siempre produce un diagnóstico;
**el programa no se considera válido** por haber sido recuperado.

Si la cabecera no se puede reparar de forma local, se descarta la instrucción
equilibrando sus bloques anidados. La primera aparición de `endwhile` dentro
de un ciclo interno no se toma como cierre del ciclo externo. Un cierre externo
se conserva para el bloque padre.

### Árbol parcial

- `MISSING` representa un token sintético insertado.
- `ERROR` conserva los tokens descartados, sin incluir la siguiente instrucción válida.
- La ausencia del `end` final no destruye el árbol ya reconocido.

La recuperación es una heurística: no determina la intención del usuario en
estructuras ambiguas, especialmente varios cierres iguales ausentes.

## Acciones semánticas y trazabilidad

- Cada instrucción abre una transacción en la tabla de símbolos. Si falla
  sintácticamente y debe descartarse, sus declaraciones y asignaciones se
  revierten. Las transacciones de instrucciones anidadas se confirman con su
  bloque externo. Se registran cambios, no copias completas de la tabla.
- Los cuerpos conservados mediante reparación local mantienen las acciones de
  sus instrucciones reconocidas; no se descarta un bloque entero solo por un
  cierre sintético.
- `rand` actualiza su destino únicamente después de reconocer la instrucción y
  validar su argumento. La variable de control de `for` no se introduce antes
  de reconocer su cabecera.
- Las expresiones sintácticamente incompletas no publican diagnósticos semánticos
  ni reducciones parciales. Los errores semánticos de otras instrucciones se conservan.
- Un resultado semántico inválido evita errores derivados en operadores externos,
  pero no oculta fallos independientes en otras subexpresiones.
- Las reducciones entre paréntesis se publican una sola vez en la traza.

## Posición de fin de archivo

`LexicalAnalyzer.Tokenize(source, includeEndOfFile: true)` incluye un token EOF
con la posición real del final del texto, incluidos comentarios, líneas vacías y
saltos LF, CRLF o CR. La interfaz utiliza esta opción y no muestra EOF en la
lista de tokens. Los llamadores que no proporcionan EOF conservan el respaldo
del parser, situado inmediatamente después del último token recibido.

## Reglas que no cambian

Los identificadores cuyo tipo no se conoce siguen aceptándose como desconocidos.
No se añadió un error por variable no declarada o fuera de ámbito. Tampoco se
añadió evaluación numérica, desbordamiento o división por cero: esas validaciones
requieren definir primero la representación y los límites numéricos del lenguaje.

## Verificación

```powershell
dotnet test BananaCompiler.Tests/BananaCompiler.Tests.csproj --configuration Release
```

`DiagnosticRecoveryTests` comprueba cantidades, códigos y posiciones, conservación
de instrucciones, ámbitos después de un cierre faltante, reversión de cambios,
orden de diagnósticos y trazas sin duplicados. Incluye los ejemplos existentes
con cantidades exactas de errores y 250 secuencias cortas de tokens malformados
generadas con una semilla fija para comprobar progreso y ausencia de excepciones.
