using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// uGUI 向けの小さな演出コルーチン群。すべて unscaled time で進むため、
    /// ゲーム内の一時停止やタイムスケールの影響を受けない。
    /// </summary>
    public static class MasuUiAnimations
    {
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

        public static float EaseInCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        public static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(t)) - 1f) * 0.5f;

        /// <summary>横方向からスライドインしつつ点滅。終了時は basePos / baseColor に戻す。</summary>
        public static IEnumerator SlideInFlash(
            Text label,
            Vector2 basePos,
            float fromOffsetX,
            float duration,
            Color baseColor,
            Color flashColor,
            int flashes)
        {
            var rt = label.rectTransform;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var p = Mathf.Clamp01(elapsed / duration);
                var e = EaseOutCubic(p);
                rt.anchoredPosition = new Vector2(basePos.x + fromOffsetX * (1f - e), basePos.y);

                var phase = Mathf.FloorToInt(p * flashes * 2f);
                var c = phase % 2 == 0 ? flashColor : baseColor;
                c.a = Mathf.Lerp(0.35f, 1f, e);
                label.color = c;
                yield return null;
            }
            rt.anchoredPosition = basePos;
            label.color = baseColor;
        }

        /// <summary>決定時の点滅（flashColor ⇄ baseColor）。終了時は baseColor。</summary>
        public static IEnumerator Flash(Text label, Color baseColor, Color flashColor, int flashes, float interval)
        {
            for (var i = 0; i < flashes; i++)
            {
                label.color = flashColor;
                yield return new WaitForSecondsRealtime(interval);
                label.color = baseColor;
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        public static IEnumerator FadeAlpha(Graphic g, float from, float to, float duration, Func<float, float> ease = null)
        {
            ease ??= EaseInOutSine;
            var elapsed = 0f;
            SetAlpha(g, from);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(g, Mathf.Lerp(from, to, ease(elapsed / duration)));
                yield return null;
            }
            SetAlpha(g, to);
        }

        public static IEnumerator SlideY(RectTransform rt, float fromY, float toY, float duration, Func<float, float> ease)
        {
            var elapsed = 0f;
            var x = rt.anchoredPosition.x;
            rt.anchoredPosition = new Vector2(x, fromY);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var y = Mathf.LerpUnclamped(fromY, toY, ease(Mathf.Clamp01(elapsed / duration)));
                rt.anchoredPosition = new Vector2(x, y);
                yield return null;
            }
            rt.anchoredPosition = new Vector2(x, toY);
        }

        public static void SetAlpha(Graphic g, float a)
        {
            if (g == null) return;
            var c = g.color;
            c.a = Mathf.Clamp01(a);
            g.color = c;
        }
    }
}
