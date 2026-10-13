"""右ステータス欄（星蓮船風）。"""

from __future__ import annotations

import tkinter as tk
from tkinter import font as tkfont

from masu.config import STAR_GAUGE_MAX
from masu import theme as T


class StatusSidebar(tk.Frame):
    def __init__(self, master: tk.Misc, *, width: int = 240) -> None:
        super().__init__(master, bg=T.STATUS_PANEL, width=width)
        self.pack_propagate(False)

        outer = tk.Frame(self, bg=T.BORDER_GOLD, padx=2, pady=2)
        outer.pack(fill="both", expand=True, padx=4, pady=4)
        inner = tk.Frame(outer, bg=T.STATUS_INNER, padx=12, pady=12)
        inner.pack(fill="both", expand=True)

        self.mode_font = tkfont.Font(family="Yu Gothic UI", size=22, weight="bold")
        self.label_font = tkfont.Font(family="Yu Gothic UI", size=11)
        self.value_font = tkfont.Font(family="Yu Gothic UI", size=16, weight="bold")
        try:
            self.timer_font = tkfont.Font(family="Consolas", size=26, weight="bold")
        except tk.TclError:
            self.timer_font = tkfont.Font(family="Courier New", size=26, weight="bold")

        self.mode_var = tk.StringVar(value="たし算")
        tk.Label(
            inner,
            textvariable=self.mode_var,
            font=self.mode_font,
            fg=T.CURSOR_GOLD,
            bg=T.STATUS_INNER,
        ).pack(pady=(0, 12))

        self.high_var = tk.StringVar(value="0")
        self.score_var = tk.StringVar(value="0")
        self.correct_var = tk.StringVar(value="0 / 0")
        self.answer_stars_var = tk.StringVar(value="")
        self.skill_stars_var = tk.StringVar(value="")

        self._row(inner, "最高得点", self.high_var, T.TEXT)
        self._row(inner, "得点", self.score_var, T.CURSOR_GOLD)
        self._row(inner, "正解", self.correct_var, T.TEXT)
        self._stars_row(inner, "答力", self.answer_stars_var)
        self._stars_row(inner, "技能", self.skill_stars_var)

        tk.Label(
            inner,
            text="タイマー",
            font=self.label_font,
            fg=T.TEXT,
            bg=T.STATUS_INNER,
            anchor="w",
        ).pack(fill="x", pady=(10, 2))
        self.time_var = tk.StringVar(value="0:00.00")
        timer_box = tk.Frame(inner, bg=T.STATUS_INNER, height=38)
        timer_box.pack(fill="x", pady=(0, 8))
        timer_box.pack_propagate(False)
        tk.Label(
            timer_box,
            textvariable=self.time_var,
            font=self.timer_font,
            fg=T.GAUGE_TIMER,
            bg=T.STATUS_INNER,
            anchor="e",
            width=9,
        ).pack(expand=True, fill="both", anchor="e")

        self.restart_btn = tk.Button(
            inner,
            text="もう一度遊ぶ",
            font=self.label_font,
            bg="#6366f1",
            fg="white",
            activebackground="#818cf8",
            relief="flat",
            cursor="hand2",
        )
        self.restart_btn.pack(fill="x", pady=(8, 0))

    def _row(
        self,
        parent: tk.Frame,
        label: str,
        var: tk.StringVar,
        color: str,
    ) -> None:
        tk.Label(
            parent,
            text=label,
            font=self.label_font,
            fg=T.TEXT,
            bg=T.STATUS_INNER,
            anchor="w",
        ).pack(fill="x")
        tk.Label(
            parent,
            textvariable=var,
            font=self.value_font,
            fg=color,
            bg=T.STATUS_INNER,
            anchor="e",
        ).pack(fill="x", pady=(0, 8))

    def _stars_row(self, parent: tk.Frame, label: str, var: tk.StringVar) -> None:
        tk.Label(
            parent,
            text=f"{label} :",
            font=self.label_font,
            fg=T.TEXT,
            bg=T.STATUS_INNER,
            anchor="w",
        ).pack(fill="x")
        tk.Label(
            parent,
            textvariable=var,
            font=tkfont.Font(family="Yu Gothic UI", size=14),
            fg=T.STAR_ON,
            bg=T.STATUS_INNER,
            anchor="e",
        ).pack(fill="x", pady=(0, 6))

    @staticmethod
    def _stars_text(filled: int, total: int = STAR_GAUGE_MAX) -> str:
        filled = max(0, min(total, filled))
        return "★" * filled + "☆" * (total - filled)

    def update_display(
        self,
        *,
        mode_en: str,
        high_score: int,
        session_score: int,
        correct_text: str,
        answer_stars: int,
        skill_stars: int,
        time_text: str,
    ) -> None:
        self.mode_var.set(mode_en)
        self.high_var.set(f"{high_score:,}")
        self.score_var.set(f"{session_score:,}")
        self.correct_var.set(correct_text)
        self.answer_stars_var.set(self._stars_text(answer_stars))
        self.skill_stars_var.set(self._stars_text(skill_stars))
        self.time_var.set(time_text)
