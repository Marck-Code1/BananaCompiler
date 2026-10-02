# Árbol de Sintaxis, Pila Semántica, Ámbitos y Memoria Simulado

Documento del proceso realizado en `BananaCompiler` para:

1. Agregar un **Syntax Tree** al `TopDownParser`.
2. Agregar una **Pila Semántica** (didáctica) para la jerarquía matemática.
3. Simular la **dirección real de memoria** de las variables en el `DataGridView` de símbolos.
4. Agregar el **ámbito (scope)** de las variables en la tabla de símbolos.

Fuentes usadas: `Docs/Grammar.md` (gramática, precedencia, pila semántica),
`Parser/TopDownParser.cs`, `Parser/SemanticStack.cs`, `Parser/SemanticValue.cs`,
`Parser/SymbolAssignment.cs`, `Parser/ParseResult.cs`, `Models/Token.cs`,
`Models/TokenKinds.cs`, `Models/Identifier.cs`, `Form1.cs` y
`Form1.Designer.cs`.

---

## 1. Syntax Tree en el `TopDownParser`

### Por qué
`Grammar.md` decía explícitamente que *"las instrucciones no se conservan en un
AST completo"*: el parser solo validaba y tipaba expresiones con una pila, y
desechaba la estructura. Eso impide visualizar la jerarquía real
(`a + b * c` → `a + (b * c)`), depurar errores, y a futuro generar código o
evaluar. El árbol conserva cada derivación aplicada.

### Qué se hizo
- **Nuevo archivo `Parser/SyntaxNode.cs`.** Nodo con:
  - `Kind`: símbolo gramatical (`S`, `INICIO`, `INS`, `ASSIGN`, `IF`, `EXP_OR`,
    `TERMINO`, `FACTOR`, `BOOL`, …) o kind de token en hojas (`ID`, `CN`, `OA1`, …).
  - `Token`: token origen (solo hojas), con línea/columna.
  - `Children`: hijos en orden de derivación; `ε` explícito cuando la
    producción vacía aplica (fidelidad con la gramática).
  - `Semantic`: `SemanticValue` inferido (solo nodos `EXP*`), para ver el tipo
    de cada subexpresión directamente en el árbol.
  - `ToTreeString()` (indentado) y `ToCompact()` (estilo Lisp).
- **`Parser/TopDownParser.cs` reescrito por dentro, misma semántica por fuera:**
  cada `Parse*` ahora devuelve su `SyntaxNode` además del `SemanticValue`:
  `ParseInstructions` → `INS`, `ParseAssignment` → `ASSIGN` (`ID OPA EXP`),
  `ParseIf` → `IF`, `ParseWhile` → `WHILE`, `ParseFor` → `FOR`,
  `ParseDoWhile` → `DO`, `ParseRand` → `RAND`, `ParseMove` → `MOVE`,
  `BREAK`/`CLEAR` hojas, y toda la cadena `EXP → EXP_OR → … → FACTOR` con sus
  nodos cola (`EXP_OR_T`, `EXP_ARIT_T`, `TERMINO_T`, `POTENCIA_T`, `EXP_REL_T`).
  En recuperación de errores se inserta un nodo `ERROR` con el token
  problemático para no romper la secuencia.
  - La raíz queda en `TopDownParser.SyntaxRoot` (`S → INICIO → start INS end`)
    y viaja en `ParseResult.SyntaxTree`.
  - Los chequeos semánticos (`RequireBoolean`, `RequireNumeric`, `break` en
    ciclo, tipos de operandos) **no cambiaron**: el árbol se construye en
    paralelo a la pila real, nunca en su lugar.

### Uso
```csharp
var parser = new TopDownParser(lexicalTokens);
ParseResult result = parser.Parse();
Console.WriteLine(result.SyntaxTree?.ToTreeString()); // árbol indentado
Console.WriteLine(result.SyntaxTree?.ToCompact());     // (ASSIGN ID('a) …)
```
El reporte del formulario muestra `Árbol: raíz S (N nodos)`.

