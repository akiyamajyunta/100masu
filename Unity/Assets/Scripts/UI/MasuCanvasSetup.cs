using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    public static class MasuCanvasSetup
    {
        /// <summary>
        /// 4:3 プリセット向け: 設計解像度 1280×960 を基準に、実ウィンドウへ等倍スケール。
        /// </summary>
        public static void Configure(CanvasScaler scaler)
        {
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(MasuDisplaySettings.DesignWidth, MasuDisplaySettings.DesignHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }
}
