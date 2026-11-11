using UnityEngine;

namespace Masu.UI
{
    /// <summary>メニューとプレイ盤面の効果音。</summary>
    public static class MasuMenuSfx
    {
        const string Folder = "Audio/";

        static AudioSource _source;
        static AudioClip _cursor;
        static AudioClip _gridCursor;
        static AudioClip _confirm;
        static AudioClip _cancel;

        public static void PlayCursor() => Play(ref _cursor, "menu_cursor");

        public static void PlayGridCursor() => Play(ref _gridCursor, "grid_cursor");

        public static void PlayConfirm() => Play(ref _confirm, "menu_confirm");

        public static void PlayCancel() => Play(ref _cancel, "menu_cancel");

        public static void SetSeVolume(float volume)
        {
            EnsureSource();
            if (_source != null)
                _source.volume = Mathf.Clamp01(volume);
        }

        static void Play(ref AudioClip clip, string resourceName)
        {
            EnsureSource();
            if (clip == null)
                clip = Resources.Load<AudioClip>(Folder + resourceName);
            if (clip == null || _source == null) return;
            _source.PlayOneShot(clip);
        }

        static void EnsureSource()
        {
            if (_source != null) return;

            if (Object.FindObjectOfType<AudioListener>() == null)
            {
                var cam = Camera.main;
                if (cam != null)
                    cam.gameObject.AddComponent<AudioListener>();
            }

            var go = new GameObject("MenuSfx");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }
    }
}
