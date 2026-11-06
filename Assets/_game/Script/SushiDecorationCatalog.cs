using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Sushi/Decoration Catalog")]
public sealed class SushiDecorationCatalog : ScriptableObject
{
    public const string ResourcePath = "Decoration/KaitenCounter";
    public string areaId = "kaiten_counter";
    [Min(0)] public int completionCoins = 1000;
    public Item[] items = {
        new Item("deco_sign", "Biển hiệu", "Sign", 2),
        new Item("deco_floor", "Sàn gỗ", "Wooden floor", 2),
        new Item("deco_lantern", "Đèn lồng", "Lanterns", 3),
        new Item("deco_counter", "Quầy gỗ", "Wooden counter", 3),
        new Item("deco_stools", "Ghế khách", "Stools", 4),
        new Item("deco_noren", "Rèm noren cửa", "Noren curtain", 4),
        new Item("deco_belt", "Băng chuyền", "Conveyor belt", 4),
        new Item("deco_shelf", "Kệ chén bát", "Dish shelf", 5),
        new Item("deco_bonsai", "Cây bonsai", "Bonsai", 5),
        new Item("deco_mural", "Tranh tường", "Wall mural", 5),
        new Item("deco_window", "Cửa sổ hoa anh đào", "Sakura window", 6),
        new Item("deco_torii", "Cổng torii trang trí", "Torii gate", 7)
    };

    [Serializable]
    public sealed class Item
    {
        public string id, nameVi, nameEn, iconKey;
        [Min(1)] public int starCost;
        public Item(string id, string vi, string en, int cost)
        { this.id = iconKey = id; nameVi = vi; nameEn = en; starCost = cost; }
    }

    public int StarsSpent(int built)
    {
        int total = 0;
        for (int i = 0; i < Mathf.Clamp(built, 0, items.Length); i++) total += items[i].starCost;
        return total;
    }
}
