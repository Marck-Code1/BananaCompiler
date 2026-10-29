# Tecnológico Nacional de México

# Instituto Tecnológico De Nuevo Laredo

&nbsp;

## Reporte de generación de código intermedio Banana

&nbsp;

Cesar Alejandro Vázquez Soto 23100218

Marco Antonio Celis Mata 23100147

&nbsp;

Ing. Sistemas Computacionales

Lenguajes y autómatas 2

&nbsp;

06

Octubre Del 2026

&nbsp;

---

## Índice de contenido

Introducción 3

Objetivo 3

Antecedentes 3

Del análisis semántico al código intermedio 3

Funcionamiento del programa 4

Visualización del AST y notaciones 4

Variables temporales y etiquetas 5

Cuádruplos y código de tres direcciones (TAC) 5

Triplos 6

Código P y la máquina de pila 7

Diagramas 8

Flujo de la generación de código intermedio 8

De un AST validado a cuatro representaciones 8

Resultados 9

Evidencias del funcionamiento 9

Evidencia 1 Visualización del AST y notaciones 10

Evidencia 2 Cuádruplos y código de tres direcciones 11

Evidencia 3 Triplos 12

Evidencia 4 Código P 13

Evidencia 5 Pila semántica 14

Conclusiones 15

&nbsp;

---

## Introducción

BananaCompiler/Banana empezó como un programa que revisaba la estructura de las instrucciones del lenguaje Banana, y después se modificó para agregar el análisis semántico. Con esta nueva ampliación, una vez que el análisis semántico valida los tipos y las variables, el programa aplana el Árbol de Sintaxis Abstracta (AST) para generar código intermedio. Está desarrollado en C# y utiliza Windows Forms para escribir o cargar programas y mostrar los resultados.

En este reporte explicamos cómo se construyen cuatro representaciones lineales e independientes de la máquina final: las notaciones prefija, infija y postfija de las expresiones, los cuádruplos con su código de tres direcciones, los triplos y el código P de una máquina de pila. También se muestra cómo el programa inyecta variables temporales y etiquetas para los saltos, y cómo la pila semántica registra las reducciones que ocurren durante el análisis.

## Objetivo

El objetivo es explicar de forma entendible cómo BananaCompiler transforma el árbol sintáctico ya validado en cuatro representaciones lineales del programa, mostrando qué recorre cada generador y los resultados que aparecen en las pestañas de la aplicación al analizar diferentes instrucciones.

## Antecedentes

### Del análisis semántico al código intermedio

En la fase anterior BananaCompiler podía revisar los tipos de las expresiones y el uso de las variables. Por ejemplo, ante la instrucción:

```
start
  resultado = "hola" + 2
end
```

el programa señala que el operador + de Banana solo acepta números. Cuando las instrucciones son correctas, el parser entrega en ParseResult un árbol sintáctico que ya comprobó tipos, ámbitos y condiciones.

Para esta fase ese árbol validado se utiliza como una estructura de solo lectura. Los generadores NotacionesGenerator, CuadruplosGenerator, TriplosGenerator y PCodeGenerator no modifican el árbol: solo lo recorren de distintas formas para construir cada representación. Cada vez que cambia el texto del editor se vuelve a analizar el programa y se actualizan todas las vistas.

## Funcionamiento del programa

Después de la ampliación, BananaCompiler puede mostrar el mismo programa validado como árbol, como tres notaciones de expresiones, como cuádruplos con su código de tres direcciones, como triplos y como código P. Las siguientes partes trabajan durante el análisis del programa escrito en el editor.

### Visualización del AST y notaciones

La pestaña Árbol y Notaciones muestra el árbol sintáctico completo, que conserva elementos de la gramática, tokens y producciones vacías, y a la derecha tres notaciones obtenidas de recorrer las expresiones:

- **Prefija (recorrido en preorden):** el operador aparece antes que sus operandos. Para la expresión x = 2 + 3 * 4 se obtiene = x + 2 * 3 4. Este recorrido se forma directamente de la jerarquía del árbol, por lo que no requiere paréntesis.
- **Infija (recorrido en inorden):** es la forma como se escribe normalmente la expresión, x = 2 + 3 * 4. Como el recorrido lineal no conserva la agrupación del árbol, el generador inserta paréntesis automáticos comparando la precedencia de los operadores vecinos; los paréntesis que escribió el usuario no se copian, se vuelven a calcular solo cuando son necesarios.
- **Postfija (recorrido en postorden):** los operandos aparecen primero y el operador al final, x 2 3 4 * + =. Para las asignaciones el símbolo = se coloca al terminar la expresión del lado derecho.

