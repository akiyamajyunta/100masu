"""
Bake theme_card_*.jpg for Masu visual select UI (256×256, object-fit cover crop).
Run from repo: python Unity/tools/bake_theme_card_previews.py
"""
from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

# Hand-placed thumbnails (main() will not overwrite).
SKIP_REGENERATE = frozenset({"theme_card_data", "theme_card_buddha"})
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Resources" / "TitleArt"
DL = Path(r"C:\Users\akiyama\Downloads\game-of-life-background")
OUT_W = OUT_H = 256


def save_cover(im: Image.Image, name: str) -> None:
    im = im.convert("RGB")
    w, h = im.size
    scale = max(OUT_W / w, OUT_H / h)
    nw, nh = max(1, int(w * scale)), max(1, int(h * scale))
    im = im.resize((nw, nh), Image.Resampling.LANCZOS)
    left = (nw - OUT_W) // 2
    top = (nh - OUT_H) // 2
    im = im.crop((left, top, left + OUT_W, top + OUT_H))
    im.save(OUT / f"{name}.jpg", quality=90)


def copy_frame(src: Path, name: str) -> None:
    save_cover(Image.open(src), name)


def bake_modern() -> None:
    c = tuple(int(x * 255) for x in (0.1, 0.14, 0.2))
    im = Image.new("RGB", (OUT_W, OUT_H), c)
    draw = ImageDraw.Draw(im)
    for y in range(OUT_H):
        t = y / OUT_H
        col = tuple(int(c[i] * (0.92 + 0.12 * t)) for i in range(3))
        draw.line([(0, y), (OUT_W, y)], fill=col)
    save_cover(im, "theme_card_modern")


def _gol_stamp(grid, cols, rows, pat, ox, oy) -> None:
    for x, y in pat:
        px, py = ox + x, oy + y
        if 0 <= px < cols and 0 <= py < rows:
            grid[py][px] = 1


def _gol_count_live(grid, cols, rows) -> int:
    return sum(grid[y][x] for y in range(rows) for x in range(cols))


def _gol_evolve_torus(grid, cols, rows) -> list[list[int]]:
    nxt = [[0] * cols for _ in range(rows)]
    for y in range(rows):
        for x in range(cols):
            n = 0
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    if dx == 0 and dy == 0:
                        continue
                    n += grid[(y + dy) % rows][(x + dx) % cols]
            if grid[y][x]:
                nxt[y][x] = 1 if n in (2, 3) else 0
            else:
                nxt[y][x] = 1 if n == 3 else 0
    return nxt


