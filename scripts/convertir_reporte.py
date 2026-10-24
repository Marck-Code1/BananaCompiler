#!/usr/bin/env python3
"""Convierte Markdown a DOCX con Pandoc/Mermaid y verifica el OOXML final.

Solo requiere Python 3 (biblioteca estandar), Pandoc y Mermaid CLI.
No modifica el Markdown original. Consulte Docs/Word/README.md.
"""

import argparse
import json
import os
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile


ROOT = Path(__file__).resolve().parents[1]
NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "wp": "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",
    "a": "http://schemas.openxmlformats.org/drawingml/2006/main",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
}
for prefix, uri in NS.items():
    ET.register_namespace(prefix, uri)


def tag(name):
    prefix, local = name.split(":")
    return f"{{{NS[prefix]}}}{local}"


def prop(parent, name, **attrs):
    child = parent.find(tag(name))
    if child is None:
        child = ET.SubElement(parent, tag(name))
    for key, value in attrs.items():
        child.set(tag("w:" + key), str(value))
    return child


def remove(parent, name):
    for child in parent.findall(tag(name)):
        parent.remove(child)


def command(name, explicit=None):
    if explicit:
        found = shutil.which(explicit) or (explicit if Path(explicit).is_file() else None)
    else:
        found = shutil.which(name + ".cmd") or shutil.which(name)
        if not found and name == "pandoc":
            for base in (os.getenv("LOCALAPPDATA"), os.getenv("ProgramFiles")):
                candidate = Path(base or "") / "Pandoc" / "pandoc.exe"
                if candidate.is_file():
                    found = str(candidate)
                    break
    if not found:
        hint = ("Instale Pandoc: https://pandoc.org/installing.html" if name == "pandoc"
                else "Instale Node.js y ejecute: npm install -g @mermaid-js/mermaid-cli")
        raise RuntimeError(f"No se encontro {name}. {hint}")
    return str(found)


def run(args, **kwargs):
    # Windows necesita cmd.exe para ejecutar los lanzadores .cmd de npm.
    if os.name == "nt" and Path(str(args[0])).suffix.lower() in (".cmd", ".bat"):
        args = [os.environ.get("COMSPEC", "cmd.exe"), "/d", "/s", "/c",
                subprocess.list2cmdline([str(arg) for arg in args])]
    return subprocess.run(args, check=True, **kwargs)


def read_package(path):
    with zipfile.ZipFile(path) as archive:
        return {name: archive.read(name) for name in archive.namelist()}


def write_package(path, entries):
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, data in entries.items():
            archive.writestr(name, data)


def xml_bytes(element):
    return ET.tostring(element, encoding="utf-8", xml_declaration=True)


def font_properties(parent, font, points):
    fonts = prop(parent, "w:rFonts", ascii=font, hAnsi=font, eastAsia=font, cs=font)
    for attribute in list(fonts.attrib):
        if "theme" in attribute.lower():
            del fonts.attrib[attribute]
    prop(parent, "w:sz", val=points * 2)
    prop(parent, "w:szCs", val=points * 2)


def canonical_order(root):
    # Word tolera algunas propiedades fuera de orden, pero se escribe OOXML ordenado.
    orders = {
        "pPr": "pStyle keepNext keepLines pageBreakBefore framePr widowControl numPr suppressLineNumbers pBdr shd tabs suppressAutoHyphens kinsoku wordWrap overflowPunct topLinePunct autoSpaceDE autoSpaceDN bidi adjustRightInd snapToGrid spacing ind contextualSpacing mirrorIndents suppressOverlap jc textDirection textAlignment textboxTightWrap outlineLvl divId cnfStyle rPr sectPr pPrChange",
        "rPr": "rStyle rFonts b bCs i iCs caps smallCaps strike dstrike outline shadow emboss imprint noProof snapToGrid vanish webHidden color spacing w kern position sz szCs highlight u effect bdr shd fitText vertAlign rtl cs em lang eastAsianLayout specVanish oMath rPrChange",
        "tblPr": "tblStyle tblpPr tblOverlap bidiVisual tblStyleRowBandSize tblStyleColBandSize tblW jc tblCellSpacing tblInd tblBorders shd tblLayout tblCellMar tblLook tblCaption tblDescription tblPrChange",
        "tcPr": "cnfStyle tcW gridSpan hMerge vMerge tcBorders shd noWrap tcMar textDirection tcFitText vAlign hideMark headers tcPrChange",
        "sectPr": "headerReference footerReference footnotePr endnotePr type pgSz pgMar paperSrc pgBorders lnNumType pgNumType cols formProt vAlign noEndnote titlePg textDirection bidi rtlGutter docGrid printerSettings sectPrChange",
        "style": "name aliases basedOn next link autoRedefine hidden uiPriority semiHidden unhideWhenUsed qFormat locked personal personalCompose personalReply rsid pPr rPr tblPr trPr tcPr tblStylePr",
    }
    for element in root.iter():
        local = element.tag.split("}")[-1]
        if local in orders:
            position = {name: index for index, name in enumerate(orders[local].split())}
            element[:] = sorted(element, key=lambda child: position.get(child.tag.split("}")[-1], 999))
        elif local in ("p", "r", "tbl", "tr", "tc"):
            first = tag("w:" + local + "Pr")
            element[:] = sorted(element, key=lambda child: 0 if child.tag == first else 1)


