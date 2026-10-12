"""難易度別ベスト10（forkid / kidgame 互換）。"""

from __future__ import annotations

from dataclasses import dataclass

from masu.config import Difficulty

LEADERBOARD_SIZE = 10
PLACEHOLDER_NAME = "-"
EMPTY_DATE = "----/--/-- --:--"
SCORE_PAD_WIDTH = 8


@dataclass(frozen=True, slots=True)
class LeaderboardEntry:
    name: str
    score: int
    played_at: str = ""
    accuracy: float = 0.0

    @classmethod
    def from_dict(cls, raw: object) -> LeaderboardEntry | None:
        if not isinstance(raw, dict):
            return None
        name = str(raw.get("name", "")).strip()
        try:
            score = int(raw.get("score", 0))
        except (TypeError, ValueError):
            return None
        played_at = str(raw.get("played_at", "")).strip()
        try:
            accuracy = float(raw.get("accuracy", 0.0))
        except (TypeError, ValueError):
            accuracy = 0.0
        return cls(
            name=name,
            score=max(0, score),
            played_at=played_at,
            accuracy=max(0.0, min(100.0, accuracy)),
        )

    def to_dict(self) -> dict[str, object]:
        return {
            "name": self.name,
            "score": self.score,
            "played_at": self.played_at,
            "accuracy": round(self.accuracy, 1),
        }


def empty_leaderboards() -> dict[str, list[LeaderboardEntry]]:
    return {d.value: [] for d in Difficulty}


def placeholder_entry() -> LeaderboardEntry:
    return LeaderboardEntry(name=PLACEHOLDER_NAME, score=0, played_at="", accuracy=0.0)


def insert_entry(
    entries: list[LeaderboardEntry],
    name: str,
    score: int,
    *,
    played_at: str,
    accuracy: float,
) -> list[LeaderboardEntry]:
    clean = (name or "").strip()
    merged = list(entries) + [
        LeaderboardEntry(
            name=clean,
            score=max(0, score),
            played_at=played_at,
            accuracy=max(0.0, min(100.0, accuracy)),
        )
    ]
    merged.sort(key=lambda e: e.score, reverse=True)
    return merged[:LEADERBOARD_SIZE]


def preview_after_insert(
    entries: list[LeaderboardEntry],
    *,
    name: str,
    score: int,
    played_at: str,
    accuracy: float,
) -> tuple[list[LeaderboardEntry], int]:
    clean = (name or "").strip()
    merged = insert_entry(
        entries,
        clean,
        score,
        played_at=played_at,
        accuracy=accuracy,
    )
    for i, entry in enumerate(merged, start=1):
        if (
            entry.name == clean
            and entry.score == max(0, score)
            and entry.played_at == played_at
        ):
            return merged, i
    return merged, min(len(merged), LEADERBOARD_SIZE)
