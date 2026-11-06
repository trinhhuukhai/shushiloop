using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Sushi/Restaurant List")]
public sealed class SushiRestaurantCatalog : ScriptableObject
{
    public const string ResourcePath = "Decoration/Restaurants";
    public const string FirstRestaurantId = "kaiten_counter";
    public SushiDecorationCatalog decorations;
    public Restaurant[] restaurants =
    {
        new Restaurant { id = FirstRestaurantId, nameVi = "Quán sushi", sign = "SUSHI",
            accent = new Color32(201, 87, 66, 255), facade = new Color32(240, 223, 192, 255) },
        new Restaurant { id = "sakura_garden", nameVi = "Vườn anh đào", sign = "SAKURA", unlockAfterLevel = 30,
            accent = new Color32(157, 86, 125, 255), facade = new Color32(241, 219, 225, 255) }
    };
    private static SushiRestaurantCatalog cached;
    public static SushiRestaurantCatalog Current => cached != null ? cached :
        cached = Resources.Load<SushiRestaurantCatalog>(ResourcePath);

    [Serializable]
    public sealed class Restaurant
    {
        public string id, nameVi, sign;
        [Min(0), Tooltip("Global level that must be passed; 30 unlocks after winning level 30.")]
        public int unlockAfterLevel;
        public Color accent = Color.red, facade = Color.white;
    }

    public Restaurant Find(string id)
    {
        foreach (var restaurant in restaurants) if (restaurant.id == id) return restaurant;
        return null;
    }

    public static bool IsUnlocked(GameDataFragment.DataPlayer player, Restaurant restaurant)
    {
        if (restaurant == null) return false;
        if (restaurant.unlockAfterLevel <= 0) return true;
        if (player == null) return false;
        // cur_level preserves unlocks for older saves that lack individual win records.
        int passed = Mathf.Max(0, player.cur_level);
        if (player.completedLevels != null)
            foreach (int index in player.completedLevels)
                if (index >= 0) passed = Mathf.Max(passed, index + 1);
        return passed >= restaurant.unlockAfterLevel;
    }
}