def configure_styles(entries, use_template):
    styles = ET.fromstring(entries["word/styles.xml"])
    if not use_template:
        defaults = prop(styles, "w:docDefaults")
        font_properties(prop(prop(defaults, "w:rPrDefault"), "w:rPr"), "Arial", 12)
        pp = prop(prop(defaults, "w:pPrDefault"), "w:pPr")
        prop(pp, "w:jc", val="both")
        prop(pp, "w:spacing", line=360, lineRule="auto", after=120)
    for style in styles.findall("w:style", NS):
        sid = style.get(tag("w:styleId"), "")
        pp = prop(style, "w:pPr") if style.get(tag("w:type")) == "paragraph" else None
        if not use_template:
            # Normalizar estilos evita fuentes tematicas heredadas de Pandoc.
            font_properties(prop(style, "w:rPr"), "Arial", 12)
            if pp is not None:
                prop(pp, "w:spacing", line=360, lineRule="auto", after=120)
                prop(pp, "w:jc", val="both")
            if sid.startswith("Heading") and pp is not None:
                level = int(sid[7:]) if sid[7:].isdigit() else 4
                font_properties(prop(style, "w:rPr"), "Arial", {1: 20, 2: 16, 3: 14}.get(level, 12))
                prop(prop(style, "w:rPr"), "w:b")
                prop(pp, "w:jc", val="left")
                prop(pp, "w:keepNext")
                prop(pp, "w:spacing", before=240, after=120, line=360, lineRule="auto")
            if sid == "Hyperlink":
                prop(prop(style, "w:rPr"), "w:color", val="0563C1")
                prop(prop(style, "w:rPr"), "w:u", val="single")
        if sid in ("SourceCode", "VerbatimChar"):
            font_properties(prop(style, "w:rPr"), "Consolas", 10)
        if sid == "SourceCode":
            prop(pp, "w:jc", val="left")
            prop(pp, "w:spacing", before=0, after=0, line=240, lineRule="auto")
            prop(pp, "w:shd", val="clear", color="auto", fill="F2F2F2")
    canonical_order(styles)
    entries["word/styles.xml"] = xml_bytes(styles)


def page_area(document, use_template):
    sections = document.findall(".//w:sectPr", NS)
    if not sections:
        raise RuntimeError("El DOCX no contiene propiedades de seccion.")
    areas = []
    for section in sections:
        margins = prop(section, "w:pgMar")
        size = prop(section, "w:pgSz")
        if not use_template:
            # Papel Carta; una pulgada son 1440 twips.
            prop(section, "w:pgSz", w=12240, h=15840)
            prop(section, "w:pgMar", top=1440, bottom=1440, left=1440, right=1440, gutter=0)
        width = int(size.get(tag("w:w"), "12240"))
        height = int(size.get(tag("w:h"), "15840"))
        areas.append((width - int(margins.get(tag("w:left"), "1440"))
                      - int(margins.get(tag("w:right"), "1440"))
                      - int(margins.get(tag("w:gutter"), "0")),
                      height - int(margins.get(tag("w:top"), "1440"))
                      - int(margins.get(tag("w:bottom"), "1440"))))
    return min(a[0] for a in areas), min(a[1] for a in areas)


