# BananaCompiler: resumen para exposición

## 1. ¿Qué hace el proyecto?

BananaCompiler es una aplicación educativa que analiza programas escritos en el lenguaje **Banana**. Permite escribir o cargar código, identificar sus tokens, revisar su estructura y detectar errores de tipos y de uso de instrucciones.

Implementa las principales tareas del **front-end de un compilador**: análisis léxico, sintáctico y semántico. También construye un árbol sintáctico y una tabla de símbolos con ámbitos y direcciones de memoria simuladas.

**Importante:** no genera código máquina ni ejecuta los programas Banana. Reconocer `write`, `read` o un ciclo no significa imprimir, pedir datos o repetir instrucciones; significa comprobar que están correctamente escritos.

## 2. ¿Qué tecnologías se utilizaron?

| Tecnología | Uso en el proyecto |
| --- | --- |
| **C# y .NET 8** | Implementación de la aplicación y de los analizadores. |
| **Windows Forms** | Ventana, botones, tablas y presentación de resultados. |
| **Scintilla5.NET / ScintillaNET** | Controles para editar el código y mostrar los tokens. El análisis del lenguaje lo hacen las clases del proyecto, no Scintilla. |
| **SQLite y Microsoft.Data.Sqlite** | Lectura de la tabla de transiciones del autómata desde `data.db`. El proyecto también referencia `System.Data.SQLite`, pero el cargador actual utiliza `Microsoft.Data.Sqlite`. |
| **xUnit y Microsoft.NET.Test.Sdk** | Pruebas automatizadas de símbolos, ámbitos y recuperación de errores. |

Se usan clases y registros para representar datos; listas para tokens y nodos; diccionarios para transiciones y símbolos; conjuntos para categorías de tokens; y pilas para atributos semánticos, contextos y transacciones.

## 3. Recorrido general del código

```text
Código fuente Banana
        |
        v
Analizador léxico + tabla de transiciones de SQLite
        |
        v
Tokens: categoría, lexema, línea y columna
        |
        v
Parser descendente + acciones semánticas
        |
        v
Árbol sintáctico + símbolos + trazas + diagnósticos
        |
        v
Resultados en la interfaz
```

1. `Program.cs` inicia la aplicación y abre `Form1`.
2. Al cargar la ventana, `Form1_Load` lee la tabla del autómata desde `data.db`.
3. Cada cambio en el editor activa `txtCode_TextChanged`: limpia los resultados anteriores y vuelve a analizar el texto.
4. `LexicalAnalyzer` produce tokens y errores léxicos. La interfaz asigna un número a cada nombre de identificador y muestra los tokens.
5. **Si no hay errores léxicos**, `TopDownParser` revisa la sintaxis y realiza las comprobaciones semánticas.
6. `ParseResult` reúne el árbol, los símbolos, las reducciones y los diagnósticos. La ventana muestra las tablas y un resumen con cantidades.

Los botones permiten cargar un programa, guardar su texto y guardar la representación de tokens.

## 4. Análisis léxico: autómata finito determinista

El análisis léxico responde: **“¿Qué elementos forman el texto?”**

- **Alfabeto:** conjunto de caracteres admitidos por el lenguaje.
- **Lexema:** texto concreto reconocido, por ejemplo `total`, `25` o `+`.
- **Token:** categoría del lexema, por ejemplo identificador, número u operador.

El reconocimiento principal utiliza un **autómata finito determinista (AFD)** representado mediante una tabla. Es determinista porque, para cada combinación de estado y símbolo de entrada, la tabla indica como máximo un siguiente estado.

Conceptualmente, un AFD se describe como `M = (Q, Σ, δ, q0, F)`:

| Elemento | Correspondencia en el proyecto |
| --- | --- |
| `Q`: estados | Números de estado de la tabla. |
| `Σ`: alfabeto | Caracteres representados por columnas de la tabla. |
| `δ`: transición | Consulta de `(estado, columna)` para obtener el siguiente estado. |
| `q0`: estado inicial | Estado `0`. |
| `F`: estados de aceptación | Estados con una categoría de token; los estados `ERROR` se distinguen de los reconocimientos válidos. |

### ¿Cómo se implementa?

`SqliteTransitionTableSource` lee la tabla `banana`: sus filas contienen `Estado`, `Token`, `Mensaje` y columnas de transición. `TransitionTable` guarda esos datos en diccionarios para consultarlos en memoria; **no se consulta SQLite por cada carácter**.

`LexicalAnalyzer` recorre el texto carácter por carácter, acumula un lexema y actualiza su estado. La columna especial `FDC` permite finalizar el reconocimiento. Cuando termina un lexema válido, emite su token y vuelve al estado inicial. También puede finalizar un token y reprocesar el carácter que inicia el siguiente.

