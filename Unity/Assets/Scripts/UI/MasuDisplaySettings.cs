using UnityEngine;

namespace Masu.UI
{
    public readonly struct DisplayOption
    {
        public readonly string Label;
        public readonly int Width;
        public readonly int Height;
        public readonly bool FullScreen;

        public DisplayOption(string label, int width, int height, bool fullScreen)
        {
            Label = label;
            Width = width;
            Height = height;
            FullScreen = fullScreen;
        }
    }

    /// <summary>
    /// 起動時に選んだ解像度をセッション固定。UI は DesignReference（4:3）基準で Canvas Scaler が拡縮。
    /// </summary>
    public static class MasuDisplaySettings
    {
        private const string PrefKeyIndex = "masu.display_index";
        private const string PrefKeyAsk = "masu.display_ask_every_time";

        public const int DesignWidth = 1280;
        public const int DesignHeight = 960;

        public static readonly DisplayOption[] Options =
        {
            new("フルスクリーン(640×480)", 640, 480, true),
            new("ウィンドウ(640×480)", 640, 480, false),
            new("ウィンドウ(960×720)", 960, 720, false),
            new("ウィンドウ(1280×960)", 1280, 960, false),
        };

        public static int ActiveIndex { get; private set; }
        public static bool SessionLocked { get; private set; }

        public static string[] Labels()
        {
            var labels = new string[Options.Length];
            for (var i = 0; i < Options.Length; i++)
                labels[i] = Options[i].Label;
            return labels;
        }

        public static int LoadSavedIndex()
        {
            var idx = PlayerPrefs.GetInt(PrefKeyIndex, 2);
            return Mathf.Clamp(idx, 0, Options.Length - 1);
        }

        public static bool LoadAskEveryTime() =>
            PlayerPrefs.GetInt(PrefKeyAsk, 1) != 0;

        public static void Save(int index, bool askEveryTime)
        {
            PlayerPrefs.SetInt(PrefKeyIndex, Mathf.Clamp(index, 0, Options.Length - 1));
            PlayerPrefs.SetInt(PrefKeyAsk, askEveryTime ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>ランチャー中のプレビュー（セッション未確定）。</summary>
        public static void ApplyPreview(int index)
        {
            if (SessionLocked) return;
            ActiveIndex = Mathf.Clamp(index, 0, Options.Length - 1);
            SetScreen(Options[ActiveIndex]);
        }

        /// <summary>ゲーム起動確定: 以降この解像度のみ（再起動まで変更しない）。</summary>
        public static void CommitSession(int index, bool askEveryTime)
        {
            ActiveIndex = Mathf.Clamp(index, 0, Options.Length - 1);
            Save(ActiveIndex, askEveryTime);
            SetScreen(Options[ActiveIndex]);
            SessionLocked = true;
        }

        public static void ApplySavedForSession()
        {
            ActiveIndex = LoadSavedIndex();
            SetScreen(Options[ActiveIndex]);
            SessionLocked = true;
        }

        public static void ReapplyActiveSession() => SetScreen(Options[ActiveIndex]);

        private static void SetScreen(DisplayOption opt)
        {
            var mode = opt.FullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(opt.Width, opt.Height, mode);
        }
    }
}