def paragraph_text(paragraph):
    parts = []
    for node in paragraph.iter():
        if node.tag == tag("w:t"):
            parts.append(node.text or "")
        elif node.tag == tag("w:br"):
            parts.append("\n")
        elif node.tag == tag("w:tab"):
            parts.append("\t")
    return "".join(parts)


def spacer():
    paragraph = ET.Element(tag("w:p"))
    pp = prop(paragraph, "w:pPr")
    prop(pp, "w:spacing", before=0, after=0, line=240, lineRule="auto")
    font_properties(prop(prop(paragraph, "w:r"), "w:rPr"), "Arial", 12)
    return paragraph


def format_document(entries, use_template):
    configure_styles(entries, use_template)
    document = ET.fromstring(entries["word/document.xml"])
    width, height = page_area(document, use_template)
    body = document.find("w:body", NS)
    for paragraph in document.findall(".//w:p", NS):
        pp = prop(paragraph, "w:pPr")
        style = pp.find("w:pStyle", NS)
        sid = style.get(tag("w:val"), "") if style is not None else ""
        if sid == "SourceCode":
            prop(pp, "w:jc", val="left")
            prop(pp, "w:spacing", before=0, after=0, line=240, lineRule="auto")
            prop(pp, "w:shd", val="clear", color="auto", fill="F2F2F2")
            prop(pp, "w:ind", left=0, right=0, firstLine=0)
            # Los bloques cortos se mantienen juntos; un bloque muy largo puede dividirse.
            if paragraph_text(paragraph).count("\n") < 32:
                prop(pp, "w:keepLines")
            for run_node in paragraph.findall(".//w:r", NS):
                font_properties(prop(run_node, "w:rPr"), "Consolas", 10)
        elif sid.startswith("Heading") or sid in ("Title", "Subtitle"):
            prop(pp, "w:jc", val="left")
            prop(pp, "w:keepNext")
        elif paragraph.find(".//w:drawing", NS) is not None:
            prop(pp, "w:jc", val="center")
            prop(pp, "w:keepNext")
            prop(pp, "w:spacing", line=240, lineRule="auto", after=120)
        elif paragraph_text(paragraph).startswith("Figura "):
            prop(pp, "w:jc", val="left")
            prop(pp, "w:keepLines")
        else:
            prop(pp, "w:jc", val="both")
        if sid != "SourceCode" and paragraph.find(".//w:drawing", NS) is None and not use_template:
            prop(pp, "w:spacing", line=360, lineRule="auto", after=120)

    for table in document.findall(".//w:tbl", NS):
        tp = prop(table, "w:tblPr")
        prop(tp, "w:tblW", type="dxa", w=width)
        prop(tp, "w:jc", val="left")
        prop(tp, "w:tblInd", type="dxa", w=0)
        prop(tp, "w:tblLayout", type="fixed")
        remove(tp, "w:tblCellSpacing")
        borders = prop(tp, "w:tblBorders")
        for side in ("top", "left", "bottom", "right", "insideH", "insideV"):
            prop(borders, "w:" + side, val="single", sz=6, color="444444", space=0)
        margins = prop(tp, "w:tblCellMar")
        for side, value in (("top", 60), ("bottom", 60), ("left", 80), ("right", 80)):
            prop(margins, "w:" + side, type="dxa", w=value)
        grid = table.find("w:tblGrid", NS)
        if grid is None or not len(grid):
            raise RuntimeError("Tabla sin cuadricula: no se puede verificar su ancho.")
        old_widths = [int(col.get(tag("w:w"), "1")) for col in grid]
        widths = [round(width * value / sum(old_widths)) for value in old_widths]
        widths[-1] += width - sum(widths)
        for col, value in zip(grid, widths):
            col.set(tag("w:w"), str(value))
        for row in table.findall("w:tr", NS):
            prop(prop(row, "w:trPr"), "w:cantSplit")
            index = 0
            for cell in row.findall("w:tc", NS):
                cp = prop(cell, "w:tcPr")
                span_node = cp.find("w:gridSpan", NS)
                span = int(span_node.get(tag("w:val"), "1")) if span_node is not None else 1
                prop(cp, "w:tcW", type="dxa", w=sum(widths[index:index + span]))
                prop(cp, "w:vAlign", val="top")
                remove(cp, "w:tcBorders")
                for paragraph in cell.findall(".//w:p", NS):
                    pp = prop(paragraph, "w:pPr")
                    prop(pp, "w:jc", val="left")
                    prop(pp, "w:ind", left=0, right=0, firstLine=0)
                    remove(pp, "w:keepNext")
                    if not use_template:
                        prop(pp, "w:spacing", before=0, after=0, line=360, lineRule="auto")
                index += span

    # Limitar ambas dimensiones con el mismo factor conserva las proporciones.
    for drawing in document.findall(".//wp:inline", NS) + document.findall(".//wp:anchor", NS):
        extent = drawing.find("wp:extent", NS)
        cx, cy = int(extent.get("cx")), int(extent.get("cy"))
        factor = min(1.0, width * 635 / cx, (height - 720) * 635 / cy)
        target = (round(cx * factor), round(cy * factor))
        extent.set("cx", str(target[0]))
        extent.set("cy", str(target[1]))
        for image_extent in drawing.findall(".//a:xfrm/a:ext", NS):
            image_extent.set("cx", str(target[0]))
            image_extent.set("cy", str(target[1]))

    for index, child in reversed(list(enumerate(body))):
        style = child.find("w:pPr/w:pStyle", NS)
        is_code = style is not None and style.get(tag("w:val")) == "SourceCode"
        if child.tag == tag("w:tbl") or is_code:
            body.insert(index + 1, spacer())
    canonical_order(document)
    entries["word/document.xml"] = xml_bytes(document)
    return width, height


