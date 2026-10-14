using UnityEngine;

namespace Masu.UI
{
    /// <summary>
    /// セッション確定後に解像度が変わったら、選んだプリセットへ戻す（ビルド版向け）。
    /// </summary>
    public sealed class MasuSessionDisplayGuard : MonoBehaviour
    {
        private int _width;
        private int _height;
        private bool _fullScreen;

        private void Start()
        {
            CaptureActive();
        }

        private void Update()
        {
            if (Application.isEditor) return;
            if (!MasuDisplaySettings.SessionLocked) return;
            if (Screen.width != _width || Screen.height != _height || Screen.fullScreen != _fullScreen)
                MasuDisplaySettings.ReapplyActiveSession();
        }

        public void Recapture() => CaptureActive();

        private void CaptureActive()
        {
            var i = MasuDisplaySettings.ActiveIndex;
            var opt = MasuDisplaySettings.Options[i];
            _width = opt.Width;
            _height = opt.Height;
            _fullScreen = opt.FullScreen;
        }
    }
}