def _gol_seed_initial(cols: int, rows: int, rng: random.Random) -> list[list[int]]:
    """Match GameOfLifeBackground.SeedInitial (chaos stamps + 9% fill)."""
    grid = [[0] * cols for _ in range(rows)]
    patterns = [
        [(1, 0), (2, 1), (0, 2), (1, 2), (2, 2)],
        [(0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (0, 0), (4, 2)],
        [(1, 0), (0, 0), (0, 1), (0, 2), (1, 2)],
        [(2, 1), (1, 2), (0, 3), (0, 4), (1, 4), (2, 4), (3, 4), (0, 5), (1, 5)],
    ]
    count = max(16, (cols * rows) // 120)
    for _ in range(count):
        pat = patterns[rng.randint(0, len(patterns) - 1)]
        ox = rng.randint(1, max(2, cols - 8))
        oy = rng.randint(1, max(2, rows - 8))
        _gol_stamp(grid, cols, rows, pat, ox, oy)
    for y in range(rows):
        for x in range(cols):
            if rng.random() < 0.09:
                grid[y][x] = 1
    return grid


def _gol_render(grid, cols, rows, cell: int, gap: int, bg, dead, live) -> Image.Image:
    im = Image.new("RGB", (cols * (cell + gap), rows * (cell + gap)), bg)
    px = im.load()
    for y in range(rows):
        for x in range(cols):
            col = live if grid[y][x] else dead
            for dy in range(cell):
                for dx in range(cell):
                    px[x * (cell + gap) + dx, y * (cell + gap) + dy] = col
    return im


def bake_cyber_gol() -> None:
    bg = (0x0D, 0x11, 0x17)
    dead = (0x05, 0x08, 0x10)
    live = (0x00, 0xE5, 0xFF)
    cols, rows = 48, 28
    cell, gap = 5, 1

    best_grid = None
    best_live = 0
    for seed in (42, 7, 2024, 1337):
        rng = random.Random(seed)
        grid = _gol_seed_initial(cols, rows, rng)
        for gen in range(28):
            live_n = _gol_count_live(grid, cols, rows)
            if gen >= 3 and live_n >= 55 and live_n > best_live:
                best_live = live_n
                best_grid = [row[:] for row in grid]
            grid = _gol_evolve_torus(grid, cols, rows)

    if best_grid is None:
        rng = random.Random(42)
        best_grid = _gol_seed_initial(cols, rows, rng)

    im = _gol_render(best_grid, cols, rows, cell, gap, bg, dead, live)
    save_cover(im, "theme_card_cyber")


def bake_cosmic_lcd() -> None:
    w, h = 480, 270
    im = Image.new("RGB", (w, h), (0, 0, 0))
    draw = ImageDraw.Draw(im)
    rng = random.Random(3)
    for _ in range(35):
        y = rng.randint(0, h - 1)
        x = rng.randint(0, w)
        sz = rng.choice([2, 3, 3])
        draw.rectangle([x, y, x + sz, y + sz], fill=(255, 255, 255))
    cx, cy = w // 2, h // 3
    rays = 16
    for i in range(rays):
        ang = i / rays * math.tau
        ex = cx + math.cos(ang) * 90
        ey = cy + math.sin(ang) * 90
        draw.line([(cx, cy), (ex, ey)], fill=(255, 255, 255), width=2)
    draw.rectangle([cx - 3, cy - 3, cx + 3, cy + 3], fill=(255, 255, 255))
    for y in range(0, h, 4):
        draw.line([(0, y), (w, y)], fill=(0, 0, 0))
    save_cover(im, "theme_card_cosmic")


def bake_data_bubble() -> None:
    w, h = 480, 270
    im = Image.new("RGB", (w, h), (0x0A, 0x10, 0x22))
    draw = ImageDraw.Draw(im)
    cols, rows = 4, 3
    gap = 6
    cw = (w - gap * (cols - 1)) // cols
    ch = (h - gap * (rows - 1)) // rows
    rng = random.Random(11)
    for row in range(rows):
        for col in range(cols):
            x0 = col * (cw + gap)
            y0 = row * (ch + gap)
            draw.rectangle([x0, y0, x0 + cw, y0 + ch], fill=(0x0C, 0x14, 0x2A))
            n = 16
            bar_w = max(2, (cw - 20 - (n - 1) * 2) // n)
            for i in range(n):
                val = rng.randint(3, n)
                t = (val - 1) / max(1, n - 1)
                r = int(40 + (56 - 40) * t)
                g = int(88 + (232 - 88) * t)
                b = int(200 + (200 - 200) * t)
                bar_h = int((ch - 16) * val / n)
                bx = x0 + 10 + i * (bar_w + 2)
                by = y0 + ch - 8 - bar_h
                draw.rectangle([bx, by, bx + bar_w, y0 + ch - 8], fill=(r, g, b))
    save_cover(im, "theme_card_data")


def bake_live_equalizer() -> None:
    w, h = 480, 270
    im = Image.new("RGB", (w, h), (0x0A, 0x10, 0x22))
    draw = ImageDraw.Draw(im)
    bars = 32
    segs = 26
    pad_x, pad_y = 18, 16
    inner_w = w - pad_x * 2
    inner_h = h - pad_y * 2
    bar_w = max(4, (inner_w - (bars - 1) * 3) // bars)
    seg_h = max(3, (inner_h - (segs - 1) * 2) // segs)
    phase = 1.2

    def rainbow(t: float) -> tuple[int, int, int]:
        stops = [(0, (32, 96, 255)), (0.22, (32, 224, 128)), (0.45, (255, 224, 32)),
                 (0.68, (255, 128, 32)), (1, (255, 64, 160))]
        t = max(0, min(1, t))
        for i in range(len(stops) - 1):
            a, ca = stops[i]
            b, cb = stops[i + 1]
            if a <= t <= b:
                u = (t - a) / (b - a)
                return tuple(int(ca[j] + (cb[j] - ca[j]) * u) for j in range(3))
        return stops[-1][1]

    for bar in range(bars):
        w1 = 0.5 + 0.5 * math.sin(phase * 1 + bar * 0.71)
        w2 = 0.5 + 0.5 * math.sin(phase * 2 + bar * 1.31)
        w3 = 0.5 + 0.5 * math.sin(phase * 0.5 + bar * 1.97)
        level = segs * max(0.08, min(1, w1 * 0.5 + w2 * 0.32 + w3 * 0.18))
        bx = pad_x + bar * (bar_w + 3)
        for s in range(segs):
            if s + 0.5 >= level:
                continue
            sy = pad_y + s * (seg_h + 2)
            t = s / max(1, segs - 1)
            draw.rectangle([bx, sy, bx + bar_w, sy + seg_h], fill=rainbow(t))
    save_cover(im, "theme_card_live")


def bake_neo_hannya() -> None:
    w, h = 480, 270
    im = Image.new("RGB", (w, h), (0, 0, 0))
    draw = ImageDraw.Draw(im)
    try:
        font = ImageFont.truetype(r"C:\Windows\Fonts\YuGothM.ttc", 22)
    except OSError:
        font = ImageFont.load_default()
    text = "般若心経"
    rng = random.Random(7)
    col_w = 34
    for i in range(w // col_w + 2):
        x = i * col_w + col_w // 2
        y0 = rng.randint(-40, h)
        for j in range(rng.randint(6, 14)):
            y = y0 + j * 30
            if y < -20 or y > h + 20:
                continue
            ch = text[(i + j) % len(text)]
            g = 180 if j else 220
            draw.text((x, y), ch, fill=(0, g, 40), font=font, anchor="mm")
    save_cover(im, "theme_card_neo")


def bake_pop_radial() -> None:
    w, h = 480, 270
    im = Image.new("RGB", (w, h), (0x1E, 0x1E, 0x1E))
    draw = ImageDraw.Draw(im)
    cx, cy = w // 2, h // 2
    rng = random.Random(99)
    for ring in range(1, 12):
        r = ring * 14
        n = 6 + ring * 3
        for i in range(n):
            ang = i / n * math.tau + ring * 0.2
            x = cx + math.cos(ang) * r
            y = cy + math.sin(ang) * r
            c = rng.choice([(255, 120, 180), (120, 200, 255), (255, 220, 100), (180, 255, 160)])
            draw.ellipse([x - 4, y - 4, x + 4, y + 4], fill=c)
    for _ in range(40):
        x, y = rng.randint(0, w), rng.randint(0, h)
        draw.ellipse([x - 2, y - 2, x + 2, y + 2], fill=(255, 255, 255))
    save_cover(im, "theme_card_pop")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    copy_frame(DL / "mandelbrot-mp4-frames" / "mandelbrot-0500.png", "theme_card_corona")
    copy_frame(DL / "julia-mp4-frames" / "julia-0500.png", "theme_card_cosmo")
    copy_frame(DL / "red-grid-pulse-frames" / "red-grid-pulse-0500.png", "theme_card_grid")
    copy_frame(DL / "pipe-screensaver-mp4-frames" / "pipe-screensaver-0500.png", "theme_card_pipe")
    copy_frame(DL / "tunnel-tornado-mp4-frames" / "tunnel-tornado-0500.png", "theme_card_saimin")
    bake_modern()
    bake_cyber_gol()
    bake_cosmic_lcd()
    if "theme_card_data" not in SKIP_REGENERATE:
        bake_data_bubble()
    bake_live_equalizer()
    bake_neo_hannya()
    bake_pop_radial()
    print("Wrote theme_card previews to", OUT)


if __name__ == "__main__":
    main()
