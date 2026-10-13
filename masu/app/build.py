"""MasuApp mixin — UI 構築."""

from __future__ import annotations

import time
import tkinter as tk
from collections.abc import Callable
from datetime import datetime
from tkinter import font as tkfont
from tkinter import messagebox

import game as game_mod
from game import (
    PENALTY_EMPTY_SECONDS,
    CellState,
    GameState,
    configure_grid,
    header_reveal_cells,
    play_cells_score_order,
)
from masu import theme as UI
from masu.anim_timings import (
    INTRO_GLOW,
    INTRO_SCALE_LEVELS,
    INTRO_SCALE_MS,
    INTRO_STAGGER_MS,
    SCORE_GLOW,
    SCORE_SCALE_LEVELS,
    SCORE_SCALE_MS,
    SCORE_STAGGER_MS,
    intro_anim_timings,
    score_anim_timings,
)
from masu.config import (
    OPERATION_LABELS,
    OPERATION_RANKING_TAB,
    OPERATION_SYMBOLS,
    STAR_GAUGE_MAX,
    Operation,
    grid_label,
)
from masu.grid_theme import (
    CELL_BORDER,
    CELL_EMPTY,
    CELL_EMPTY_FG,
    CELL_SIZE,
    FOCUS_INSET,
    FOCUS_PULSE_MS,
    FOCUS_PULSE_STEPS,
    FOCUS_RING_BRIGHT,
    FOCUS_RING_DIM,
    FOCUS_SCALE,
    FONT_FAMILY,
    HEADER_NUM,
    HEADER_NUM_FG,
    HEADER_SLOT,
    HEADER_SLOT_FG,
    HEADER_TILE_BOTTOM,
    HEADER_TILE_TOP,
    PLUS_BG,
    PLUS_FG,
    PROGRESS_FG,
    TEXT_MUTED,
)
from masu.leaderboard import format_accuracy, format_score, preview_after_insert
from masu.menu_views import LeaderboardTable, NameKeyboardView
from masu.name_keyboard import (
    ENTRY_NAME_MAX_LEN,
    KEYBOARD_ROWS,
    NameKeyboard,
    format_name_slots,
)
from masu.mark_assets import load_mark_photos
from masu.scoring import accuracy_percent, compute_session_score
from masu.screens import RESULT_MENU_ITEMS, AppScreen
from masu.sidebar import StatusSidebar


