using System;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// テーマ変更時に Graphic の色を自動更新する（任意でプレハブに付与）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class MasuThemeableGraphic : MonoBehaviour
    {
        public enum ColorSlot
        {
            Text,
            TextDim,
            CursorGold,
            MainPanel,
            Bg,
            GameFrameBorder,
        }

        public ColorSlot Slot = ColorSlot.Text;

        private Graphic _graphic;

        private void Awake() => _graphic = GetComponent<Graphic>();

        private void OnEnable()
        {
            MasuThemeManager.ThemeChanged += OnThemeChanged;
            Apply(MasuThemeManager.ActiveDefinition);
        }

        private void OnDisable() => MasuThemeManager.ThemeChanged -= OnThemeChanged;

        private void OnThemeChanged(MasuThemeDefinition _) => Apply(MasuThemeManager.ActiveDefinition);

        private void Apply(MasuThemeDefinition def)
        {
            if (_graphic == null) return;
            _graphic.color = Slot switch
            {
                ColorSlot.Text => MasuTheme.Text,
                ColorSlot.TextDim => MasuTheme.TextDim,
                ColorSlot.CursorGold => MasuTheme.CursorGold,
                ColorSlot.MainPanel => MasuTheme.MainPanel,
                ColorSlot.Bg => MasuTheme.Bg,
                ColorSlot.GameFrameBorder => MasuTheme.GameFrameBorder,
                _ => _graphic.color,
            };
        }
    }
}
