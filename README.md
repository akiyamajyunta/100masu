# 100マス計算（足し算）

tkinter 製の 100 マス計算ゲームです。UI とスコア保存は [forkid](https://github.com/akiyamajyunta/forkid)（なぞなぞ大作戦）の構成を 100 マス用に移植しています。

## 起動

```bash
python app.py
```

## スコア

- 保存先: `~/.100masu/scores.json`
- 難易度（グリッドサイズ）別の最高得点・ランキング TOP10

## 構成

- `app.py` … GUI・ゲーム画面
- `game.py` … 100 マス計算ロジック
- `masu/` … スコア・サイドバー・テーマ
