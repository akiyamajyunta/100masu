"""MasuApp mixin — ゲーム進行・結果."""

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


class SessionMixin:
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
        self._score_scale_ms, self._score_stagger_ms = score_anim_timings(
            game_mod.PLAY_CELL_COUNT
        )
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

