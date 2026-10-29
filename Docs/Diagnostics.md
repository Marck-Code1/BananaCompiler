# Diagnósticos y recuperación de errores

## Resultado y presentación

`ParseResult.Errors` y `ParseResult.SemanticErrors` siguen disponibles por
separado. `ParseResult.Diagnostics` combina ambos grupos y los ordena por
línea, columna y categoría. Cada diagnóstico incluye:

- `Kind`: sintáctico o semántico.
- `Code`: identificador interno estable del problema, no visible en la interfaz.
- `Token`: ubicación principal.
- `Message`: texto para mostrar al usuario.
- `RelatedToken`: apertura relacionada, cuando falta un delimitador o un cierre.

La interfaz muestra `Línea:Columna` y un mensaje en español mediante
`Diagnostic.Format()`, por ejemplo: `Sintáctico: Falta 'then' antes de 'write'.`
Los códigos se conservan solo para identificar los diagnósticos en el código y
en las pruebas; no se muestran al usuario. Los errores
de condiciones señalan el inicio de la expresión, no la palabra `if` o `while`.
Los errores de operadores señalan el operador e indican los tipos recibidos.
Los mensajes de `for` y `move` distinguen límites y argumentos.

Los errores léxicos también se muestran con `Línea:Columna`. Se conservan
como tokens `ERROR` cuyo `Lexeme` contiene el mensaje, en su tabla visual
separada; no forman parte de `ParseResult.Diagnostics` ni tienen códigos
`SYN_` o `SEM_`.

## Errores léxicos definidos

La tabla SQLite contiene cinco estados de error, todos con mensaje. El lexer
define además los mensajes de carácter fuera del alfabeto y cadena sin cerrar.

| Origen | Mensaje | Ejemplo de entrada |
| --- | --- | --- |
| Estado 106 de `data.db` | Operador aritmético no válido | `+.` |
| Estado 107 de `data.db` | Operador lógico no válido | `&` o `\|` |
| Estado 108 de `data.db` | Operador relacional no válido | `<.` |
| Estado 109 de `data.db` | Constante numérica no válida | `1.`, `1E` o `1E+` |
| Estado 110 de `data.db` | Identificador no válido | `1abc` |
| `LexicalAnalyzer` | Carácter no válido en el alfabeto | `€` fuera de una cadena o comentario |
| `LexicalAnalyzer` | Cadena sin terminar | Una comilla de apertura sin cierre en la línea |

Las categorías de los cinco primeros mensajes proceden de las transiciones
del autómata, no de una clasificación posterior por expresiones regulares.
El error se ubica al inicio del lexema afectado; un carácter no reconocido
sin lexema pendiente se ubica en su propia columna. Un `#` o un carácter
ajeno al alfabeto dentro de una cadena o comentario no genera por sí solo
un error léxico.

## Códigos principales

| Código | Significado |
| --- | --- |
| `SEM_UNDECLARED_VARIABLE` | Variable inexistente o no visible en el ámbito actual |
| `SEM_UNKNOWN_TYPE` | Símbolo visible sin tipo conocido; comprobación defensiva interna |
| `SEM_ASSIGNMENT_TYPE` | Asignación incompatible con el tipo fijo del destino (también en `rand`) |
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
| `SEM_CONDITION_TYPE` | Condición cuyo tipo no es booleano |
| `SEM_ARGUMENT_TYPE` | Argumento o límite cuyo tipo no es numérico |
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
  validar su argumento y la compatibilidad de su resultado entero. `read`
  requiere un destino ya declarado y conserva su tipo. La variable de control
  de `for` se introduce después de su cabecera con el tipo común de sus límites;
  si la cabecera es inválida se marca `<inválido>` como enlace de recuperación.
- Las expresiones sintácticamente incompletas no publican diagnósticos semánticos
  ni reducciones parciales, tanto si se descartan como si se repara localmente
  un paréntesis. Los errores semánticos de otras instrucciones y argumentos
  completos se conservan.
- Un resultado semántico inválido evita errores derivados en operadores externos,
  pero no oculta fallos independientes en otras subexpresiones.
- Las reducciones entre paréntesis se publican una sola vez en la traza.

## Posición de fin de archivo

`LexicalAnalyzer.Tokenize(source, includeEndOfFile: true)` incluye un token EOF
con la posición real del final del texto, incluidos comentarios, líneas vacías y
saltos LF, CRLF o CR. La interfaz utiliza esta opción y no muestra EOF en la
lista de tokens. Los llamadores que no proporcionan EOF conservan el respaldo
del parser, situado inmediatamente después del último token recibido.

## Reglas semánticas de nombres y asignaciones

