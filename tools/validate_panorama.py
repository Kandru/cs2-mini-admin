#!/usr/bin/env python3
"""Validate the MiniAdmin Panorama addon (workshop/content/panorama) against the C# contract.

Checks (cs2-panorama-ruleset §13):
- admin_v1.xml is up to date with tools/gen_admin_panorama.py and parses
- pool sizes in AdminMenuIds.cs match the generator
- every panel id / dialog var spelled in AdminMenuIds.cs exists in the layout, every layout var is used,
  every Button id is handled in C#
- Panels and Labels carry hittest="false", Buttons carry none; Label text is one {s:var} or a glyph
- the stylesheet uses only the allowed CSS subset; classes written from C# exist in the stylesheet
- dialog variable budget (1244 per layout)
"""
from __future__ import annotations

import importlib.util
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / "workshop" / "content" / "panorama"
LAYOUT = CONTENT / "layout" / "custom_game" / "miniadmin" / "admin_v1.xml"
STYLE = CONTENT / "styles" / "custom_game" / "miniadmin" / "admin_v1.vcss"
IDS_CS = ROOT / "src" / "Hud" / "AdminMenuIds.cs"
GENERATOR = ROOT / "tools" / "gen_admin_panorama.py"

VAR_BUDGET = 1244
# language-neutral literals; team abbreviations are literals because the client localizes
# dialog values that equal a game token ("CT" became "AT-Seite" on a German client)
GLYPHS = {"//", "‹", "›", ":", "T", "CT"}
POOLS = ["Tabs", "TeamRows", "SpecChips", "Stats", "DetailPages", "Reasons", "Durations", "Give", "Maps", "Records", "RecordKinds", "LogRows"]
GEN_POOLS = {
    "Tabs": "TABS", "TeamRows": "TEAM_ROWS", "SpecChips": "SPEC_CHIPS", "Stats": "STATS",
    "DetailPages": "DETAIL_PAGES", "Reasons": "REASONS", "Durations": "DURATIONS", "Give": "GIVE",
    "Maps": "MAPS", "Records": "RECORDS", "RecordKinds": "RECORD_KINDS", "LogRows": "LOG_ROWS",
}

VAR_RE = re.compile(r"\{s:([A-Za-z0-9_]+)\}")
DIALOG_ONLY_RE = re.compile(r"^\{s:[A-Za-z0-9_]+\}$")
HIDDEN_CLASS = re.compile(r"(?<![\w-])(?:hidden|Hidden)(?![\w-])")
BAD_CSS = re.compile(
    r"(?:"
    r"(?:[{;]|^)\s*display\s*:"
    r"|\b(?:rgba?|hsla?|calc|var)\s*\("
    r"|background-size\s*:\s*contain\b"
    r"|@media\b"
    r"|@define\b"
    r"|::(?:before|after)\b"
    r"|:(?:selected|disabled|focus)\b"
    r"|visibility\s*:\s*hidden\b"
    r"|overflow\s*:\s*hidden\b"
    r"|font-size\s*:\s*\d+(?:px|em|rem|%)\b"
    r"|vertical-align\s*:\s*center\b"
    r"|background-blur\b"
    r"|(?:[{;]|^)\s*transition\s*:"
    r"|\bmargin(?:-(?:top|right|bottom|left))?\s*:[^;]*-\d"
    r"|transition-property\s*:[^;]*box-shadow"
    r")",
    re.M | re.I,
)
INT_CONST_RE = re.compile(r"const\s+int\s+(\w+)\s*=\s*(\d+)\s*;")
ARRAY_RE = re.compile(r"string\[\]\s+(\w+)\s*=\s*\[([^\]]*)\]\s*;")
INDEXED_RE = re.compile(r'Indexed\("([^"]+)",\s*(\w+)\)')
FIELDED_RE = re.compile(r'Fielded\("([^"]+)",\s*"([^"]+)",\s*(\w+)\)')
KEYED_RE = re.compile(r'Keyed\("([^"]+)",\s*(\w+)(?:,\s*"([^"]*)")?\)')
LITERAL_RE = re.compile(r'"(ma[-_][A-Za-z0-9_-]*)"')
CLASS_ARRAYS = ["Views", "Phases", "Teams", "DetailPageClasses"]
CLASS_CONSTS = re.compile(r'const\s+string\s+\w+Class\s*=\s*"([^"]+)"')


def fail(msg: str) -> None:
    print(f"ERROR: {msg}", file=sys.stderr)
    raise SystemExit(1)


