"""MasuApp mixin — 画面遷移."""

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


class NavigationMixin:
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

