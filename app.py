"""100マス計算（足し算）— tkinter GUI。"""

from __future__ import annotations

import time
import tkinter as tk
from collections.abc import Callable
from tkinter import font as tkfont
from tkinter import messagebox

from datetime import datetime

from game import (
    GRID_SIZE,
    PENALTY_EMPTY_SECONDS,
    PLAY_CELL_COUNT,
    CellState,
    GameState,
    header_reveal_cells,
    play_cells_score_order,
)
from masu.config import (
    DIFFICULTY_LABEL_EN,
    STAR_GAUGE_MAX,
    TIME_BAR_CAP_SEC,
    difficulty_from_grid,
)
from masu.leaderboard import preview_after_insert
from masu.name_dialog import ask_player_name
from masu import theme as UI
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


class MasuApp:
    def __init__(self, root: tk.Tk) -> None:
        self.root = root
        self.root.title("100マス計算（足し算）")
        self.root.configure(bg=UI.BG)
        self.root.minsize(820, 520)

        self.score_store = ScoreStore.load()
        self.difficulty = difficulty_from_grid(GRID_SIZE)
        self.display_score = 0
        self.last_accuracy = 0.0
        self.last_played_at = ""

        self.state = GameState()
        self.labels: dict[tuple[int, int], tk.Label] = {}
        self.play_cell_frames: dict[tuple[int, int], tk.Frame] = {}
        self.timer_job: str | None = None
        self.timer_paused_at: float | None = None
        self.intro_animating = False
        self.revealed_headers: set[tuple[int, int]] = set()
        self._intro_gen = 0
        self.score_animating = False
        self.score_marks: dict[tuple[int, int], str] = {}
        self._score_gen = 0
        self.cell_font: tkfont.Font | None = None
        self.focus_blink_job: str | None = None
        self.focus_overlay_active = False

        self._build_ui()
        self._bind_keys()
        self.restart()

    def _build_ui(self) -> None:
        title_font = tkfont.Font(family=FONT_FAMILY, size=22, weight="bold")
        stat_font = tkfont.Font(family=FONT_FAMILY, size=12)
        timer_font = tkfont.Font(family=FONT_FAMILY, size=20, weight="bold")
        cell_font = tkfont.Font(family=FONT_FAMILY, size=14, weight="bold")
        self.cell_font = cell_font

        self.timer_var = tk.StringVar(value="0:00.0")
        self.guide_var = tk.StringVar(value="")

        body = tk.Frame(self.root, bg=UI.BG)
        body.pack(fill="both", expand=True)

        main_outer = tk.Frame(body, bg=UI.BG)
        main_outer.pack(side="left", fill="both", expand=True, padx=(12, 6), pady=12)

        panel_gold = tk.Frame(main_outer, bg=UI.BORDER_GOLD, padx=3, pady=3)
        panel_gold.pack(fill="both", expand=True)
        main_panel = tk.Frame(panel_gold, bg=UI.MAIN_INNER, padx=16, pady=12)
        main_panel.pack(fill="both", expand=True)

        tk.Label(
            main_panel,
            text="100マス計算",
            font=title_font,
            fg=UI.TEXT,
            bg=UI.MAIN_INNER,
        ).pack()
        tk.Label(
            main_panel,
            text="左と上の数字を足して入力（矢印 / WASD で移動、Enter で採点）",
            font=stat_font,
            fg=UI.TEXT_DIM,
            bg=UI.MAIN_INNER,
        ).pack(pady=(4, 0))
        tk.Label(
            main_panel,
            textvariable=self.guide_var,
            font=stat_font,
            fg=PROGRESS_FG,
            bg=UI.MAIN_INNER,
        ).pack(pady=(6, 0))

        grid_wrap = tk.Frame(main_panel, bg=UI.MAIN_INNER, padx=4, pady=8)
        grid_wrap.pack()
        self.grid_frame = tk.Frame(grid_wrap, bg=CELL_BORDER)
        self.grid_frame.pack()
        for i in range(GRID_SIZE):
            self.grid_frame.columnconfigure(i, minsize=CELL_SIZE, weight=1)
            self.grid_frame.rowconfigure(i, minsize=CELL_SIZE, weight=1)

        for ri in range(GRID_SIZE):
            for ci in range(GRID_SIZE):
                border = tk.Frame(
                    self.grid_frame,
                    bg=CELL_BORDER,
                    width=CELL_SIZE,
                    height=CELL_SIZE,
                )
                border.grid(row=ri, column=ci, padx=0, pady=0, sticky="nsew")
                border.grid_propagate(False)
                if ri >= 1 and ci >= 1:
                    focus_frame = tk.Frame(border, bg=CELL_BORDER)
                    focus_frame.pack(fill="both", expand=True, padx=1, pady=1)
                    lbl = tk.Label(
                        focus_frame,
                        font=cell_font,
                        relief="flat",
                        borderwidth=0,
                        bg=CELL_EMPTY,
                        anchor="center",
                        justify="center",
                    )
                    lbl.pack(fill="both", expand=True)
                    self.play_cell_frames[(ri, ci)] = focus_frame
                    self.labels[(ri, ci)] = lbl
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

        legend = tk.Frame(main_panel, bg=UI.MAIN_INNER)
        legend.pack(pady=(4, 4))
        for text, bg, inner in (
            ("未入力", CELL_EMPTY, False),
            ("選択中", FOCUS_RING_BRIGHT, True),
        ):
            item = tk.Frame(legend, bg=UI.MAIN_INNER)
            item.pack(side="left", padx=8)
            if inner:
                swatch = tk.Frame(item, bg=FOCUS_RING_BRIGHT)
                swatch.pack(side="left", padx=(0, 4))
                tk.Label(swatch, width=2, bg=CELL_EMPTY).pack(
                    padx=2, pady=2
                )
            else:
                swatch = tk.Frame(item, bg=CELL_BORDER)
                swatch.pack(side="left", padx=(0, 4))
                tk.Label(swatch, width=2, bg=bg).pack(padx=1, pady=1)
            tk.Label(
                item, text=text, font=stat_font, fg=UI.TEXT_DIM, bg=UI.MAIN_INNER
            ).pack(side="left")

        self.sidebar = StatusSidebar(body, width=250)
        self.sidebar.pack(side="right", fill="y", padx=(6, 12), pady=12)
        self.sidebar.restart_btn.configure(command=self.restart)

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
        ):
            self.root.bind_all(seq, self._on_key, add="+")
        for d in "0123456789":
            self.root.bind_all(d, self._on_key, add="+")

    def _on_key(self, event: tk.Event) -> str | None:
        if self.state.finished or self.intro_animating or self.score_animating:
            return "break"
        keysym = event.keysym
        char = event.char

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
            self.state.handle_digit(char)
            self.refresh()
            return "break"

        return None

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
        self.difficulty = difficulty_from_grid(GRID_SIZE)
        self._stop_focus_blink()
        self.state.reset()
        self.state.start_monotonic = None
        self.timer_var.set("0:00.0")
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
        ow = int(w * FOCUS_SCALE)
        oh = int(h * FOCUS_SCALE)
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
        widget = self.labels[(gr, gc)]
        cell = widget
        while cell.master is not self.grid_frame:
            cell = cell.master
        self.grid_frame.update_idletasks()
        return cell.winfo_x(), cell.winfo_y(), CELL_SIZE, CELL_SIZE

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
    ) -> None:
        x, y, w, h = self._grid_cell_rect(gr, gc)

        glow = tk.Label(self.grid_frame, bg=glow_color, borderwidth=0)
        tile_wrap = tk.Frame(
            self.grid_frame,
            bg=CELL_BORDER,
            borderwidth=0,
            highlightthickness=0,
        )
        tile_top = tk.Label(
            tile_wrap,
            text=text,
            font=self.cell_font,
            bg=tile_top_bg,
            fg=fg,
            borderwidth=0,
            relief="flat",
            anchor="center",
            justify="center",
        )
        tile_bottom = tk.Label(
            tile_wrap,
            text="",
            font=self.cell_font,
            bg=tile_bottom_bg,
            borderwidth=0,
            relief="flat",
            height=1,
        )
        tile_top.pack(fill="both", expand=True)
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

    def _score_symbol(self, row: int, col: int) -> str:
        cell = self.state.cells[(row, col)]
        if cell.state == CellState.CORRECT:
            return "○"
        return "×"

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
        symbol = self._score_symbol(row, col)
        if symbol == "○":
            top_bg, bottom_bg, sym_fg = SCORE_OK_TOP, SCORE_OK_BOTTOM, SCORE_OK_FG
        else:
            top_bg, bottom_bg, sym_fg = SCORE_NG_TOP, SCORE_NG_BOTTOM, SCORE_NG_FG

        def on_pop_complete() -> None:
            if gen != self._score_gen:
                return
            self.score_marks[(row, col)] = symbol
            self.refresh()
            self.root.after(
                SCORE_STAGGER_MS,
                lambda n=index: self._run_score_animation(n + 1),
            )

        self._animate_tile_pop(
            row,
            col,
            symbol,
            tile_top_bg=top_bg,
            tile_bottom_bg=bottom_bg,
            fg=sym_fg,
            glow_color=SCORE_GLOW,
            scale_levels=SCORE_SCALE_LEVELS,
            scale_ms=SCORE_SCALE_MS,
            is_stale=lambda: gen != self._score_gen,
            on_complete=on_pop_complete,
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
            self.timer_var.set(
                GameState.format_time(self.state.elapsed_seconds())
            )
            self.timer_job = self.root.after(100, self._tick_timer)

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
        self.last_accuracy = accuracy_percent(correct, PLAY_CELL_COUNT)
        elapsed = self.state.elapsed_seconds()
        self.display_score = compute_session_score(
            elapsed, correct, PLAY_CELL_COUNT, self.difficulty
        )
        self.score_store.record_session(self.difficulty, self.display_score)
        self.last_played_at = datetime.now().strftime("%Y/%m/%d %H:%M")
        self.state.finish()
        self._stop_focus_blink()
        self.focus_overlay.place_forget()
        if self.timer_job:
            self.root.after_cancel(self.timer_job)
            self.timer_job = None
        self.timer_var.set(GameState.format_time(self.state.elapsed_seconds()))
        self.guide_var.set("")
        self.score_marks = {}
        self.score_animating = True
        self._score_gen += 1
        self.refresh()
        self._run_score_animation()

    def _show_score_results(self) -> None:
        final = GameState.format_time(self.state.elapsed_seconds())
        correct = self.state.correct_count()
        wrong = self.state.wrong_count()
        empty = self.state.empty_count()
        penalty = self.state.penalty_seconds
        if wrong == 0 and empty == 0:
            title = "クリア！"
            summary = "すべて正解です。"
        else:
            title = "採点結果"
            parts = []
            if wrong:
                parts.append(f"不正解: {wrong} マス")
            if empty:
                parts.append(
                    f"未入力: {empty} マス（+{PENALTY_EMPTY_SECONDS * empty} 秒）"
                )
            summary = " / ".join(parts)

        messagebox.showinfo(
            title,
            f"{summary}\n\n"
            f"正解: {correct} / {PLAY_CELL_COUNT}\n"
            f"得点: {self.display_score:,}\n"
            f"ペナルティ: +{penalty} 秒\n"
            f"最終タイム: {final}",
        )
        self.refresh()
        if messagebox.askyesno(
            "スコア登録",
            "ランキングに登録しますか？",
        ):
            _, rank = preview_after_insert(
                self.score_store.leaderboard(self.difficulty),
                name="-",
                score=self.display_score,
                played_at=self.last_played_at,
                accuracy=self.last_accuracy,
            )
            name = ask_player_name(self.root, preview_rank=rank)
            if name:
                self.score_store.add_leaderboard_entry(
                    self.difficulty,
                    name,
                    self.display_score,
                    played_at=self.last_played_at,
                    accuracy=self.last_accuracy,
                )
                self.refresh()

    def _update_sidebar(self) -> None:
        s = self.state
        if self.score_marks:
            correct_n = sum(1 for m in self.score_marks.values() if m == "○")
        elif s.finished:
            correct_n = s.correct_count()
        else:
            correct_n = s.filled_count()
        correct_text = (
            f"{s.correct_count()} / {PLAY_CELL_COUNT}"
            if s.finished
            else f"{s.filled_count()} / {PLAY_CELL_COUNT}"
        )
        answer_stars = int(STAR_GAUGE_MAX * correct_n / PLAY_CELL_COUNT)
        skill_stars = max(
            0,
            STAR_GAUGE_MAX - min(STAR_GAUGE_MAX, s.penalty_count),
        )
        elapsed = s.elapsed_seconds() if s.start_monotonic is not None else 0.0
        if s.finished:
            elapsed = s.elapsed_seconds()
        time_ratio = min(1.0, elapsed / TIME_BAR_CAP_SEC)
        self.sidebar.update_display(
            mode_en=DIFFICULTY_LABEL_EN[self.difficulty.value],
            high_score=self.score_store.high_score(self.difficulty),
            session_score=self.display_score,
            correct_text=correct_text,
            answer_stars=answer_stars,
            skill_stars=skill_stars,
            time_ratio=time_ratio,
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
            highlightthickness=0,
            relief="flat",
            borderwidth=0,
            anchor="center",
            justify="center",
        )

    def refresh(self) -> None:
        s = self.state
        self._update_end_guide()
        if not s.finished:
            self.timer_var.set(GameState.format_time(s.elapsed_seconds()))
        self._update_sidebar()

        for gr in range(GRID_SIZE):
            for gc in range(GRID_SIZE):
                lbl = self.labels[(gr, gc)]

                if gr == 0 and gc == 0:
                    self._style_label(lbl, "＋", PLUS_BG, PLUS_FG)
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
                else:
                    frame = self.play_cell_frames[(gr, gc)]
                    frame.config(bg=CELL_BORDER)
                    lbl.pack_configure(padx=0, pady=0)
                    mark = self.score_marks.get((gr, gc))
                    if mark:
                        if mark == "○":
                            self._style_label(
                                lbl, mark, SCORE_OK_BOTTOM, SCORE_OK_FG
                            )
                        else:
                            self._style_label(
                                lbl, mark, SCORE_NG_BOTTOM, SCORE_NG_FG
                            )
                        continue
                    focused = (
                        gr == s.focus_row
                        and gc == s.focus_col
                        and not s.finished
                        and not self.intro_animating
                        and not self.score_animating
                    )
                    cell = s.cells[(gr, gc)]
                    text = "" if focused else cell.value
                    self._style_label(lbl, text, CELL_EMPTY, CELL_EMPTY_FG)

        self._sync_focus_overlay()


def main() -> None:
    root = tk.Tk()
    MasuApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
