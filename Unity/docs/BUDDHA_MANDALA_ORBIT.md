# 仏テーマ：曼荼羅カメラ周回（疑似3D）

## 概要

- **静止テクスチャ** `Resources/TitleArt/theme_buddha_mandala.png`（透過 PNG、線画・配色は素材そのまま）
- `Masu/UI/MandalaCameraOrbit`：曼荼羅を**時計回り 60 秒／周**で回転しつつ、ヨー／ピッチ＋弱パースでカメラ周回（Rect は固定）
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
| `BuddhaMandalaOrbitFocal` | パースの弱さ（大きいほど弱い） |
| `BuddhaMandalaSpinPeriodSeconds` | 時計回り 1 周の秒数（60） |

## UV とはみ出し

回転・カメラ歪みで UV が 0–1 を超えると、テクスチャ **Repeat** 設定では切れた絵が反対側（左下など）から繰り返し表示される。`theme_buddha_mandala.png` は **Clamp**、シェーダは範囲外を透明にしている。

## シェーダがピンクのとき

`Assets/Shaders/MasuUiMandalaCameraOrbit.shader` がプロジェクトに含まれているか確認し、Unity で再インポートしてください。

## プレビュー用アニメ

既存 `theme_buddha_overlay.gif.bytes` は変更していません。別途プレビューが必要なら `Unity/Temp/` 等にエクスポートしてください。
