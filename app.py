"""100マス計算（足し算）— tkinter GUI。"""

from __future__ import annotations

import time
import tkinter as tk
from collections.abc import Callable
from datetime import datetime
from enum import StrEnum, auto
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
from masu.config import (
    GRID_SIZES,
    OPERATION_LABELS,
    OPERATION_RANKING_TAB,
    OPERATION_SYMBOLS,
    STAR_GAUGE_MAX,
    Operation,
    grid_label,
)
from masu import theme as UI
from masu.launcher import resolve_display_settings
from masu.leaderboard import format_accuracy, format_score, preview_after_insert
from masu.mark_assets import load_mark_photos
from masu.menu_views import LeaderboardTable, NameKeyboardView
from masu.name_keyboard import (
    ENTRY_NAME_MAX_LEN,
    KEYBOARD_ROWS,
    NameKeyboard,
    format_name_slots,
)
from masu.scoring import accuracy_percent, compute_session_score
from masu.score_store import ScoreStore
from masu.sidebar import StatusSidebar

BG = "#0f172a"
PANEL = "#1e293b"
HEADER_NUM = "#334155"
HEADER_NUM_FG = "#f8fafc"
HEADER_SLOT = "#1a2332"
HEADER_SLOT_FG = "#475569"
TILE_SHADOW = "#0b1220"
PLUS_BG = "#78350f"
PLUS_FG = "#fde68a"
CELL_EMPTY = "#475569"
CELL_EMPTY_FG = "#f1f5f9"
FOCUS_RING_BRIGHT = "#5cff9a"
FOCUS_RING_DIM = "#2eb86a"
FOCUS_PULSE_STEPS = 30
FOCUS_PULSE_MS = 30
ACCENT = "#6366f1"
TEXT_MUTED = "#94a3b8"
TIMER_FG = "#34d399"
PROGRESS_FG = "#38bdf8"
CELL_BORDER = "#1e293b"

FONT_FAMILY = "Yu Gothic UI"

INTRO_SCALE_LEVELS = (1.85, 1.55, 1.28, 1.1, 1.0)
INTRO_SCALE_MS = 20
INTRO_STAGGER_MS = 30
INTRO_GLOW = "#e2e8f0"
SCORE_SCALE_LEVELS = (1.85, 1.55, 1.28, 1.1, 1.0)
SCORE_SCALE_MS = 20
SCORE_STAGGER_MS = 30
SCORE_GLOW = "#e2e8f0"
SCORE_OK_TOP = "#059669"
SCORE_OK_BOTTOM = "#064e3b"
SCORE_OK_FG = "#ecfdf5"
SCORE_NG_TOP = "#dc2626"
SCORE_NG_BOTTOM = "#450a0a"
SCORE_NG_FG = "#fee2e2"
HEADER_TILE_TOP = "#5a7ea8"
HEADER_TILE_BOTTOM = "#334155"
FOCUS_INSET = 3
FOCUS_SCALE = 1.1
CELL_SIZE = 48

RESULT_MENU_ITEMS = ("タイトルへ戻る", "スコアを記録する")


class AppScreen(StrEnum):
    TITLE = auto()
    HELP = auto()
    MODE = auto()
    GRID = auto()
    GAME = auto()
    SCORE_RANKING = auto()


