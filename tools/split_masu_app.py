"""One-off: split app.py MasuApp into masu/app mixins."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main() -> None:
    src = (ROOT / "app.py").read_text(encoding="utf-8")
    m = re.search(r"^class MasuApp:\n", src, re.M)
    if not m:
        raise SystemExit("no MasuApp class")
    start = m.end()
    end = src.find("\ndef main()", start)
    body = src[start:end]

    groups = {
        "build": (
            "_build_ui",
            "_parse_geometry",
            "_compute_cell_size",
            "_update_subtitle_wrap",
            "_sync_cell_font_size",
            "_reload_mark_photos",
            "_build_grid_widgets",
            "_build_menu_frames",
            "_build_result_overlay",
        ),
        "navigation": (
            "_show_screen",
            "_refresh_menu_highlights",
            "_nav_vertical",
            "_start_game",
            "_on_root_configure",
            "_maybe_resize_grid",
            "_restart_current_game",
            "_return_to_title",
            "_reset_result_flow",
        ),
        "keyboard": (
            "_bind_keys",
            "_on_key",
            "_on_key_title",
            "_on_key_help",
            "_on_key_mode",
            "_on_key_grid",
            "_on_key_ranking",
            "_on_key_game",
            "_on_key_result",
        ),
        "grid_view": (
            "_lerp_hex_color",
            "_stop_focus_blink",
            "_focus_pulse_tick",
            "_start_focus_blink",
            "_sync_focus_overlay",
            "_grid_cell_rect",
            "_grid_cell_rect_in_wrap",
            "_play_canvas_center",
            "_draw_play_cell",
            "_style_label",
            "refresh",
        ),
        "animations": (
            "restart",
            "_run_intro_animation",
            "_animate_tile_pop",
            "_cell_is_correct",
            "_run_score_animation",
        ),
        "session": (
            "_pause_timer",
            "_end_pause_on_clock",
            "_resume_timer",
            "_tick_timer",
            "_update_end_guide",
            "_prompt_end_game",
            "_finish_with_score",
            "_show_score_results",
            "_show_result_panel",
            "_begin_score_record",
            "_submit_score_entry",
            "_update_sidebar",
        ),
    }

    lines = body.splitlines(keepends=True)
    methods: dict[str, str] = {}
    i = 0
    while i < len(lines):
        line = lines[i]
        static = False
        if line.startswith("    @staticmethod"):
            static = True
            i += 1
            if i >= len(lines):
                break
            line = lines[i]
        if not line.startswith("    def "):
            i += 1
            continue
        name = line.split("def ", 1)[1].split("(", 1)[0].strip()
        start = i - (1 if static else 0)
        i += 1
        while i < len(lines):
            nxt = lines[i]
            if nxt.startswith("    @staticmethod") or nxt.startswith("    def "):
                break
            i += 1
        methods[name] = "".join(lines[start:i])

    for g, names in groups.items():
        for n in names:
            if n not in methods:
                raise SystemExit(f"missing method {n} in group {g}")

    out_dir = ROOT / "masu" / "app"
    out_dir.mkdir(parents=True, exist_ok=True)

    common_header = '''"""MasuApp mixin — {title}."""

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


class {class_name}:
'''

    titles = {
        "build": "UI 構築",
        "navigation": "画面遷移",
        "keyboard": "キーボード入力",
        "grid_view": "盤面描画",
        "animations": "アニメーション",
        "session": "ゲーム進行・結果",
    }

    for g, names in groups.items():
        class_name = "".join(p.capitalize() for p in g.split("_")) + "Mixin"
        parts = [common_header.format(title=titles[g], class_name=class_name)]
        for n in names:
            parts.append(methods[n])
        (out_dir / f"{g}.py").write_text("".join(parts), encoding="utf-8")

    if "__init__" not in methods:
        raise SystemExit("no __init__")
    (out_dir / "_init_body.txt").write_text(methods["__init__"], encoding="utf-8")
    print("ok", out_dir)


if __name__ == "__main__":
    main()
