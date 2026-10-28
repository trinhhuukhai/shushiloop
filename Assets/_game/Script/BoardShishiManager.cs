using System.Collections.Generic;
using UnityEngine;

public enum FoodPickMode { Tap, Chopsticks, Magnet }

public sealed class FoodPickup
{
    public readonly FoodShushi food;
    public readonly int amount;
    public FoodPickup(FoodShushi food, int amount) { this.food = food; this.amount = amount; }
}

public class BoardShishiManager : SingletonMonoBehaviour<BoardShishiManager>
{
    [SerializeField] private Transform tf;
    [SerializeField] private FoodQueue columnPrefab;
    [SerializeField] private FoodShushi ingredientPrefab;
    [SerializeField] private float columnWidth = 1.4f;
    [SerializeField] private float firstRowOffset = -1f;
    [SerializeField] private ConveyorManager conveyorManager;
    [Header("Băng chuyền cột sau khi lấy nguyên liệu")]
    [SerializeField, Min(0)] private float trayHoldDuration = 0.04f;
    private readonly List<FoodQueue> foodQueues = new List<FoodQueue>();
    private readonly Dictionary<string, LineRenderer> groupViews = new Dictionary<string, LineRenderer>();
    private readonly Dictionary<string, SpriteRenderer> groupArt = new Dictionary<string, SpriteRenderer>();
    private float linkFlashUntil;

    public IReadOnlyList<FoodQueue> FoodQueues => foodQueues;

    public void Init(List<FoodQueueData> boardData)
    {
        ClearBoard();
        EnsureIngredientPool();
        float startX = -((boardData.Count - 1) / 2f) * columnWidth;
        for (int i = 0; i < boardData.Count; i++)
        {
            FoodQueue column = Instantiate(columnPrefab, tf != null ? tf : transform);
            column.columnIndex = i;
            column.Board = this;
            column.transform.localPosition = new Vector3(startX + i * columnWidth, firstRowOffset, 0f);
            column.SetOuterRails(i == 0, i == boardData.Count - 1);
            column.gameObject.SetActive(true);
            foodQueues.Add(column);
            column.OnInit(boardData[i]);
            column.SetConveyor(conveyorManager);
        }
    }

    public bool IsEmpty()
    {
        foreach (FoodQueue column in foodQueues) if (column.IsPickupPending || !column.IsEmpty()) return false;
        return true;
    }

    private void EnsureIngredientPool()
    {
        if (ingredientPrefab == null)
            throw new System.InvalidOperationException("Assign the shared Ingredient prefab to BoardShishiManager.");
        // Tất cả type dùng cùng một prefab và cùng một pool; type chỉ quyết định art/dữ liệu.
        foreach (PoolType type in System.Enum.GetValues(typeof(PoolType)))
            if (type != PoolType.None) SimplePool.RegisterPrefab(type, ingredientPrefab);
    }

    public bool AllRemainingHeadsFrozen()
    {
        bool hasFood = false;
        foreach (FoodQueue column in foodQueues)
        {
            if (column.IsPickupPending) return false;
            FoodShushi top = column.GetTopFood();
            if (top == null) continue;
            hasFood = true;
            if (top.IsHidden || top.IsRevealing) return false;
            if (top.IceLayers == 0) return false;
        }
        return hasFood;
    }

    public bool HasPendingMovement
    {
        get
        {
            foreach (FoodQueue column in foodQueues)
                if (column.IsPickupPending || column.GetTopFood()?.IsRevealing == true) return true;
            return false;
        }
    }

    public bool HasAvailableTap(int freeSlots)
    {
        foreach (FoodQueue column in foodQueues)
            if (TryBuildTap(column, null, false, out int requiredSlots) && requiredSlots <= freeSlots)
                return true;
        return false;
    }

    public bool TryBuildTap(FoodQueue column, out List<FoodPickup> plan)
    {
        plan = new List<FoodPickup>();
        return TryBuildTap(column, plan, true, out _);
    }

