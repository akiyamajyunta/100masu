using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// 森の写真＋赤選択帯が上下に動くメニュー（クラシック/クイック、四則、マス目など）。
    /// </summary>
    public sealed class MasuForestSelectMenu
    {
        private static readonly Color MenuTextBlack = new(0.06f, 0.06f, 0.06f);

        public readonly GameObject Panel;
        private readonly RectTransform _highlight;
        private readonly float[] _rowCenterY;
        private readonly Text[] _titleLabels;

        public Text TitleLabel(int index) => _titleLabels[index];
        public float RowY(int index) => _rowCenterY[index];
        public int RowCount => _rowCenterY.Length;

        public static MasuForestSelectMenu Create(
            Transform menuRoot,
            string panelName,
            ForestMenuRow[] rows,
            ForestMenuLayout? layout = null,
            string instruction = null)
        {
            layout ??= ForestMenuLayout.ForRowCount(rows.Length);

            var panelGo = new GameObject(panelName, typeof(RectTransform));
            panelGo.transform.SetParent(menuRoot, false);
            MasuUiFactory.Stretch(panelGo.GetComponent<RectTransform>());

            if (!string.IsNullOrEmpty(instruction))
                MasuInstructionBanner.Create(panelGo.transform, instruction);

            var rowY = layout.RowCenterY(rows.Length);

            var bar = MasuSelectBarImage.Create(panelGo.transform, "SelectBar", layout.BarWidth, layout.BarHeight);
            var highlight = bar.rectTransform;
            highlight.anchoredPosition = new Vector2(layout.BarOffsetX, rowY[0]);

            var titles = new Text[rows.Length];
            for (var i = 0; i < rows.Length; i++)
            {
                var cy = rowY[i];
                var title = MasuUiFactory.CreateText(panelGo.transform, $"Title{i}", rows[i].Title,
                    layout.TitleFontSize, MenuTextBlack, TextAnchor.MiddleCenter);
                MasuFonts.ApplyMincho(title, layout.TitleFontSize, bold: true);
                var titleRt = title.rectTransform;
                titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.5f);
                titleRt.pivot = new Vector2(0.5f, 0.5f);
                titleRt.anchoredPosition = new Vector2(0f, cy + layout.TitleOffsetY);
                titleRt.sizeDelta = new Vector2(layout.TextWidth, layout.TitleFontSize + 12f);
                titles[i] = title;

                if (!string.IsNullOrEmpty(rows[i].Subtitle))
                {
                    var sub = MasuUiFactory.CreateText(panelGo.transform, $"Sub{i}", rows[i].Subtitle,
                        layout.SubtitleFontSize, MenuTextBlack, TextAnchor.MiddleCenter);
                    MasuFonts.Apply(sub, layout.SubtitleFontSize);
                    sub.fontStyle = FontStyle.Bold;
                    var glow = sub.gameObject.AddComponent<Outline>();
                    glow.effectColor = new Color(1f, 1f, 1f, 0.35f);
                    glow.effectDistance = new Vector2(1.5f, -1.5f);
                    var subRt = sub.rectTransform;
                    subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0.5f);
                    subRt.pivot = new Vector2(0.5f, 0.5f);
                    subRt.anchoredPosition = new Vector2(0f, cy + layout.SubtitleOffsetY);
                    subRt.sizeDelta = new Vector2(layout.TextWidth, layout.SubtitleFontSize + 16f);
                }
            }

            return new MasuForestSelectMenu(panelGo, highlight, rowY, titles);
        }

        private MasuForestSelectMenu(GameObject panel, RectTransform highlight, float[] rowY, Text[] titles)
        {
            Panel = panel;
            _highlight = highlight;
            _rowCenterY = rowY;
            _titleLabels = titles;
        }

        public void SetHighlightIndex(int index, bool immediate)
        {
            index = Mathf.Clamp(index, 0, _rowCenterY.Length - 1);
            var y = _rowCenterY[index];
            if (immediate)
                _highlight.anchoredPosition = new Vector2(_highlight.anchoredPosition.x, y);
        }

        public void SetHighlightY(float y) =>
            _highlight.anchoredPosition = new Vector2(_highlight.anchoredPosition.x, y);

        public float HighlightY => _highlight.anchoredPosition.y;

        public void CollectEntranceSlideTargets(List<RectTransform> targets)
        {
            if (targets == null || Panel == null) return;
            var root = Panel.transform;
            if (root.Find("SelectBar") is RectTransform bar)
                targets.Add(bar);
            for (var i = 0; i < _titleLabels.Length; i++)
            {
                if (_titleLabels[i] != null)
                    targets.Add(_titleLabels[i].rectTransform);
                var sub = root.Find($"Sub{i}");
                if (sub is RectTransform subRt)
                    targets.Add(subRt);
            }
        }
    }

    /// <summary>2枚目モック基準のレイアウト（1280×960 設計座標）。</summary>
    public sealed class ForestMenuLayout
    {
        public float BarWidth = 720f;
        public float BarHeight = 156f;
        public float BarOffsetX;
        public float TextWidth = 900f;
        public int TitleFontSize = 64;
        public int SubtitleFontSize = 24;
        public float TitleOffsetY = 20f;
        public float SubtitleOffsetY = -32f;
        public float TwoRowStep = 220f;
        public float TwoRowCenterY = -20f;
        public float MultiRowStep = 118f;

        public float[] RowCenterY(int count)
        {
            if (count == 2)
            {
                return new[]
                {
                    TwoRowCenterY + TwoRowStep * 0.5f,
                    TwoRowCenterY - TwoRowStep * 0.5f
                };
            }

            var step = count <= 4 ? MultiRowStep : 92f;
            var center = TwoRowCenterY;
            var rowY = new float[count];
            for (var i = 0; i < count; i++)
                rowY[i] = center + (count - 1) * step * 0.5f - i * step;
            return rowY;
        }

        public static ForestMenuLayout ForRowCount(int count)
        {
            var layout = PlayStyleMock();
            if (count == 2) return layout;
            layout.BarWidth = 720f;
            layout.BarHeight = count <= 4 ? 140f : 120f;
            layout.TwoRowCenterY = -10f;
            layout.TitleFontSize = count <= 4 ? 64 : 54;
            layout.SubtitleFontSize = count <= 4 ? 22 : 20;
            layout.TitleOffsetY = 0f;
            layout.SubtitleOffsetY = -30f;
            return layout;
        }

        /// <summary>クラシック / クイック画面（モック2枚目: 帯 約800×244、見出し大）。</summary>
        public static ForestMenuLayout PlayStyleMock() => new()
        {
            BarWidth = 800f,
            BarHeight = 244f,
            TwoRowStep = 291f,
            TwoRowCenterY = -16f,
            TitleFontSize = 96,
            SubtitleFontSize = 28,
            TitleOffsetY = 40f,
            SubtitleOffsetY = -62f
        };
    }

    public readonly struct ForestMenuRow
    {
        public readonly string Title;
        public readonly string Subtitle;

        public ForestMenuRow(string title, string subtitle = "")
        {
            Title = title;
            Subtitle = subtitle;
        }
    }
}
