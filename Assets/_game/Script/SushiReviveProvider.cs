using System;
using UnityEngine;

// Implement bằng ví xu / SDK quảng cáo của game, rồi gán vào LevelManager.
// Chỉ trả true sau khi đã trừ tiền hoặc nhận reward thành công.
public abstract class SushiReviveProvider : MonoBehaviour
{
    public abstract bool TrySpendCoins(int amount);
    public abstract void ShowRewardedAd(Action<bool> onRewarded);
}