Las tres notaciones comparten un mismo contador de variables temporales (t1, t2, …). Las condiciones de if, while, do y los límites de for se muestran a través de una temporal, por ejemplo:

```
x = 5
if Condición (t1) = !(x < 10) then
  write x
endif
```

Con esto se puede ver en qué momento el programa evalúa la condición que después generará los saltos.

### Variables temporales y etiquetas

Cuando los generadores convierten las expresiones a código lineal, no pueden representar una operación compuesta en una sola instrucción. Por eso inyectan variables temporales lógicas (T1, T2, … en cuádruplos y código de tres direcciones, t1, t2, … en las notaciones) para guardar resultados parciales, y etiquetas (L1, L2, …) para marcar los destinos de los saltos.

Cada operación aritmética evalúa primero sus operandos y guarda su resultado en una nueva temporal; si el programa necesita otra temporal más adelante, el contador continúa con el siguiente número. Las etiquetas se numeran en el orden en que se crean y cada estructura de control usa dos: una para el inicio o la condición y otra para la salida del ciclo o del bloque. Por ejemplo, un while coloca una etiqueta antes de la condición y otra después del cuerpo, con el salto de regreso a la primera.

### Cuádruplos y código de tres direcciones (TAC)

Un cuádruplo es una instrucción de cuatro campos: Operador, Arg1, Arg2 y Resultado. La pestaña Cuádruplos y Código 3D muestra la secuencia en una tabla y, al mismo tiempo, su traducción como sentencias lineales de máximo tres direcciones. Para la asignación x = 15 + 15 * 2 - 10 el programa genera:

| # | Operador | Arg1 | Arg2 | Resultado |
|---|----------|------|------|-----------|
| 1 | = | 5 | | x |
| 2 | * | 15 | 2 | T1 |
| 3 | + | 15 | T1 | T2 |
| 4 | - | T2 | 10 | T3 |
| 5 | = | T3 | | sum |

Las instrucciones que reciben el resultado de la operación utilizan su tercer campo para el destino; los cuádruplos que no tienen un segundo operando, como la asignación simple o los signos unarios, dejan Arg2 vacío, y los saltos, las lecturas y las etiquetas lo rellenan con el comodín "_". En el código de tres direcciones cada cuádruplo ocupa una línea:

```
x = 5
T1 = 15 * 2
T2 = 15 + T1
T3 = T2 - 10
sum = T3
```

Las condiciones se traducen con un salto condicional que apunta a una etiqueta:

```
ifFalse T1 goto L1
...
L1:
```

Todas las variables y temporales aparecen por su nombre, nunca como direcciones de memoria, lo que mantiene el código intermedio independiente de la máquina final.

### Triplos

Los triplos son otra representación lineal de tres campos, pero a diferencia de los cuádruplos no utilizan variables temporales. El resultado de cada operación queda identificado únicamente por la posición numérica de la instrucción, que los operadores posteriores citan como argumento entre paréntesis; por ejemplo, (0) hace referencia al resultado de la instrucción 0. Para la misma expresión x = 2 + 3 * 4:

| # | Triplo |
|---|--------|
| 0 | ( * , 3 , 4 ) |
| 1 | ( + , 2 , (0) ) |
| 2 | ( = , x , (1) ) |

Este formato ahorra los nombres de las temporales, porque cada instrucción es su propio resultado. Como contraparte, cualquier cambio en el orden de las instrucciones obliga a renumerar las referencias, ya que los argumentos apuntan a posiciones y no a nombres.

### Código P y la máquina de pila

El código P simula una máquina de pila: los operandos se colocan en la pila y las operaciones retiran sus valores y depositan el resultado. La pestaña Código P muestra las instrucciones estándar del conjunto didáctico:

- **LDC** carga una constante en la pila; **LOD** carga el valor de una variable; **STO** almacena el tope de la pila en una variable.
- **ADD, SUB, MUL, DIV, MOD, PWR** ejecutan la aritmética con los dos valores superiores de la pila; **EQU, NEQ, LT, LEQ, GTR, GEQ** calculan las comparaciones.
- **JMP** es el salto incondicional a una etiqueta y **JPC** el salto condicional que se ejecuta cuando el valor booleano de la pila es falso; **LAB** marca la posición de la etiqueta.

