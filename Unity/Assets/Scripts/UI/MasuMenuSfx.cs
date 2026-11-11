using UnityEngine;

namespace Masu.UI
{
    /// <summary>メニューとプレイ盤面の効果音。</summary>
    public static class MasuMenuSfx
    {
        const string Folder = "Audio/";
        const int VoiceCount = 32;

        static AudioSource[] _voices;
        static int _nextVoice;
        static AudioClip _cursor;
        static AudioClip _gridCursor;
        static AudioClip _confirm;
        static AudioClip _cancel;
        static AudioClip _nameConfirm;
        static AudioClip _cellPop;
        static AudioClip _dopamineHit;

        public static void PlayCursor() => Play(ref _cursor, "menu_cursor");

        public static void PlayGridCursor() => Play(ref _gridCursor, "grid_cursor");

        public static void PlayConfirm() => Play(ref _confirm, "menu_confirm");

        public static void PlayCancel() => Play(ref _cancel, "menu_cancel");

        public static void PlayNameConfirm() => Play(ref _nameConfirm, "name_confirm");

        /// <summary>ノーマル時、見出しの出現と採点スタンプ。</summary>
        public static void PlayNormalCellPop()
        {
            if (MasuVisualSettings.ActiveProfile != MasuVisualProfile.Normal) return;
            Play(ref _cellPop, "normal_cell_pop");
        }

        /// <summary>ドーパミン時、見出しの出現と採点スタンプ。</summary>
        public static void PlayDopamineHit()
        {
            if (MasuVisualSettings.ActiveProfile != MasuVisualProfile.Dopamine) return;
            Play(ref _dopamineHit, "dopamine_hit");
        }

        public static void SetSeVolume(float volume)
        {
            EnsureVoices();
            var level = Mathf.Clamp01(volume);
            if (_voices == null) return;
            foreach (var voice in _voices)
            {
                if (voice != null)
                    voice.volume = level;
            }
        }

        static void Play(ref AudioClip clip, string resourceName)
        {
            EnsureVoices();
            if (clip == null)
                clip = Resources.Load<AudioClip>(Folder + resourceName);
            if (clip == null || _voices == null) return;

            var voice = NextFreeVoice();
            voice.clip = clip;
            voice.Play();
        }

        static AudioSource NextFreeVoice()
        {
            for (var i = 0; i < _voices.Length; i++)
            {
                var index = (_nextVoice + i) % _voices.Length;
                var voice = _voices[index];
                if (voice != null && !voice.isPlaying)
                {
                    _nextVoice = (index + 1) % _voices.Length;
                    return voice;
                }
            }

            var fallback = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            return fallback;
        }

        static void EnsureVoices()
        {
            if (_voices != null && _voices.Length == VoiceCount && _voices[0] != null) return;

            if (Object.FindObjectOfType<AudioListener>() == null)
            {
                var cam = Camera.main;
                if (cam != null)
                    cam.gameObject.AddComponent<AudioListener>();
            }

            var volume = _voices != null && _voices.Length > 0 && _voices[0] != null
                ? _voices[0].volume
                : MasuSoundSettings.SeVolume;

            var go = new GameObject("MenuSfx");
            Object.DontDestroyOnLoad(go);
            _voices = new AudioSource[VoiceCount];
            for (var i = 0; i < VoiceCount; i++)
            {
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = volume;
                source.priority = 64;
                _voices[i] = source;
            }
            _nextVoice = 0;
        }
    }
}
