using UnityEngine;

namespace Masu.UI
{
    /// <summary>オーバーレイ Image の RectTransform をテーマごとに切り替える。</summary>
    public static class MasuThemeOverlayRectLayout
    {
        public static void ApplyGifOverlay(RectTransform rt, MasuThemeGameOverlayLayout layout)
        {
            if (rt == null) return;

            switch (layout)
            {
                case MasuThemeGameOverlayLayout.CenterFit:
                    ApplyCenterFit(rt, MasuThemePlayfieldVisualLayout.CenterFitOverlaySizePixels);
                    break;
                case MasuThemeGameOverlayLayout.BottomRightFit:
                    ApplyBottomRightFit(
                        rt,
                        MasuThemePlayfieldVisualLayout.CenterFitOverlaySizePixels,
                        MasuThemePlayfieldVisualLayout.BottomRightFitOverlaySizePixels,
                        MasuThemePlayfieldVisualLayout.CornerFitOverlayMarginPixels);
                    break;
                case MasuThemeGameOverlayLayout.BuddhaMandalaOnGameBackground:
                    ApplyBuddhaMandalaOnGameBackground(rt);
                    break;
                default:
                    MasuUiFactory.StretchFullBleed(rt, MasuThemePlayfieldVisualLayout.BackdropBleedPixels);
                    break;
            }
        }

        static void ApplyCenterFit(RectTransform rt, float sizePixels)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizePixels);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sizePixels);
            rt.localScale = Vector3.one;
        }

        static void ApplyBottomRightFit(
            RectTransform rt,
            float baseSizePixels,
            float sizePixels,
            float marginPixels)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizePixels);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sizePixels);
            rt.anchoredPosition = new Vector2(-marginPixels, marginPixels)
                + MasuThemePlayfieldVisualLayout.BottomRightFitCenterNudgePixels;
            rt.localScale = Vector3.one;
        }

        /// <summary>
        /// 涅槃背景（左プレイ列）の右下。スコア枠や盤面 UI の手前には出さない（背景スタック内）。
        /// </summary>
        public static void ApplyBuddhaMandalaOnGameBackground(RectTransform rt)
        {
            if (rt == null) return;

            var size = MasuThemePlayfieldVisualLayout.BuddhaMandalaOverlaySizePixels;
            var margin = MasuThemePlayfieldVisualLayout.CornerFitOverlayMarginPixels;

            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            rt.anchoredPosition = new Vector2(-margin, margin)
                + MasuThemePlayfieldVisualLayout.BuddhaMandalaOnBackgroundNudgePixels;
            rt.localScale = Vector3.one;
        }
    }
}
