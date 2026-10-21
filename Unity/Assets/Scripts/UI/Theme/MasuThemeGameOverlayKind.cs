namespace Masu.UI
{
    /// <summary>プレイ画面オーバーレイの再生形式。</summary>
    public enum MasuThemeGameOverlayKind
    {
        /// <summary><see cref="MasuThemeDefinition"/> の GIF / 静止画フィールドから自動判定。</summary>
        Auto = 0,
        None = 1,
        StaticTexture = 2,
        GifBytes = 3,
        /// <summary>Resources の VideoClip（将来拡張）。</summary>
        Video = 4,
    }
}
