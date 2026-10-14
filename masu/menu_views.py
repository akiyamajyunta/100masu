"""タイトル・ランキングなどメニュー用ウィジェット。"""

from __future__ import annotations

import tkinter as tk
from tkinter import font as tkfont

from masu import theme as T
from masu.leaderboard import (
    LEADERBOARD_SIZE,
    LeaderboardEntry,
    is_placeholder,
    padded_entries,
    row_display,
)

FONT_FAMILY = "Yu Gothic UI"


class LeaderboardTable(tk.Frame):
    def __init__(
        self,
        master: tk.Misc,
        *,
        highlight_rank: int | None = None,
        **kwargs,
    ) -> None:
        super().__init__(master, bg=T.MAIN_INNER, **kwargs)
        self.highlight_rank = highlight_rank
        self._row_labels: list[tuple[tk.Label, ...]] = []
        head = tkfont.Font(family=FONT_FAMILY, size=10, weight="bold")
        row = tkfont.Font(family=FONT_FAMILY, size=11)
        self._head_font = head
        self._row_font = row

        header = tk.Frame(self, bg=T.MAIN_INNER)
        header.pack(fill="x", pady=(0, 4))
        for text, width, anchor in (
            ("順位", 4, "w"),
            ("名前", 12, "w"),
            ("得点", 10, "e"),
            ("日時", 16, "w"),
            ("正答率", 8, "e"),
        ):
            tk.Label(
                header,
                text=text,
                font=head,
                fg=T.TEXT_DIM,
                bg=T.MAIN_INNER,
                width=width,
                anchor=anchor,
            ).pack(side="left", padx=2)

        body = tk.Frame(self, bg=T.MAIN_INNER)
        body.pack(fill="both", expand=True)
        for _ in range(LEADERBOARD_SIZE):
            line = tk.Frame(body, bg=T.MAIN_INNER)
            line.pack(fill="x", pady=1)
            labels: list[tk.Label] = []
            for width, anchor in ((4, "w"), (12, "w"), (10, "e"), (16, "w"), (8, "e")):
                lbl = tk.Label(
                    line,
                    text="",
                    font=row,
                    fg=T.TEXT,
                    bg=T.MAIN_INNER,
                    width=width,
                    anchor=anchor,
                )
                lbl.pack(side="left", padx=2)
                labels.append(lbl)
            self._row_labels.append(tuple(labels))

    def set_entries(
        self,
        entries: list[LeaderboardEntry],
        *,
        highlight_rank: int | None = None,
    ) -> None:
        self.highlight_rank = highlight_rank
        rows = padded_entries(entries)
        for i, (labels, entry) in enumerate(
            zip(self._row_labels, rows, strict=True), start=1
        ):
            name, score, played_at, accuracy = row_display(entry)
            filled = not is_placeholder(entry)
            highlighted = highlight_rank is not None and i == highlight_rank
            rank_fg = T.CURSOR_GOLD if highlighted else T.TEXT_DIM
            name_fg = T.CURSOR_GOLD if highlighted else (T.TEXT if filled else T.TEXT_DIM)
            score_fg = T.CURSOR_GOLD if (highlighted or filled) else T.TEXT_DIM
            values = (f"No{i}.", name, score, played_at, accuracy)
            fgs = (rank_fg, name_fg, score_fg, name_fg, name_fg)
            for lbl, val, fg in zip(labels, values, fgs, strict=True):
                lbl.config(text=val, fg=fg)


class NameKeyboardView(tk.Frame):
    """画面上キーボード（表示のみ。操作は MasuApp が処理）。"""

    def __init__(self, master: tk.Misc, keyboard_rows: tuple[tuple[str, ...], ...]) -> None:
        super().__init__(master, bg="#1a5866")
        self._keys: dict[tuple[int, int], tk.Label] = {}
        key_font = tkfont.Font(family=FONT_FAMILY, size=9)
        for ri, line in enumerate(keyboard_rows):
            row_frame = tk.Frame(self, bg="#1a5866")
            row_frame.pack(pady=1)
            for ci, label in enumerate(line):
                lbl = tk.Label(
                    row_frame,
                    text=label,
                    font=key_font,
                    width=3 if len(label) == 1 else 4,
                    bg="#e4e8f0",
                    fg="#334155",
                    relief="flat",
                    padx=2,
                    pady=2,
                )
                lbl.pack(side="left", padx=1)
                self._keys[(ri, ci)] = lbl

    def highlight(self, row: int, col: int) -> None:
        for (r, c), lbl in self._keys.items():
            if r == row and c == col:
                lbl.config(bg=T.CURSOR_GOLD, fg="#0f172a")
            else:
                lbl.config(bg="#e4e8f0", fg="#334155")