    // A null plan checks the same pickup rules without allocations or click feedback.
    private bool TryBuildTap(FoodQueue column, List<FoodPickup> plan, bool showFeedback, out int requiredSlots)
    {
        requiredSlots = 0;
        if (column == null || column.Board != this || !column.isActiveAndEnabled || column.IsPickupPending) return false;
        FoodShushi top = column.GetTopFood();
        if (top == null) return false;
        if (top.IceLayers > 0 || top.IsHidden || top.IsRevealing)
        {
            if (showFeedback) column.Shake();
            return false;
        }
        if (top.LinkId > 0)
        {
            FoodShushi partner = FindLinkPartner(top);
            if (partner == null || partner.Owner.IsPickupPending || partner.Owner.GetTopFood() != partner || !partner.CanPick())
            {
                if (showFeedback)
                {
                    column.Shake();
                    partner?.Owner.Shake();
                    linkFlashUntil = Time.unscaledTime + 0.3f;
                }
                return false;
            }
            requiredSlots = top.Amount + partner.Amount;
            plan?.Add(new FoodPickup(top, top.Amount));
            plan?.Add(new FoodPickup(partner, partner.Amount));
        }
        else if (top.TrayId > 0)
        {
            foreach (FoodShushi food in column.Foods)
            {
                if (food.TrayId != top.TrayId) break;
                requiredSlots += food.Amount;
                plan?.Add(new FoodPickup(food, food.Amount));
            }
        }
        else
        {
            if (!top.CanPick()) return false;
            requiredSlots = top.Amount;
            plan?.Add(new FoodPickup(top, top.Amount));
        }
        // Empty tray compartments left by a booster can still be cleared with zero slots.
        return true;
    }

    public bool TryBuildChopsticks(FoodShushi food, out List<FoodPickup> plan)
    {
        plan = new List<FoodPickup>();
        if (food == null || food.Owner == null || food.Owner.IsPickupPending || food.Owner.Board != this || !food.CanUseChopsticks ||
            food.Owner.IndexOf(food) >= 4) return false;
        plan.Add(new FoodPickup(food, food.Amount));
        return true;
    }

    public List<FoodPickup> BuildMagnet(PoolType type, int limit)
    {
        var candidates = new List<FoodShushi>();
        foreach (FoodQueue column in foodQueues)
            foreach (FoodShushi food in column.Foods)
                if (!column.IsPickupPending && food.Type == type && food.Amount > 0 && !food.IsRevealing) candidates.Add(food);
        candidates.Sort((a, b) =>
        {
            int priority = MagnetPriority(a).CompareTo(MagnetPriority(b));
            if (priority != 0) return priority;
            int row = a.Owner.IndexOf(a).CompareTo(b.Owner.IndexOf(b));
            return row != 0 ? row : a.Owner.columnIndex.CompareTo(b.Owner.columnIndex);
        });
        var plan = new List<FoodPickup>();
        foreach (FoodShushi food in candidates)
        {
            if (limit <= 0) break;
            int count = Mathf.Min(food.Amount, limit);
            plan.Add(new FoodPickup(food, count));
            limit -= count;
        }
        return plan;
    }

    private static int MagnetPriority(FoodShushi food) => food.IceLayers > 0 ? 2 :
        food.LinkId > 0 || food.TrayId > 0 ? 1 : 0;

    private FoodShushi FindLinkPartner(FoodShushi food)
    {
        foreach (FoodQueue column in foodQueues)
            foreach (FoodShushi candidate in column.Foods)
                if (candidate != food && candidate.LinkId == food.LinkId) return candidate;
        return null;
    }

