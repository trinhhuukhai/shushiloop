using System;
using UnityEngine;

// Implement bằng ví xu / SDK quảng cáo của game, rồi gán vào LevelManager.
// Chỉ trả true sau khi đã trừ tiền hoặc nhận reward thành công.
public abstract class SushiReviveProvider : MonoBehaviour
{
    public virtual bool CanShowRewardedAd => true;
    // Quy định thao tác thử thanh toán tiền hồi sinh cho các provider cụ thể.
    public abstract bool TrySpendCoins(int amount);
    // Quy định cách mở quảng cáo thưởng và trả kết quả qua callback.
    public abstract void ShowRewardedAd(Action<bool> onRewarded);
}
