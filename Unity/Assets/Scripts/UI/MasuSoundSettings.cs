using UnityEngine;

namespace Masu.UI
{
    /// <summary>SE / BGM の5段階音量。変更は即時に保存し、再生へ反映する。</summary>
    public static class MasuSoundSettings
    {
        public const int MaxLevel = 4;

        const string PrefSe = "masu_se_level";
        const string PrefBgm = "masu_bgm_level";

        public static int SeLevel { get; private set; } = MaxLevel;
        public static int BgmLevel { get; private set; } = MaxLevel;

        public static float SeVolume => SeLevel / (float)MaxLevel;
        public static float BgmVolume => BgmLevel / (float)MaxLevel;

        public static void Load()
        {
            SeLevel = Mathf.Clamp(PlayerPrefs.GetInt(PrefSe, MaxLevel), 0, MaxLevel);
            BgmLevel = Mathf.Clamp(PlayerPrefs.GetInt(PrefBgm, MaxLevel), 0, MaxLevel);
            Apply();
        }

        public static void SetSeLevel(int level)
        {
            level = Mathf.Clamp(level, 0, MaxLevel);
            if (level == SeLevel) return;
            SeLevel = level;
            PlayerPrefs.SetInt(PrefSe, level);
            PlayerPrefs.Save();
            Apply();
        }

        public static void SetBgmLevel(int level)
        {
            level = Mathf.Clamp(level, 0, MaxLevel);
            if (level == BgmLevel) return;
            BgmLevel = level;
            PlayerPrefs.SetInt(PrefBgm, level);
            PlayerPrefs.Save();
            Apply();
        }

        public static void Apply()
        {
            MasuMenuSfx.SetSeVolume(SeVolume);
            MasuBgm.SetVolume(BgmVolume);
        }
    }
}
