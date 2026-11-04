using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>オプション＞ビジュアルのテーマカードグリッド（キーボード＋マウス）。</summary>
    public sealed class MasuVisualThemeSelectUI
    {
        private Text _selectedName;
        private Text _hint;
        private readonly List<ThemeCard> _cards = new();
        private int _cursor;
        private readonly Action _onBack;

        private sealed class ThemeCard
        {
            public MasuThemeDefinition Definition;
            public RectTransform Root;
            public Outline SelectionOutline;
            public Image Thumbnail;
            public Button Button;
        }

        public static MasuVisualThemeSelectUI Build(Transform panel, Action onBack)
        {
            var ui = new MasuVisualThemeSelectUI(onBack);
            ui.Create(panel);
            return ui;
        }

        private MasuVisualThemeSelectUI(Action onBack) => _onBack = onBack;

        private void Create(Transform panel)
        {
            var themes = MasuThemeCatalog.All;
            var activeId = MasuThemeManager.ActiveDefinition?.ThemeId;

            _selectedName = MasuUiFactory.CreateText(panel, "ThemeName", "", MasuVisualThemeLayout.SelectedNameFontSize,
                MasuTheme.MenuTitleText, TextAnchor.UpperCenter);
            var nameRt = _selectedName.rectTransform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0, MasuVisualThemeLayout.SelectedNameTopY);
            nameRt.sizeDelta = new Vector2(720, 56);
            MasuFonts.ApplyRankingMincho(_selectedName, MasuVisualThemeLayout.SelectedNameFontSize, bold: true);

            var count = themes.Count;
            var cols = MasuVisualThemeLayout.GridColumns;
            var gridW = cols * MasuVisualThemeLayout.GridCellSize + (cols - 1) * MasuVisualThemeLayout.GridSpacingX;
            var startX = -gridW * 0.5f + MasuVisualThemeLayout.GridCellSize * 0.5f;

            for (var i = 0; i < count; i++)
            {
                var def = themes[i];
                var row = i / cols;
                var col = i % cols;
                var x = startX + col * (MasuVisualThemeLayout.GridCellSize + MasuVisualThemeLayout.GridSpacingX);
                var y = MasuVisualThemeLayout.GridTopY
                        - row * (MasuVisualThemeLayout.GridCellSize + MasuVisualThemeLayout.GridSpacingY);

                var card = CreateCard(panel, def, new Vector2(x, y));
                _cards.Add(card);
                if (def.ThemeId == activeId)
                    _cursor = i;
            }

            _hint = MasuUiFactory.CreateText(panel, "ThemeHint",
                "←→↑↓ 選択　Enter 決定（戻る）　Esc オプションへ",
                MasuVisualThemeLayout.BackHintFontSize, Color.white, TextAnchor.LowerCenter);
            MasuUiFactory.ApplyRankingScreenJapanese(_hint, MasuVisualThemeLayout.BackHintFontSize);
            var hintRt = _hint.rectTransform;
            hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0, MasuVisualThemeLayout.BackHintBottom);
            hintRt.sizeDelta = new Vector2(920, 36);

            RefreshSelection(applyTheme: false);
        }

        private ThemeCard CreateCard(Transform panel, MasuThemeDefinition def, Vector2 pos)
        {
            var black = MasuVisualThemeLayout.CardBlackBorderPx;
            var white = MasuVisualThemeLayout.CardWhiteBorderPx;
            var inner = black + white;

            var go = new GameObject($"ThemeCard_{def.ThemeId}", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(MasuVisualThemeLayout.GridCellSize, MasuVisualThemeLayout.GridCellSize);

            var blackGo = new GameObject("BlackBorder", typeof(RectTransform), typeof(Image));
            blackGo.transform.SetParent(go.transform, false);
            MasuUiFactory.Stretch(blackGo.GetComponent<RectTransform>());
            var blackImg = blackGo.GetComponent<Image>();
            blackImg.color = Color.black;
            var selectionOutline = blackGo.AddComponent<Outline>();
            selectionOutline.effectColor = Color.clear;
            selectionOutline.effectDistance = new Vector2(
                MasuVisualThemeLayout.CardSelectionRingOutsetPx,
                -MasuVisualThemeLayout.CardSelectionRingOutsetPx);
            selectionOutline.useGraphicAlpha = false;

            var whiteGo = new GameObject("WhiteFrame", typeof(RectTransform), typeof(Image));
            whiteGo.transform.SetParent(go.transform, false);
            MasuUiFactory.Stretch(whiteGo.GetComponent<RectTransform>(), black, black, black, black);
            whiteGo.GetComponent<Image>().color = Color.white;

            var thumbGo = new GameObject("Thumbnail", typeof(RectTransform), typeof(Image));
            thumbGo.transform.SetParent(go.transform, false);
            MasuUiFactory.Stretch(thumbGo.GetComponent<RectTransform>(), inner, inner, inner, inner);
            var thumbnail = thumbGo.GetComponent<Image>();
            thumbnail.raycastTarget = true;
            thumbnail.preserveAspect = false;
            thumbnail.type = Image.Type.Simple;

            var previewKey = MasuThemeCardPreview.ResolveResourceKey(def);
            MasuThemeBackdropApplier.ApplyBackdrop(thumbnail, previewKey, def.Palette.ThemeCardTint);

            var button = go.AddComponent<Button>();
            button.targetGraphic = thumbnail;
            var captured = def;
            button.onClick.AddListener(() =>
            {
                var idx = _cards.FindIndex(c => c.Definition == captured);
                if (idx >= 0)
                {
                    _cursor = idx;
                    RefreshSelection(applyTheme: true);
                }
            });

            return new ThemeCard
            {
                Definition = def,
                Root = rt,
                SelectionOutline = selectionOutline,
                Thumbnail = thumbnail,
                Button = button,
            };
        }

        public void SyncToActiveTheme()
        {
            var activeId = MasuThemeManager.ActiveDefinition?.ThemeId;
            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Definition.ThemeId == activeId)
                {
                    _cursor = i;
                    break;
                }
            }

            RefreshSelection(applyTheme: false);
        }

        public void CollectEntranceTargets(List<RectTransform> targets)
        {
            foreach (var card in _cards)
                if (card.Root != null)
                    targets.Add(card.Root);
            if (_selectedName != null)
                targets.Add(_selectedName.rectTransform);
        }

        public bool HandleInput()
        {
            if (_cards.Count == 0) return false;
            var cols = MasuVisualThemeLayout.GridColumns;
            var changed = false;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                _cursor = Mathf.Max(0, _cursor - 1);
                changed = true;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                _cursor = Mathf.Min(_cards.Count - 1, _cursor + 1);
                changed = true;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _cursor = Mathf.Max(0, _cursor - cols);
                changed = true;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _cursor = Mathf.Min(_cards.Count - 1, _cursor + cols);
                changed = true;
            }

            if (changed)
                RefreshSelection(applyTheme: true);

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                _onBack?.Invoke();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _onBack?.Invoke();
                return true;
            }

            return false;
        }

        private void RefreshSelection(bool applyTheme)
        {
            if (_cards.Count == 0) return;
            _cursor = Mathf.Clamp(_cursor, 0, _cards.Count - 1);
            var active = _cards[_cursor].Definition;
            _selectedName.text = active.DisplayNameJa;

            for (var i = 0; i < _cards.Count; i++)
            {
                var sel = i == _cursor;
                var card = _cards[i];
                if (card.SelectionOutline != null)
                {
                    card.SelectionOutline.effectColor = sel
                        ? active.Palette.ThemeCardSelectedBorder
                        : Color.clear;
                }
            }

            if (applyTheme)
                MasuThemeManager.SetActiveTheme(active.ThemeId);
        }
    }
}
