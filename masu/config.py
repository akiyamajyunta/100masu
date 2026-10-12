"""難易度・UI 定数。"""

from __future__ import annotations

from enum import StrEnum

STAR_GAUGE_MAX = 8
TIME_BAR_CAP_SEC = 600.0

DIFFICULTY_LABEL_EN: dict[str, str] = {
    "easy": "EASY",
    "normal": "NORMAL",
    "hard": "HARD",
}


class Difficulty(StrEnum):
    EASY = "easy"
    NORMAL = "normal"
    HARD = "hard"


def difficulty_from_grid(grid_size: int) -> Difficulty:
    if grid_size <= 3:
        return Difficulty.EASY
    if grid_size <= 4:
        return Difficulty.NORMAL
    return Difficulty.HARD