Hay tratamientos específicos fuera de la tabla: los comentarios empiezan con `#` y se ignoran hasta el final de la línea; las cadenas se leen entre comillas dobles. Se reportan caracteres inválidos, cadenas sin terminar y secuencias mal formadas, como un número seguido inmediatamente por letras.

No se usa un generador léxico ni se construye un AFN durante la ejecución: el programa utiliza directamente la tabla del autómata y rutinas auxiliares.

### Tokens principales

| Categoría | Código | Ejemplo |
| --- | --- | --- |
| Identificador | `ID` | `total` |
| Número entero, real o exponencial | `CN`, `CNR`, `CNE` | `25`, `2.5`, notación científica |
| Cadena | `CAD` | `"hola"` |
| Operador aritmético | `OA1` a `OA6` | `+`, `-`, `*`, `/`, `^`, `%` |
| Operador relacional | `OR1` a `OR6` | `<`, `<=`, `>`, `>=`, `==`, `!=` |
| Operador lógico | `OL1`, `OL2`, `OL3` | `&&`, `\|\|`, `!` |
| Asignación y delimitadores | `OPA`, `CE5`, `CE6`, `CE7` | `=`, `(`, `)`, `,` |
| Palabra reservada | `PR1` a `PR21` | `start`, `end`, `if`, `write` |
| Fin de entrada | `EOF` | Marca interna del final del texto. |

**Relación con la teoría:** las categorías léxicas se relacionan con lenguajes regulares, reconocibles por autómatas finitos. Los bloques anidados requieren un análisis adicional.

## 5. Análisis sintáctico: gramática libre de contexto

El análisis sintáctico responde: **“¿Los tokens están acomodados según las reglas del lenguaje?”**

Se utiliza una **gramática libre de contexto**, de tipo 2 en la jerarquía de Chomsky. Cada producción tiene un no terminal en su lado izquierdo. Sus elementos son:

- **Terminales:** tokens como `ID`, `OPA` y las palabras reservadas.
- **No terminales:** estructuras como `INICIO`, `INS`, `ASSIGN` y `EXP`.
- **Producciones:** reglas para construir esas estructuras.
- **Símbolo inicial:** `S`.
- **`ε`:** secuencia vacía, útil para permitir cuerpos sin instrucciones.

Algunas reglas, escritas de forma simplificada, son:

```text
S      -> INICIO
INICIO -> start INS end
INS    -> IN INS | ε
ASSIGN -> ID = EXP
WRITE  -> write EXP
IF     -> if EXP then INS IF_T
IF_T   -> endif | else INS endif
```

`TopDownParser` implementa un **parser descendente recursivo**: empieza por `S` y llama métodos como `ParseAssignment`, `ParseIf` y `ParseWhile`. Decide qué regla aplicar observando los tokens; `Expect` comprueba el token requerido y avanza si coincide. No utiliza una tabla LR ni un generador de parsers.

La sintaxis admite asignaciones, `write`, `read`, `if/else`, `while`, `for`, `do…while`, `rand`, `move`, `break` y `clear`. Los saltos de línea **no son terminadores de instrucciones**: la estructura se reconoce por los tokens.

**Relación con los autómatas de pila:** una gramática libre de contexto puede reconocerse con un autómata de pila. Aquí no se programa uno mediante una tabla explícita; la pila de llamadas de los métodos recursivos permite manejar expresiones y bloques anidados. La pila semántica tiene otra función: manejar tipos y atributos.

### Precedencia y asociatividad

La organización de los métodos de expresión determina este orden, de mayor a menor prioridad:

```text
Paréntesis -> potencia -> signos unarios -> *, /, %
           -> +, - binarios -> ! -> comparaciones -> && -> ||
```

Por ejemplo, `2 + 3 * 4` se estructura como `2 + (3 * 4)`. La potencia es asociativa por la derecha: `a ^ b ^ c` significa `a ^ (b ^ c)`. Las sumas y restas binarias se agrupan por la izquierda. No se permiten comparaciones encadenadas como `a < b < c`.

## 6. Árbol sintáctico

`SyntaxNode` conserva la estructura reconocida: cada nodo tiene una categoría, hijos y, cuando corresponde, un token con su posición y un atributo semántico.

El árbol incluye símbolos de la gramática y tokens, por lo que se aproxima a un **árbol de análisis sintáctico**, no solamente a un AST simplificado. Sirve para observar cómo se construyó el programa y cómo se agruparon sus expresiones.

La raíz es `S`. Si hay recuperación de errores, pueden aparecer nodos `ERROR` y `MISSING`. La interfaz muestra la raíz y la cantidad de nodos; las funciones `ToTreeString` y `ToCompact` permiten obtener representaciones textuales del árbol.