class BuildMixin:
    def _build_ui(self) -> None:
        title_font = tkfont.Font(family=FONT_FAMILY, size=22, weight="bold")
        stat_font = tkfont.Font(family=FONT_FAMILY, size=12)
        cell_font = tkfont.Font(family=FONT_FAMILY, size=14, weight="bold")
        self.cell_font = cell_font
        self._menu_title_font = tkfont.Font(family=FONT_FAMILY, size=28, weight="bold")
        self._menu_heading_font = tkfont.Font(family=FONT_FAMILY, size=16, weight="bold")
        self._menu_body_font = tkfont.Font(family=FONT_FAMILY, size=12)
        self._menu_small_font = tkfont.Font(family=FONT_FAMILY, size=10)

        self.guide_var = tk.StringVar(value="")

        self.shell = tk.Frame(self.root, bg=UI.BG)
        self.shell.pack(fill="both", expand=True)

        self._build_menu_frames(title_font, stat_font)
        self.game_layer = tk.Frame(self.shell, bg=UI.BG)
        body = self.game_layer
        body.grid_columnconfigure(0, weight=1)
        body.grid_columnconfigure(1, weight=0, minsize=250)
        body.grid_rowconfigure(0, weight=1)

        self.main_outer = tk.Frame(body, bg=UI.BG)
        self.main_outer.grid(row=0, column=0, sticky="nsew", padx=(12, 6), pady=12)
        main_outer = self.main_outer

        self.sidebar = StatusSidebar(body, width=250)
        self.sidebar.grid(row=0, column=1, sticky="ns", padx=(6, 12), pady=12)

        panel_gold = tk.Frame(main_outer, bg=UI.BORDER_GOLD, padx=3, pady=3)
        panel_gold.pack(fill="both", expand=True)
        main_panel = tk.Frame(panel_gold, bg=UI.MAIN_INNER, padx=12, pady=8)
        main_panel.pack(fill="both", expand=True)
        main_panel.pack_propagate(True)

        tk.Label(
            main_panel,
            text="100マス計算",
            font=title_font,
            fg=UI.TEXT,
            bg=UI.MAIN_INNER,
        ).pack()
        self.subtitle_label = tk.Label(
            main_panel,
            text="左と上の数字を演算して入力（矢印/WASD・Enterで採点）",
            font=stat_font,
            fg=UI.TEXT_DIM,
            bg=UI.MAIN_INNER,
            wraplength=520,
            justify="center",
        )
        self.subtitle_label.pack(pady=(2, 0))
        tk.Label(
            main_panel,
            textvariable=self.guide_var,
            font=stat_font,
            fg=PROGRESS_FG,
            bg=UI.MAIN_INNER,
        ).pack(pady=(6, 0))

        self.grid_wrap = tk.Frame(main_panel, bg=UI.MAIN_INNER)
        self.grid_wrap.pack(anchor="n")
        self.main_panel = main_panel

        self.sidebar.restart_btn.configure(command=self._restart_current_game)

        self._build_result_overlay(main_panel, stat_font)

    def _parse_geometry(self) -> tuple[int, int]:
        self.root.update_idletasks()
        w, h = self.root.winfo_width(), self.root.winfo_height()
        if w > 1 and h > 1:
            return w, h
        geo = self.root.geometry()
        try:
            wh = geo.split("+")[0]
            pw, ph = wh.split("x")
            return int(pw), int(ph)
        except ValueError:
            return 960, 720

    def _compute_cell_size(self) -> int:
        """左パネル内に 10×10 正方形グリッド全体が収まる辺長。"""
        self.root.update_idletasks()
        n = game_mod.GRID_SIZE
        avail_w = 280
        avail_h = 280

        if hasattr(self, "main_outer"):
            self.main_outer.update_idletasks()
            mw = self.main_outer.winfo_width()
            mh = self.main_outer.winfo_height()
            if mw > 1 and mh > 1:
                avail_w = mw - 36
                header_bottom = 118
                if hasattr(self, "grid_wrap"):
                    self.grid_wrap.update_idletasks()
                    gy = self.grid_wrap.winfo_y()
                    if gy > 0:
                        header_bottom = gy + 6
                avail_h = mh - header_bottom - 36

        if avail_w < 80 or avail_h < 80:
            win_w, win_h = self._parse_geometry()
            avail_w = max(avail_w, win_w - 320)
            avail_h = max(avail_h, win_h - 200)

        gutter = n + 4
        anim_reserve = n // 2 + 4
        cs = min(
            (avail_w - gutter) // n,
            (avail_h - gutter - anim_reserve) // n,
        )
        cs = int(cs * 0.94)
        return max(16, min(cs, CELL_SIZE))

    def _update_subtitle_wrap(self) -> None:
        if not hasattr(self, "subtitle_label"):
            return
        self.main_panel.update_idletasks()
        w = self.main_panel.winfo_width()
        if w > 80:
            self.subtitle_label.config(wraplength=max(160, w - 24))

    def _sync_cell_font_size(self) -> None:
        if self.cell_font is None:
            return
        px = max(8, min(14, self.cell_size // 2 + 2))
        self.cell_font.configure(size=px)

    def _reload_mark_photos(self) -> None:
        mark_px = max(20, self.cell_size - 4)
        self._mark_maru, self._mark_batsu = load_mark_photos(
            self.root, target_px=mark_px
        )

    def _build_grid_widgets(self, cell_font: tkfont.Font) -> None:
        self.cell_size = self._compute_cell_size()
        self._sync_cell_font_size()
        self._reload_mark_photos()
        if self.grid_frame is not None:
            self.grid_frame.destroy()
        self.labels.clear()
        self.play_canvases.clear()
        self.play_cell_frames.clear()

        size = game_mod.GRID_SIZE
        cs = self.cell_size
        total = size * cs
        self._grid_anim_pad = max(10, cs // 2)
        wrap_w = total + self._grid_anim_pad * 2
        wrap_h = total + self._grid_anim_pad * 2

        self.grid_wrap.config(width=wrap_w, height=wrap_h)
        self.grid_wrap.pack_propagate(False)

        self.grid_frame = tk.Frame(
            self.grid_wrap,
            bg=CELL_BORDER,
            width=total,
            height=total,
        )
        self.grid_frame.place(
            x=self._grid_anim_pad,
            y=self._grid_anim_pad,
            width=total,
            height=total,
        )
        self.grid_frame.pack_propagate(False)

        for ri in range(size):
            for ci in range(size):
                border = tk.Frame(
                    self.grid_frame,
                    bg=CELL_BORDER,
                    width=cs,
                    height=cs,
                )
                border.place(x=ci * cs, y=ri * cs, width=cs, height=cs)
                border.pack_propagate(False)
                if ri >= 1 and ci >= 1:
                    focus_frame = tk.Frame(border, bg=CELL_BORDER)
                    focus_frame.pack(fill="both", expand=True, padx=1, pady=1)
                    cv = tk.Canvas(
                        focus_frame,
                        bg=CELL_EMPTY,
                        highlightthickness=0,
                        bd=0,
                        relief="flat",
                    )
                    cv.pack(fill="both", expand=True)
                    self.play_cell_frames[(ri, ci)] = focus_frame
                    self.play_canvases[(ri, ci)] = cv
                else:
                    lbl = tk.Label(
                        border,
                        font=cell_font,
                        relief="flat",
                        borderwidth=0,
                        anchor="center",
                        justify="center",
                    )
                    lbl.pack(fill="both", expand=True, padx=1, pady=1)
                    self.labels[(ri, ci)] = lbl

        self.focus_overlay = tk.Frame(self.grid_frame, bg=FOCUS_RING_BRIGHT)
        self.focus_overlay_label = tk.Label(
            self.focus_overlay,
            font=cell_font,
            bg=CELL_EMPTY,
            fg=CELL_EMPTY_FG,
            relief="flat",
            borderwidth=0,
            anchor="center",
            justify="center",
        )
        self.focus_overlay_label.pack(
            fill="both",
            expand=True,
            padx=FOCUS_INSET,
            pady=FOCUS_INSET,
        )
        self.focus_overlay.place_forget()
        self.grid_frame.update_idletasks()
        self._update_subtitle_wrap()

    def _build_menu_frames(
        self, title_font: tkfont.Font, stat_font: tkfont.Font
    ) -> None:
        self.menu_layer = tk.Frame(self.shell, bg=UI.BG)
        self.menu_frames: dict[AppScreen, tk.Frame] = {}

        title = tk.Frame(self.menu_layer, bg=UI.BG)
        self.menu_frames[AppScreen.TITLE] = title
        tk.Label(
            title,
            text="100マス計算",
            font=self._menu_title_font,
            fg=UI.TEXT,
            bg=UI.BG,
        ).pack(pady=(48, 8))
        tk.Label(
            title,
            text="足し算の100マス計算（オフライン）",
            font=self._menu_body_font,
            fg=UI.TEXT_DIM,
            bg=UI.BG,
        ).pack(pady=(0, 32))
        self.title_menu_labels: list[tk.Label] = []
        for label in ("はじめる", "操作説明", "RESULT"):
            lbl = tk.Label(title, text="", font=self._menu_heading_font, bg=UI.BG)
            lbl.pack(pady=6)
            self.title_menu_labels.append(lbl)
        tk.Label(
            title,
            text="↑↓ で選ぶ　Enter で決定　Esc で終了",
            font=self._menu_small_font,
            fg=UI.TEXT_DIM,
            bg=UI.BG,
        ).pack(side="bottom", pady=24)

        help_f = tk.Frame(self.menu_layer, bg=UI.BG)
        self.menu_frames[AppScreen.HELP] = help_f
        tk.Label(
            help_f,
            text="操作説明",
            font=self._menu_heading_font,
            fg=UI.TEXT,
            bg=UI.BG,
        ).pack(pady=(32, 16))
        help_box = tk.Frame(help_f, bg=UI.BG)
        help_box.pack()
        for line in (
            "矢印キー / WASD … マス移動",
            "0〜9 … 答えの入力",
            "Enter … 終了して採点（確認あり）",
            "Backspace … 入力の削除",
            "",
            "演算 → マス目（4×4〜12×12）の順で選択",
            "わり算 … 小数第1位まで（第2位四捨五入）",
            "誤答 +10秒 / 未入力 +20秒（採点時）",
        ):
            tk.Label(
                help_box,
                text=line,
                font=stat_font,
                fg=UI.TEXT_DIM,
                bg=UI.BG,
            ).pack(anchor="w", pady=2)
        tk.Label(
            help_f,
            text="Enter / Esc で タイトルへ",
            font=self._menu_small_font,
            fg=UI.CURSOR_GOLD,
            bg=UI.BG,
        ).pack(side="bottom", pady=24)

        mode_f = tk.Frame(self.menu_layer, bg=UI.BG)
        self.menu_frames[AppScreen.MODE] = mode_f
        tk.Label(
            mode_f,
            text="演算を選ぶ",
            font=self._menu_heading_font,
            fg=UI.TEXT,
            bg=UI.BG,
        ).pack(pady=(48, 24))
        self.mode_menu_labels: list[tk.Label] = []
        for _op in self.operations:
            lbl = tk.Label(mode_f, text="", font=self._menu_heading_font, bg=UI.BG)
            lbl.pack(pady=10)
            self.mode_menu_labels.append(lbl)
        tk.Label(
            mode_f,
            text="Enter でマス目選択　Esc でタイトル",
            font=self._menu_small_font,
            fg=UI.TEXT_DIM,
            bg=UI.BG,
        ).pack(side="bottom", pady=24)

        grid_f = tk.Frame(self.menu_layer, bg=UI.BG)
        self.menu_frames[AppScreen.GRID] = grid_f
        tk.Label(
            grid_f,
            text="マス目を選ぶ",
            font=self._menu_heading_font,
            fg=UI.TEXT,
            bg=UI.BG,
        ).pack(pady=(48, 8))
        self.grid_op_hint_var = tk.StringVar(value="")
        tk.Label(
            grid_f,
            textvariable=self.grid_op_hint_var,
            font=self._menu_body_font,
            fg=UI.TEXT_DIM,
            bg=UI.BG,
        ).pack(pady=(0, 16))
        self.grid_menu_labels: list[tk.Label] = []
        for _size in self.grid_sizes:
            lbl = tk.Label(grid_f, text="", font=self._menu_heading_font, bg=UI.BG)
            lbl.pack(pady=10)
            self.grid_menu_labels.append(lbl)
        tk.Label(
            grid_f,
            text="Enter で開始　Esc で演算選択へ",
            font=self._menu_small_font,
            fg=UI.TEXT_DIM,
            bg=UI.BG,
        ).pack(side="bottom", pady=24)

        rank_f = tk.Frame(self.menu_layer, bg=UI.BG)
        self.menu_frames[AppScreen.SCORE_RANKING] = rank_f
        tk.Label(
            rank_f,
            text="RESULT",
            font=self._menu_heading_font,
            fg=UI.TEXT,
            bg=UI.BG,
        ).pack(pady=(16, 8))
        tab_row = tk.Frame(rank_f, bg=UI.BG)
        tab_row.pack(fill="x", padx=24)
        self.ranking_tab_labels: list[tk.Label] = []
        for op in self.operations:
            lbl = tk.Label(
                tab_row,
                text=OPERATION_RANKING_TAB[op],
                font=self._menu_body_font,
                fg=UI.TEXT_DIM,
                bg=UI.BG,
                padx=16,
            )
            lbl.pack(side="left", expand=True)
            self.ranking_tab_labels.append(lbl)
        grid_tab_row = tk.Frame(rank_f, bg=UI.BG)
        grid_tab_row.pack(fill="x", padx=24, pady=(8, 0))
        self.ranking_grid_tab_labels: list[tk.Label] = []
        for size in self.grid_sizes:
            lbl = tk.Label(
                grid_tab_row,
                text=grid_label(size),
                font=self._menu_body_font,
                fg=UI.TEXT_DIM,
                bg=UI.BG,
                padx=10,
            )
            lbl.pack(side="left", expand=True)
            self.ranking_grid_tab_labels.append(lbl)
        table_wrap = tk.Frame(rank_f, bg=UI.MAIN_INNER, padx=12, pady=8)
        table_wrap.pack(fill="both", expand=True, padx=24, pady=8)
        self.ranking_table = LeaderboardTable(table_wrap)
        self.ranking_table.pack(fill="both", expand=True)
        tk.Label(
            rank_f,
            text="↑↓ でマス目　←→ で演算　Esc でタイトルへ",
            font=self._menu_small_font,
            fg=UI.TEXT_DIM,
            bg=UI.BG,
        ).pack(pady=12)

    def _build_result_overlay(
        self, main_panel: tk.Frame, stat_font: tkfont.Font
    ) -> None:
        self.result_overlay = tk.Frame(main_panel, bg="#101828")
        self.result_overlay.place(relx=0, rely=0, relwidth=1, relheight=1)
        self.result_overlay.place_forget()

        self.result_title_var = tk.StringVar(value="採点結果")
        tk.Label(
            self.result_overlay,
            textvariable=self.result_title_var,
            font=self._menu_heading_font,
            fg=UI.TEXT,
            bg="#101828",
        ).pack(pady=(12, 4))
        self.result_summary_var = tk.StringVar(value="")
        tk.Label(
            self.result_overlay,
            textvariable=self.result_summary_var,
            font=stat_font,
            fg=UI.TEXT_DIM,
            bg="#101828",
        ).pack(pady=(0, 8))

        self.result_menu_labels: list[tk.Label] = []
        menu_box = tk.Frame(self.result_overlay, bg="#101828")
        menu_box.pack(side="bottom", pady=16)
        for _ in RESULT_MENU_ITEMS:
            lbl = tk.Label(menu_box, text="", font=self._menu_body_font, bg="#101828")
            lbl.pack(pady=4)
            self.result_menu_labels.append(lbl)

        self.result_confirm_frame = tk.Frame(self.result_overlay, bg="#101828")
        tk.Label(
            self.result_confirm_frame,
            text="スコア登録",
            font=self._menu_body_font,
            fg=UI.TEXT,
            bg="#101828",
        ).pack(pady=(8, 4))
        self.result_preview_table = LeaderboardTable(self.result_confirm_frame)
        self.result_preview_table.pack(fill="both", expand=True, padx=8, pady=4)
        tk.Label(
            self.result_confirm_frame,
            text="Enter で名前入力　Esc で戻る",
            font=self._menu_small_font,
            fg=UI.TEXT_DIM,
            bg="#101828",
        ).pack(pady=4)

        self.result_entry_frame = tk.Frame(self.result_overlay, bg="#1a5866")
        self.result_entry_line_var = tk.StringVar(value="")
        tk.Label(
            self.result_entry_frame,
            textvariable=self.result_entry_line_var,
            font=self._menu_body_font,
            fg=UI.TEXT,
            bg="#1a5866",
            anchor="w",
        ).pack(fill="x", padx=12, pady=8)
        kb_wrap = tk.Frame(self.result_entry_frame, bg="#1a5866")
        kb_wrap.pack(fill="both", expand=True, padx=8, pady=4)
        self.name_keyboard_view = NameKeyboardView(kb_wrap, KEYBOARD_ROWS)
        self.name_keyboard_view.pack()
        tk.Label(
            self.result_entry_frame,
            text="↑↓←→ で選択　Enter で決定　Backspace で1文字消す　「戻」で登録",
            font=self._menu_small_font,
            fg=UI.TEXT_DIM,
            bg="#1a5866",
        ).pack(pady=6)

