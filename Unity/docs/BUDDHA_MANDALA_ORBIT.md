# 仏テーマ：曼荼羅カメラ周回（疑似3D）

## 概要

- **静止テクスチャ** `Resources/TitleArt/theme_buddha_mandala.png`（透過 PNG、線画・配色は素材そのまま）
- **絵は回転しない**。`Masu/UI/MandalaCameraOrbit` が UV をヨー／ピッチ＋弱パースで歪め、カメラ周回のみ表現（Rect は固定）
- シェーダ参照: `Resources/Theme/MasuBuddhaMandalaOrbit.mat`（`Shader.Find` 失敗時のフォールバック）
- **UI**: `RawImage` + `MasuThemeBuddhaMandalaOrbitView`（raycast なし）
- 仏テーマのプレイ画面では **GIF オーバーレイの代わり**にこのビューを表示（`theme_buddha_overlay.gif.bytes` は上書きせず残置）

## 調整

`MasuThemePlayfieldVisualLayout.cs`

| 定数 | 意味（初期値） |
|------|----------------|
| `BuddhaMandalaOverlaySizePixels` | 表示枠一辺（520×2.5＝1300） |
| `BuddhaMandalaOnBackgroundNudgePixels` | 位置微調整（+X で右） |
| `BuddhaMandalaOnGameBackground` | `theme_buddha_game_bg` の子・背景右下（盤面 UI の背面） |
| `BuddhaMandalaOrbitYawAmplitudeDegrees` / `Pitch…` | 約 ±17.5° |
| `BuddhaMandalaOrbitYawSpeed` / `PitchSpeed` | 周回の速さ |
| `BuddhaMandalaOrbitFocal` | パースの弱さ（大きいほど弱い、初期 9） |

## シェーダがピンクのとき

`Assets/Shaders/MasuUiMandalaCameraOrbit.shader` がプロジェクトに含まれているか確認し、Unity で再インポートしてください。

## プレビュー用アニメ

既存 `theme_buddha_overlay.gif.bytes` は変更していません。別途プレビューが必要なら `Unity/Temp/` 等にエクスポートしてください。