## 7. Análisis semántico y pilas

El análisis semántico responde: **“Aunque la estructura sea correcta, ¿las operaciones tienen sentido según las reglas implementadas?”**

Se realiza durante el reconocimiento sintáctico mediante **acciones semánticas**, una idea relacionada con las gramáticas de atributos y la traducción dirigida por sintaxis. Los valores de las expresiones llevan atributos de tipo y, en algunos casos, un valor conocido.

- `SemanticValue` representa un tipo y un posible valor. Los tipos son `Entero`, `Decimal`, `Exponencial`, `string` y `bool`; también existen estados desconocido e inválido.
- `SemanticStack` es una pila **LIFO**: el último elemento que entra es el primero que sale. Se apilan operandos, se extraen al procesar un operador y se apila el resultado tipado.
- `SemanticPile` registra una traza didáctica de las reducciones y del estado de la pila. No sustituye la pila de atributos reales.

Comprobaciones principales:

- La aritmética requiere operandos numéricos conocidos.
- `&&`, `||` y `!` requieren operandos booleanos conocidos.
- Las condiciones de los ciclos y del `if` deben ser booleanas.
- Los límites del `for` y los argumentos numéricos de `rand` y `move` deben tener tipos compatibles.
- `break` solo puede aparecer dentro de un ciclo.
- Un nombre debe estar declarado en un ámbito visible antes de utilizarse.
- La primera asignación válida con `=` declara e infiere el tipo; las siguientes deben ser compatibles y conservan ese tipo.
- `read` exige un destino existente y conserva su tipo; `rand` produce un entero y puede declarar su destino; `for` infiere un control numérico local de sus límites.

Los tipos numéricos se combinan dando prioridad a `Exponencial`, después `Decimal` y después `Entero`. Las asignaciones permiten ampliación numérica hacia el tipo del destino, pero no reducción ni cambios entre números, cadenas y booleanos. **Una variable inexistente o fuera de ámbito es un error semántico**.

Estas reducciones calculan principalmente **tipos**, no el resultado numérico de las operaciones. Una asignación literal sencilla puede conservar su valor; una expresión compuesta no se evalúa como lo haría un intérprete.

## 8. Tabla de símbolos, ámbitos y memoria simulada

`ScopedSymbolTable` registra nombre, tipo, valor conocido, ámbito, dirección y tamaño de cada símbolo. Mantiene una pila de ámbitos, empezando por `global`.

La búsqueda va desde el ámbito más interno al global. `Assign` modifica el símbolo visible más cercano o lo crea si no existe. `Declare` crea un símbolo en el ámbito actual, permitiendo **ocultamiento o shadowing** de uno externo; se utiliza, por ejemplo, para la variable de control de un `for`. Estas operaciones almacenan símbolos; el parser valida primero los tipos fijos y las reglas de declaración del lenguaje.

Al salir de un bloque, sus símbolos dejan de ser visibles, pero se conservan para el reporte. El número de identificador mostrado en los tokens identifica un nombre léxico: el mismo nombre puede aparecer en varias filas semánticas si pertenece a ámbitos distintos.

Al finalizar el análisis se asignan direcciones desde `0x1000`, alineadas a 4 bytes. Los tamaños didácticos son: entero 4, decimal 8, exponencial 16, booleano 1 y cadena `2 × longitud + 2`. Los tipos desconocidos muestran tamaño 0, aunque se reserva un espacio simulado mínimo de 4 bytes.

**Estas direcciones no son direcciones reales de variables de C# ni reservas de memoria de un programa ejecutado.** Son una representación educativa de distribución y alineación.

## 9. Errores y recuperación

| Tipo de error | Qué detecta | Ejemplo |
| --- | --- | --- |
| Léxico | Caracteres o lexemas inválidos. | Una cadena sin cerrar. |
| Sintáctico | Incumplimiento de la gramática. | `x = 1 +` sin segundo operando. |
| Semántico | Uso incompatible con las reglas de tipos o contexto. | `x = "hola" + 2` o `break` fuera de un ciclo. |

Los errores incluyen ubicaciones y mensajes. El parser intenta continuar mediante **sincronización**, buscando inicios de instrucciones o cierres de bloques. En algunos casos inserta tokens sintéticos para representar un delimitador faltante, sin considerar válido el programa por haberlo recuperado.

Cada instrucción registra sus cambios en una transacción de la tabla de símbolos. Si falla y se descarta, esos cambios se revierten para no contaminar las instrucciones siguientes. Estas transacciones son del analizador, **no transacciones de SQLite**.

## 10. Mapa resumido de archivos

