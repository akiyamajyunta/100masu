namespace Masu.UI
{
    /// <summary>プレイ画面オーバーレイの RectTransform 配置。</summary>
    public enum MasuThemeGameOverlayLayout
    {
        /// <summary>和風 GIF など、画面全面に引き伸ばし。</summary>
        FullBleed = 0,

        /// <summary>中央にアスペクト比を保って収める。</summary>
        CenterFit = 1,

        /// <summary>右下にアスペクト比を保って収める（マンダラ等）。</summary>
        BottomRightFit = 2,

        /// <summary>仏プレイ背景の右下（スコア列の左・盤面 UI の背面）。</summary>
        BuddhaMandalaOnGameBackground = 3,
    }
}
