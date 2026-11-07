#!/usr/bin/env python3
"""Generate 'Pedal Degrade.prs.xml' (Build §3). Not deployed.

    python gen_presets.py          write the bundle
    python gen_presets.py --check  verify declarations and ranges against PedalDegrade.cs
"""
import re
import sys
from pathlib import Path
from xml.sax.saxutils import quoteattr

HERE = Path(__file__).resolve().parent
MACHINE = "Pedal Degrade"
OUT = HERE / f"{MACHINE}.prs.xml"
SRC = HERE / "PedalDegrade.cs"

# Global parameters in declaration order: name -> (min, max, default)
PARAMS = {
    "Smp. Rate": (1, 64, 16),
    "Bit Depth": (1, 16, 6),
}

# (Smp. Rate divisor, Bit Depth). Hz figures assume a 44.1 kHz host.
PRESETS = {
    "Clean":             (1, 16),   # bypass
    "Classic Degrade":   (16, 6),   # 2756 Hz, 6 bit (the machine default)
    "12-bit Sampler":    (2, 12),   # 22050 Hz
    "8-bit Console":     (4, 8),    # 11025 Hz
    "Walkie Crunch":     (8, 5),    # 5513 Hz
    "4-bit Grit":        (3, 4),    # 14700 Hz
    "1-bit Fuzz":        (1, 1),
    "Aliased Shimmer":   (6, 16),   # rate only
    "Subterranean":      (64, 3),   # 689 Hz
}


def parse_decls():
    text = SRC.read_text(encoding="utf-8")
    decls = []
    for m in re.finditer(r"\[ParameterDecl\((.*?)\)\]", text, re.S):
        body = m.group(1)
        if re.search(r"IsStateless\s*=\s*true", body):
            continue
        name = re.search(r'Name\s*=\s*"([^"]+)"', body).group(1)
        decls.append((name, body))
    return decls


def check():
    ok = True
    names = [n for n, _ in parse_decls()]
    if names != list(PARAMS):
        print(f"declaration order mismatch: source {names} vs script {list(PARAMS)}")
        ok = False
    for name, (pmin, pmax, _) in PARAMS.items():
        for preset, vals in PRESETS.items():
            v = vals[list(PARAMS).index(name)]
            if not pmin <= v <= pmax:
                print(f"{preset}: {name}={v} outside {pmin}..{pmax}")
                ok = False
    print("check OK" if ok else "check FAILED")
    return ok


def write():
    lines = ['<?xml version="1.0" encoding="utf-8"?>', "<PresetDictionary>"]
    for key, vals in PRESETS.items():
        lines.append(f"  <Item Key={quoteattr(key)}>")
        lines.append(f"    <Preset Machine={quoteattr(MACHINE)}>")
        lines.append("      <Parameters>")
        for idx, (name, v) in enumerate(zip(PARAMS, vals)):
            lines.append(
                f'        <Parameter Name={quoteattr(name)} Group="1" Index="{idx}" Track="0" Value="{v}" />')
        lines.append("      </Parameters>")
        lines.append("      <Attributes />")
        lines.append("      <Comment></Comment>")
        lines.append("    </Preset>")
        lines.append("  </Item>")
    lines.append("</PresetDictionary>")
    with open(OUT, "w", encoding="utf-8-sig", newline="\r\n") as f:
        f.write("\n".join(lines) + "\n")
    print(f"wrote {OUT.name} ({len(PRESETS)} presets)")


if __name__ == "__main__":
    if not check():
        sys.exit(1)
    if "--check" not in sys.argv:
        write()
