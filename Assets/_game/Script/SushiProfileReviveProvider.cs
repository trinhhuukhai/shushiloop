using System;

// Uses the project's shared saved wallet. Connect an ad SDK via a provider subclass.
public sealed class SushiProfileReviveProvider : SushiReviveProvider
{
    public override bool CanShowRewardedAd => false;

    // Kiểm tra ví đã nạp và đủ tiền rồi trừ chi phí hồi sinh.
    public override bool TrySpendCoins(int amount)
    {
        if (amount < 0 || DataManager.ins == null || !DataManager.ins.isLoaded ||
            GameDataFragment.cur == null || GameDataFragment.cur.dataPlayer == null ||
            GameDataFragment.cur.dataPlayer.money < amount) return false;
        GameDataFragment.cur.ChangeMoney(-amount, "revive");
        return true;
    }

    // Trả kết quả không có thưởng vì provider hiện chưa tích hợp quảng cáo.
    public override void ShowRewardedAd(Action<bool> onRewarded) => onRewarded?.Invoke(false);
}
