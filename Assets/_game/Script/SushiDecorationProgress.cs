using System.Collections.Generic;
using UnityEngine;

// Stars come from first-time level wins. Spending them never changes campaign progress.
public static class SushiDecorationProgress
{
    private static SushiDecorationCatalog cachedCatalog;
    public static SushiDecorationCatalog Catalog => cachedCatalog != null ? cachedCatalog :
        cachedCatalog = Resources.Load<SushiDecorationCatalog>(SushiDecorationCatalog.ResourcePath);

    private static GameDataFragment.RestaurantProgress Find(GameDataFragment.DataPlayer player, string id) =>
        player?.restaurants?.Find(progress => progress.id == id);

    public static int Built(GameDataFragment.DataPlayer player, SushiDecorationCatalog catalog,
        string restaurantId = SushiRestaurantCatalog.FirstRestaurantId) => player == null || catalog == null ? 0 :
        Mathf.Clamp(restaurantId == SushiRestaurantCatalog.FirstRestaurantId ? player.restaurantDecorationCount :
            Find(player, restaurantId)?.built ?? 0, 0, catalog.items.Length);

    public static int AvailableStars(GameDataFragment.DataPlayer player, SushiDecorationCatalog catalog)
    {
        if (player == null || catalog == null) return 0;
        var wins = new HashSet<int>();
        if (player.completedLevels != null)
            foreach (int level in player.completedLevels) if (level >= 0) wins.Add(level);
        int spent = catalog.StarsSpent(Built(player, catalog));
        if (player.restaurants != null)
            foreach (var progress in player.restaurants)
                if (progress.id != SushiRestaurantCatalog.FirstRestaurantId) spent += Mathf.Max(0, progress.starsSpent);
        return Mathf.Max(0, wins.Count - spent);
    }

    // expectedId rejects stale/repeated taps even when the player can afford the next item too.
    public static bool TryBuild(GameDataFragment.DataPlayer player, SushiDecorationCatalog catalog,
        string expectedId, out int reward) =>
        Build(player, catalog, SushiRestaurantCatalog.FirstRestaurantId, expectedId, out reward);

    public static bool TryBuild(GameDataFragment.DataPlayer player, SushiRestaurantCatalog restaurants,
        string restaurantId, string expectedId, out int reward)
    {
        reward = 0;
        if (restaurants == null || !SushiRestaurantCatalog.IsUnlocked(player, restaurants.Find(restaurantId))) return false;
        return Build(player, restaurants.decorations, restaurantId, expectedId, out reward);
    }

    private static bool Build(GameDataFragment.DataPlayer player, SushiDecorationCatalog catalog,
        string restaurantId, string expectedId, out int reward)
    {
        reward = 0;
        if (player == null || catalog == null) return false;
        int built = Built(player, catalog, restaurantId);
        if (built >= catalog.items.Length) return false;
        var next = catalog.items[built];
        if (next.id != expectedId || AvailableStars(player, catalog) < next.starCost) return false;
        bool first = restaurantId == SushiRestaurantCatalog.FirstRestaurantId;
        var progress = Find(player, restaurantId);
        if (first) player.restaurantDecorationCount = built + 1;
        else
        {
            if (progress == null)
            {
                progress = new GameDataFragment.RestaurantProgress { id = restaurantId };
                if (player.restaurants == null) player.restaurants = new List<GameDataFragment.RestaurantProgress>();
                player.restaurants.Add(progress);
            }
            progress.built = built + 1;
            progress.starsSpent += next.starCost;
        }
        if (built + 1 == catalog.items.Length && !(first ? player.restaurantCompletionRewardClaimed : progress.rewardClaimed))
        {
            reward = Mathf.Max(0, catalog.completionCoins);
            player.money += reward;
            if (first) player.restaurantCompletionRewardClaimed = true;
            else progress.rewardClaimed = true;
        }
        return true;
    }
}
