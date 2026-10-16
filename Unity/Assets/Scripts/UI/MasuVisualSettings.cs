using UnityEngine;

namespace Masu.UI
{
    public enum MasuVisualProfile
    {
        Normal,
        Dopamine
    }

    public static class MasuVisualSettings
    {
        private const string PrefKeyProfile = "masu_visual_profile";

        public static MasuVisualProfile ActiveProfile { get; private set; } = MasuVisualProfile.Normal;

        public static void Load()
        {
            var idx = PlayerPrefs.GetInt(PrefKeyProfile, (int)MasuVisualProfile.Normal);
            ActiveProfile = (MasuVisualProfile)Mathf.Clamp(idx, 0, 1);
        }

        public static void SetProfile(MasuVisualProfile profile)
        {
            ActiveProfile = profile;
            PlayerPrefs.SetInt(PrefKeyProfile, (int)profile);
            PlayerPrefs.Save();
        }

        public static string LabelJa(this MasuVisualProfile profile) => profile switch
        {
            MasuVisualProfile.Normal => "ノーマル",
            MasuVisualProfile.Dopamine => "ドーパミン",
            _ => profile.ToString()
        };

        public static string MenuLabel(MasuVisualProfile profile)
        {
            var baseLabel = profile.LabelJa();
            return ActiveProfile == profile ? $"{baseLabel} … 使用中" : baseLabel;
        }
    }
}
