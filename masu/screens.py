"""画面遷移の識別子。"""

from __future__ import annotations

from enum import StrEnum, auto

RESULT_MENU_ITEMS = ("タイトルへ戻る", "スコアを記録する")


class AppScreen(StrEnum):
    TITLE = auto()
    HELP = auto()
    MODE = auto()
    GRID = auto()
    GAME = auto()
    SCORE_RANKING = auto()
