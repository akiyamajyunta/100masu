"""演算モード・UI 定数。"""

from __future__ import annotations

from enum import StrEnum

STAR_GAUGE_MAX = 8
TIME_BAR_CAP_SEC = 600.0

# 本番グリッド（10×10 = 81 マス）
DEFAULT_GRID_SIZE = 10


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
