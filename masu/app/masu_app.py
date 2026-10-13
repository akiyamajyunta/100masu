"""tkinter メインアプリケーション（mixin 合成）。"""

from __future__ import annotations

import tkinter as tk
from tkinter import font as tkfont

from masu.anim_timings import INTRO_SCALE_MS, INTRO_STAGGER_MS, SCORE_SCALE_MS, SCORE_STAGGER_MS
from masu.app.animations import AnimationsMixin
from masu.app.build import BuildMixin
from masu.app.grid_view import GridViewMixin
from masu.app.keyboard import KeyboardMixin
from masu.app.navigation import NavigationMixin
from masu.app.session import SessionMixin
from masu.config import GRID_SIZES, Operation
from masu.grid_theme import CELL_SIZE
from masu import theme as UI
from masu.name_keyboard import NameKeyboard
from masu.score_store import ScoreStore
from masu.screens import AppScreen
from game import GameState


class MasuApp(
    BuildMixin,
    NavigationMixin,
    KeyboardMixin,
    GridViewMixin,
    AnimationsMixin,
    SessionMixin,
):
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
        self._intro_scale_ms = INTRO_SCALE_MS
        self._intro_stagger_ms = INTRO_STAGGER_MS
        self._score_scale_ms = SCORE_SCALE_MS
        self._score_stagger_ms = SCORE_STAGGER_MS
        self._score_timer_display_centis: int | None = None
        self._score_timer_roll: int = 0
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