def png_dimensions(data):
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise RuntimeError("Mermaid CLI no genero un PNG valido.")
    return struct.unpack(">II", data[16:24])


def ast_text(value):
    """Texto visible; los diagramas se comparan como PNG, no como codigo Mermaid."""
    if isinstance(value, dict):
        kind = value.get("t")
        if kind == "Str":
            return value["c"]
        if kind in ("Space", "SoftBreak", "LineBreak"):
            return " "
        if kind == "Code":
            return value["c"][1]
        if kind == "CodeBlock":
            return "" if "mermaid" in value["c"][0][1] else value["c"][1]
        if kind == "Image":
            return ""
        return ast_text(value.get("c", []))
    if isinstance(value, list):
        return "".join(ast_text(item) for item in value)
    return ""


def list_shapes(value):
    result = []
    if isinstance(value, dict):
        if value.get("t") == "BulletList":
            result.append(["BulletList", len(value["c"])])
        if value.get("t") == "OrderedList":
            result.append(["OrderedList", value["c"][0][0], len(value["c"][1])])
        for item in value.values():
            result.extend(list_shapes(item))
    elif isinstance(value, list):
        for item in value:
            result.extend(list_shapes(item))
    return result


def wrap_mermaid_labels(source):
    # Las etiquetas Markdown de Mermaid permiten ajustar lineas sin cambiar texto/grafo.
    def replace(match):
        opening, text, closing = match.groups()
        text = text.strip('"')
        if "`" in text:
            return match.group(0)
        return opening + '"`' + text + '`"' + closing
    return re.sub(r"(\[|\{)([^\[\]{}\n]+)(\]|\})", replace, source)


def transform_ast(value, assets, cli, config, puppeteer, width, height, statistics):
    if isinstance(value, dict):
        if value.get("t") == "CodeBlock":
            attrs, code = value["c"]
            if "mermaid" in attrs[1]:
                number = len(statistics["diagrams"]) + 1
                source = assets / f"figura-{number:02d}.mmd"
                rendered_source = assets / f"figura-{number:02d}.render.mmd"
                image = assets / f"figura-{number:02d}.png"
                source.write_text(code + "\n", encoding="utf-8")
                rendered_source.write_text(wrap_mermaid_labels(code) + "\n", encoding="utf-8")
                args = [cli, "-i", str(rendered_source), "-o", str(image), "-c", str(config),
                        "-b", "white", "-s", "2"]
                cli_help = run([cli, "--help"], stdout=subprocess.PIPE).stdout.decode("utf-8")
                if "--size <size>" in cli_help:
                    args += ["--size", "2200"]
                else:
                    args += ["-w", "2200", "-H", "2200"]
                if puppeteer:
                    args += ["-p", str(puppeteer)]
                run(args)
                px, py = png_dimensions(image.read_bytes())
                inches = min(width / 1440, (height / 1440 - 1.0) * px / py)
                statistics["diagrams"].append({"source": str(source), "png": str(image),
                                                "pixels": [px, py]})
                return {"t": "Para", "c": [{"t": "Image", "c": [
                    ["", [], [["width", f"{inches:.4f}in"]]], [], [image.as_posix(), ""]]}]}
            statistics["code"].append(code)
        if value.get("t") == "Table":
            statistics["tables"] += 1
        if value.get("t") == "Header":
            statistics["headings"] += 1
        if value.get("t") == "Link":
            statistics["links"] += 1
        return {key: transform_ast(item, assets, cli, config, puppeteer, width, height, statistics)
                for key, item in value.items()}
    if isinstance(value, list):
        return [transform_ast(item, assets, cli, config, puppeteer, width, height, statistics)
                for item in value]
    return value


