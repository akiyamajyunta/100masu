using UnityEngine;
using UnityEngine.EventSystems;

namespace Masu.UI
{
    public static class MasuAutoStart
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Object.FindObjectOfType<MasuAppController>() != null)
                return;

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Object.DontDestroyOnLoad(es);
            }

            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera", typeof(Camera));
                camGo.tag = "MainCamera";
                var cam = camGo.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = MasuTheme.Bg;
            }

            var root = new GameObject("MasuGame");
            root.AddComponent<MasuAppController>();
        }
    }
}
