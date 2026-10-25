using TMPro;
using UnityEngine;

public sealed class CanvasLeadBoard : CanvasHomePage
{
    public TMP_Text progressValue;

    protected override void Refresh()
    {
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        if (progressValue != null)
            progressValue.text = (player?.completedLevels?.Count ?? 0).ToString();
    }
}