Usar una variable inexistente o fuera de ámbito genera `SEM_UNDECLARED_VARIABLE`
en el token del nombre. No se confunde un nombre no declarado con un valor
desconocido en tiempo de ejecución: una variable inicializada y luego leída
con `read` conserva un tipo válido aunque su valor ya no se conozca.

La primera asignación válida declara implícitamente; las siguientes conservan
el tipo y requieren compatibilidad. `SEM_ASSIGNMENT_TYPE` señala el destino y
explica los tipos de origen y destino. Los fallos no sobrescriben el último
símbolo válido y no impiden analizar instrucciones posteriores. Las reglas
completas de tipos, `read`, `rand` y `for` están en [Grammar.md](Grammar.md).

`SEM_UNKNOWN_TYPE` protege la resolución ante un símbolo interno sin tipo.
Las reglas actuales no crean esas variables a partir de instrucciones válidas:
una asignación válida infiere el tipo, `read` no declara y `for` inválido usa
un tipo de recuperación. Por tanto, no se exige inventar una declaración sin
tipo en el lenguaje para demostrar este diagnóstico defensivo.

## Ejemplos y ubicaciones de los códigos

| Código | Ejemplo | Token señalado |
| --- | --- | --- |
| `SYN_EXPECTED_TOKEN` | `start read 1 end` | `1`, donde se esperaba un identificador |
| `SYN_MISSING_EXPRESSION` | `start x = write 1 end` | `=`, después del cual falta la expresión |
| `SYN_MISSING_OPERAND` | `start x = 1 + write 1 end` | `+`, después del cual falta un operando |
| `SYN_MISSING_TOKEN` | `start if true write 1 endif end` | `write`, antes del cual falta `then` |
| `SYN_MISSING_CLOSER` | `start while true do end` | `end`, conservado como cierre externo |
| `SYN_MISMATCHED_CLOSER` | `start if true then endwhile end` | `endwhile`, cierre equivocado |
| `SYN_UNEXPECTED_CLOSER` | `start endif end` | `endif`, sin bloque correspondiente |
| `SYN_DUPLICATE_ELSE` | `start if true then else else endif end` | El segundo `else` |
| `SYN_CHAINED_COMPARISON` | `start x = 1 < 2 < 3 end` | El segundo `<` |
| `SYN_UNEXPECTED_TOKEN` | `start x = 1 99 end` | `99`, que no puede continuar la instrucción |
| `SEM_UNDECLARED_VARIABLE` | `start write missing end` | `missing` |
| `SEM_ASSIGNMENT_TYPE` | `start x = 1 x = "text" end` | El destino `x` de la segunda asignación |
| `SEM_TYPE_MISMATCH` | `start x = true + 1 end` | `+`, con los tipos de ambos operandos |
| `SEM_CONDITION_TYPE` | `start if 1 then endif end` | `1`, inicio de la condición |
| `SEM_ARGUMENT_TYPE` | `start move 1, true end` | `true`, segundo argumento |
| `SEM_BREAK_OUTSIDE_LOOP` | `start break end` | `break` |

El fin de archivo también puede ser la ubicación principal de un error por
token o cierre faltante. Para delimitadores recuperados, `RelatedToken` conserva
la apertura correspondiente cuando procede.

No se añadió evaluación numérica, desbordamiento o división por cero: esas
validaciones requieren definir primero la representación y los límites
numéricos del lenguaje y quedan fuera del alcance acordado.

## Verificación

```powershell
dotnet test BananaCompiler.Tests/BananaCompiler.Tests.csproj --configuration Release
```

`DiagnosticRecoveryTests` comprueba cantidades, códigos y posiciones, conservación
de instrucciones, ámbitos después de un cierre faltante, reversión de cambios,
orden de diagnósticos y trazas sin duplicados. Incluye los ejemplos existentes
con cantidades exactas de errores y 250 secuencias cortas de tokens malformados
generadas con una semilla fija para comprobar progreso y ausencia de excepciones.

La auditoría de diagnósticos añadió:

- `LexerDiagnosticTests`: los siete mensajes léxicos, sus posiciones, los cinco
  estados de error de SQLite y el tratamiento de lexemas incompletos, comentarios
  y cadenas.
- `DiagnosticDefinitionTests`: significado y ubicación de los diez códigos
  sintácticos; matrices de 350 combinaciones de operadores binarios, 15 unarios,
  25 asignaciones y 15 condiciones, además de argumentos y contexto de `break`.
- Casos de recuperación de paréntesis que descartan semántica parcial sin
  retirar errores independientes de otras instrucciones o argumentos.

Resultado de la versión auditada: **208 pruebas superadas**, sin fallos ni
omisiones. Los recorridos internos de matrices y secuencias no se suman como
pruebas xUnit independientes. Evaluación numérica y errores de ejecución
siguen fuera del alcance; no se afirma detectar división por cero o desbordamiento.
