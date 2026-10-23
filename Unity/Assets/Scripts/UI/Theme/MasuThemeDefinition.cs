using UnityEngine;

namespace Masu.UI
{
    /// <summary>
    /// 1 テーマ分の定義。Project 内にアセットを追加するか、<see cref="MasuThemeCatalog"/> の組み込み定義を使う。
    /// </summary>
    [CreateAssetMenu(fileName = "MasuTheme", menuName = "Masu/Theme Definition")]
    public sealed class MasuThemeDefinition : ScriptableObject
    {
        [Tooltip("PlayerPrefs 等で使う一意 ID（例: wafuu）")]
        public string ThemeId = "wafuu";

        [Tooltip("ビジュアル選択画面に表示する名前")]
        public string DisplayNameJa = "和風";

        public MasuThemePalette Palette;

        [Header("Resources/TitleArt 内のファイル名（拡張子なし）")]
        public string MenuPhotoResource = "title_photo";
        public string MenuScrimResource = "title_scrim";
        public string MenuScrimEffectResource = "title_scrim_effect";
        public string GameBackgroundResource = "title_game_bg";
        public MasuThemeGameBackgroundKind GameBackgroundKind = MasuThemeGameBackgroundKind.Auto;

        [Header("Playfield overlay")]
        public MasuThemeGameOverlayKind GameOverlayKind = MasuThemeGameOverlayKind.Auto;
        [Tooltip("Kind が Auto 以外のときの TitleArt キー（GIF は .gif 付き、動画は拡張子なし）")]
        public string GameOverlayResource = "";
        /// <summary>TitleArt/*.gif.bytes（例: title_game_overlay.gif）。Auto 時に使用。</summary>
        public string GameOverlayGifResource = "title_game_overlay.gif";
        /// <summary>静止オーバーレイ PNG 等。Auto 時に使用。</summary>
        public string GameOverlayTextureResource = "";
        [Tooltip("静止オーバーレイを縦スクロール（px/s）。0 で停止。")]
        public float GameOverlayScrollSpeed = 0f;
        [Tooltip("GIF オーバーレイの配置（和風=全面、仏=中央など）。")]
        public MasuThemeGameOverlayLayout GameOverlayLayout = MasuThemeGameOverlayLayout.FullBleed;
        public string TitleSidePanelResource = "title_panel";
        public string InstructionBannerResource = "title_instruction_banner";
        public string SelectBarResource = "title_select_bar";

        [Tooltip("写真が無いテーマ用のメニュー背景色")]
        public Color MenuBackdropFallback = new(0.12f, 0.18f, 0.14f, 1f);

        [Tooltip("ゲーム背景が無いときの単色")]
        public Color GameBackdropFallback = new(0.08f, 0.1f, 0.14f, 1f);
    }
}
