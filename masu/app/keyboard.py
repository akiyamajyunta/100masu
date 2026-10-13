"""MasuApp mixin — キーボード入力."""

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


class KeyboardMixin:
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

