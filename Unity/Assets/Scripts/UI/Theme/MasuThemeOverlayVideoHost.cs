using UnityEngine;

namespace Masu.UI
{
    /// <summary>
    /// 動画オーバーレイ用の薄いラッパー（Unity Video パッケージ未導入でもコンパイル可能）。
    /// 導入後は同一 GameObject に VideoPlayer を付け、サブクラスまたは拡張で実装する。
    /// </summary>
    public abstract class MasuThemeOverlayVideoHost : MonoBehaviour
    {
        public abstract bool TryPlayResource(string titleArtClipKey);
        public abstract void StopPlayback();
    }
}
