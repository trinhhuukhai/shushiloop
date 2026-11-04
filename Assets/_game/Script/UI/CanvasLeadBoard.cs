using TMPro;
using UnityEngine;

public sealed class CanvasLeadBoard : CanvasHomePage
{
    public TMP_Text progressValue;

    // Hiển thị số màn đã hoàn thành từ dữ liệu người chơi trên trang bảng xếp hạng.
    protected override void Refresh()
    {
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        if (progressValue != null)
            progressValue.text = (player?.completedLevels?.Count ?? 0).ToString();
    }
}