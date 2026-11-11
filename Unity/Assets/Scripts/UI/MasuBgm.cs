using UnityEngine;

namespace Masu.UI
{
    /// <summary>BGM 用の音量。曲がなくても音量変更は即座にこの再生源へ反映する。</summary>
    public static class MasuBgm
    {
        static AudioSource _source;

        public static void SetVolume(float volume)
        {
            Ensure();
            _source.volume = Mathf.Clamp01(volume);
        }

        static void Ensure()
        {
            if (_source != null) return;
            var go = new GameObject("Bgm");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
        }
    }
}
