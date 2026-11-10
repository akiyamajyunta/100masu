"""Sharpen soft background GIFs and write looping TitleArt assets."""
from __future__ import annotations

import shutil
import subprocess
import uuid
from pathlib import Path

from PIL import Image

ROOT = Path(r"C:\Users\akiyama\Downloads\game-of-life-background")
OUT_DIR = Path(r"C:\Users\akiyama\Project\100masu\Unity\Assets\Resources\TitleArt")
TMP = Path(r"C:\Users\akiyama\Project\100masu\Unity\Temp\theme_gif_bake")

# id, source, unsharp amount, contrast, saturation
JOBS = [
    ("suimyaku", "game-bg-02-ocean-caustics.gif", 1.15, 1.10, 1.12),
    ("ryusei", "game-bg-03-star-drift.gif", 1.85, 1.18, 1.15),
    ("komorebi", "game-bg-05-cloud-layer.gif", 0.85, 1.08, 1.08),
    ("holy", "game-bg-08-synth-dusk.gif", 0.75, 1.06, 1.10),
    ("marble", "game-bg-09-plasma-flow.gif", 1.55, 1.14, 1.12),
    ("energy", "game-bg-10-radar-ring.gif", 0.35, 1.04, 1.05),
    ("disco", "game-bg-23-deep-nebula.gif", 1.05, 1.08, 1.12),
]

GIF_META = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def encode(src: Path, dst: Path, amount: float, contrast: float, saturation: float) -> None:
    filt = (
        f"[0:v]unsharp=7:7:{amount}:3:3:0.25,"
        f"eq=contrast={contrast}:saturation={saturation},split[a][b];"
        f"[a]palettegen=max_colors=256:stats_mode=diff:reserve_transparent=0[p];"
        f"[b][p]paletteuse=dither=sierra2_4a:diff_mode=rectangle"
    )
    subprocess.run(
        [
            "ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
            "-i", str(src),
            "-filter_complex", filt,
            "-loop", "0",
            str(dst),
        ],
        check=True,
    )


def loop_count(path: Path) -> int | None:
    data = path.read_bytes()
    idx = data.find(b"NETSCAPE2.0")
    if idx < 0 or idx + 15 >= len(data):
        return None
    return data[idx + 13] + (data[idx + 14] << 8)


def write_card(gif: Path, jpg: Path) -> None:
    im = Image.open(gif)
    im.seek(im.n_frames // 2)
    frame = im.convert("RGB")
    frame.thumbnail((960, 540), Image.Resampling.LANCZOS)
    frame.save(jpg, quality=92, optimize=True)


def main() -> None:
    TMP.mkdir(parents=True, exist_ok=True)
    for theme_id, src_name, amount, contrast, saturation in JOBS:
        src = ROOT / src_name
        baked = TMP / f"{theme_id}.gif"
        print(f"encode {theme_id} sharpen={amount}", flush=True)
        encode(src, baked, amount, contrast, saturation)
        dest = OUT_DIR / f"theme_{theme_id}.gif.bytes"
        shutil.copyfile(baked, dest)
        meta = dest.with_suffix(dest.suffix + ".meta")
        if not meta.exists():
            meta.write_text(GIF_META.format(guid=uuid.uuid4().hex), encoding="utf-8")
        card = OUT_DIR / f"theme_card_{theme_id}.jpg"
        write_card(baked, card)
        print(
            f"  {dest.name} {dest.stat().st_size/1024/1024:.2f} MB loop={loop_count(dest)}",
            flush=True,
        )


if __name__ == "__main__":
    main()
