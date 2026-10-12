"""スコア登録用の名前入力。"""

from __future__ import annotations

import tkinter as tk
from tkinter import font as tkfont
from tkinter import simpledialog

from masu import theme as T


def ask_player_name(parent: tk.Misc, *, preview_rank: int) -> str | None:
    name = simpledialog.askstring(
        "スコア登録",
        f"ランキング {preview_rank} 位の見込みです。\n"
        "名前を入力してください（空欄でキャンセル）:",
        parent=parent,
    )
    if name is None:
        return None
    clean = name.strip()
    return clean if clean else None