### Ejemplo (`total = total + i` dentro de un `for`)
```
ASSIGN
+- ID 'total'
+- OPA '='
+- EXP [Entero]
   +- EXP_OR [Entero]
      +- EXP_AND [Entero]
         +- EXP_REL [Entero]
            +- EXP_NOT [Entero]
               +- EXP_ARIT [Entero]        <- nivel +,- binario
                  +- TERMINO [Entero]     <- left: total
                  +- EXP_ARIT_T
                     +- OA1 '+'
                     +- TERMINO [Entero]  <- right: i
```
Se ve la jerarquía: `+` une dos `TERMINO`, y `*` (si existiera) quedaría
anidado **dentro** de un `TERMINO`, es decir, más profundo = mayor precedencia.

---

## 2. Pila Semántica (jerarquía matemática)

### Por qué
La pila real (`SemanticStack`) es interna y se vacía tras cada expresión: no se
puede observar *en qué orden* se redujo `a + b * c ^ d` ni con qué nivel de
precedencia. Para enseñar/depurar la jerarquía hacía falta una pila visible.

### Qué se hizo
- **Nuevo archivo `Parser/SemanticPile.cs`** (independiente de
  `TopDownParser.cs`, salvo que el parser la alimenta; no altera el tipado).
  - **Tabla de jerarquía** `PrecedenceOf()` / `AssociativityOf()`, copiada de
    `Grammar.md` § precedencia. Acepta kind (`OA1`, `OR1`) **y** lexema
    (`+`, `<`) porque la traza registra lexemas legibles:
    | Nivel | Operadores | Assoc. |
    |---|---|---|
    | 7 | `^` | derecha |
    | 5 | `*`, `/`, `%` | izquierda |
    | 4 | `+`, `-` binarios | izquierda |
    | 3 | `!` | derecha |
    | 2 | `<`, `<=`, `>`, `>=`, `==`, `!=` | izquierda |
    | 1 | `&&` | izquierda |
    | 0 | `\|\|` | izquierda |
    (unarios `+`/`-` y paréntesis se modelan en el árbol, no en la tabla).
  - **Traza runtime**: `PushOperand`, `ReduceUnary`, `ReduceBinary` registran
    `"{izq} {op} {der} => {tipo} [op … nivel=N assoc=…]"` más fotos de la pila
    (`Snapshots`) y forma postfija (`ToPostfix()`).
  - En el parser, `ReduceBinaryTraced` / `ReduceUnaryTraced` capturan las
    etiquetas de la pila didáctica **antes** de la reducción real y vuelcan el
    resultado después; si la reducción falla (tipo inválido) la pila didáctica
    se restaura igual que la real. `ParseExpressionWithTree` vacía la pila por
    expresión (como la real) y acumula el segmento en
    `TopDownParser.SemanticPileLog` / `ParseResult.SemanticPileLog`.
- Lo ya existente (`SemanticStack` + `SemanticValue` + `Grammar.md`
  § "Pila semántica") se conservó intacto como mecanismo de tipado; la nueva
  pila es solo observabilidad.

### Uso
```csharp
foreach (string step in result.SemanticPileLog)
    Console.WriteLine(step);
// L3: total < 10 => bool  [op '<' nivel=2 assoc=izquierda]
// L2: b * c => Entero  [op '*' nivel=5 assoc=izquierda]
// L2: a + (b * c:Entero) => Entero  [op '+' nivel=4 assoc=izquierda]
```
El reporte muestra `Pila semántica: N reducciones`. El orden de las líneas
*es* la jerarquía: `*` (5) se reduce antes que `+` (4); `^` anida a la derecha.

---

## 3. Simulación de memoria real en la tabla de símbolos

### Por qué
La grilla solo mostraba `# / Nombre / Tipo / Valor`: sin dirección ni tamaño
es imposible razonar layout, alineación o fragmentación como en un compilador
real.

### Qué se hizo
- **`Parser/ScopedSymbolTable.SizeOf()`**: `Entero=4`, `Decimal=8`,
  `Exponencial=16`, `bool=1`, `string=2×caracteres+2` (UTF-16 + terminador),
  desconocido `=0` (reserva mínima de 4 igual, sin tipo aún).
