"""演算モード・マス目・UI 定数。"""

from __future__ import annotations

from enum import StrEnum

STAR_GAUGE_MAX = 8
TIME_BAR_CAP_SEC = 600.0

# 1辺あたりの「入力できるマス」の数（4×4 なら 16 マス）
GRID_SIZES: tuple[int, ...] = (4, 6, 8, 9, 12)


class Operation(StrEnum):
    ADD = "add"
    SUB = "sub"
    MUL = "mul"
    DIV = "div"


OPERATION_LABELS: dict[Operation, str] = {
    Operation.ADD: "たし算",
    Operation.SUB: "ひき算",
    Operation.MUL: "かけ算",
    Operation.DIV: "わり算",
}

OPERATION_SYMBOLS: dict[Operation, str] = {
    Operation.ADD: "＋",
    Operation.SUB: "−",
    Operation.MUL: "×",
    Operation.DIV: "÷",
}

OPERATION_RANKING_TAB: dict[Operation, str] = {
    Operation.ADD: "たし算",
    Operation.SUB: "ひき算",
    Operation.MUL: "かけ算",
    Operation.DIV: "わり算",
}


def grid_label(grid_size: int) -> str:
    return f"{grid_size}×{grid_size}"


def board_key(operation: Operation, grid_size: int) -> str:
    return f"{operation.value}:{int(grid_size)}"


def all_board_keys() -> list[str]:
    return [board_key(op, g) for op in Operation for g in GRID_SIZES]