| Archivo o grupo | Responsabilidad |
| --- | --- |
| `Program.cs` | Punto de entrada de la aplicación. |
| `Form1.cs` | Conecta editor, analizadores, tablas, reporte y carga/guardado. |
| `Form1.Designer.cs`, `Form1.resx`, `Properties/` | Controles visuales y recursos de la ventana. |
| `BananaCompiler.csproj` | Configuración de .NET, referencias y copia de la base de datos. |
| `Models/Token.cs`, `TokenKinds.cs`, `Identifier.cs` | Representación de tokens, categorías e identificadores léxicos. |
| `Lexer/LexicalAnalyzer.cs`, `LexerTokenFacts.cs` | Recorrido del texto, emisión de tokens y reglas auxiliares. |
| `Lexer/TransitionTable.cs`, `TransitionTableRow.cs` | Representación y consulta del autómata. |
| `Lexer/SqliteTransitionTableSource.cs`, `data.db` | Carga y almacenamiento de su tabla de transiciones. |
| `Parser/TopDownParser.cs` | Reconocimiento de la gramática, construcción del árbol, acciones semánticas y recuperación. |
| `Parser/SyntaxNode.cs` | Nodos y representaciones textuales del árbol. |
| `Parser/SemanticValue.cs`, `SemanticStack.cs`, `SemanticPile.cs` | Tipos, atributos, pila y traza de reducciones. |
| `Parser/ScopedSymbolTable.cs`, `SymbolAssignment.cs` | Símbolos, ámbitos, reversión de cambios y memoria simulada. |
| `Parser/ParseError.cs`, `SemanticError.cs`, `Diagnostic.cs`, `ParseFailure.cs` | Datos de errores, mensajes y señal interna de fallo sintáctico. |
| `Parser/ParseResult.cs` | Resultado completo del análisis. |
| `BananaCompiler.Tests/` | Pruebas automatizadas de tipos, declaración implícita, visibilidad, instrucciones y recuperación de errores. |
| `Samples/` y `Docs/` | Programas de ejemplo y documentación del lenguaje. |

## 11. Ejemplo para explicarlo en clase

```banana
start
  x = 2 + 3 * 4
  if x > 10 then
    write x
  endif
end
```

1. **Léxico:** reconoce `start` como `PR16`, `x` como `ID`, `=` como `OPA`, `2` como `CN`, `+` como `OA1`, etc. En la pantalla el identificador puede verse como `ID1`; internamente su categoría sigue siendo `ID`.
2. **Sintáctico:** reconoce un programa con una asignación y un `if`. La expresión queda agrupada como `2 + (3 * 4)`.
3. **Semántico:** deduce que la expresión de `x` es entera y que `x > 10` produce un booleano, válido como condición.
4. **Símbolos:** registra `x` en el ámbito global con tipo entero y dirección simulada.
5. **Resultados:** genera el árbol y la traza de reducciones. **No calcula `x = 14`, no decide si entra al `if` y no ejecuta `write`.**

## 12. Guion corto para exponer

> “Nuestro proyecto es un analizador educativo para el lenguaje Banana, hecho en C# con .NET y Windows Forms. Primero usa un autómata finito determinista, cuya tabla se carga desde SQLite, para convertir caracteres en tokens. Después, un parser descendente recursivo verifica esos tokens con una gramática libre de contexto y construye un árbol sintáctico. Durante ese proceso se validan tipos y reglas como el uso de break dentro de ciclos, mediante atributos y una pila semántica. Además, una tabla de símbolos maneja ámbitos y simula direcciones de memoria. Finalmente, la interfaz muestra tokens, símbolos y errores. Su alcance es analizar el programa, no ejecutarlo ni generar código máquina.”

### Preguntas frecuentes

- **¿Por qué no basta con un AFD?** Porque clasifica lexemas, pero los paréntesis y bloques con anidamiento arbitrario requieren memoria adicional; por eso se utiliza la gramática y el parser recursivo.
- **¿Qué diferencia hay entre sintaxis y semántica?** La sintaxis comprueba la forma; la semántica comprueba tipos y restricciones de contexto.
- **¿Se usan expresiones regulares para analizar el texto?** No como motor de reconocimiento en esta implementación. Las categorías regulares se reconocen mediante la tabla de estados y rutinas auxiliares.
- **¿SQLite guarda los programas o las variables?** En este flujo se utiliza para cargar el autómata; los símbolos se mantienen en memoria durante el análisis.
- **¿Es un compilador completo?** No. Implementa su front-end educativo; no hay generación de código ni ejecución del lenguaje Banana.

Para más detalle: [gramática](Grammar.md), [árbol y símbolos](SyntaxTree.md) y [diagnósticos](Diagnostics.md).