- **Segmento simulado** desde `0x1000` con alineación a 4 bytes (regla x86
  simplificada): cada variable nueva recibe `dirección = base + offset
  alineado`; el offset avanza `max(tamaño, 4)`. Si el tipo se conoce después,
  se **conserva la dirección** y solo se actualiza el tamaño (documenta
  fragmentación/reasignación real).
- `SymbolAssignment` extendido (con defaults, compatible): `Scope`,
  `Address`, `Size` + `AddressHex` (`0x1004`).
- `Form1.Designer.cs`: columnas nuevas **Ámbito**, **Dirección**,
  **Tamaño (bytes)**; `Form1.cs` las llena y **ordena por dirección**
  (orden de memoria, no alfabético); `Models/Identifier.cs` replica los campos.

### Ejemplo
| Nombre | Tipo | Valor | Ámbito | Dirección | Tamaño |
|---|---|---|---|---|---|
| total | Entero | 0 | global | 0x1000 | 4 |
| tmp | Entero | 1 | global > while@3 | 0x1004 | 4 |

---

## 4. Ámbitos (scopes) en la tabla de símbolos

### Por qué
Todo vivía en un diccionario global plano: una `tmp` dentro de un `while` y
otra fuera colisionaban y no se sabía dónde se declaró cada variable.

### Qué se hizo
- **Nuevo `Parser/ScopedSymbolTable.cs`**: pila de ámbitos, raíz `global`;
  cada bloque apila `if@Línea`, `else@Línea`, `while@Línea`, `for@Línea`,
  `do@Línea` (se desapila con `try/finally`, incluso con errores). Cada símbolo
  guarda su ruta de declaración (`global > while@3`).
- **Visibilidad por prefijo + shadowing**: un identificador es visible si su
  ámbito es prefijo de la ruta actual; ante duplicados gana el más interno.
  `GetIdentifierValue` ahora busca así (tipado respeta scopes).
- Las instrucciones que introducen variables las **declaran en su ámbito**:
  asignación (`ASSIGN`), control del `for`, `read` y `rand` (estas últimas con
  tipo desconocido hasta que se les asigne).
- `ParseResult.ScopedSymbols` expone la lista en orden de memoria;
  `SymbolAssignments` (diccionario plano) se conservó para compatibilidad.

---

## 5. Verificación

- `dotnet build BananaCompiler.csproj`: **0 advertencias, 0 errores**.
- Arnés temporal (proyecto consola que compila solo `Models + Lexer
  (sin SQLite) + Parser`, 5 casos): precedencia en árbol, `^` asociativo a la
  derecha, ámbito+dirección de `tmp` en `while`, error semántico `1 && 2`
  preservado, variable de `for` con scope `for@L`. **5/5 PASS**.
- Casos límite conocidos: `string` mide por UTF-16 estimado; shadowing entre
  ramas hermanas crea entradas separadas (correcto); la dirección no se mueve
  al cambiar el tipo (documentado como fragmentación).

## 6. Archivos tocados/creados

| Archivo | Cambio |
|---|---|
| `Parser/SyntaxNode.cs` | **nuevo**: nodo del árbol + impresiones |
| `Parser/SemanticPile.cs` | **nuevo**: jerarquía + traza didáctica |
| `Parser/ScopedSymbolTable.cs` | **nuevo**: ámbitos + memoria simulada |
| `Parser/TopDownParser.cs` | construye árbol, alimenta pila, maneja scopes |
| `Parser/ParseResult.cs` | porta `SyntaxTree`, `ScopedSymbols`, `SemanticPileLog` |
| `Parser/SymbolAssignment.cs` | + `Scope`, `Address`, `Size`, `AddressHex` |
| `Models/Identifier.cs` | + `Scope`, `Address`, `Size` |
| `Form1.cs` | columnas en grilla, orden por dirección, conteos en reporte |
| `Form1.Designer.cs` | columnas Ámbito / Dirección / Tamaño |
| `Docs/SyntaxTree.md` | este documento |
