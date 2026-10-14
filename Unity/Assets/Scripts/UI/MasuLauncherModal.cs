using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// 起動時の表示設定モーダル（Python launcher.py / tk ダイアログ相当）。
    /// </summary>
    public sealed class MasuLauncherModal
    {
        private readonly GameObject _root;
        private readonly Text[] _optionLabels;
        private readonly Text _askLabel;

        public int SelectedIndex { get; set; }
        public bool AskEveryTime { get; set; }
        public bool IsOpen => _root.activeSelf;

        public MasuLauncherModal(Transform canvas)
        {
            _root = MasuUiFactory.CreatePanel(canvas, "LauncherModal", new Color(0, 0, 0, 0.45f));
            _root.transform.SetAsLastSibling();

            var box = MasuUiFactory.CreateCenteredPanel(
                _root.transform, "Box", new Color(0.94f, 0.94f, 0.94f, 1f),
                new Vector2(420, 320), Vector2.zero);

            var titleBg = MasuUiFactory.CreateCenteredPanel(
                box.transform, "TitleBar", new Color(0.75f, 0.78f, 0.85f, 1f),
                new Vector2(420, 32), new Vector2(0, 144));

            var titleText = MasuUiFactory.CreateText(titleBg.transform, "Title", "100マス計算", 18,
                new Color(0.1f, 0.1f, 0.15f), TextAnchor.MiddleLeft);
            MasuUiFactory.Stretch(titleText.rectTransform, 10, 0, 10, 0);

            var prompt = MasuUiFactory.CreateText(box.transform, "Prompt", "ウィンドウモードを選択してください", 16,
                new Color(0.15f, 0.15f, 0.2f), TextAnchor.MiddleLeft);
            prompt.rectTransform.anchorMin = prompt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            prompt.rectTransform.sizeDelta = new Vector2(380, 24);
            prompt.rectTransform.anchoredPosition = new Vector2(0, 100);

            var labels = MasuDisplaySettings.Labels();
            _optionLabels = new Text[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                _optionLabels[i] = MasuUiFactory.CreateText(box.transform, $"Opt{i}", labels[i], 15,
                    new Color(0.1f, 0.1f, 0.15f), TextAnchor.MiddleLeft);
                _optionLabels[i].rectTransform.anchorMin = _optionLabels[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _optionLabels[i].rectTransform.sizeDelta = new Vector2(380, 24);
                _optionLabels[i].rectTransform.anchoredPosition = new Vector2(0, 70 - i * 28);
            }

            _askLabel = MasuUiFactory.CreateText(box.transform, "Ask", "", 15,
                new Color(0.1f, 0.1f, 0.15f), TextAnchor.MiddleLeft);
            _askLabel.rectTransform.anchorMin = _askLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _askLabel.rectTransform.sizeDelta = new Vector2(380, 24);
            _askLabel.rectTransform.anchoredPosition = new Vector2(0, -45);

            var launchGo = MasuUiFactory.CreateCenteredPanel(
                box.transform, "LaunchBtn", new Color(0.88f, 0.9f, 0.95f, 1f),
                new Vector2(160, 32), new Vector2(0, -110));
            launchGo.AddComponent<Outline>().effectColor = new Color(0.4f, 0.45f, 0.55f);
            MasuUiFactory.CreateText(launchGo.transform, "Label", "ゲーム起動", 16,
                new Color(0.1f, 0.1f, 0.2f), TextAnchor.MiddleCenter);

            _root.SetActive(false);
        }

        public void Open(int selectedIndex, bool askEveryTime)
        {
            SelectedIndex = selectedIndex;
            AskEveryTime = askEveryTime;
            _root.transform.SetAsLastSibling();
            _root.SetActive(true);
            Refresh();
        }

        public void Close() => _root.SetActive(false);

        public void Refresh()
        {
            var labels = MasuDisplaySettings.Labels();
            for (var i = 0; i < _optionLabels.Length; i++)
            {
                var mark = i == SelectedIndex ? "●" : "○";
                _optionLabels[i].text = $"  {mark}  {labels[i]}";
                _optionLabels[i].fontStyle = i == SelectedIndex ? FontStyle.Bold : FontStyle.Normal;
            }

            _askLabel.text = AskEveryTime ? "  ☑  起動時に毎回聞く" : "  ☐  起動時に毎回聞く";
        }
    }
}
