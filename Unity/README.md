# 100マス計算 — Unity 版

Python プロトタイプ（リポジトリルートの `masu/`）と同じルール・スコア保存形式の PC クライアントです。

## 必要環境

- Unity **2022.3 LTS**（`ProjectSettings/ProjectVersion.txt` 参照）
- 初回オープン時: **Window → TextMeshPro → Import TMP Essential Resources**

## 開き方

1. Unity Hub → **Add** → この `Unity` フォルダを選択
2. シーン `Assets/Scenes/Main.unity` を開く（空シーンでも `MasuAutoStart` が自動起動します）
3. **Play** で実行

## 起動・画面サイズ

1. ビルドした EXE（または Play）起動 → **ウィンドウモード** モーダル
2. 解像度を選び **ゲーム起動** → その解像度でセッション固定（プレイ中は変更不可）
3. **起動時に毎回聞く** のチェックは Python 版と同様（OFF で次回からモーダル省略）

UI は **1280×960（4:3）** を設計基準に Canvas Scaler で等倍スcaleします。  
**Player Settings**: ウィンドウのユーザーによるリサイズは無効（`resizableWindow: 0`）。

エディタの Game ビューを自由に伸ばすと見た目がずれることがあります。**実機確認は Standalone ビルド**を推奨します。

## 操作

Python 版と同様: タイトル → 演算 → マス数 → プレイスタイル → プレイ。記録は ←→ 演算・↑↓ 盤面。

## スコア保存

Python と共有: `%USERPROFILE%\.100masu\scores.json`（キー形式 `add:4` など）

## ビルド

**File → Build Settings** で Windows/Mac/Linux Standalone を選び Build。

Steamworks 連携は未実装です。