    // Chỉ commit sau khi băng chuyền đã giữ đủ slot và tạo đủ từng nguyên liệu.
    internal void CommitPickup(IReadOnlyList<FoodPickup> plan, FoodPickMode mode, float takeoffDelay)
    {
        var removedRows = new Dictionary<FoodQueue, int>();
        if (mode == FoodPickMode.Tap && plan[0].food.TrayId > 0)
        {
            string key = "tray:" + plan[0].food.TrayId;
            FoodQueue column = plan[0].food.Owner;
            if (groupArt.TryGetValue(key, out var artwork))
            {
                column.CopyTrayFrame(artwork.gameObject);
                artwork.gameObject.SetActive(false);
            }
            if (groupViews.TryGetValue(key, out var line))
            {
                column.CopyTrayFrame(line.gameObject);
                line.gameObject.SetActive(false);
            }
        }
        var changedColumns = new HashSet<FoodQueue>();
        foreach (FoodPickup pickup in plan)
        {
            FoodShushi food = pickup.food;
            FoodQueue column = food.Owner;
            changedColumns.Add(column);
            if (pickup.amount != food.Amount || (mode != FoodPickMode.Tap && food.TrayId > 0)) continue;
            removedRows.TryGetValue(column, out int count);
            removedRows[column] = count + 1;
        }
        var iceDamage = new Dictionary<FoodShushi, int>();
        if (mode == FoodPickMode.Tap)
        {
            foreach (FoodQueue source in changedColumns)
            {
                foreach (FoodQueue neighbor in foodQueues)
                {
                    if (Mathf.Abs(neighbor.columnIndex - source.columnIndex) != 1) continue;
                    // Mỗi hàng có ô ở cột nguồn chuyển động chỉ gây một lần sát thương.
                    for (int row = 0; row < Mathf.Min(4, source.Count); row++)
                    {
                        FoodShushi ice = neighbor.FoodAtVisibleRow(row);
                        if (ice == null || ice.IceLayers == 0) continue;
                        iceDamage.TryGetValue(ice, out int damage);
                        iceDamage[ice] = damage + 1;
                    }
                }
            }
        }
        foreach (FoodPickup pickup in plan)
        {
            FoodShushi food = pickup.food;
            if (mode != FoodPickMode.Tap && food.LinkId > 0 && pickup.amount == food.Amount)
            {
                FindLinkPartner(food)?.BreakLink();
                food.BreakLink();
            }
            food.Owner.Consume(food, pickup.amount, mode != FoodPickMode.Tap);
        }
        foreach (var entry in iceDamage) entry.Key.BreakIce(entry.Value);
        foreach (FoodQueue column in changedColumns)
            if (removedRows.TryGetValue(column, out int count))
                column.AdvanceAfterPickup(count, takeoffDelay + trayHoldDuration);
            else column.RefreshRows();
    }

    public void ShakePickup(IReadOnlyList<FoodPickup> plan)
    {
        var shaken = new HashSet<FoodQueue>();
        foreach (FoodPickup pickup in plan)
            if (pickup.food.Owner != null && shaken.Add(pickup.food.Owner)) pickup.food.Owner.Shake();
        linkFlashUntil = Time.unscaledTime + 0.3f;
    }

    public bool SwapRow()
    {
        foreach (FoodQueue column in foodQueues) column.CompleteColumnMovement();
        var rows = new List<List<FoodShushi>>();
        foreach (FoodQueue column in foodQueues) rows.Add(column.BuildSwapRows());
        if (!CanCollectAllLinks(rows))
        {
            // A head link can wrap past another pair in only one of its columns.
            // Keep the original link sequence in the proposed link positions; ordinary
            // tiles and whole trays retain their rotation, including the row-4 boundary.
            for (int c = 0; c < foodQueues.Count; c++)
            {
                var links = new List<FoodShushi>();
                foreach (FoodShushi food in foodQueues[c].Foods)
                    if (food.LinkId > 0) links.Add(food);
                int next = 0;
                for (int r = 0; r < rows[c].Count; r++)
                    if (rows[c][r].LinkId > 0) rows[c][r] = links[next++];
            }
            // No partial commit or booster charge if the input board is already invalid.
            if (!CanCollectAllLinks(rows)) return false;
        }
        bool changed = false;
        for (int c = 0; c < foodQueues.Count; c++) changed |= foodQueues[c].ApplySwapRows(rows[c]);
        return changed;
    }

    private static bool CanCollectAllLinks(List<List<FoodShushi>> rows)
    {
        // Ignore ice/hidden state here; only verify that link pairs cannot block each other.
        // Inspect the full columns so a pair below the visible window is not overlooked.
        var heads = new int[rows.Count];
        bool moved;
        do
        {
            moved = false;
            for (int c = 0; c < rows.Count; c++)
            {
                if (heads[c] >= rows[c].Count) continue;
                int link = rows[c][heads[c]].LinkId;
                if (link == 0) { heads[c]++; moved = true; continue; }
                for (int n = 0; n < rows.Count; n++)
                {
                    if (n == c || heads[n] >= rows[n].Count || rows[n][heads[n]].LinkId != link) continue;
                    heads[c]++; heads[n]++; moved = true;
                    break;
                }
            }
        } while (moved);
        for (int c = 0; c < rows.Count; c++)
            if (heads[c] < rows[c].Count) return false;
        return true;
    }

    public void BreakHeadIce()
    {
        foreach (FoodQueue column in foodQueues) column.GetTopFood()?.BreakIce(5);
    }

