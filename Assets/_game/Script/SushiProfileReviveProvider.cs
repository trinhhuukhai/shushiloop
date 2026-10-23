using System;

// Uses the project's shared saved wallet. Connect an ad SDK via a provider subclass.
public sealed class SushiProfileReviveProvider : SushiReviveProvider
{
    public override bool CanShowRewardedAd => false;

    public override bool TrySpendCoins(int amount)
    {
        if (amount < 0 || DataManager.ins == null || !DataManager.ins.isLoaded ||
            GameDataFragment.cur == null || GameDataFragment.cur.dataPlayer == null ||
            GameDataFragment.cur.dataPlayer.money < amount) return false;
        GameDataFragment.cur.ChangeMoney(-amount, "revive");
        return true;
    }

    public override void ShowRewardedAd(Action<bool> onRewarded) => onRewarded?.Invoke(false);
}
