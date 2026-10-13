"""100マス計算のゲームロジック（四則演算）。"""

from __future__ import annotations

import random
import re
import time
from dataclasses import dataclass, field
from enum import Enum
from typing import Dict, Tuple

from masu.config import Operation

# 入力マス数（辺）。外枠 GRID_SIZE = PLAY_SIZE + 1（ヘッダー1行1列）
PLAY_SIZE = 4
GRID_SIZE = PLAY_SIZE + 1

PLAY_ROW_MIN = 1
PLAY_COL_MIN = 1
PLAY_ROW_MAX = PLAY_SIZE
PLAY_COL_MAX = PLAY_SIZE
PLAY_CELL_COUNT = PLAY_SIZE * PLAY_SIZE


def configure_grid(play_size: int) -> None:
    """入力できるマス目（例: 4 → 4×4=16マス）。ヘッダー行・列を除く。"""
    global GRID_SIZE, PLAY_SIZE, PLAY_ROW_MAX, PLAY_COL_MAX, PLAY_CELL_COUNT
    n = max(4, min(12, int(play_size)))
    PLAY_SIZE = n
    GRID_SIZE = n + 1
    PLAY_ROW_MAX = n
    PLAY_COL_MAX = n
    PLAY_CELL_COUNT = n * n


PENALTY_WRONG_SECONDS = 10
PENALTY_EMPTY_SECONDS = 20
EMPTY_PENALTY_MARK = "__empty__"

INITIAL_FOCUS_ROW = 1
INITIAL_FOCUS_COL = 1

_DIV_INPUT_RE = re.compile(r"^\d+(?:\.\d)?$")
_INT_INPUT_RE = re.compile(r"^-?\d+$")


def header_reveal_cells() -> list[tuple[int, int]]:
    """開始アニメ: 縦ヘッダー（左列）→ 横ヘッダー（上段）。"""
    cells: list[tuple[int, int]] = []
    for row in range(1, GRID_SIZE):
        cells.append((row, 0))
    for col in range(1, GRID_SIZE):
        cells.append((0, col))
    return cells


def play_cells_score_order() -> list[tuple[int, int]]:
    """採点アニメ: 列優先（縦方向に下へ）→ 次の列。"""
    cells: list[tuple[int, int]] = []
    for col in range(PLAY_COL_MIN, PLAY_COL_MAX + 1):
        for row in range(PLAY_ROW_MIN, PLAY_ROW_MAX + 1):
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
    scoring_play_elapsed: float | None = None
    finished: bool = False

    @staticmethod
    def _unique_header_digits(count: int) -> list[int]:
        pool_max = max(9, count)
        pool = list(range(1, pool_max + 1))
        if count <= len(pool):
            return random.sample(pool, count)
        head = random.sample(pool, len(pool))
        tail = random.choices(pool, k=count - len(pool))
        return head + tail

    def reset(self) -> None:
        self.header = [[0] * GRID_SIZE for _ in range(GRID_SIZE)]
        col_count = GRID_SIZE - 1
        row_count = GRID_SIZE - 1
        top_digits = self._unique_header_digits(col_count)
        left_digits = self._unique_header_digits(row_count)
        for c in range(1, GRID_SIZE):
            self.header[0][c] = top_digits[c - 1]
        for r in range(1, GRID_SIZE):
            self.header[r][0] = left_digits[r - 1]

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
        self.scoring_play_elapsed = None
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
        if self.scoring_play_elapsed is not None:
            return self.scoring_play_elapsed + self.penalty_seconds
        if self.start_monotonic is None:
            return 0.0
        return (time.monotonic() - self.start_monotonic) + self.penalty_seconds

    def freeze_scoring_clock(self) -> None:
        """採点アニメ開始時: プレイ経過だけ固定し、ペナルティは後から加算。"""
        if self.start_monotonic is None:
            self.scoring_play_elapsed = 0.0
        else:
            self.scoring_play_elapsed = max(
                0.0, time.monotonic() - self.start_monotonic
            )

    def grade_cell_at_score(self, row: int, col: int) -> bool:
        """採点アニメ用: 1 マスを採点し、不正解・未入力ならペナルティを即加算。"""
        cell = self.cells.get((row, col))
        if cell is None:
            return False
        if not cell.value.strip():
            cell.value = ""
            cell.state = CellState.EMPTY
            self.apply_empty_penalty(cell)
            return False
        self.validate_cell(row, col, cell.value)
        return cell.state == CellState.CORRECT

    @staticmethod
    def elapsed_to_centis(seconds: float) -> int:
        return int(round(max(0.0, seconds) * 100))

    @staticmethod
    def format_time(seconds: float) -> str:
        """分:秒.センチ秒（0.01秒単位）。"""
        centis_total = GameState.elapsed_to_centis(seconds)
        minutes, rem = divmod(centis_total, 6000)
        sec, centis = divmod(rem, 100)
        if minutes > 0:
            return f"{minutes}:{sec:02d}.{centis:02d}"
        return f"0:{sec:02d}.{centis:02d}"

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
        edge = PLAY_SIZE
        if self.operation is Operation.ADD:
            return 0 <= parsed <= edge * 2
        if self.operation is Operation.MUL:
            return 0 <= parsed <= edge * edge
        if self.operation is Operation.SUB:
            return -edge <= parsed <= edge
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
