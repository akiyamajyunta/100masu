"""MasuApp mixin — アニメーション."""

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
    INTRO_SCALE_LEVELS,
    INTRO_SCALE_MS,
    INTRO_STAGGER_MS,
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


class AnimationsMixin:
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
        self._score_timer_display_centis = None
        self._score_timer_roll = 0
        self._stop_focus_blink()
        self.state.operation = self.operation
        self.state.reset()
        self.state.start_monotonic = None
        self._intro_scale_ms, self._intro_stagger_ms = intro_anim_timings(
            len(header_reveal_cells())
        )
        self._clear_grid_pop_layers()
        self.refresh()
        self._run_intro_animation()

    def _run_intro_animation(self, index: int = 0) -> None:
        gen = self._intro_gen
        cells = header_reveal_cells()
        if index >= len(cells):
            if gen != self._intro_gen:
                return
            self.intro_animating = False
            self._clear_grid_pop_layers()
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
            self._refresh_header_cell(gr, gc)
            self.root.after(
                self._intro_stagger_ms,
                lambda n=index: self._run_intro_animation(n + 1),
            )

        self._animate_tile_pop(
            gr,
            gc,
            text,
            tile_top_bg=HEADER_TILE_TOP,
            tile_bottom_bg=HEADER_TILE_BOTTOM,
            fg=HEADER_NUM_FG,
            scale_levels=INTRO_SCALE_LEVELS,
            scale_ms=self._intro_scale_ms,
            is_stale=lambda: gen != self._intro_gen,
            on_complete=on_pop_complete,
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
        scale_levels: tuple[float, ...],
        scale_ms: int,
        is_stale: Callable[[], bool],
        on_complete: Callable[[], None],
        mark_image: tk.PhotoImage | None = None,
    ) -> None:
        """マス上でタイルだけを拡大縮小（白 glow は使わない）。"""
        pop_root = self.grid_wrap
        x, y, w, h = self._grid_cell_rect_in_wrap(gr, gc)

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

        pop_state = {"done": False, "after": None}

        def cancel_scheduled() -> None:
            job = pop_state["after"]
            if not job:
                return
            try:
                self.root.after_cancel(job)
            except tk.TclError:
                pass
            pop_state["after"] = None

        def cleanup(*, invoke_complete: bool = False) -> None:
            if pop_state["done"]:
                return
            pop_state["done"] = True
            cancel_scheduled()
            try:
                if tile_wrap.winfo_exists():
                    tile_wrap.place_forget()
                    tile_wrap.destroy()
            except tk.TclError:
                pass
            self._clear_grid_pop_layers()
            if invoke_complete and not is_stale():
                on_complete()

        def show_scale(scale_index: int = 0) -> None:
            if pop_state["done"]:
                return
            if is_stale():
                cleanup()
                return
            if scale_index >= len(scale_levels):
                cleanup(invoke_complete=True)
                return

            scale = scale_levels[scale_index]
            tile_w = max(w, int(w * scale))
            tile_h = max(h, int(h * scale))
            tile_x = x + (w - tile_w) // 2
            tile_y = y + (h - tile_h) // 2

            tile_wrap.place(x=tile_x, y=tile_y, width=tile_w, height=tile_h)
            tile_wrap.lift()
            pop_state["after"] = self.root.after(
                scale_ms,
                lambda n=scale_index + 1: show_scale(n),
            )

        show_scale(0)

    def _run_score_animation(self, index: int = 0) -> None:
        gen = self._score_gen
        order = play_cells_score_order()
        if index >= len(order):
            if gen != self._score_gen:
                return
            self.score_animating = False
            self._clear_grid_pop_layers()
            self.refresh()
            self._finalize_score_after_animation()
            self._show_score_results()
            return

        row, col = order[index]
        correct = self.state.grade_cell_at_score(row, col)
        self._update_scoring_timer_display()
        self._update_sidebar()
        answer = self.state.cells[(row, col)].value
        mark_image = self._mark_maru if correct else self._mark_batsu
        tile_bg, sym_fg = CELL_EMPTY, CELL_EMPTY_FG

        def on_pop_complete() -> None:
            if gen != self._score_gen:
                return
            self.score_marks[(row, col)] = correct
            self._draw_play_cell(row, col)
            self.root.after(
                self._score_stagger_ms,
                lambda n=index: self._run_score_animation(n + 1),
            )

        self._animate_tile_pop(
            row,
            col,
            answer,
            tile_top_bg=tile_bg,
            tile_bottom_bg=tile_bg,
            fg=sym_fg,
            scale_levels=SCORE_SCALE_LEVELS,
            scale_ms=self._score_scale_ms,
            is_stale=lambda: gen != self._score_gen,
            on_complete=on_pop_complete,
            mark_image=mark_image,
        )

