using TMPro;
using UnityEngine;

public sealed class CanvasShop : CanvasHomePage
{
    public TMP_Text[] inventoryCounts;

    protected override void Refresh()
    {
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        int[] counts = player == null ? new int[3] : new[]
        {
            player.boosterClick, player.boosterSuffle, player.boosterHut
        };
        for (int i = 0; inventoryCounts != null && i < inventoryCounts.Length && i < counts.Length; i++)
            if (inventoryCounts[i] != null) inventoryCounts[i].text = Mathf.Max(0, counts[i]).ToString();
    }
}