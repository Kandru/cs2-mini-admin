#!/usr/bin/env python3
"""Check that every translation key the C# code uses exists in all language files (and nothing is missing in one).

Literal keys are found directly; interpolated keys like $"menu.act.{AdminMenuIds.ActionKeys[a]}" are expanded
with the string arrays they index. Action log keys come from the LogAction calls.
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
LANG = SRC / "lang"
PREFIXES = ("menu.", "command.", "chat.", "log.", "core.", "detection.")

LITERAL_RE = re.compile(r'"((?:menu|command|chat|log|core|detection)\.[A-Za-z0-9_.]+)"')
INTERP_RE = re.compile(r'\$"((?:menu|command|chat|log)\.[A-Za-z0-9_.]*)\{([^}]+)\}([A-Za-z0-9_.]*)"')
ARRAY_RE = re.compile(r"string\[\]\s+(\w+)\s*=\s*\[([^\]]*)\]\s*;")
LOG_RE = re.compile(r'LogAction\([^;]*?"([a-z]+)"')
LOG_TERNARY_RE = re.compile(r'LogAction\(admin,\s*\w+\s*\?\s*"([a-z]+)"\s*:\s*"([a-z]+)"')


def fail(msg: str) -> None:
    print(f"ERROR: {msg}", file=sys.stderr)
    raise SystemExit(1)


def main() -> None:
    source = "\n".join(p.read_text(encoding="utf-8") for p in sorted(SRC.rglob("*.cs")))
    arrays = {name: re.findall(r'"([^"]*)"', body) for name, body in ARRAY_RE.findall(source)}

    keys = set(LITERAL_RE.findall(source))
    for prefix, expr, suffix in INTERP_RE.findall(source):
        if prefix == "log.action.":
            continue
        name = re.search(r"(\w+)\[[^\]]+\]$", expr.strip())
        if name and name.group(1) in arrays:
            values = arrays[name.group(1)]
        elif expr.strip() in ("reason", "r"):
            values = [str(i) for i in range(4)]
        elif expr.strip().startswith("TabKey("):
            values = ["players", "server", "maps", "records", "log"]
        elif expr.strip() == "key" and prefix == "menu.rec.":
            values = arrays["RecordKindKeys"]
        elif expr.strip() == "key" and prefix == "menu.act.":
            values = arrays["ActionKeys"]
        elif expr.strip() == "key" and prefix == "menu.ps.":
            values = arrays["PlayerStepKeys"]
        else:
            fail(f"cannot expand ${prefix}{{{expr}}}{suffix}")
        keys.update(f"{prefix}{v}{suffix}" for v in values)

    actions = set(LOG_RE.findall(source))
    for a, b in LOG_TERNARY_RE.findall(source):
        actions.update((a, b))
    actions.update(arrays["PlayerStepKeys"])  # SetPlayerValue logs the stepper key
    keys.update(f"log.action.{a}" for a in actions)
    keys = {k for k in keys if k.startswith(PREFIXES) and not k.endswith(".")}

    files = sorted(LANG.glob("*.json"))
    loaded = {f.name: json.loads(f.read_text(encoding="utf-8")) for f in files}
    ok = True
    for name, data in loaded.items():
        missing = sorted(keys - data.keys())
        if missing:
            ok = False
            print(f"ERROR: {name} misses {len(missing)} key(s): {missing}", file=sys.stderr)
    names = list(loaded)
    for other in names[1:]:
        diff = sorted(loaded[names[0]].keys() ^ loaded[other].keys())
        if diff:
            ok = False
            print(f"ERROR: {names[0]} and {other} differ: {diff}", file=sys.stderr)
    unused = sorted(set(loaded[names[0]]) - keys)
    if not ok:
        raise SystemExit(1)
    print(f"OK: {len(keys)} key(s) used, {len(files)} language file(s)" + (f", unused: {unused}" if unused else ""))


if __name__ == "__main__":
    main()