def load_generator():
    sys.dont_write_bytecode = True
    spec = importlib.util.spec_from_file_location("gen_admin_panorama", GENERATOR)
    if spec is None or spec.loader is None:
        fail(f"cannot load {GENERATOR}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def count(consts: dict[str, int], name: str) -> int:
    if name.isdigit():
        return int(name)
    if name not in consts:
        fail(f"AdminMenuIds.cs: unknown count {name}")
    return consts[name]


def cs_contract(source: str) -> tuple[set[str], set[str], dict[str, int], dict[str, list[str]]]:
    """Expand AdminMenuIds.cs into the panel ids and dialog vars it spells."""
    consts = {name: int(value) for name, value in INT_CONST_RE.findall(source)}
    arrays = {name: re.findall(r'"([^"]*)"', body) for name, body in ARRAY_RE.findall(source)}
    names: set[str] = set()
    for prefix, n in INDEXED_RE.findall(source):
        names.update(f"{prefix}{i}" for i in range(count(consts, n)))
    for prefix, suffix, n in FIELDED_RE.findall(source):
        names.update(f"{prefix}{i}{suffix}" for i in range(count(consts, n)))
    for prefix, keys, suffix in KEYED_RE.findall(source):
        if keys not in arrays:
            fail(f"AdminMenuIds.cs: unknown key array {keys}")
        names.update(f"{prefix}{key}{suffix}" for key in arrays[keys])
    # plain literals, without the prefixes passed to the helpers above
    stripped = KEYED_RE.sub("", FIELDED_RE.sub("", INDEXED_RE.sub("", source)))
    names.update(LITERAL_RE.findall(stripped))
    ids = {n for n in names if n.startswith("ma-")}
    variables = {n for n in names if n.startswith("ma_")}
    return ids, variables, consts, arrays


def strip_comments(css: str) -> str:
    # keep the line count so error lines still match the file
    return re.sub(r"/\*.*?\*/", lambda m: "\n" * m.group(0).count("\n"), css, flags=re.S)


def check_css(css: str) -> None:
    if HIDDEN_CLASS.search(css):
        fail(f"{STYLE.name}: forbidden class name hidden/Hidden")
    bad = BAD_CSS.search(css)
    if bad:
        line = css.count("\n", 0, bad.start()) + 1
        fail(f"{STYLE.name}:{line}: unsupported CSS near {bad.group(0).strip()!r}")
    if css.count("{") != css.count("}"):
        fail(f"{STYLE.name}: unbalanced braces")


def main() -> None:
    gen = load_generator()
    for path, text in gen.render().items():
        if not path.is_file() or path.read_text(encoding="utf-8").replace("\r\n", "\n") != text:
            fail(f"{path.name}: out of date - run tools/gen_admin_panorama.py")

    text = LAYOUT.read_text(encoding="utf-8")
    if HIDDEN_CLASS.search(text):
        fail(f"{LAYOUT.name}: forbidden class name hidden/Hidden")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as ex:
        fail(f"{LAYOUT.name}: XML parse error: {ex}")

    xml_ids: set[str] = set()
    button_ids: set[str] = set()
    xml_vars: set[str] = set()
    xml_classes: set[str] = set()
    for element in root.iter():
        tag = element.tag
        panel_id = element.attrib.get("id")
        if panel_id:
            if panel_id in xml_ids:
                fail(f"{LAYOUT.name}: duplicate id {panel_id}")
            xml_ids.add(panel_id)
        xml_classes.update(element.attrib.get("class", "").split())
        if tag in ("Panel", "Label") and element.attrib.get("hittest") != "false":
            fail(f"{LAYOUT.name}: <{tag} id={panel_id!r} class={element.attrib.get('class')!r}> needs hittest=\"false\"")
        if tag == "Button":
            if "hittest" in element.attrib:
                fail(f"{LAYOUT.name}: Button {panel_id!r} must not carry hittest")
            if not panel_id:
                fail(f"{LAYOUT.name}: Button without id")
            button_ids.add(panel_id)
        value = element.attrib.get("text")
        if value is not None:
            value = value.strip()
            if not DIALOG_ONLY_RE.fullmatch(value) and value not in GLYPHS:
                fail(f"{LAYOUT.name}: Label text must be one {{s:var}} or a glyph (got {value!r})")
            xml_vars.update(VAR_RE.findall(value))

    source = IDS_CS.read_text(encoding="utf-8")
    cs_ids, cs_vars, consts, arrays = cs_contract(source)
    for pool in POOLS:
        if consts.get(pool) != getattr(gen, GEN_POOLS[pool]):
            fail(f"AdminMenuIds.{pool} = {consts.get(pool)} but generator {GEN_POOLS[pool]} = {getattr(gen, GEN_POOLS[pool])}")

    missing_ids = cs_ids - xml_ids
    if missing_ids:
        fail(f"C# panel ids missing from XML: {sorted(missing_ids)}")
    missing_vars = cs_vars - xml_vars
    if missing_vars:
        fail(f"C# dialog vars missing from XML: {sorted(missing_vars)}")
    unused_vars = xml_vars - cs_vars
    if unused_vars:
        fail(f"XML dialog vars unused in C#: {sorted(unused_vars)}")
    unhandled = button_ids - cs_ids
    if unhandled:
        fail(f"XML buttons without a C# id: {sorted(unhandled)}")
    if len(xml_vars) > VAR_BUDGET:
        fail(f"{len(xml_vars)} dialog vars exceed the budget of {VAR_BUDGET}")

    css = strip_comments(STYLE.read_text(encoding="utf-8"))
    check_css(css)
    written = set(CLASS_CONSTS.findall(source))
    for name in CLASS_ARRAYS:
        written.update(arrays.get(name, []))
    css_classes = set(re.findall(r"\.([A-Za-z][\w-]*)", css))
    missing_css = written - css_classes
    if missing_css:
        fail(f"classes written from C# without a rule in {STYLE.name}: {sorted(missing_css)}")
    unstyled = {c for c in xml_classes if c[0].isupper() and c not in css_classes}
    if unstyled:
        fail(f"layout classes without a rule in {STYLE.name}: {sorted(unstyled)}")

    print(
        f"OK: {LAYOUT.name}: {len(xml_ids)} panel id(s), {len(button_ids)} button(s), "
        f"{len(xml_vars)}/{VAR_BUDGET} dialog var(s)"
    )


if __name__ == "__main__":
    main()