def verify(entries, width, height, statistics, use_template):
    doc = ET.fromstring(entries["word/document.xml"])
    errors = []

    def check(condition, message):
        if not condition:
            errors.append(message)

    tables = doc.findall(".//w:tbl", NS)
    check(len(tables) == statistics["tables"], "Cambio el numero de tablas.")
    for i, table in enumerate(tables, 1):
        check(table.find("w:tblPr/w:tblW", NS).get(tag("w:w")) == str(width),
              f"Tabla {i}: ancho incorrecto.")
        check(sum(int(c.get(tag("w:w"))) for c in table.findall("w:tblGrid/w:gridCol", NS)) == width,
              f"Tabla {i}: cuadricula no coincide con el ancho util.")
        for side in ("top", "left", "bottom", "right", "insideH", "insideV"):
            edge = table.find("w:tblPr/w:tblBorders/w:" + side, NS)
            check(edge is not None and edge.get(tag("w:val")) == "single",
                  f"Tabla {i}: falta borde {side}.")
        for paragraph in table.findall(".//w:p", NS):
            check(paragraph.find("w:pPr/w:jc", NS).get(tag("w:val")) == "left",
                  f"Tabla {i}: texto no alineado a izquierda.")
        for row in table.findall("w:tr", NS):
            check(sum(int(c.get(tag("w:w"))) for c in row.findall("w:tc/w:tcPr/w:tcW", NS)) == width,
                  f"Tabla {i}: anchos de celdas incorrectos.")

    code = [p for p in doc.findall(".//w:p", NS)
            if p.find("w:pPr/w:pStyle", NS) is not None
            and p.find("w:pPr/w:pStyle", NS).get(tag("w:val")) == "SourceCode"]
    check([paragraph_text(p) for p in code] == statistics["code"],
          "El contenido o la indentacion de los bloques de codigo cambio.")
    for i, paragraph in enumerate(code, 1):
        check(paragraph.find("w:pPr/w:jc", NS).get(tag("w:val")) == "left", f"Codigo {i}: alineacion.")
        check(paragraph.find("w:pPr/w:spacing", NS).get(tag("w:line")) == "240", f"Codigo {i}: interlineado.")
        check(paragraph.find("w:pPr/w:shd", NS).get(tag("w:fill")) == "F2F2F2", f"Codigo {i}: fondo.")
        for run_node in paragraph.findall(".//w:r", NS):
            check(run_node.find("w:rPr/w:rFonts", NS).get(tag("w:ascii")) == "Consolas",
                  f"Codigo {i}: fuente.")
            check(run_node.find("w:rPr/w:sz", NS).get(tag("w:val")) == "20", f"Codigo {i}: tamano.")

    children = list(doc.find("w:body", NS))
    for i, child in enumerate(children):
        if child.tag == tag("w:tbl") or any(child is p for p in code):
            check(i + 1 < len(children) and children[i + 1].tag == tag("w:p")
                  and not paragraph_text(children[i + 1]), "Falta espacio despues de tabla/codigo.")

    drawings = doc.findall(".//wp:inline", NS) + doc.findall(".//wp:anchor", NS)
    relationships = ET.fromstring(entries["word/_rels/document.xml.rels"])
    rels = {r.get("Id"): r.get("Target") for r in relationships}
    embedded = []
    for drawing in drawings:
        extent = drawing.find("wp:extent", NS)
        cx, cy = int(extent.get("cx")), int(extent.get("cy"))
        check(cx <= width * 635 + 1 and cy <= (height - 720) * 635 + 1,
              "Una imagen excede el area disponible.")
        for blip in drawing.findall(".//a:blip", NS):
            target = rels.get(blip.get(tag("r:embed")))
            if target:
                entry = "word/" + target
                check(entry in entries, f"Falta imagen incorporada: {target}")
                embedded.append(entries.get(entry, b""))
    for diagram in statistics["diagrams"]:
        data = Path(diagram["png"]).read_bytes()
        check(data in embedded, "Un diagrama Mermaid no esta incorporado en el Word.")
        for drawing in drawings:
            blip = drawing.find(".//a:blip", NS)
            if blip is not None and entries.get("word/" + rels.get(blip.get(tag("r:embed")), "")) == data:
                extent = drawing.find("wp:extent", NS)
                px, py = diagram["pixels"]
                check(abs(int(extent.get("cx")) / int(extent.get("cy")) - px / py) < 0.005,
                      "Un diagrama perdio su proporcion original.")

    all_text = "\n".join(paragraph_text(p) for p in doc.findall(".//w:p", NS))
    for marker in statistics["markers"]:
        check(marker in all_text, f"Falta marcador pendiente: {marker}")
    for caption in statistics["captions"]:
        check(caption in all_text, f"Falta pie: {caption}")
    headings = [p for p in doc.findall(".//w:p", NS)
                if p.find("w:pPr/w:pStyle", NS) is not None
                and p.find("w:pPr/w:pStyle", NS).get(tag("w:val"), "").startswith("Heading")]
    check(len(headings) == statistics["headings"], "Cambio la jerarquia de titulos.")
    check(len(doc.findall(".//w:hyperlink", NS)) >= statistics["links"], "Faltan enlaces.")
    if not use_template:
        for margins in doc.findall(".//w:sectPr/w:pgMar", NS):
            check(all(margins.get(tag("w:" + side)) == "1440"
                      for side in ("top", "bottom", "left", "right")), "Margenes distintos de una pulgada.")
        styles = ET.fromstring(entries["word/styles.xml"])
        normal = next(s for s in styles.findall("w:style", NS) if s.get(tag("w:styleId")) == "Normal")
        check(normal.find("w:rPr/w:rFonts", NS).get(tag("w:ascii")) == "Arial", "Fuente general incorrecta.")
        check(normal.find("w:rPr/w:sz", NS).get(tag("w:val")) == "24", "Tamano general incorrecto.")
        check(normal.find("w:pPr/w:spacing", NS).get(tag("w:line")) == "360", "Interlineado general incorrecto.")
    if errors:
        raise RuntimeError("Verificacion fallida:\n- " + "\n- ".join(errors))
    return {"verified": True, "tables": len(tables), "code_blocks": len(code),
            "mermaid_png_embedded": len(statistics["diagrams"]), "images": len(drawings),
            "headings": len(headings), "pending_markers": len(statistics["markers"]),
            "figure_captions": len(statistics["captions"]),
            "usable_width_inches": round(width / 1440, 4),
            "checks": ["bordes y anchos de tablas/celdas", "alineacion de tablas",
                       "codigo: contenido, indentacion, Consolas 10, fondo e interlineado",
                       "espacio posterior a tablas/codigo", "PNG incorporados y proporciones",
                       "imagenes dentro de margenes", "titulos, enlaces, pies y marcadores",
                       "Arial 12, interlineado 1.5 y margenes de 1 pulgada" if not use_template
                       else "estilos y margenes de la plantilla conservados"]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", nargs="?", type=Path, default=ROOT / "Reporte.md")
    parser.add_argument("--output", type=Path, default=ROOT / "Reporte.docx")
    parser.add_argument("--reference-doc", type=Path, help="Plantilla DOCX de referencia para Pandoc")
    parser.add_argument("--assets-dir", type=Path, default=ROOT / "Docs/Word/diagramas")
    parser.add_argument("--pandoc", help="Ruta de pandoc si no esta en el PATH")
    parser.add_argument("--mmdc", help="Ruta de Mermaid CLI si no esta en el PATH")
    parser.add_argument("--puppeteer-config", type=Path, help="Configuracion opcional de navegador para Mermaid CLI")
    args = parser.parse_args()
    source, output, assets = args.input.resolve(), args.output.resolve(), args.assets_dir.resolve()
    if not source.is_file():
        raise RuntimeError(f"No existe el Markdown: {source}")
    if args.reference_doc and not args.reference_doc.is_file():
        raise RuntimeError(f"No existe la plantilla: {args.reference_doc}")
    pandoc, cli = command("pandoc", args.pandoc), command("mmdc", args.mmdc)
    assets.mkdir(parents=True, exist_ok=True)
    output.parent.mkdir(parents=True, exist_ok=True)
    config = ROOT / "Docs/Word/mermaid-config.json"
    text = source.read_text(encoding="utf-8-sig")
    statistics = {"diagrams": [], "code": [], "tables": 0, "headings": 0, "links": 0,
                  "markers": (re.findall(r"\[INSERTAR CAPTURA[^\]]*\]", text)
                              + re.findall(r"\*\*((?:CAPTURA|FOTO) PENDIENTE[^*]+)\*\*", text)),
                  "captions": re.findall(r"\*\*(Figura \d+\.)", text)}
    with tempfile.TemporaryDirectory(prefix="banana-word-") as temporary:
        temp = Path(temporary)
        reference = temp / "reference.docx"
        if args.reference_doc:
            shutil.copyfile(args.reference_doc, reference)
        else:
            reference.write_bytes(run([pandoc, "--print-default-data-file=reference.docx"],
                                      stdout=subprocess.PIPE).stdout)
        ref_entries = read_package(reference)
        configure_styles(ref_entries, bool(args.reference_doc))
        ref_doc = ET.fromstring(ref_entries["word/document.xml"])
        width, height = page_area(ref_doc, bool(args.reference_doc))
        ref_entries["word/document.xml"] = xml_bytes(ref_doc)
        write_package(reference, ref_entries)
        # GFM conserva listas, enlaces automaticos y escapes de pipes en tablas.
        ast = json.loads(run([pandoc, str(source), "--from=gfm", "--to=json"],
                             stdout=subprocess.PIPE).stdout.decode("utf-8"))
        original_text = re.sub(r"\s+", " ", ast_text(ast["blocks"])).strip()
        original_lists = list_shapes(ast["blocks"])
        ast = transform_ast(ast, assets, cli, config, args.puppeteer_config,
                            width, height, statistics)
        ast["meta"]["lang"] = {"t": "MetaString", "c": "es-MX"}
        json_path = temp / "document.json"
        json_path.write_text(json.dumps(ast, ensure_ascii=False), encoding="utf-8")
        raw = temp / "pandoc.docx"
        run([pandoc, str(json_path), "--from=json", "--to=docx", "--standalone",
             "--syntax-highlighting=none", "--reference-doc=" + str(reference),
             "--resource-path=" + str(source.parent), "--output=" + str(raw)], cwd=source.parent)
        entries = read_package(raw)
        width, height = format_document(entries, bool(args.reference_doc))
        report = verify(entries, width, height, statistics, bool(args.reference_doc))
        final = temp / "formatted.docx"
        write_package(final, entries)
        # Verificar tambien el paquete reabierto, no solo el XML en memoria.
        verify(read_package(final), width, height, statistics, bool(args.reference_doc))
        roundtrip = json.loads(run([pandoc, str(final), "--from=docx", "--to=json"],
                                  stdout=subprocess.PIPE).stdout.decode("utf-8"))
        if re.sub(r"\s+", " ", ast_text(roundtrip["blocks"])).strip() != original_text:
            raise RuntimeError("El texto del DOCX no coincide con el Markdown original.")
        if list_shapes(roundtrip["blocks"]) != original_lists:
            raise RuntimeError("Las listas o su numeracion no coinciden con el original.")
        report["text_preserved"] = True
        report["lists_preserved"] = True
        report["checks"].append("comparacion de texto completo y listas mediante lectura del DOCX con Pandoc")
        shutil.copyfile(final, output)
    report["input"] = source.name
    report["output"] = output.name
    report["pandoc"] = run([pandoc, "--version"], stdout=subprocess.PIPE).stdout.decode().splitlines()[0]
    report["mermaid_cli"] = run([cli, "--version"], stdout=subprocess.PIPE).stdout.decode().strip()
    report_path = output.with_suffix(".verificacion.json")
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Word generado: {output}")
    print(f"Verificacion: {report_path}")
    print(f"Tablas: {report['tables']}; bloques de codigo: {report['code_blocks']}; "
          f"diagramas PNG: {report['mermaid_png_embedded']}; marcadores: {report['pending_markers']}")


if __name__ == "__main__":
    try:
        main()
    except (RuntimeError, OSError, subprocess.CalledProcessError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        sys.exit(1)
