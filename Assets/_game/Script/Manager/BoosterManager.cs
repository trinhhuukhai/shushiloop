using UnityEngine;

// Cờ "đang dùng booster". FoodInputController là nơi duy nhất bật/tắt các cờ này
// (bật khi vào chế độ dùng booster, tắt khi dùng xong / huỷ / thua / về Home).
public class BoosterManager : Singleton<BoosterManager>
{
    public bool isUsingBoosterClick = false;
    public bool isUsingBoosterShuffle = false;
    public bool isUsingBoosterMagnet = false;

    public bool IsUsingAny => isUsingBoosterClick || isUsingBoosterShuffle || isUsingBoosterMagnet;

    // Xóa toàn bộ cờ đang dùng booster khi rời hoặc khởi tạo lại màn chơi.
    public void ResetUsing()
    {
        isUsingBoosterClick = false;
        isUsingBoosterShuffle = false;
        isUsingBoosterMagnet = false;
    }
}