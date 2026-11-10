using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Trang Shop: danh sách gói (Remove Ads, Starter, các Bundle, gói xu) xếp bằng Layout Group.
// Gói mua một lần sẽ ẩn khỏi shop sau khi mua, các gói phía sau tự đẩy lên.
public sealed class CanvasShop : CanvasHomePage
{
    [Serializable]
    public sealed class Pack
    {
        public string id;
        public string title;
        [Tooltip("Giá hiển thị trên nút mua (chưa nối IAP).")]
        public string price = "6.99$";
        public int coins;
        public int tongs;
        public int magnet;
        public int swap;
        [Tooltip("Số giờ vô hạn mạng.")]
        public int unlimitedHours;
        public bool removeAds;
        [Tooltip("Bật: mua xong gói biến mất khỏi shop. Tắt: gói mua lại được nhiều lần (vd. gói xu).")]
        public bool hideAfterPurchase = true;
    }

    [Tooltip("Danh sách gói theo đúng thứ tự các nút mua trong prefab.")]
    public Pack[] packs = Array.Empty<Pack>();
    [Tooltip("Nhãn giá trên từng nút mua, cùng thứ tự với packs.")]
    public TMP_Text[] priceLabels = Array.Empty<TMP_Text>();
    [Tooltip("Object gốc của từng gói (cùng thứ tự với packs); bị ẩn khi gói đã mua.")]
    public GameObject[] packViews = Array.Empty<GameObject>();
    [Tooltip("Content của ScrollRect (có VerticalLayoutGroup) để dựng lại layout khi ẩn gói.")]
    public RectTransform listContent;
    [Tooltip("Nhóm lưới gói xu; tự ẩn khi không còn gói xu nào.")]
    public GameObject coinGrid;

    // Giữ tương thích với bố cục Shop cũ.
    [HideInInspector] public TMP_Text[] inventoryCounts;

    // Gọi khi người chơi bấm mua; IAP lắng nghe ở đây và gọi CompletePurchase(pack) khi thanh toán thành công.
    public static event Action<Pack> PurchaseRequested;

    private static CanvasShop instance;
    private const string BoughtKey = "shop_bought_";

    private void Awake() => instance = this;

    public static bool IsBought(Pack pack) =>
        pack != null && pack.hideAfterPurchase && PlayerPrefs.GetInt(BoughtKey + pack.id, 0) == 1;

    // Cập nhật giá, ẩn gói đã mua rồi dựng lại layout để các gói sau đẩy lên.
    protected override void Refresh()
    {
        for (int i = 0; i < priceLabels.Length && i < packs.Length; i++)
            if (priceLabels[i] != null && packs[i] != null) priceLabels[i].text = packs[i].price;
        for (int i = 0; i < packViews.Length && i < packs.Length; i++)
            //if (packViews[i] != null) packViews[i].SetActive(!IsBought(packs[i]));
        if (coinGrid != null)
        {
            bool any = false;
            foreach (Transform child in coinGrid.transform) any |= child.gameObject.activeSelf;
            //coinGrid.SetActive(any);
        }
        if (listContent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
    }

    // Nút mua của gói thứ index (gắn sẵn trong prefab).
    public void Buy(int index)
    {
        if (index < 0 || index >= packs.Length || packs[index] == null || IsBought(packs[index])) return;
        Pack pack = packs[index];
        if (PurchaseRequested != null) { PurchaseRequested.Invoke(pack); return; }
#if UNITY_EDITOR
        // Chưa có IAP: trong Editor coi như mua thành công để thử phần thưởng và layout.
        Debug.Log($"[Shop] Chưa tích hợp IAP – giả lập mua thành công gói '{pack.id}' (chỉ trong Editor).", this);
        CompletePurchase(pack);
#else
        Debug.Log($"[Shop] Chưa tích hợp IAP – bấm mua gói '{pack.id}' ({pack.price}).", this);
#endif
    }

    // IAP gọi khi thanh toán thành công: trao thưởng, đánh dấu gói một lần đã mua và làm mới shop.
    public static void CompletePurchase(Pack pack)
    {
        if (pack == null) return;
        GrantPack(pack);
        //if (pack.hideAfterPurchase)
        //{
        //    PlayerPrefs.SetInt(BoughtKey + pack.id, 1);
        //    PlayerPrefs.Save();
        //}
        if (instance != null && instance.isActiveAndEnabled) instance.Refresh();
    }

    // Trao phần thưởng của gói (xu, booster, mạng vô hạn).
    public static void GrantPack(Pack pack)
    {
        var data = GameDataFragment.cur;
        if (pack == null || data == null) return;
        if (pack.coins > 0) data.ChangeMoney(pack.coins, "shop_" + pack.id);
        if (pack.tongs > 0) data.ChangeBoosterCount(BoosterType.Click, pack.tongs, "shop_" + pack.id);
        if (pack.swap > 0) data.ChangeBoosterCount(BoosterType.Shuffle, pack.swap, "shop_" + pack.id);
        if (pack.magnet > 0) data.ChangeBoosterCount(BoosterType.Magnet, pack.magnet, "shop_" + pack.id);
        if (pack.unlimitedHours > 0) LivesDataFragment.cur?.AddUnlimitedLivesHours(pack.unlimitedHours);
    }

    // Chuột phải vào component -> xoá trạng thái đã mua để thử lại.
    [ContextMenu("Reset purchased packs")]
    public void ResetPurchases()
    {
        foreach (Pack pack in packs)
            if (pack != null) PlayerPrefs.DeleteKey(BoughtKey + pack.id);
        PlayerPrefs.Save();
        if (isActiveAndEnabled) Refresh();
    }
}
