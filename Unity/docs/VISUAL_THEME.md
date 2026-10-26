# ビジュアル（テーマ）選択機能

## 概要

- 現行の森・竹・明朝 UI は **`wafuu`（和風）** テーマとして定義されています。
- `MasuThemeManager` が色（`MasuTheme`）と背景スプライトを一括適用します。
- 選択は `PlayerPrefs` キー `masu_visual_theme_id` に保存され、起動時に復元されます。

## スクリプト構成

| ファイル | 役割 |
|---------|------|
| `MasuThemePalette.cs` | 色のデータセット |
| `MasuThemeDefinition.cs` | ScriptableObject（1テーマ） |
| `MasuThemeCatalog.cs` | 組み込み4種 + `Resources/ThemeCatalog` の SO をマージ |
| `MasuThemeManager.cs` | 読み込み・保存・適用・`ThemeChanged` イベント |
| `MasuThemeGameVisualConfig.cs` | テーマ定義 → プレイ画面ビジュアル（背景＋オーバーレイ種別） |
| `MasuThemePlayfieldVisualController.cs` | オーバーレイの Play / Stop ライフサイクル |
| `MasuThemeResourceCache.cs` | TitleArt のロード・キャッシュ |
| `MasuThemeBackdropApplier.cs` | 静止画 `Image` への適用 |
| `MasuThemePlayfieldVisualLayout.cs` | プレイ背景の bleed / PPU 定数 |
| `MasuThemeSpriteBindings.cs` | 差し替え対象 `Image` の登録 |
| `MasuVisualThemeSelectUI.cs` | オプション＞ビジュアル画面のカードグリッド |
| `MasuThemeableGraphic.cs` | （任意）個別 UI に色スロットを紐付け |

## ビジュアル選択画面（実装済み）

コードで `OptionsVisual` パネルに自動構築されます。

- **6列グリッド**のカード（`MasuVisualThemeLayout` でサイズ・間隔を調整）
- **←→↑↓** でカード選択 → **即時プレビュー**（`MasuThemeManager.SetActiveTheme`）
- **クリック**でも同様
- 上部に選択中テーマ名（例: 和風）
- **Enter / Esc** でオプション画面へ戻る

### Unity Editor で手動レイアウトする場合

1. `OptionsVisual` パネル直下に空の `Grid` を作成
2. `Grid Layout Group`: Fixed Column = 6, Cell Size = 96×96, Spacing = 14
3. 子に `Button` + `Image`（プレビュー色）+ `Text`（テーマ名）
4. `onClick` → `MasuThemeManager.SetActiveTheme("theme_id")`
5. 既存の `MasuVisualThemeSelectUI` と置き換える場合は `MasuAppController.BuildMenuPanels` を編集

## 新しいテーマを追加する

### 方法 A: ScriptableObject（推奨）

1. Project で右クリック → **Create → Masu → Theme Definition**
2. `ThemeId`（英小文字・一意）、`DisplayNameJa`、`Palette`、各 `Resources/TitleArt` キーを設定
3. アセットを `Assets/Resources/ThemeCatalog/` に配置
4. 同じ `ThemeId` の組み込み定義がある場合は **SO が優先**されます

### 方法 B: コード

`MasuThemeCatalog.CreateBuiltInThemes()` に `CreateRuntime(...)` を追加し、`MasuThemePalette` に色セットを足します。

## 適用される要素

**メイン画面（タイトル・森メニュー・記録・オプション等）は常に和風のまま**です。選択テーマは **プレイ画面** に入ったときだけ反映されます。

| 要素 | 切り替え |
|------|----------|
| タイトル／メニュー背景・赤帯・竹バナー | 常に `wafuu`（変更しない） |
| ゲーム背景 | 選択テーマ |
| 盤面・スコア枠・マス色・フォーカス | 選択テーマ（`ShowScreen(Game)` 時） |

`title_instruction_banner` 等は `MasuThemeManager.RegisterInstructionBannerArt` で登録した Image に差し替え可能（将来拡張用）。

## 起動フロー

1. `Awake`: `MasuThemeManager.Initialize()` → PlayerPrefs から ID 読み込み → `MasuTheme.ApplyPalette`
2. `BuildUi()` 後: `RegisterThemeBindings()` → `ApplyActive()` でテクスチャ反映
3. テーマ変更時: `OnVisualThemeChanged` でプレイ中なら盤面を `Rebuild`

## 組み込みテーマ

| ThemeId | 表示名 | 備考 |
|---------|--------|------|
| `wafuu` | 和風 | 既存 TitleArt 素材 |
| `cool` | クール | `theme_cool_game_bg` + 雪オーバーレイ（GIF があれば GIF、なければ `theme_cool_overlay` PNG） |
| `modern` | モダン | 単色背景プレビュー（写真キー空） |
| `cyber` | サイバー | ライフゲーム背景（`GameOfLifeBackground`）＋ネオン系パレット |
| `live` | ライブ | 虹色イコライザ背景（`EqualizerBackground`）＋紺／ピンク系パレット |
| `corona` | コロナ | マンデルブロ背景（`MandelbrotBackground`）＋暖色パレット |
| `cosmo` | コスモ | ジュリア背景（`JuliaBackground` / `julia-background.js` 相当）＋シアン系パレット |
| `pop` | ポップ | パステル系パレット |
| `buddha` | 仏 | 涅槃像＋右下曼荼羅（`theme_buddha_mandala.png`＋`MasuThemeBuddhaMandalaOrbitView`）。`Unity/docs/BUDDHA_MANDALA_ORBIT.md` |

和風以外は `title_photo` / `title_game_bg` を割り当てるか、SO で専用 PNG を指定してください。

GIF オーバーレイの配置は `MasuThemeDefinition.GameOverlayLayout`（`FullBleed` / `CenterFit` / `BottomRightFit`）で指定します。
