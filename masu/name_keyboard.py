"""スコア登録用の画面上キーボード（ロジック）。"""

from __future__ import annotations

from dataclasses import dataclass

KEY_BS = "BS"
KEY_END = "戻"
KEY_SPACE = "□"

KEYBOARD_COLS = 13

KEYBOARD_ROWS: tuple[tuple[str, ...], ...] = (
    tuple("ABCDEFGHIJKLM"),
    tuple("NOPQRSTUVWXYZ"),
    tuple("abcdefghijklm"),
    tuple("nopqrstuvwxyz"),
    tuple("0123456789+-="),
    tuple(".,!?@:;[]()_/"),
    ("{", "}", "|", "~", "^", "#", "$", "%", "&", "*", KEY_SPACE, KEY_BS, KEY_END),
)

ENTRY_NAME_MAX_LEN = 10


def format_name_slots(name: str, max_len: int) -> str:
    n = max(1, max_len)
    buf = (name or "")[:n]
    return buf + "-" * (n - len(buf))


@dataclass
class NameKeyboard:
    row: int = 0
    col: int = 0

    @property
    def rows(self) -> tuple[tuple[str, ...], ...]:
        return KEYBOARD_ROWS

    def selected_key(self) -> str:
        line = self.rows[self.row]
        return line[self.col]

    def move(self, dr: int, dc: int) -> None:
        if not self.rows:
            return
        r = self.row
        c = self.col
        if dr != 0:
            r = (r + dr) % len(self.rows)
            line = self.rows[r]
            c = min(c, len(line) - 1)
        if dc != 0:
            line = self.rows[r]
            c = (c + dc) % len(line)
        self.row = r
        self.col = c

    def apply_key(
        self,
        name: str,
        max_len: int,
    ) -> tuple[str, bool]:
        key = self.selected_key()
        if key == KEY_END:
            return name, True
        if key == KEY_BS:
            return name[:-1], False
        if key == KEY_SPACE:
            if len(name) >= max_len:
                return name, False
            return name + " ", False
        if len(name) >= max_len:
            return name, False
        return name + key, False
