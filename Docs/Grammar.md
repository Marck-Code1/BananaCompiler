# Gramática

## Símbolo inicial

```text
S -> INICIO

INICIO -> PR16 INS PR5
```

## Lista de instrucciones

```text
INS -> IN INS
INS -> ε
```

## Instrucciones

```text
IN -> ASSIGN
IN -> WRITE
IN -> READ
IN -> IF
IN -> WHILE
IN -> FOR
IN -> DO
IN -> RAND
IN -> MOVE
IN -> BREAK
IN -> CLEAR
```

## Producciones de las instrucciones

```text
ASSIGN -> ID OPA EXP

WRITE -> PR21 EXP

READ -> PR15 ID

IF -> PR12 EXP PR17 INS IF_T

IF_T -> PR7
IF_T -> PR4 INS PR7

WHILE -> PR20 EXP PR3 INS PR8

FOR -> PR10 ID PR11 EXP PR18 EXP PR3 INS PR6

DO -> PR3 INS PR5 PR20 EXP

RAND -> PR14 ID CE7 EXP

MOVE -> PR13 EXP CE7 EXP

BREAK -> PR1

CLEAR -> PR2
```

## Expresiones

### Expresión general

```text
EXP -> EXP_OR
```

### OR lógico

```text
EXP_OR -> EXP_AND EXP_OR_T

EXP_OR_T -> OL2 EXP_AND EXP_OR_T
EXP_OR_T -> ε
```

### AND lógico

```text
EXP_AND -> EXP_REL EXP_AND_T

EXP_AND_T -> OL1 EXP_REL EXP_AND_T
EXP_AND_T -> ε
```

### Expresiones relacionales

```text
EXP_REL -> EXP_NOT EXP_REL_T

EXP_REL_T -> OPR EXP_NOT
EXP_REL_T -> ε
```

### Negación lógica

```text
EXP_NOT -> OL3 EXP_NOT
EXP_NOT -> EXP_ARIT
```

### Expresiones aritméticas

```text
EXP_ARIT -> TERMINO EXP_ARIT_T

EXP_ARIT_T -> OA1 TERMINO EXP_ARIT_T
EXP_ARIT_T -> OA2 TERMINO EXP_ARIT_T
EXP_ARIT_T -> ε
```

### Términos

```text
TERMINO -> UNARIO TERMINO_T

TERMINO_T -> OA3 UNARIO TERMINO_T
TERMINO_T -> OA4 UNARIO TERMINO_T
TERMINO_T -> OA6 UNARIO TERMINO_T
TERMINO_T -> ε
```

### Signos unarios

```text
UNARIO -> OA1 UNARIO
UNARIO -> OA2 UNARIO
UNARIO -> POTENCIA
```

### Potencia

```text
POTENCIA -> FACTOR POTENCIA_T

POTENCIA_T -> OA5 UNARIO
POTENCIA_T -> ε
```

La recursión de `POTENCIA` hace que `^` sea asociativo por la derecha:

```text
a ^ b ^ c
```

se interpreta como:

```text
a ^ (b ^ c)
```

## Expresiones primarias

```text
FACTOR -> ID
FACTOR -> CN
FACTOR -> CNR
FACTOR -> CNE
FACTOR -> CAD
FACTOR -> BOOL
FACTOR -> CE5 EXP CE6
```

## Valores booleanos

```text
BOOL -> PR19
BOOL -> PR9
```

## Operadores relacionales

```text
OPR -> OR1
OPR -> OR2
OPR -> OR3
OPR -> OR4
OPR -> OR5
OPR -> OR6
```

## Tabla de precedencia resultante

De mayor a menor precedencia:

| Nivel | Operadores                       | Asociatividad  |
| ----: | -------------------------------- | -------------- |
|     1 | `()`                             | Agrupación     |
|     2 | `^`                              | Derecha        |
|     3 | `+`, `-` unarios                 | Derecha        |
|     4 | `*`, `/`, `%`                    | Izquierda      |
|     5 | `+`, `-` binarios                | Izquierda      |
|     6 | `!`                              | Derecha        |
|     7 | `<`, `<=`, `>`, `>=`, `==`, `!=` | Izquierda      |
|     8 | `&&`                             | Izquierda      |
|     9 | `\|\|`                           | Izquierda      |

Por ejemplo:

```text
a + b * c < 10 && !x || y
```

queda estructuralmente como:

```text
((a + (b * c)) < 10 && (!x)) || y
```

## Tokens

En esta gramática, los tokens representan los símbolos terminales. El parser no analiza el código fuente directamente; recibe la secuencia de tokens generada previamente por el analizador léxico.

Las constantes numéricas no incluyen signo. `+` y `-` siempre se tokenizan como
`OA1` y `OA2`, respectivamente; el parser determina si funcionan como
operadores binarios o como signos unarios.

La gramática solo determina si una expresión está bien formada. Restricciones como
usar booleanos con `&&`, números con `+` o `break` únicamente dentro de un ciclo
pertenecen al análisis semántico. El parser ejecuta esas acciones durante el
reconocimiento mediante una pila semántica. Cada reducción produce un valor de
expresión tipado; las instrucciones se conservan en `ParseResult.SyntaxTree`,
incluyendo nodos `ERROR` y `MISSING` cuando hubo recuperación.

## Pila semántica

Durante el reconocimiento de cada expresión, el parser mantiene una pila de
valores semánticos. Los factores apilan su tipo y valor; cada operador extrae
sus operandos, valida la combinación de tipos y apila el resultado de la
reducción. Las reducciones producen los tipos `Entero`, `Decimal`,
`Exponencial`, `string` o `bool`; un identificador cuyo tipo todavía no se
conoce se conserva como valor desconocido para permitir su uso posterior.

La pila se vacía al terminar cada expresión y se restaura si la expresión
contiene un error sintáctico. El resultado del parser expone los diagnósticos
semánticos en `ParseResult.SemanticErrors`. Entre las restricciones validadas
están los operandos booleanos de `&&`, `||` y `!`, los operandos numéricos de
las operaciones aritméticas, las condiciones booleanas y el uso de `break`
dentro de ciclos.

Los diagnósticos y la recuperación de instrucciones y bloques se describen en
[Diagnostics.md](Diagnostics.md). La recuperación no convierte un programa
con errores en uno válido ni hace que los saltos de línea terminen instrucciones.

```text
ID -> identificador
CN -> constante_numérica_entera
CNR -> constante_numérica_real
CNE -> constante_numérica_exponencial
CAD -> cadena

OA1 -> +
OA2 -> -
OA3 -> *
OA4 -> /
OA5 -> ^
OA6 -> %

OR1 -> <
OR2 -> <=
OR3 -> >
OR4 -> >=
OR5 -> ==
OR6 -> !=

OL1 -> &&
OL2 -> ||
OL3 -> !

OPA -> =

CE5 -> (
CE6 -> )
CE7 -> ,

PR1 -> break
PR2 -> clear
PR3 -> do
PR4 -> else
PR5 -> end
PR6 -> endfor
PR7 -> endif
PR8 -> endwhile
PR9 -> false
PR10 -> for
PR11 -> from
PR12 -> if
PR13 -> move
PR14 -> rand
PR15 -> read
PR16 -> start
PR17 -> then
PR18 -> to
PR19 -> true
PR20 -> while
PR21 -> write
```