class MasuApp:
    def __init__(self, root: tk.Tk) -> None:
        self.root = root
        self.root.title("100マス計算（足し算）")
        self.root.configure(bg=UI.BG)
        self.root.minsize(640, 480)

        self.score_store = ScoreStore.load()
        self.operation = Operation.ADD
        self.grid_size = GRID_SIZES[0]
        self.pending_operation = Operation.ADD
        self.display_score = 0
        self.last_accuracy = 0.0
        self.last_played_at = ""
        self._result_score_saved = False

        self.current = AppScreen.TITLE
        self.title_menu = 0
        self.mode_cursor = 0
        self.grid_cursor = 0
        self.ranking_operation_cursor = 0
        self.ranking_grid_cursor = 0
        self.grid_sizes = list(GRID_SIZES)
        self.result_view = "menu"
        self.result_menu = 0
        self.entry_name = ""
        self.entry_preview_rank = 1
        self.name_keyboard = NameKeyboard()
        self.operations = [
            Operation.ADD,
            Operation.SUB,
            Operation.MUL,
            Operation.DIV,
        ]

        self.state = GameState()
        self.labels: dict[tuple[int, int], tk.Label] = {}
        self.play_canvases: dict[tuple[int, int], tk.Canvas] = {}
        self.play_cell_frames: dict[tuple[int, int], tk.Frame] = {}
        self.timer_job: str | None = None
        self.timer_paused_at: float | None = None
        self.intro_animating = False
        self.revealed_headers: set[tuple[int, int]] = set()
        self._intro_gen = 0
        self.score_animating = False
        self.score_marks: dict[tuple[int, int], bool] = {}
        self._mark_maru: tk.PhotoImage | None = None
        self._mark_batsu: tk.PhotoImage | None = None
        self._score_gen = 0
        self.cell_font: tkfont.Font | None = None
        self.focus_blink_job: str | None = None
        self.focus_overlay_active = False

        self._resize_job: str | None = None
        self._last_root_size = (0, 0)
        self.grid_frame: tk.Frame | None = None
        self.cell_size = CELL_SIZE

        self._build_ui()
        self._bind_keys()
        self.root.bind("<Configure>", self._on_root_configure, add="+")
        self._show_screen(AppScreen.TITLE)

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

    def _show_screen(self, screen: AppScreen) -> None:
        self.current = screen
        self.result_overlay.place_forget()
        if screen is AppScreen.GAME:
            self.menu_layer.pack_forget()
            self.game_layer.pack(fill="both", expand=True)
        else:
            self.game_layer.pack_forget()
            self.menu_layer.pack(fill="both", expand=True)
            for f in self.menu_frames.values():
                f.pack_forget()
            frame = self.menu_frames[screen]
            frame.pack(fill="both", expand=True)
        self._refresh_menu_highlights()

    def _refresh_menu_highlights(self) -> None:
        if self.current is AppScreen.TITLE:
            items = ("はじめる", "操作説明", "RESULT")
            for i, lbl in enumerate(self.title_menu_labels):
                sel = i == self.title_menu
                prefix = "▶ " if sel else "  "
                lbl.config(
                    text=prefix + items[i],
                    fg=UI.CURSOR_GOLD if sel else UI.TEXT,
                )
        elif self.current is AppScreen.MODE:
            for i, (op, lbl) in enumerate(
                zip(self.operations, self.mode_menu_labels, strict=True)
            ):
                sel = i == self.mode_cursor
                prefix = "▶ " if sel else "  "
                lbl.config(
                    text=prefix + OPERATION_LABELS[op],
                    fg=UI.CURSOR_GOLD if sel else UI.TEXT,
                )
        elif self.current is AppScreen.GRID:
            op = self.pending_operation
            self.grid_op_hint_var.set(OPERATION_LABELS[op])
            for i, (size, lbl) in enumerate(
                zip(self.grid_sizes, self.grid_menu_labels, strict=True)
            ):
                sel = i == self.grid_cursor
                prefix = "▶ " if sel else "  "
                lbl.config(
                    text=prefix + grid_label(size),
                    fg=UI.CURSOR_GOLD if sel else UI.TEXT,
                )
        elif self.current is AppScreen.SCORE_RANKING:
            for i, (_op, lbl) in enumerate(
                zip(self.operations, self.ranking_tab_labels, strict=True)
            ):
                sel = i == self.ranking_operation_cursor
                lbl.config(
                    fg=UI.CURSOR_GOLD if sel else UI.TEXT_DIM,
                )
            for i, (_size, lbl) in enumerate(
                zip(self.grid_sizes, self.ranking_grid_tab_labels, strict=True)
            ):
                sel = i == self.ranking_grid_cursor
                lbl.config(
                    fg=UI.CURSOR_GOLD if sel else UI.TEXT_DIM,
                )
            picked_op = self.operations[self.ranking_operation_cursor]
            picked_grid = self.grid_sizes[self.ranking_grid_cursor]
            self.ranking_table.set_entries(
                self.score_store.leaderboard(picked_op, picked_grid)
            )

    def _nav_vertical(self, delta: int, cursor: int, count: int) -> int:
        if count <= 0:
            return cursor
        return (cursor + delta) % count

    def _start_game(self, operation: Operation, grid_size: int) -> None:
        self.operation = operation
        self.grid_size = grid_size
        configure_grid(grid_size)
        self.state.operation = operation
        self._reset_result_flow()
        self._result_score_saved = False
        self._show_screen(AppScreen.GAME)
        self.root.update_idletasks()
        if self.cell_font is not None:
            self._build_grid_widgets(self.cell_font)
        self.restart()

    def _on_root_configure(self, event: tk.Event) -> None:
        if event.widget is not self.root:
            return
        size = (event.width, event.height)
        if size == self._last_root_size:
            return
        self._last_root_size = size
        if self.current is not AppScreen.GAME or not self.play_canvases:
            return
        if self._resize_job:
            self.root.after_cancel(self._resize_job)
        self._resize_job = self.root.after(120, self._maybe_resize_grid)

    def _maybe_resize_grid(self) -> None:
        self._resize_job = None
        if self.current is not AppScreen.GAME or self.cell_font is None:
            return
        new_cs = self._compute_cell_size()
        if new_cs == self.cell_size:
            return
        self._build_grid_widgets(self.cell_font)
        self._update_subtitle_wrap()
        self.refresh()
        self._sync_focus_overlay()

    def _restart_current_game(self) -> None:
        if self.current is AppScreen.GAME:
            self.restart()

    def _return_to_title(self) -> None:
        self._reset_result_flow()
        self.result_overlay.place_forget()
        self.title_menu = 0
        self._show_screen(AppScreen.TITLE)

    def _reset_result_flow(self) -> None:
        self.result_view = "menu"
        self.result_menu = 0
        self.entry_name = ""
        self.name_keyboard = NameKeyboard()

    def _bind_keys(self) -> None:
        for seq in (
            "<Up>",
            "<Down>",
            "<Left>",
            "<Right>",
            "<w>",
            "<W>",
            "<a>",
            "<A>",
            "<s>",
            "<S>",
            "<d>",
            "<D>",
            "<BackSpace>",
            "<Return>",
            "<KP_Enter>",
            "<Escape>",
        ):
            self.root.bind_all(seq, self._on_key, add="+")
        for d in "0123456789":
            self.root.bind_all(d, self._on_key, add="+")

    def _on_key(self, event: tk.Event) -> str | None:
        keysym = event.keysym
        char = event.char

        if self.result_overlay.winfo_ismapped():
            return self._on_key_result(keysym, char)

        if self.current is AppScreen.TITLE:
            return self._on_key_title(keysym)
        if self.current is AppScreen.HELP:
            return self._on_key_help(keysym)
        if self.current is AppScreen.MODE:
            return self._on_key_mode(keysym)
        if self.current is AppScreen.GRID:
            return self._on_key_grid(keysym)
        if self.current is AppScreen.SCORE_RANKING:
            return self._on_key_ranking(keysym)
        if self.current is not AppScreen.GAME:
            return None
        return self._on_key_game(event, keysym, char)

    def _on_key_title(self, keysym: str) -> str:
        if keysym == "Up":
            self.title_menu = self._nav_vertical(-1, self.title_menu, 3)
            self._refresh_menu_highlights()
        elif keysym == "Down":
            self.title_menu = self._nav_vertical(1, self.title_menu, 3)
            self._refresh_menu_highlights()
        elif keysym in ("Return", "KP_Enter"):
            if self.title_menu == 0:
                self.mode_cursor = 0
                self._show_screen(AppScreen.MODE)
            elif self.title_menu == 1:
                self._show_screen(AppScreen.HELP)
            else:
                self.ranking_operation_cursor = 0
                self.ranking_grid_cursor = 0
                self._show_screen(AppScreen.SCORE_RANKING)
        elif keysym == "Escape":
            self.root.quit()
        return "break"

    def _on_key_help(self, keysym: str) -> str:
        if keysym in ("Return", "KP_Enter", "Escape"):
            self._show_screen(AppScreen.TITLE)
        return "break"

    def _on_key_mode(self, keysym: str) -> str:
        if keysym == "Up":
            self.mode_cursor = self._nav_vertical(-1, self.mode_cursor, len(self.operations))
            self._refresh_menu_highlights()
        elif keysym == "Down":
            self.mode_cursor = self._nav_vertical(1, self.mode_cursor, len(self.operations))
            self._refresh_menu_highlights()
        elif keysym in ("Return", "KP_Enter"):
            self.pending_operation = self.operations[self.mode_cursor]
            self.grid_cursor = 0
            self._show_screen(AppScreen.GRID)
        elif keysym == "Escape":
            self._show_screen(AppScreen.TITLE)
        return "break"

    def _on_key_grid(self, keysym: str) -> str:
        n = len(self.grid_sizes)
        if keysym == "Up":
            self.grid_cursor = self._nav_vertical(-1, self.grid_cursor, n)
            self._refresh_menu_highlights()
        elif keysym == "Down":
            self.grid_cursor = self._nav_vertical(1, self.grid_cursor, n)
            self._refresh_menu_highlights()
        elif keysym in ("Return", "KP_Enter"):
            self._start_game(
                self.pending_operation,
                self.grid_sizes[self.grid_cursor],
            )
        elif keysym == "Escape":
            self._show_screen(AppScreen.MODE)
        return "break"

    def _on_key_ranking(self, keysym: str) -> str:
        n_op = len(self.operations)
        n_grid = len(self.grid_sizes)
        if keysym in ("Left",):
            self.ranking_operation_cursor = (self.ranking_operation_cursor - 1) % n_op
            self._refresh_menu_highlights()
        elif keysym in ("Right",):
            self.ranking_operation_cursor = (self.ranking_operation_cursor + 1) % n_op
            self._refresh_menu_highlights()
        elif keysym in ("Up",):
            self.ranking_grid_cursor = (self.ranking_grid_cursor - 1) % n_grid
            self._refresh_menu_highlights()
        elif keysym in ("Down",):
            self.ranking_grid_cursor = (self.ranking_grid_cursor + 1) % n_grid
            self._refresh_menu_highlights()
        elif keysym == "Escape":
            self._show_screen(AppScreen.TITLE)
        return "break"

    def _on_key_game(
        self, event: tk.Event, keysym: str, char: str
    ) -> str | None:
        if self.state.finished or self.intro_animating or self.score_animating:
            return "break"

        moves = {
            "Up": (-1, 0),
            "w": (-1, 0),
            "W": (-1, 0),
            "Down": (1, 0),
            "s": (1, 0),
            "S": (1, 0),
            "Left": (0, -1),
            "a": (0, -1),
            "A": (0, -1),
            "Right": (0, 1),
            "d": (0, 1),
            "D": (0, 1),
        }
        if keysym in moves or char in moves:
            dr, dc = moves.get(keysym) or moves[char]
            self.state.move_focus(dr, dc)
            self.refresh()
            return "break"

        if keysym == "BackSpace":
            self.state.handle_backspace()
            self.refresh()
            self._update_end_guide()
            return "break"

        if keysym in ("Return", "KP_Enter"):
            self.state.commit_focus()
            self.refresh()
            self._prompt_end_game()
            return "break"

        if char and char.isdigit():
            self.state.handle_char(char)
            self.refresh()
            return "break"

        if self.state.operation is Operation.SUB and keysym in ("minus", "underscore"):
            self.state.handle_char("-")
            self.refresh()
            return "break"

        if self.state.operation is Operation.DIV and keysym in (
            "period",
            "KP_Decimal",
        ):
            self.state.handle_char(".")
            self.refresh()
            return "break"

        if keysym == "Escape":
            self._return_to_title()
            return "break"

        return None

    def _on_key_result(self, keysym: str, char: str) -> str:
        if self.result_view == "entry":
            if keysym == "Escape":
                self.result_view = "confirm"
                self._show_result_panel()
                return "break"
            if keysym == "Up":
                self.name_keyboard.move(-1, 0)
            elif keysym == "Down":
                self.name_keyboard.move(1, 0)
            elif keysym == "Left":
                self.name_keyboard.move(0, -1)
            elif keysym == "Right":
                self.name_keyboard.move(0, 1)
            elif keysym == "BackSpace":
                self.entry_name = self.entry_name[:-1]
            elif keysym in ("Return", "KP_Enter"):
                new_name, done = self.name_keyboard.apply_key(
                    self.entry_name,
                    ENTRY_NAME_MAX_LEN,
                )
                self.entry_name = new_name
                if done:
                    self._submit_score_entry()
            self._show_result_panel()
            return "break"

        if self.result_view == "confirm":
            if keysym == "Escape":
                self.result_view = "menu"
                self._show_result_panel()
            elif keysym in ("Return", "KP_Enter"):
                self.name_keyboard = NameKeyboard()
                self.result_view = "entry"
                self._show_result_panel()
            return "break"

        if keysym == "Up":
            self.result_menu = self._nav_vertical(-1, self.result_menu, 2)
            self._show_result_panel()
        elif keysym == "Down":
            self.result_menu = self._nav_vertical(1, self.result_menu, 2)
            self._show_result_panel()
        elif keysym in ("Return", "KP_Enter"):
            if self.result_menu == 0:
                self._return_to_title()
            else:
                self._begin_score_record()
        return "break"

    def restart(self) -> None:
        self._intro_gen += 1
        if self.timer_job:
            self.root.after_cancel(self.timer_job)
            self.timer_job = None
        self.timer_paused_at = None
        self.intro_animating = True
        self.revealed_headers = set()
        self.score_animating = False
        self.score_marks = {}
        self._score_gen += 1
        self.display_score = 0
        self.last_accuracy = 0.0
        self._stop_focus_blink()
        self.state.operation = self.operation
        self.state.reset()
        self.state.start_monotonic = None
        self.refresh()
        self._run_intro_animation()

    def _run_intro_animation(self, index: int = 0) -> None:
        gen = self._intro_gen
        cells = header_reveal_cells()
        if index >= len(cells):
            if gen != self._intro_gen:
                return
            self.intro_animating = False
            self.state.start_monotonic = time.monotonic()
            self._tick_timer()
            self.refresh()
            return

        gr, gc = cells[index]
        text = str(self.state.header[gr][gc])

        def on_pop_complete() -> None:
            if gen != self._intro_gen:
                return
            self.revealed_headers.add((gr, gc))
            self.refresh()
            self.root.after(
                INTRO_STAGGER_MS,
                lambda n=index: self._run_intro_animation(n + 1),
            )

        self._animate_tile_pop(
            gr,
            gc,
            text,
            tile_top_bg=HEADER_TILE_TOP,
            tile_bottom_bg=HEADER_TILE_BOTTOM,
            fg=HEADER_NUM_FG,
            glow_color=INTRO_GLOW,
            scale_levels=INTRO_SCALE_LEVELS,
            scale_ms=INTRO_SCALE_MS,
            is_stale=lambda: gen != self._intro_gen,
            on_complete=on_pop_complete,
        )

    @staticmethod
    def _lerp_hex_color(color_a: str, color_b: str, t: float) -> str:
        t = max(0.0, min(1.0, t))
        parts_a = (int(color_a[i : i + 2], 16) for i in (1, 3, 5))
        parts_b = (int(color_b[i : i + 2], 16) for i in (1, 3, 5))
        mixed = [
            int(a + (b - a) * t) for a, b in zip(parts_a, parts_b, strict=True)
        ]
        return f"#{mixed[0]:02x}{mixed[1]:02x}{mixed[2]:02x}"

    def _stop_focus_blink(self) -> None:
        if self.focus_blink_job:
            self.root.after_cancel(self.focus_blink_job)
            self.focus_blink_job = None
        self.focus_overlay_active = False

    def _focus_pulse_tick(self, step: int = 0, ascending: bool = True) -> None:
        if not self.focus_overlay_active:
            return
        span = FOCUS_PULSE_STEPS - 1
        if ascending:
            t = step / span
        else:
            t = (span - step) / span
        color = self._lerp_hex_color(FOCUS_RING_DIM, FOCUS_RING_BRIGHT, t)
        self.focus_overlay.config(bg=color)

        if step + 1 >= FOCUS_PULSE_STEPS:
            next_step = 0
            next_ascending = not ascending
        else:
            next_step = step + 1
            next_ascending = ascending

        self.focus_blink_job = self.root.after(
            FOCUS_PULSE_MS,
            lambda s=next_step, a=next_ascending: self._focus_pulse_tick(s, a),
        )

    def _start_focus_blink(self) -> None:
        self._stop_focus_blink()
        self.focus_overlay_active = True
        self.focus_overlay.config(bg=FOCUS_RING_DIM)
        self._focus_pulse_tick(step=0, ascending=True)

    def _sync_focus_overlay(self) -> None:
        s = self.state
        if (
            s.finished
            or self.intro_animating
            or self.score_animating
            or not s.is_play_cell(s.focus_row, s.focus_col)
        ):
            self.focus_overlay.place_forget()
            self._stop_focus_blink()
            return

        gr, gc = s.focus_row, s.focus_col
        x, y, w, h = self._grid_cell_rect(gr, gc)
        side = min(w, h)
        ow = int(side * FOCUS_SCALE)
        oh = int(side * FOCUS_SCALE)
        ox = x + (w - ow) // 2
        oy = y + (h - oh) // 2

        text = s.play_cell_display(gr, gc)
        fg = TEXT_MUTED if text == "｜" else CELL_EMPTY_FG
        self.focus_overlay_label.config(text=text, fg=fg)
        self.focus_overlay.place(x=ox, y=oy, width=ow, height=oh)
        self.focus_overlay.lift()
        if not self.focus_blink_job:
            self._start_focus_blink()

    def _grid_cell_rect(self, gr: int, gc: int) -> tuple[int, int, int, int]:
        """grid_frame 座標系でのマス矩形。"""
        cs = self.cell_size
        return gc * cs, gr * cs, cs, cs

    def _grid_cell_rect_in_wrap(self, gr: int, gc: int) -> tuple[int, int, int, int]:
        """grid_wrap 座標系（ポップアニメ用）。"""
        pad = getattr(self, "_grid_anim_pad", 0)
        x, y, w, h = self._grid_cell_rect(gr, gc)
        return pad + x, pad + y, w, h

    def _play_canvas_center(self, cv: tk.Canvas) -> tuple[int, int]:
        cv.update_idletasks()
        w = cv.winfo_width()
        h = cv.winfo_height()
        if w < 2:
            w = self.cell_size - 2
        if h < 2:
            h = self.cell_size - 2
        return w // 2, h // 2

    def _draw_play_cell(self, gr: int, gc: int) -> None:
        cv = self.play_canvases[(gr, gc)]
        s = self.state
        cv.delete("all")
        cv.config(bg=CELL_EMPTY)
        cx, cy = self._play_canvas_center(cv)

        if (gr, gc) in self.score_marks:
            text = s.cells[(gr, gc)].value
            photo = (
                self._mark_maru
                if self.score_marks[(gr, gc)]
                else self._mark_batsu
            )
            if text:
                cv.create_text(
                    cx,
                    cy,
                    text=text,
                    fill=CELL_EMPTY_FG,
                    font=self.cell_font,
                )
            cv.create_image(cx, cy, image=photo, anchor=tk.CENTER)
            return

        focused = (
            gr == s.focus_row
            and gc == s.focus_col
            and not s.finished
            and not self.intro_animating
            and not self.score_animating
        )
        cell = s.cells[(gr, gc)]
        text = "" if focused else cell.value
        if text:
            cv.create_text(
                cx,
                cy,
                text=text,
                fill=CELL_EMPTY_FG,
                font=self.cell_font,
            )

    def _animate_tile_pop(
        self,
        gr: int,
        gc: int,
        text: str,
        *,
        tile_top_bg: str,
        tile_bottom_bg: str,
        fg: str,
        glow_color: str,
        scale_levels: tuple[float, ...],
        scale_ms: int,
        is_stale: Callable[[], bool],
        on_complete: Callable[[], None],
        mark_image: tk.PhotoImage | None = None,
    ) -> None:
        pop_root = self.grid_wrap
        x, y, w, h = self._grid_cell_rect_in_wrap(gr, gc)

        glow = tk.Label(pop_root, bg=glow_color, borderwidth=0)
        tile_wrap = tk.Frame(
            pop_root,
            bg=CELL_BORDER,
            borderwidth=0,
            highlightthickness=0,
        )
        tile_top = tk.Label(
            tile_wrap,
            text=text,
            image=mark_image,
            compound=tk.CENTER if mark_image is not None else tk.NONE,
            font=self.cell_font,
            bg=tile_top_bg,
            fg=fg,
            borderwidth=0,
            relief="flat",
            anchor="center",
            justify="center",
        )
        if mark_image is not None:
            tile_top.image = mark_image
        tile_bottom = tk.Label(
            tile_wrap,
            text="",
            font=self.cell_font,
            bg=tile_bottom_bg,
            borderwidth=0,
            relief="flat",
            height=0 if mark_image is not None else 1,
        )
        tile_top.pack(fill="both", expand=True)
        if mark_image is None:
            tile_bottom.pack(fill="x")

        def cleanup() -> None:
            glow.destroy()
            tile_wrap.destroy()

        def show_scale(scale_index: int = 0) -> None:
            if is_stale():
                cleanup()
                return
            if scale_index >= len(scale_levels):
                cleanup()
                on_complete()
                return

            scale = scale_levels[scale_index]
            tile_w = max(w, int(w * scale))
            tile_h = max(h, int(h * scale))
            tile_x = x + (w - tile_w) // 2
            tile_y = y + (h - tile_h) // 2

            glow_pad = max(2, int(6 * (scale - 1.0)))
            if scale > 1.02:
                glow.place(
                    x=tile_x - glow_pad,
                    y=tile_y - glow_pad,
                    width=tile_w + glow_pad * 2,
                    height=tile_h + glow_pad * 2,
                )
                glow.lift()
            else:
                glow.place_forget()

            tile_wrap.place(x=tile_x, y=tile_y, width=tile_w, height=tile_h)
            tile_wrap.lift()
            self.root.after(
                scale_ms,
                lambda n=scale_index: show_scale(n + 1),
            )

        show_scale(0)

    def _cell_is_correct(self, row: int, col: int) -> bool:
        return self.state.cells[(row, col)].state == CellState.CORRECT

    def _run_score_animation(self, index: int = 0) -> None:
        gen = self._score_gen
        order = play_cells_score_order()
        if index >= len(order):
            if gen != self._score_gen:
                return
            self.score_animating = False
            self.refresh()
            self._show_score_results()
            return

        row, col = order[index]
        correct = self._cell_is_correct(row, col)
        answer = self.state.cells[(row, col)].value
        mark_image = self._mark_maru if correct else self._mark_batsu
        tile_bg, sym_fg = CELL_EMPTY, CELL_EMPTY_FG

        def on_pop_complete() -> None:
            if gen != self._score_gen:
                return
            self.score_marks[(row, col)] = correct
            self.refresh()
            self.root.after(
                SCORE_STAGGER_MS,
                lambda n=index: self._run_score_animation(n + 1),
            )

        self._animate_tile_pop(
            row,
            col,
            answer,
            tile_top_bg=tile_bg,
            tile_bottom_bg=tile_bg,
            fg=sym_fg,
            glow_color=SCORE_GLOW,
            scale_levels=SCORE_SCALE_LEVELS,
            scale_ms=SCORE_SCALE_MS,
            is_stale=lambda: gen != self._score_gen,
            on_complete=on_pop_complete,
            mark_image=mark_image,
        )

    def _pause_timer(self) -> None:
        if self.state.finished or self.timer_paused_at is not None:
            return
        if self.timer_job:
            self.root.after_cancel(self.timer_job)
            self.timer_job = None
        self.timer_paused_at = time.monotonic()

    def _end_pause_on_clock(self) -> None:
        if self.timer_paused_at is None:
            return
        paused_for = time.monotonic() - self.timer_paused_at
        if self.state.start_monotonic is not None:
            self.state.start_monotonic += paused_for
        self.timer_paused_at = None

    def _resume_timer(self) -> None:
        if self.state.finished or self.timer_paused_at is None:
            return
        self._end_pause_on_clock()
        self._tick_timer()

    def _tick_timer(self) -> None:
        if not self.state.finished and self.timer_paused_at is None:
            self.sidebar.time_var.set(
                GameState.format_time(self.state.elapsed_seconds())
            )
            self.timer_job = self.root.after(10, self._tick_timer)

    def _update_end_guide(self) -> None:
        if self.state.finished or self.intro_animating or self.score_animating:
            self.guide_var.set("")
            return
        self.guide_var.set("Enter キーで終了・採点の確認")

    def _prompt_end_game(self) -> None:
        if self.state.finished or self.intro_animating or self.score_animating:
            return

        self._pause_timer()
        end_now = messagebox.askyesno(
            "採点の確認",
            "終了して採点しますか？\n\n"
            "「はい」… 終了して採点する\n"
            "「いいえ」… 入力を続ける",
        )
        if end_now:
            self._end_pause_on_clock()
            self._finish_with_score()
        else:
            self._resume_timer()
            self._update_end_guide()

    def _finish_with_score(self) -> None:
        self.state.validate_all_cells()
        correct = self.state.correct_count()
        play_n = game_mod.PLAY_CELL_COUNT
        self.last_accuracy = accuracy_percent(correct, play_n)
        elapsed = self.state.elapsed_seconds()
        self.display_score = compute_session_score(
            elapsed, correct, play_n, self.operation
        )
        if not self._result_score_saved:
            self.score_store.record_session(
                self.operation, self.grid_size, self.display_score
            )
            self._result_score_saved = True
        self.last_played_at = datetime.now().strftime("%Y/%m/%d %H:%M")
        self.state.finish()
        self._stop_focus_blink()
        self.focus_overlay.place_forget()
        if self.timer_job:
            self.root.after_cancel(self.timer_job)
            self.timer_job = None
        self.guide_var.set("")
        self._update_sidebar()
        self.score_marks = {}
        self.score_animating = True
        self._score_gen += 1
        self.refresh()
        self._run_score_animation()

    def _show_score_results(self) -> None:
        wrong = self.state.wrong_count()
        empty = self.state.empty_count()
        if wrong == 0 and empty == 0:
            self.result_title_var.set("クリア！")
            self.result_summary_var.set("すべて正解です。")
        else:
            self.result_title_var.set("採点結果")
            parts = []
            if wrong:
                parts.append(f"不正解: {wrong} マス")
            if empty:
                parts.append(
                    f"未入力: {empty} マス（+{PENALTY_EMPTY_SECONDS * empty} 秒）"
                )
            final = GameState.format_time(self.state.elapsed_seconds())
            parts.append(
                f"正解 {self.state.correct_count()}/{game_mod.PLAY_CELL_COUNT}  "
                f"得点 {self.display_score:,}  "
                f"タイム {final}"
            )
            self.result_summary_var.set(" / ".join(parts))
        self._reset_result_flow()
        self._show_result_panel()
        self.result_overlay.lift()
        self.result_overlay.place(relx=0, rely=0, relwidth=1, relheight=1)

    def _show_result_panel(self) -> None:
        self.result_confirm_frame.pack_forget()
        self.result_entry_frame.pack_forget()
        for lbl in self.result_menu_labels:
            lbl.pack_forget()

        if self.result_view == "confirm":
            entries, rank = preview_after_insert(
                self.score_store.leaderboard(self.operation, self.grid_size),
                name=self.entry_name.strip() or "-",
                score=self.display_score,
                played_at=self.last_played_at,
                accuracy=self.last_accuracy,
            )
            self.result_preview_table.set_entries(entries, highlight_rank=rank)
            self.result_confirm_frame.pack(fill="both", expand=True)
        elif self.result_view == "entry":
            rank = self.entry_preview_rank
            name_field = format_name_slots(self.entry_name, ENTRY_NAME_MAX_LEN)
            score_s = format_score(self.display_score)
            date_s = self.last_played_at or "----/--/-- --:--"
            acc_s = format_accuracy(self.last_accuracy)
            self.result_entry_line_var.set(
                f"No.{rank:02d}  {name_field}  {score_s}  {date_s}  {acc_s}"
            )
            self.name_keyboard_view.highlight(
                self.name_keyboard.row,
                self.name_keyboard.col,
            )
            self.result_entry_frame.pack(fill="both", expand=True)
        else:
            for i, (text, lbl) in enumerate(
                zip(RESULT_MENU_ITEMS, self.result_menu_labels, strict=True)
            ):
                sel = i == self.result_menu
                prefix = "▶ " if sel else "  "
                lbl.config(
                    text=prefix + text,
                    fg=UI.CURSOR_GOLD if sel else UI.TEXT,
                )
                lbl.pack(pady=4)

    def _begin_score_record(self) -> None:
        self.entry_name = ""
        _, rank = preview_after_insert(
            self.score_store.leaderboard(self.operation, self.grid_size),
            name="-",
            score=self.display_score,
            played_at=self.last_played_at,
            accuracy=self.last_accuracy,
        )
        self.entry_preview_rank = rank
        self.name_keyboard = NameKeyboard()
        self.result_view = "confirm"
        self._show_result_panel()

    def _submit_score_entry(self) -> None:
        name = self.entry_name.strip()
        if name:
            self.score_store.add_leaderboard_entry(
                self.operation,
                self.grid_size,
                name,
                self.display_score,
                played_at=self.last_played_at,
                accuracy=self.last_accuracy,
            )
        self.entry_name = ""
        self.result_view = "menu"
        self._update_sidebar()
        self._show_result_panel()

    def _update_sidebar(self) -> None:
        s = self.state
        if self.score_marks:
            correct_n = sum(1 for m in self.score_marks.values() if m)
        elif s.finished:
            correct_n = s.correct_count()
        else:
            correct_n = s.filled_count()
        play_n = game_mod.PLAY_CELL_COUNT
        correct_text = (
            f"{s.correct_count()} / {play_n}"
            if s.finished
            else f"{s.filled_count()} / {play_n}"
        )
        answer_stars = int(STAR_GAUGE_MAX * correct_n / play_n)
        skill_stars = max(
            0,
            STAR_GAUGE_MAX - min(STAR_GAUGE_MAX, s.penalty_count),
        )
        elapsed = s.elapsed_seconds() if s.start_monotonic is not None else 0.0
        if s.finished:
            elapsed = s.elapsed_seconds()
        time_text = GameState.format_time(elapsed)
        self.sidebar.update_display(
            mode_en=(
                f"{OPERATION_LABELS[self.operation]} "
                f"{grid_label(self.grid_size)}"
            ),
            high_score=self.score_store.high_score(
                self.operation, self.grid_size
            ),
            session_score=self.display_score,
            correct_text=correct_text,
            answer_stars=answer_stars,
            skill_stars=skill_stars,
            time_text=time_text,
        )

    def _style_label(
        self,
        lbl: tk.Label,
        text: str,
        bg: str,
        fg: str,
    ) -> None:
        lbl.config(
            text=text,
            bg=bg,
            fg=fg,
            image="",
            compound=tk.NONE,
            highlightthickness=0,
            relief="flat",
            borderwidth=0,
            anchor="center",
            justify="center",
        )
        lbl.image = None

    def refresh(self) -> None:
        if not self.play_canvases:
            return
        s = self.state
        self._update_end_guide()
        self._update_sidebar()

        size = game_mod.GRID_SIZE
        for gr in range(size):
            for gc in range(size):
                if gr >= 1 and gc >= 1:
                    self._draw_play_cell(gr, gc)
                    continue

                lbl = self.labels[(gr, gc)]
                if gr == 0 and gc == 0:
                    sym = OPERATION_SYMBOLS[self.operation]
                    self._style_label(lbl, sym, PLUS_BG, PLUS_FG)
                elif gr == 0:
                    if (gr, gc) in self.revealed_headers:
                        self._style_label(
                            lbl,
                            str(s.header[0][gc]),
                            HEADER_NUM,
                            HEADER_NUM_FG,
                        )
                    else:
                        self._style_label(lbl, "", HEADER_SLOT, HEADER_SLOT_FG)
                elif gc == 0:
                    if (gr, gc) in self.revealed_headers:
                        self._style_label(
                            lbl,
                            str(s.header[gr][0]),
                            HEADER_NUM,
                            HEADER_NUM_FG,
                        )
                    else:
                        self._style_label(lbl, "", HEADER_SLOT, HEADER_SLOT_FG)

        self._sync_focus_overlay()


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


if __name__ == "__main__":
    main()
