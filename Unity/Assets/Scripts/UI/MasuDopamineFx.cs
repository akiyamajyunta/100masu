namespace Masu.UI
{
    /// <summary>オプション「エフェクト＞ドーパミン」選択時のみ有効な視覚演出。SE/BGM は将来実装。</summary>
    public static class MasuDopamineFx
    {
        public static bool Enabled => MasuVisualSettings.ActiveProfile == MasuVisualProfile.Dopamine;

        // SOUND: イントロのガシャコン着地 — 重い金属衝突 SE
        public static void OnIntroPanelSlam() { }

        // SOUND: 画面揺れ＋火花 — 低域の爆発／ガス噴出
        public static void OnIntroImpactBurst() { }

        // SOUND: 数字キー入力 — 短い打撃／リレー音
        public static void OnDigitKey() { }

        // SOUND: マス確定（カーソル移動）— 金床・ロック音
        public static void OnCellLockGlam() { }

        // SOUND: 採点正解 — 花火／大当たりジングル
        public static void OnScoreCorrectFirework() { }

        // SOUND: 採点不正解 — 落雷／焦げる破裂音
        public static void OnScoreWrongCharred() { }
    }
}