    private void LateUpdate()
    {
        var groups = new Dictionary<string, List<FoodShushi>>();
        foreach (FoodQueue column in foodQueues)
        {
            foreach (FoodShushi food in column.Foods)
            {
                string key = food.LinkId > 0 ? "link:" + food.LinkId : food.TrayId > 0 ? "tray:" + food.TrayId : null;
                if (key == null) continue;
                if (!groups.TryGetValue(key, out var members)) groups[key] = members = new List<FoodShushi>();
                members.Add(food);
            }
        }
        foreach (var entry in groupViews) entry.Value.gameObject.SetActive(groups.ContainsKey(entry.Key));
        foreach (var entry in groupArt) entry.Value.gameObject.SetActive(groups.ContainsKey(entry.Key));
        foreach (var group in groups)
        {
            if (!groupViews.TryGetValue(group.Key, out LineRenderer line))
            {
                line = new GameObject(group.Key).AddComponent<LineRenderer>();
                line.transform.SetParent(transform, false);
                line.material = SushiWorldVisual.LineMaterial;
                line.useWorldSpace = true;
                line.sortingOrder = 3;
                line.startWidth = line.endWidth = 0.06f;
                groupViews[group.Key] = line;
            }
            bool isLink = group.Key.StartsWith("link:");
            var members = group.Value;
            bool visible = false;
            foreach (var food in members) visible |= food.Owner.VisibleRowOf(food) < 4;
            line.gameObject.SetActive(visible);
            Color color = isLink && Time.unscaledTime < linkFlashUntil ? Color.yellow : new Color(0.65f, 0.35f, 0.12f);
            line.startColor = line.endColor = color;
            Vector3 first = VisiblePosition(members[0]);
            Vector3 last = VisiblePosition(members[members.Count - 1]);
            if (isLink)
            {
                line.loop = false;
                line.positionCount = 2;
                line.SetPosition(0, first);
                line.SetPosition(1, last);
            }
            else
            {
                line.loop = true;
                line.positionCount = 4;
                line.SetPosition(0, first + new Vector3(-0.55f, 0.45f, 0));
                line.SetPosition(1, first + new Vector3(0.55f, 0.45f, 0));
                line.SetPosition(2, last + new Vector3(0.55f, -0.45f, 0));
                line.SetPosition(3, last + new Vector3(-0.55f, -0.45f, 0));
            }
            SushiArtCatalog catalog = SushiArtCatalog.Default;
            Sprite sprite = catalog != null ? (isLink ? catalog.linkedTile : catalog.rigidTray) : null;
            if (sprite == null) continue;
            if (!groupArt.TryGetValue(group.Key, out SpriteRenderer artwork))
            {
                artwork = new GameObject(group.Key + " art").AddComponent<SpriteRenderer>();
                artwork.transform.SetParent(transform, false);
                artwork.sharedMaterial = SushiWorldVisual.SpriteMaterial;
                groupArt[group.Key] = artwork;
            }
            artwork.gameObject.SetActive(visible);
            artwork.sprite = sprite;
            artwork.color = isLink && Time.unscaledTime < linkFlashUntil ? Color.yellow : Color.white;
            artwork.transform.position = (first + last) / 2;
            if (isLink)
            {
                Vector3 direction = last - first;
                artwork.sortingOrder = 4;
                artwork.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                artwork.transform.localScale = new Vector3(Mathf.Max(0.4f, direction.magnitude - 0.9f) / sprite.bounds.size.x, 0.35f / sprite.bounds.size.y, 1);
            }
            else
            {
                artwork.sortingOrder = -1;
                artwork.transform.rotation = Quaternion.identity;
                artwork.transform.localScale = new Vector3(1.3f / sprite.bounds.size.x, (Vector3.Distance(first, last) + 1.18f) / sprite.bounds.size.y, 1);
            }
            line.gameObject.SetActive(false);
        }
    }

    private static Vector3 VisiblePosition(FoodShushi food)
    {
        int row = Mathf.Min(3, food.Owner.VisibleRowOf(food));
        FoodShushi visible = food.Owner.FoodAtVisibleRow(row);
        return (visible != null ? visible.transform.position : food.transform.position) + Vector3.back * 0.1f;
    }

    public void ClearBoard()
    {
        foreach (var view in groupViews.Values) if (view != null) Destroy(view.gameObject);
        foreach (var view in groupArt.Values) if (view != null) Destroy(view.gameObject);
        groupViews.Clear();
        groupArt.Clear();
        foreach (FoodQueue column in foodQueues)
        {
            if (column == null) continue;
            column.ClearFoods();
            column.gameObject.SetActive(false);
            Destroy(column.gameObject);
        }
        foodQueues.Clear();
    }
}
