# 100マス計算 — Unity 版

Python プロトタイプ（リポジトリルートの `masu/`）と同じルール・スコア保存形式の PC クライアントです。

## 必要環境

- Unity **2022.3 LTS**（`ProjectSettings/ProjectVersion.txt` 参照）
- 初回オープン時: **Window → TextMeshPro → Import TMP Essential Resources**

## 開き方

1. Unity Hub → **Add** → この `Unity` フォルダを選択
2. シーン `Assets/Scenes/Main.unity` を開く（空シーンでも `MasuAutoStart` が自動起動します）
3. **Play** で実行

## 操作

Python 版と同様: タイトル → 演算 → マス数 → プレイ。RESULT は ←→ 演算・↑↓ マス数。

## スコア保存

Python と共有: `%USERPROFILE%\.100masu\scores.json`（キー形式 `add:4` など）

## ビルド

**File → Build Settings** で Windows/Mac/Linux Standalone を選び Build。

Steamworks 連携は未実装です。