Para soportar las instrucciones nativas del lenguaje Banana se crearon instrucciones extendidas personalizadas: **INP** (lectura de una variable con read), **OUT** (escritura con write), **RND** (número aleatorio con rand), **MOV** (desplazamiento con move) y **CLR** (limpieza de pantalla con clear). Para la asignación x = 2 + 3 * 4 el programa genera:

```
LDC 2
LDC 3
LDC 4
MUL
ADD
STO x
```

La pila también funciona durante el análisis: la pestaña Pila Semántica presenta el registro que el analizador guarda en cada reducción, con la línea donde ocurrió, la operación aplicada y su precedencia y asociabilidad, por ejemplo L3: x * 2 => Entero [op '*' nivel=5 assoc=izquierda].

## Diagramas

Los siguientes diagramas muestran el recorrido de la generación de código intermedio y la forma en que un mismo árbol alimenta a las cuatro representaciones.

### Flujo de la generación de código intermedio

[Insertar diagrama]

Figura 1. Recorrido de BananaCompiler desde el programa escrito en el editor hasta la presentación de notaciones, cuádruplos, triplos, código P y pila semántica en la interfaz.

### De un AST validado a cuatro representaciones

[Insertar diagrama]

Figura 2. El árbol validado se recorre en preorden, inorden y postorden para las notaciones, y se aplana en orden de ejecución para los cuádruplos, los triplos y el código P.

## Resultados

### Evidencias del funcionamiento

Las siguientes capturas muestran los resultados de BananaCompiler al analizar distintos programas. En ellas se pueden observar las notaciones de las expresiones, las variables temporales, las etiquetas de los saltos y las instrucciones de cada representación, a partir del mismo árbol validado.

### Evidencia 1 Visualización del AST y notaciones

[Insertar captura de la pestaña Árbol y Notaciones]

Figura 3. El programa muestra el árbol sintáctico del programa validado y, al analizar una expresión aritmética, las notaciones prefija, infija y postfija obtenidas de sus recorridos en preorden, inorden y postorden con paréntesis automáticos.

### Evidencia 2 Cuádruplos y código de tres direcciones

[Insertar captura de la pestaña Cuádruplos y Código 3D]

Figura 4. La tabla presenta los cuádruplos con sus campos Operador, Arg1, Arg2 y Resultado, y el panel derecho traduce la misma secuencia como código de tres direcciones con temporales T1, T2, … y etiquetas L1, L2, … para los saltos.

### Evidencia 3 Triplos

[Insertar captura de la pestaña Triplos]

Figura 5. La representación de triplos muestra las mismas operaciones sin variables temporales; los resultados se referencian mediante el índice numérico de la instrucción, como (0) y (1).

### Evidencia 4 Código P

[Insertar captura de la pestaña Código P]

Figura 6. La máquina de pila carga constantes y variables con LDC y LOD, almacena con STO y resuelve las condiciones con JMP y JPC; las instrucciones extendidas INP, OUT, RND, MOV y CLR aparecen en los programas que utilicen read, write, rand, move y clear.

### Evidencia 5 Pila semántica

[Insertar captura de la pestaña Pila Semántica]

Figura 7. El registro muestra una reducción por línea con la instrucción que la generó, la línea del programa, la operación aplicada y su precedencia y asociabilidad, que es la información que utiliza el análisis semántico antes de generar el código intermedio.

## Conclusiones

Con la ampliación de BananaCompiler un solo análisis semántico alimenta simultáneamente cuatro representaciones lógicas y lineales del código. Las notaciones muestran la estructura de las expresiones desde tres puntos de vista, los cuádruplos y el código de tres direcciones aplanan el programa en orden de ejecución, los triplos comprueban que la misma secuencia se puede escribir referenciando los resultados por posición, y el código P lo prepara para una máquina de pila.

Las variables temporales y las etiquetas permiten representar en formato lineal lo que en el árbol era jerárquico: cada operación guarda su resultado en una temporal y cada condición se convierte en saltos hacia etiquetas numeradas. Como todos los generadores recorren el árbol sin modificarlo, la validación de las fases anteriores se mantiene intacta y las vistas se actualizan en conjunto con cada nuevo análisis.

En las capturas podemos ver que las cuatro representaciones coinciden sobre el mismo programa, cada una con su propio formato pero con el mismo orden de operaciones. Este resultado deja preparado el terreno para una futura generación de código objeto: el código intermedio ya está en formato lineal y nombrado, por lo que las siguientes fases pueden traducirlo a instrucciones de la máquina final sin volver a validar el programa.
