"""最高得点・ランキングの永続化（~/.100masu/scores.json）。"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path

from masu.config import Operation, all_board_keys, board_key
from masu.leaderboard import LeaderboardEntry, empty_leaderboards, insert_entry

SCORES_PATH = Path.home() / ".100masu" / "scores.json"


@dataclass
class ScoreStore:
    high_by_difficulty: dict[str, int]
    leaderboards: dict[str, list[LeaderboardEntry]] = field(
        default_factory=empty_leaderboards
    )

    @classmethod
    def empty(cls) -> ScoreStore:
        keys = all_board_keys()
        return cls(
            high_by_difficulty={k: 0 for k in keys},
            leaderboards=empty_leaderboards(),
        )

    @classmethod
    def load(cls) -> ScoreStore:
        if not SCORES_PATH.is_file():
            return cls.empty()
        try:
            data = json.loads(SCORES_PATH.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return cls.empty()
        store = cls.empty()
        raw_high = data.get("high_by_difficulty", {})
        if isinstance(raw_high, dict):
            for key, val in raw_high.items():
                k = str(key)
                try:
                    store.high_by_difficulty[k] = max(0, int(val))
                except (TypeError, ValueError):
                    continue
        raw_boards = data.get("leaderboards", {})
        if isinstance(raw_boards, dict):
            for key, items in raw_boards.items():
                k = str(key)
                if not isinstance(items, list):
                    continue
                parsed: list[LeaderboardEntry] = []
                for item in items:
                    entry = LeaderboardEntry.from_dict(item)
                    if entry is not None:
                        parsed.append(entry)
                store.leaderboards[k] = parsed
        return store

    def save(self) -> None:
        SCORES_PATH.parent.mkdir(parents=True, exist_ok=True)
        payload = {
            "high_by_difficulty": self.high_by_difficulty,
            "leaderboards": {
                key: [e.to_dict() for e in entries]
                for key, entries in self.leaderboards.items()
            },
        }
        SCORES_PATH.write_text(
            json.dumps(payload, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    def high_score(self, operation: Operation, grid_size: int) -> int:
        return self.high_by_difficulty.get(board_key(operation, grid_size), 0)

    def record_session(
        self, operation: Operation, grid_size: int, session_score: int
    ) -> int:
        key = board_key(operation, grid_size)
        prev = self.high_by_difficulty.get(key, 0)
        new_high = max(prev, session_score)
        if new_high != prev:
            self.high_by_difficulty[key] = new_high
            self.save()
        return new_high

    def leaderboard(
        self, operation: Operation, grid_size: int
    ) -> list[LeaderboardEntry]:
        return list(self.leaderboards.get(board_key(operation, grid_size), []))

    def add_leaderboard_entry(
        self,
        operation: Operation,
        grid_size: int,
        name: str,
        score: int,
        *,
        played_at: str,
        accuracy: float,
    ) -> None:
        key = board_key(operation, grid_size)
        current = self.leaderboards.get(key, [])
        self.leaderboards[key] = insert_entry(
            current,
            name,
            score,
            played_at=played_at,
            accuracy=accuracy,
        )
        self.save()
