# Regenerar el reporte de Word

Desde la raíz de BananaCompiler, ejecutar en PowerShell:

```powershell
py -3 scripts/convertir_reporte.py
```

Se generan `Reporte.docx`, `Reporte.verificacion.json` y los diagramas en
`Docs/Word/diagramas/`. El archivo `Reporte.md` no se modifica.

## Requisitos

- Python 3. No requiere paquetes adicionales de Python.
- [Pandoc](https://pandoc.org/installing.html).
- [Node.js](https://nodejs.org/) y Mermaid CLI:

```powershell
npm install -g @mermaid-js/mermaid-cli
```

Si Mermaid CLI indica que falta el navegador de Puppeteer, seguir la instalación
del navegador solicitada por esa versión de CLI. También puede proporcionarse
`--puppeteer-config archivo.json` con `executablePath` de un navegador compatible.

El script busca Pandoc en el PATH y, en Windows, también en
`%LOCALAPPDATA%\Pandoc` y `%ProgramFiles%\Pandoc`. Se pueden especificar rutas:

```powershell
py -3 scripts/convertir_reporte.py --pandoc "C:\ruta\pandoc.exe" --mmdc "C:\ruta\mmdc.cmd"
```

## Plantilla de Word opcional

```powershell
py -3 scripts/convertir_reporte.py --reference-doc "MiPlantilla.docx"
```

La plantilla debe ser un `.docx` de referencia para Pandoc. Se conservan sus
estilos tipográficos y márgenes; el contenido de la plantilla no se inserta en
el reporte. Las reglas solicitadas de tablas, código, justificación y ajuste de
imágenes se aplican igualmente. Sin plantilla se usa papel Carta, Arial 12,
interlineado 1.5 y márgenes de una pulgada, con títulos de tamaño jerarquizado.

## Otro archivo de entrada o salida

```powershell
py -3 scripts/convertir_reporte.py Reporte.md --output Reporte.docx
```

## Diagramas y capturas pendientes

- `figura-01.mmd`, `figura-02.mmd` y `figura-03.mmd` conservan las fuentes Mermaid.
- Los archivos `.render.mmd` contienen el mismo grafo y texto con etiquetas que
  permiten ajustar líneas para mejorar la legibilidad. También son editables.
- Los `.png` se generan mediante Mermaid CLI y se incorporan al Word. No se
  vinculan como recursos externos; el Word puede distribuirse por separado.
- `mermaid-config.json` permite cambiar el aspecto de los diagramas.
- Para cambios permanentes, editar los bloques Mermaid de `Reporte.md`, ya que
  el script vuelve a extraerlos al regenerar el documento.
- Los marcadores visibles `[INSERTAR CAPTURA ...]` se conservan. Para agregar
  capturas, reemplazarlos en el Markdown por imágenes, por ejemplo
  `![Captura del programa](imagenes/figura-04-tipos-direcciones.png)`, y conservar
  el pie de figura existente. Los ejemplos dentro de comentarios HTML no se
  incorporan al Word hasta quitar los delimitadores del comentario.

## Comprobaciones automáticas

Después de convertir con Pandoc, el script ajusta el OOXML del `.docx` y verifica:

- Bordes exteriores e interiores y ancho total de cada tabla y sus celdas.
- Texto de las tablas alineado a la izquierda y espacio posterior.
- Bloques de código idénticos al original, incluida la indentación, en Consolas
  10, con fondo gris, interlineado sencillo, alineación izquierda y espacio posterior.
- Los PNG Mermaid efectivamente incorporados, sus proporciones y el ajuste a
  los márgenes de todas las imágenes.
- Cantidad de títulos, enlaces, pies de figura y marcadores pendientes.
- Texto completo y listas, incluida su numeración, mediante una segunda lectura
  del Word con Pandoc. Se excluyen únicamente las fuentes Mermaid, sustituidas
  por sus PNG incorporados.
- Arial 12, interlineado 1.5 y márgenes de una pulgada cuando no hay plantilla.

El resumen se guarda en `Reporte.verificacion.json`. Si una comprobación falla,
el script termina con un error antes de reemplazar el Word de salida. La revisión
de OOXML no sustituye una revisión visual en Word cuando se cambia el contenido,
la plantilla o las imágenes.
