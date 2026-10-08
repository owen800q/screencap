#!/usr/bin/env python3
"""Render the 'capture' pixel icon from icons.svg into src/RetroCap/Assets/app.ico.

The 16x16 sprite is rendered once by headless Chromium, then scaled up with
nearest-neighbour so it stays pixel art at 32/48/256 px.
Usage: python3 tools/gen_appicon.py [path-to-chromium]
"""
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SVG = ROOT / "design/retro-sap-gui/icons.svg"
OUT = ROOT / "src/RetroCap/Assets/app.ico"
CHROME = sys.argv[1] if len(sys.argv) > 1 else "chromium"

with tempfile.TemporaryDirectory() as tmp:
    html = Path(tmp) / "icon.html"
    html.write_text(
        "<html><body style='margin:0;background:transparent'>"
        + SVG.read_text().replace('style="display:none"', 'width="0" height="0" style="position:absolute"')
        + "<svg width='16' height='16' style='display:block'><use href='#capture'/></svg></body></html>")
    png = Path(tmp) / "icon.png"
    subprocess.run([CHROME, "--headless", "--no-sandbox", "--disable-gpu", "--hide-scrollbars",
                    "--default-background-color=00000000", "--force-device-scale-factor=1",
                    "--window-size=200,200", f"--screenshot={png}", html.as_uri()],
                   check=True, capture_output=True)
    base = Image.open(png).convert("RGBA").crop((0, 0, 16, 16))
    sizes = [16, 32, 48, 256]
    imgs = [base.resize((s, s), Image.NEAREST) for s in sizes]
    OUT.parent.mkdir(parents=True, exist_ok=True)
    imgs[-1].save(OUT, format="ICO", sizes=[(s, s) for s in sizes], append_images=imgs[:-1])
    imgs[-1].save(ROOT / "design/retro-sap-gui/app-icon-256.png")
print(f"wrote {OUT.relative_to(ROOT)}")
