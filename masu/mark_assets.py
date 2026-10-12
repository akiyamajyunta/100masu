"""採点マーク（〇・×）の透過 PNG。"""

from __future__ import annotations

import tkinter as tk
from pathlib import Path

ASSETS_DIR = Path(__file__).resolve().parent.parent / "assets"
MARU_PATH = ASSETS_DIR / "mark_maru.png"
BATSU_PATH = ASSETS_DIR / "mark_batsu.png"


def load_mark_photos(
    master: tk.Misc, *, target_px: int
) -> tuple[tk.PhotoImage, tk.PhotoImage]:
    """セル内表示用に縮小した PhotoImage（参照を保持すること）。"""
    return (
        _load_scaled(master, MARU_PATH, target_px),
        _load_scaled(master, BATSU_PATH, target_px),
    )


def _load_scaled(master: tk.Misc, path: Path, target_px: int) -> tk.PhotoImage:
    if not path.is_file():
        raise FileNotFoundError(f"採点マーク画像がありません: {path}")
    img = tk.PhotoImage(master=master, file=str(path))
    w, h = img.width(), img.height()
    max_side = max(w, h)
    if max_side > target_px:
        factor = max(1, (max_side + target_px - 1) // target_px)
        img = img.subsample(factor, factor)
    return img
