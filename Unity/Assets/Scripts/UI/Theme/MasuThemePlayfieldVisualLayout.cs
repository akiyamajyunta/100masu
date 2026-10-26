namespace Masu.UI
{
    /// <summary>プレイ画面の背景・オーバーレイのレイアウト定数（1280 基準、MasuForestSelectMenu と同様に数値のみ集約）。</summary>
    public static class MasuThemePlayfieldVisualLayout
    {
        public const float BackdropBleedPixels = 32f;
        /// <summary>1280 基準で中央マンダラ GIF の一辺（px）。</summary>
        public const float CenterFitOverlaySizePixels = 520f;
        public const float CornerFitOverlayMarginPixels = 24f;
        /// <summary>右下曼荼羅の一辺（px）。1280 基準で画面の約 4 割弱。</summary>
        public const float BottomRightFitOverlaySizePixels = 520f;
        /// <summary>右下 GIF の中心を左上へ寄せる（anchoredPosition 加算、px）。</summary>
        public static readonly UnityEngine.Vector2 BottomRightFitCenterNudgePixels = new(-40f, 40f);

        public const float BuddhaBackdropPanAmplitudePixels = 32f;
        public const float BuddhaBackdropPanVerticalScale = 0.7f;
        public const float BuddhaBackdropPanSpeedX = 0.247f;
        public const float BuddhaBackdropPanSpeedY = 0.195f;
        public const float BuddhaBackdropZoomAmplitude = 0.055f;
        public const float BuddhaBackdropZoomSpeed = 0.364f;
        /// <summary>画角（引き／寄り）の追加スケール揺れ。ズームとは別周期。</summary>
        public const float BuddhaBackdropFovAmplitude = 0.032f;
        public const float BuddhaBackdropFovSpeed = 0.118f;
        /// <summary>画角回転（度）。枠内に収まるよう動的にクランプ。</summary>
        public const float BuddhaBackdropRotateAmplitudeDegrees = 0.9f;
        public const float BuddhaBackdropRotateSpeed = 0.208f;
        /// <summary>ズーム最小 1 のとき使えるパン余裕（子の bleed 内に収める）。</summary>
        public const float BuddhaBackdropPanBleedSafety = 0.88f;

        /// <summary>疑似3Dオービット（マンダラ GIF）。</summary>
        public const float BuddhaGifOrbitYawAmplitudeDegrees = 80f;
        public const float BuddhaGifOrbitPitchAmplitudeDegrees = 16f;
        public const float BuddhaGifOrbitYawSpeed = 0.338f;
        public const float BuddhaGifOrbitPitchSpeed = 0.251f;

        /// <summary>仏曼荼羅の一辺（px）。<see cref="BottomRightFitOverlaySizePixels"/> の 2.5 倍。</summary>
        public const float BuddhaMandalaOverlaySizePixels = BottomRightFitOverlaySizePixels * 2.5f;
        /// <summary>涅槃背景上の曼荼羅位置微調整（+X で右へ）。</summary>
        public static readonly UnityEngine.Vector2 BuddhaMandalaOnBackgroundNudgePixels = new(88f, 40f);

        public const string BuddhaMandalaTextureResource = "theme_buddha_mandala";
        public const float BuddhaMandalaOrbitYawAmplitudeDegrees = 17.5f;
        public const float BuddhaMandalaOrbitPitchAmplitudeDegrees = 17.5f;
        public const float BuddhaMandalaOrbitYawSpeed = 0.21f;
        public const float BuddhaMandalaOrbitPitchSpeed = 0.16f;
        public const float BuddhaMandalaOrbitFocal = 3.2f;
        /// <summary>曼荼羅テクスチャの時計回り 1 周の秒数。</summary>
        public const float BuddhaMandalaSpinPeriodSeconds = 60f;
        /// <summary>カメラ周回の回転中心（UV、絵の中心）。</summary>
        public static readonly UnityEngine.Vector2 BuddhaMandalaOrbitPivotUv = new(0.5f, 0.5f);

        public const float OverlaySpritePixelsPerUnit = 100f;
        public const string TitleArtFolder = "TitleArt";
    }
}
