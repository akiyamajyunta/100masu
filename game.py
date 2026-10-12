"""100マス計算のゲームロジック（四則演算）。"""

from __future__ import annotations

import random
import re
import time
from dataclasses import dataclass, field
from enum import Enum
from typing import Dict, Tuple

from masu.config import Operation

# デフォルトは 4×4（開発）。起動時に configure_grid で変更
GRID_SIZE = 4

PLAY_ROW_MIN = 1
PLAY_COL_MIN = 1
PLAY_ROW_MAX = GRID_SIZE - 1
PLAY_COL_MAX = GRID_SIZE - 1
PLAY_CELL_COUNT = (GRID_SIZE - 1) ** 2


def configure_grid(size: int) -> None:
    """グリッドサイズを変更し、プレイ領域定数を更新する。"""
    global GRID_SIZE, PLAY_ROW_MAX, PLAY_COL_MAX, PLAY_CELL_COUNT
    n = max(3, min(10, int(size)))
    GRID_SIZE = n
    PLAY_ROW_MAX = GRID_SIZE - 1
    PLAY_COL_MAX = GRID_SIZE - 1
    PLAY_CELL_COUNT = (GRID_SIZE - 1) ** 2


PENALTY_WRONG_SECONDS = 10
PENALTY_EMPTY_SECONDS = 20
EMPTY_PENALTY_MARK = "__empty__"

INITIAL_FOCUS_ROW = 1
INITIAL_FOCUS_COL = 1

_DIV_INPUT_RE = re.compile(r"^\d+(?:\.\d)?$")
_INT_INPUT_RE = re.compile(r"^-?\d+$")


def header_reveal_cells() -> list[tuple[int, int]]:
    cells: list[tuple[int, int]] = []
    for col in range(1, GRID_SIZE):
        cells.append((0, col))
    for row in range(1, GRID_SIZE):
        cells.append((row, 0))
    return cells


def play_cells_score_order() -> list[tuple[int, int]]:
    cells: list[tuple[int, int]] = []
    for row in range(PLAY_ROW_MIN, PLAY_ROW_MAX + 1):
        for col in range(PLAY_COL_MIN, PLAY_COL_MAX + 1):
            cells.append((row, col))
    return cells


def compute_expected(a: int, b: int, operation: Operation) -> float:
    """正解値（わり算は小数第2位四捨五入で小数第1位）。"""
    if operation is Operation.ADD:
        return float(a + b)
    if operation is Operation.SUB:
        return float(a - b)
    if operation is Operation.MUL:
        return float(a * b)
    return round(a / b, 1)


def format_expected_display(value: float, operation: Operation) -> str:
    if operation is Operation.DIV:
        return f"{value:.1f}"
    iv = int(value)
    return str(iv)


class CellState(str, Enum):
    EMPTY = "empty"
    CORRECT = "correct"


@dataclass
class PlayCell:
    value: str = ""
    state: CellState = CellState.EMPTY
    last_penalized_value: str | None = None


