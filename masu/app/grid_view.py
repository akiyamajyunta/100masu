"""MasuApp mixin — 盤面描画."""

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


class GridViewMixin:
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

    def _clear_grid_pop_layers(self) -> None:
        """grid_wrap 上のポップ用オーバーレイをすべて除去（盤本体は残す）。"""
        if self.grid_frame is None or not hasattr(self, "grid_wrap"):
            return
        keep = self.grid_frame
        for child in list(self.grid_wrap.winfo_children()):
            if child is keep:
                continue
            try:
                child.destroy()
            except tk.TclError:
                pass
        try:
            if keep.winfo_exists():
                keep.lift()
            self.grid_wrap.update_idletasks()
        except tk.TclError:
            pass

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

    def _refresh_header_cell(self, gr: int, gc: int) -> None:
        if (gr, gc) not in self.labels:
            return
        s = self.state
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
                self._refresh_header_cell(gr, gc)

        self._sync_focus_overlay()

