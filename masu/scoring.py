"""100マス計算のセッション得点。"""

from __future__ import annotations

from masu.config import Operation

SCORE_BASE = 10000
SCORE_MIN = 100
_SPEED_MIN_FACTOR = 0.25
_SPEED_TIER_END_SEC = 600

_OPERATION_MULT: dict[Operation, float] = {
    Operation.ADD: 1.0,
    Operation.SUB: 1.2,
    Operation.MUL: 1.5,
    Operation.DIV: 1.8,
}


def speed_factor(elapsed_sec: float) -> float:
    elapsed = max(0.0, elapsed_sec)
    sec = int(elapsed)
    if sec >= _SPEED_TIER_END_SEC:
        return _SPEED_MIN_FACTOR
    frac = elapsed - sec
    f0 = _factor_at_whole_second(sec)
    f1 = _factor_at_whole_second(sec + 1)
    return f0 + (f1 - f0) * frac


def _factor_at_whole_second(second: int) -> float:
    second = max(0, min(second, _SPEED_TIER_END_SEC))
    t = second / _SPEED_TIER_END_SEC
    return _SPEED_MIN_FACTOR + (1.0 - _SPEED_MIN_FACTOR) * (1.0 - t)


def accuracy_percent(correct: int, total: int) -> float:
    if total <= 0:
        return 0.0
    return max(0.0, min(100.0, 100.0 * correct / total))


def compute_session_score(
    elapsed_sec: float,
    correct: int,
    total: int,
    operation: Operation,
) -> int:
    acc = correct / total if total else 0.0
    mult = _OPERATION_MULT[operation]
    raw = SCORE_BASE * speed_factor(elapsed_sec) * mult * (0.25 + 0.75 * acc)
    return max(SCORE_MIN, int(raw))