@dataclass
class GameState:
    operation: Operation = Operation.ADD
    header: list[list[int]] = field(default_factory=list)
    cells: Dict[Tuple[int, int], PlayCell] = field(default_factory=dict)
    focus_row: int = INITIAL_FOCUS_ROW
    focus_col: int = INITIAL_FOCUS_COL
    buffer: str = ""
    penalty_count: int = 0
    penalty_seconds: int = 0
    start_monotonic: float | None = None
    finished: bool = False

    @staticmethod
    def random_digit() -> int:
        return random.randint(1, 9)

    def reset(self) -> None:
        self.header = [[0] * GRID_SIZE for _ in range(GRID_SIZE)]
        for c in range(1, GRID_SIZE):
            self.header[0][c] = self.random_digit()
        for r in range(1, GRID_SIZE):
            self.header[r][0] = self.random_digit()

        self.cells = {}
        for r in range(PLAY_ROW_MIN, PLAY_ROW_MAX + 1):
            for c in range(PLAY_COL_MIN, PLAY_COL_MAX + 1):
                self.cells[(r, c)] = PlayCell()

        self.focus_row = INITIAL_FOCUS_ROW
        self.focus_col = INITIAL_FOCUS_COL
        self.buffer = ""
        self.penalty_count = 0
        self.penalty_seconds = 0
        self.start_monotonic = time.monotonic()
        self.finished = False
        self.sync_buffer_from_focus()

    def is_play_cell(self, row: int, col: int) -> bool:
        return (
            PLAY_ROW_MIN <= row <= PLAY_ROW_MAX
            and PLAY_COL_MIN <= col <= PLAY_COL_MAX
        )

    def expected_answer(self, row: int, col: int) -> float:
        a = self.header[row][0]
        b = self.header[0][col]
        return compute_expected(a, b, self.operation)

    def elapsed_seconds(self) -> float:
        if self.start_monotonic is None:
            return 0.0
        return (time.monotonic() - self.start_monotonic) + self.penalty_seconds

    @staticmethod
    def format_time(seconds: float) -> str:
        minutes = int(seconds // 60)
        sec = seconds % 60
        if minutes > 0:
            return f"{minutes}:{sec:04.1f}"
        return f"0:{sec:04.1f}"

    def correct_count(self) -> int:
        return sum(1 for c in self.cells.values() if c.state == CellState.CORRECT)

    def wrong_count(self) -> int:
        return sum(
            1
            for c in self.cells.values()
            if c.value.strip() and c.state != CellState.CORRECT
        )

    def empty_count(self) -> int:
        return sum(1 for c in self.cells.values() if not c.value.strip())

    def effective_value(self, row: int, col: int) -> str:
        if row == self.focus_row and col == self.focus_col:
            return self.buffer
        return self.cells[(row, col)].value

    def filled_count(self) -> int:
        n = 0
        for r in range(PLAY_ROW_MIN, PLAY_ROW_MAX + 1):
            for c in range(PLAY_COL_MIN, PLAY_COL_MAX + 1):
                if self.effective_value(r, c).strip():
                    n += 1
        return n

    def all_play_cells_filled(self) -> bool:
        return self.filled_count() == PLAY_CELL_COUNT

    def sync_buffer_from_focus(self) -> None:
        cell = self.cells.get((self.focus_row, self.focus_col))
        self.buffer = cell.value if cell else ""

    def apply_penalty(self, cell: PlayCell, value: str) -> None:
        if cell.last_penalized_value == value:
            return
        cell.last_penalized_value = value
        self.penalty_count += 1
        self.penalty_seconds += PENALTY_WRONG_SECONDS

    def apply_empty_penalty(self, cell: PlayCell) -> None:
        if cell.last_penalized_value == EMPTY_PENALTY_MARK:
            return
        cell.last_penalized_value = EMPTY_PENALTY_MARK
        self.penalty_count += 1
        self.penalty_seconds += PENALTY_EMPTY_SECONDS

    @staticmethod
    def parse_answer(raw: str, operation: Operation) -> float | None:
        trimmed = raw.strip()
        if not trimmed:
            return None
        if operation is Operation.DIV:
            if not _DIV_INPUT_RE.match(trimmed):
                return None
            return round(float(trimmed), 1)
        if not _INT_INPUT_RE.match(trimmed):
            return None
        return float(int(trimmed))

    @staticmethod
    def answers_match(expected: float, parsed: float, operation: Operation) -> bool:
        if operation is Operation.DIV:
            return round(parsed, 1) == expected
        return parsed == expected

    @staticmethod
    def canonical_value(parsed: float, operation: Operation) -> str:
        if operation is Operation.DIV:
            return f"{round(parsed, 1):.1f}"
        return str(int(parsed))

    def validate_cell(self, row: int, col: int, raw_value: str) -> None:
        cell = self.cells.get((row, col))
        if cell is None:
            return

        trimmed = raw_value.strip()
        if trimmed == "":
            cell.value = ""
            cell.state = CellState.EMPTY
            cell.last_penalized_value = None
            return

        parsed = self.parse_answer(trimmed, self.operation)
        if parsed is None:
            self.apply_penalty(cell, trimmed)
            cell.value = trimmed
            cell.state = CellState.EMPTY
            return

        expected = self.expected_answer(row, col)
        if self.answers_match(expected, parsed, self.operation):
            cell.value = self.canonical_value(parsed, self.operation)
            cell.state = CellState.CORRECT
            cell.last_penalized_value = None
            return

        value_str = self.canonical_value(parsed, self.operation)
        self.apply_penalty(cell, value_str)
        cell.value = value_str
        cell.state = CellState.EMPTY

    def commit_focus(self) -> None:
        if not self.is_play_cell(self.focus_row, self.focus_col):
            return
        cell = self.cells[(self.focus_row, self.focus_col)]
        cell.value = self.buffer.strip()
        if not cell.value:
            cell.state = CellState.EMPTY
            cell.last_penalized_value = None
        self.sync_buffer_from_focus()

    def validate_all_cells(self) -> None:
        self.commit_focus()
        for (_row, _col), cell in self.cells.items():
            if not cell.value.strip():
                cell.value = ""
                cell.state = CellState.EMPTY
                self.apply_empty_penalty(cell)
                continue
            self.validate_cell(_row, _col, cell.value)

    def move_focus(self, dr: int, dc: int) -> None:
        if self.finished:
            return
        self.commit_focus()
        self.focus_row = max(
            PLAY_ROW_MIN, min(PLAY_ROW_MAX, self.focus_row + dr)
        )
        self.focus_col = max(
            PLAY_COL_MIN, min(PLAY_COL_MAX, self.focus_col + dc)
        )
        self.sync_buffer_from_focus()

    def _buffer_would_be_valid(self, candidate: str) -> bool:
        if candidate == "":
            return True
        if self.operation is Operation.DIV:
            return bool(_DIV_INPUT_RE.match(candidate)) or candidate.endswith(".")
        if self.operation is Operation.SUB:
            if candidate == "-":
                return True
            return bool(_INT_INPUT_RE.match(candidate))
        return candidate.isdigit()

    def _buffer_within_range(self, candidate: str) -> bool:
        if not candidate or candidate == "-" or candidate.endswith("."):
            return True
        parsed = self.parse_answer(candidate, self.operation)
        if parsed is None:
            return False
        if self.operation is Operation.ADD:
            return 0 <= parsed <= 18
        if self.operation is Operation.MUL:
            return 0 <= parsed <= 81
        if self.operation is Operation.SUB:
            return -8 <= parsed <= 8
        return 0.0 <= parsed <= 9.0

    def handle_char(self, ch: str) -> None:
        if self.finished or not self.is_play_cell(self.focus_row, self.focus_col):
            return

        if self.operation is Operation.SUB and ch == "-" and self.buffer == "":
            self.buffer = "-"
            return

        if self.operation is Operation.DIV and ch == ".":
            if "." in self.buffer:
                return
            if not self.buffer:
                self.buffer = "0."
            else:
                self.buffer = self.buffer + "."
            return

        if not ch.isdigit():
            return

        candidate = self.buffer + ch
        if not self._buffer_would_be_valid(candidate):
            return
        if not self._buffer_within_range(candidate):
            return
        self.buffer = candidate

    def handle_digit(self, digit: str) -> None:
        self.handle_char(digit)

    def handle_backspace(self) -> None:
        if self.finished or not self.is_play_cell(self.focus_row, self.focus_col):
            return
        cell = self.cells[(self.focus_row, self.focus_col)]
        self.buffer = self.buffer[:-1]
        cell.value = self.buffer
        if self.buffer == "":
            cell.state = CellState.EMPTY
            cell.last_penalized_value = None

    def finish(self) -> None:
        self.finished = True

    def play_cell_display(self, row: int, col: int) -> str:
        cell = self.cells[(row, col)]
        focused = (
            row == self.focus_row
            and col == self.focus_col
            and not self.finished
        )
        if focused and self.buffer:
            return self.buffer
        if cell.value:
            return cell.value
        if focused:
            return "｜"
        return ""
