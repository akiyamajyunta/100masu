"""アプリ起動（表示設定 → メインウィンドウ）。"""

from __future__ import annotations

import tkinter as tk

from masu.app.masu_app import MasuApp
from masu.launcher import resolve_display_settings


def main() -> None:
    display = resolve_display_settings()
    if display is None:
        return
    root = tk.Tk()
    if display.fullscreen:
        root.attributes("-fullscreen", True)
    else:
        root.geometry(f"{display.width}x{display.height}")
    root.minsize(640, 480)
    root.update_idletasks()
    MasuApp(root)
    root.mainloop()
