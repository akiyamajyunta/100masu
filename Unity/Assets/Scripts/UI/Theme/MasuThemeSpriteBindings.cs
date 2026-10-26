using System.Collections.Generic;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>テーマ適用時に差し替える Image 参照。MasuAppController が登録する。</summary>
    public sealed class MasuThemeSpriteBindings
    {
        public Image TitleBackdropPhoto;
        public Image TitleBackdropScrim;
        public Image ForestBackdropPhoto;
        public Image ForestBackdropScrim;
        public Image GameBackground;
        public GameOfLifeBackground GameOfLife;
        public MandelbrotBackground Mandelbrot;
        public JuliaBackground Julia;
        public EqualizerBackground Equalizer;
        public MasuUiGifPlayer GameOverlayGif;
        public MasuThemeBuddhaMandalaOrbitView GameBuddhaMandalaOrbit;
        public Image GameOverlayStatic;
        /// <summary>動画オーバーレイ（Video パッケージ導入後に <see cref="MasuThemeOverlayVideoHost"/> を割当）。</summary>
        public MasuThemeOverlayVideoHost GameOverlayVideo;
        public Image TitleSidePanel;
        public readonly List<Image> InstructionBannerArts = new();
    }
}
