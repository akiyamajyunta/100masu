using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>画面揺れ・全画面フラッシュ・火花／花火（uGUI）。</summary>
    public sealed class MasuDopamineScreenFx : MonoBehaviour
    {
        sealed class Spark
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 Vel;
            public float Life;
        }

        RectTransform _shakeTarget;
        Image _flash;
        Vector2 _shakeBase;
        float _shakeT;
        float _shakeMag;
        readonly List<Spark> _sparks = new();

        public void Initialize(RectTransform shakeTarget)
        {
            _shakeTarget = shakeTarget;
            var flashGo = new GameObject("Flash", typeof(RectTransform), typeof(Image));
            flashGo.transform.SetParent(transform, false);
            _flash = flashGo.GetComponent<Image>();
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _flash.raycastTarget = false;
            MasuUiFactory.Stretch(_flash.rectTransform);
        }

        void LateUpdate()
        {
            if (_shakeTarget != null)
            {
                if (_shakeT > 0f)
                {
                    _shakeT -= Time.unscaledDeltaTime;
                    var m = _shakeMag * Mathf.Clamp01(_shakeT / 0.4f);
                    var j = new Vector2(
                        (Mathf.PerlinNoise(Time.unscaledTime * 48f, 0f) - 0.5f) * 2f * m,
                        (Mathf.PerlinNoise(0f, Time.unscaledTime * 52f) - 0.5f) * 2f * m);
                    _shakeTarget.anchoredPosition = _shakeBase + j;
                }
                else
                    _shakeTarget.anchoredPosition = _shakeBase;
            }

            for (var i = _sparks.Count - 1; i >= 0; i--)
            {
                var s = _sparks[i];
                s.Life -= Time.unscaledDeltaTime;
                if (s.Life <= 0f)
                {
                    Destroy(s.Rt.gameObject);
                    _sparks.RemoveAt(i);
                    continue;
                }
                s.Rt.anchoredPosition += s.Vel * Time.unscaledDeltaTime;
                s.Vel *= 0.92f;
                var c = s.Img.color;
                c.a = Mathf.Clamp01(s.Life / 0.55f);
                s.Img.color = c;
            }

            if (_flash != null && _flash.color.a > 0f)
            {
                var c = _flash.color;
                c.a = Mathf.MoveTowards(c.a, 0f, Time.unscaledDeltaTime * 3.5f);
                _flash.color = c;
            }
        }

        public void CaptureShakeBase()
        {
            if (_shakeTarget != null)
                _shakeBase = _shakeTarget.anchoredPosition;
        }

        public void Shake(float seconds, float magnitude)
        {
            if (_shakeTarget == null) return;
            _shakeT = Mathf.Max(_shakeT, seconds);
            _shakeMag = Mathf.Max(_shakeMag, magnitude);
        }

        public void FlashScreen(Color color, float peakAlpha, float hold = 0.02f)
        {
            if (_flash == null) return;
            _flash.color = new Color(color.r, color.g, color.b, peakAlpha);
        }

        public void BurstSparks(Vector2 anchoredPosition, int count, float speed, bool fireworkColors)
        {
            for (var i = 0; i < count; i++)
            {
                var go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(Random.Range(4f, 10f), Random.Range(4f, 14f));
                rt.anchoredPosition = anchoredPosition;
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                if (fireworkColors)
                    img.color = Color.HSVToRGB(Random.value, 0.85f, 1f);
                else
                    img.color = Random.value > 0.5f
                        ? new Color(1f, 0.85f, 0.2f, 1f)
                        : new Color(1f, 0.4f, 0.15f, 1f);
                var ang = Random.Range(0f, Mathf.PI * 2f);
                var vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(speed * 0.5f, speed);
                _sparks.Add(new Spark { Rt = rt, Img = img, Vel = vel, Life = Random.Range(0.35f, 0.65f) });
            }
        }
    }
}
