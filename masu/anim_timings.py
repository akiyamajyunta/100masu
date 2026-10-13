"""開始・採点アニメの速度（マス数に応じて補間＋上限）。"""

from __future__ import annotations

INTRO_SCALE_LEVELS = (1.85, 1.55, 1.28, 1.1, 1.0)
INTRO_SCALE_MS = 20
INTRO_STAGGER_MS = 30
INTRO_SCALE_MS_MIN = 10
INTRO_STAGGER_MS_MIN = 8
INTRO_ANIM_TARGET_SEC = 1.2
INTRO_REF_HEADER_COUNT = 8
INTRO_MAX_HEADER_COUNT = 24
INTRO_GLOW = "#e2e8f0"

SCORE_SCALE_LEVELS = (1.85, 1.55, 1.28, 1.1, 1.0)
SCORE_SCALE_MS = 20
SCORE_STAGGER_MS = 30
SCORE_SCALE_MS_MIN = 6
SCORE_STAGGER_MS_MIN = 4
SCORE_ANIM_TARGET_SEC = 4.0
SCORE_REF_CELL_COUNT = 16
SCORE_MAX_CELL_COUNT = 144
SCORE_GLOW = "#e2e8f0"


def _anim_scale_ms_for_count(
    *,
    base_ms: int,
    min_ms: int,
    count: int,
    ref_count: int,
    max_count: int,
) -> int:
    if count <= ref_count:
        return base_ms
    span = max(1, max_count - ref_count)
    t = min(1.0, (count - ref_count) / span)
    scaled = int(round(base_ms + (min_ms - base_ms) * t))
    return max(min_ms, scaled)


def _anim_ms_from_time_budget(
    *,
    cell_count: int,
    scale_levels: tuple[float, ...],
    base_scale_ms: int,
    min_scale_ms: int,
    base_stagger_ms: int,
    min_stagger_ms: int,
    target_sec: float,
    ref_count: int,
    max_count: int,
) -> tuple[int, int]:
    n = max(1, cell_count)
    scale_ms = _anim_scale_ms_for_count(
        base_ms=base_scale_ms,
        min_ms=min_scale_ms,
        count=n,
        ref_count=ref_count,
        max_count=max_count,
    )
    stagger_ms = _anim_scale_ms_for_count(
        base_ms=base_stagger_ms,
        min_ms=min_stagger_ms,
        count=n,
        ref_count=ref_count,
        max_count=max_count,
    )
    pop_ms = len(scale_levels) * scale_ms
    budget_ms = target_sec * 1000
    total_ms = n * (pop_ms + stagger_ms)
    if total_ms <= budget_ms:
        return scale_ms, stagger_ms

    per_cell_ms = budget_ms / n
    if per_cell_ms <= pop_ms:
        scale_ms = max(
            min_scale_ms,
            int(per_cell_ms / max(len(scale_levels), 1)),
        )
        pop_ms = len(scale_levels) * scale_ms
    stagger_ms = max(min_stagger_ms, int(per_cell_ms - pop_ms))
    return scale_ms, stagger_ms


def intro_anim_timings(header_count: int) -> tuple[int, int]:
    return _anim_ms_from_time_budget(
        cell_count=header_count,
        scale_levels=INTRO_SCALE_LEVELS,
        base_scale_ms=INTRO_SCALE_MS,
        min_scale_ms=INTRO_SCALE_MS_MIN,
        base_stagger_ms=INTRO_STAGGER_MS,
        min_stagger_ms=INTRO_STAGGER_MS_MIN,
        target_sec=INTRO_ANIM_TARGET_SEC,
        ref_count=INTRO_REF_HEADER_COUNT,
        max_count=INTRO_MAX_HEADER_COUNT,
    )


def score_anim_timings(play_cell_count: int) -> tuple[int, int]:
    return _anim_ms_from_time_budget(
        cell_count=play_cell_count,
        scale_levels=SCORE_SCALE_LEVELS,
        base_scale_ms=SCORE_SCALE_MS,
        min_scale_ms=SCORE_SCALE_MS_MIN,
        base_stagger_ms=SCORE_STAGGER_MS,
        min_stagger_ms=SCORE_STAGGER_MS_MIN,
        target_sec=SCORE_ANIM_TARGET_SEC,
        ref_count=SCORE_REF_CELL_COUNT,
        max_count=SCORE_MAX_CELL_COUNT,
    )
