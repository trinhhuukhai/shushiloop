using System;
using System.Collections.Generic;

public sealed class SushiDishOrder
{
    public int Index { get; }
    public SushiRecipe Recipe { get; }
    public IReadOnlyList<OrderItem> Ingredients => ingredients;
    public bool IsComplete { get; internal set; }
    // Reserved from assembly until the plated dish reaches the order panel.
    public bool IsPrepared { get; internal set; }
    public bool IsOnTable { get; internal set; }
    private readonly List<OrderItem> ingredients = new List<OrderItem>();

    // Tạo đơn món runtime từ công thức và gom lượng cần của từng loại nguyên liệu.
    public SushiDishOrder(int index, SushiRecipe recipe)
    {
        Index = index; Recipe = recipe;
        foreach (PoolType type in recipe.Ingredients)
        {
            OrderItem ingredient = ingredients.Find(item => item.typeFood == type);
            if (ingredient == null) ingredients.Add(ingredient = new OrderItem { typeFood = type });
            ingredient.requiredAmount++;
        }
    }

    public PoolType FoundationType
    {
        get
        {
            foreach (OrderItem ingredient in ingredients)
                if (ingredient.typeFood == PoolType.SushiRice) return PoolType.SushiRice;
            foreach (OrderItem ingredient in ingredients)
                if (ingredient.typeFood == PoolType.EmptyCup) return PoolType.EmptyCup;
            return PoolType.None;
        }
    }

    // Kiểm tra phần nền đã đủ để ghép nguyên liệu tiếp theo, tùy chọn tính cả phần đang bay tới.
    public bool CanStartIngredient(PoolType type, bool includeIncomingFoundation = false)
    {
        PoolType foundation = FoundationType;
        if (foundation == PoolType.None || type == foundation) return true;
        foreach (OrderItem ingredient in ingredients)
            if (ingredient.typeFood == foundation)
                return ingredient.receivedAmount + (includeIncomingFoundation ? ingredient.incomingAmount : 0) >= ingredient.requiredAmount;
        return false;
    }

    // Tìm mục nguyên liệu còn thiếu và chưa được giữ đủ lượt giao.
    public OrderItem FindAvailableIngredient(PoolType type)
    {
        if (IsComplete) return null;
        return ingredients.Find(item => item.typeFood == type && item.CanReceive);
    }

    // Kiểm tra tất cả nguyên liệu của món đã được nhận đủ.
    internal bool HasAllIngredients()
    {
        foreach (var ingredient in ingredients) if (!ingredient.IsComplete) return false;
        return ingredients.Count > 0;
    }

    // Kiểm tra hai món có thể trao đổi phần nền chung mà không làm sai tiến độ nguyên liệu.
    internal bool CanTransferFoundationTo(SushiDishOrder target)
    {
        PoolType foundation = FoundationType;
        if (target == null || target == this || foundation == PoolType.None || target.FoundationType != foundation ||
            !IsPrepared || IsComplete || IsOnTable || target.IsComplete || target.IsOnTable) return false;
        OrderItem sourceBase = ingredients.Find(item => item.typeFood == foundation);
        OrderItem targetBase = target.ingredients.Find(item => item.typeFood == foundation);
        if (sourceBase == null || targetBase == null || !sourceBase.IsComplete || sourceBase.incomingAmount != 0 ||
            sourceBase.requiredAmount != targetBase.requiredAmount) return false;
        int sourceProgress = 0, targetProgress = 0;
        foreach (OrderItem item in ingredients)
        {
            int progress = item.receivedAmount + item.incomingAmount;
            if (progress == 0) continue;
            OrderItem matching = target.ingredients.Find(other => other.typeFood == item.typeFood);
            if (!IsSharedBaseIngredient(item.typeFood) || !target.IsSharedBaseIngredient(item.typeFood) ||
                matching == null || matching.requiredAmount != item.requiredAmount) return false;
            sourceProgress += progress;
        }
        foreach (OrderItem item in target.ingredients)
        {
            int progress = item.receivedAmount + item.incomingAmount;
            if (progress == 0) continue;
            OrderItem matching = ingredients.Find(other => other.typeFood == item.typeFood);
            if (!IsSharedBaseIngredient(item.typeFood) || !target.IsSharedBaseIngredient(item.typeFood) ||
                matching == null || matching.requiredAmount != item.requiredAmount) return false;
            targetProgress += progress;
        }
        if (target.IsPrepared != (targetProgress > 0)) return false;
        return sourceProgress > targetProgress;
    }

    // Xác định nguyên liệu thuộc phần nền dùng chung, gồm cơm và rong biển ở nhóm món phù hợp.
    internal bool IsSharedBaseIngredient(PoolType type) => type == FoundationType ||
        (type == PoolType.Seaweed && FoundationType == PoolType.SushiRice &&
            (Recipe.Family == SushiDishFamily.Maki || Recipe.Family == SushiDishFamily.Gunkan) &&
            ingredients.Exists(item => item.typeFood == PoolType.Seaweed));

    // Lấy tiến độ nguyên liệu dự kiến sau khi dùng phần nền từ món nguồn.
    internal OrderItem ProjectedIngredient(PoolType type, SushiDishOrder foundationSource)
    {
        if (foundationSource != null && IsSharedBaseIngredient(type) && foundationSource.IsSharedBaseIngredient(type))
        {
            OrderItem shared = foundationSource.ingredients.Find(item => item.typeFood == type);
            if (shared != null) return shared;
        }
        return ingredients.Find(item => item.typeFood == type);
    }

    // Trao đổi bộ đếm nguyên liệu phần nền giữa hai món và đồng bộ trạng thái chuẩn bị.
    internal bool TryTransferFoundationTo(SushiDishOrder target)
    {
        if (!CanTransferFoundationTo(target)) return false;
        foreach (OrderItem item in ingredients)
        {
            if (!IsSharedBaseIngredient(item.typeFood) || !target.IsSharedBaseIngredient(item.typeFood)) continue;
            OrderItem matching = target.ingredients.Find(other => other.typeFood == item.typeFood);
            if (matching == null) continue;
            int received = item.receivedAmount, incoming = item.incomingAmount;
            item.receivedAmount = matching.receivedAmount;
            item.incomingAmount = matching.incomingAmount;
            matching.receivedAmount = received;
            matching.incomingAmount = incoming;
        }
        IsPrepared = ingredients.Exists(item => item.receivedAmount > 0 || item.incomingAmount > 0);
        target.IsPrepared = true;
        return true;
    }
}

internal static class SushiDishSelection
{
    // Available ingredients decide the recipe. The card's position only identifies its delivery target.
    // Chọn món nhận nguyên liệu với lượng thiếu ngoài nguồn hiện có ít nhất, rồi ưu tiên phần còn lại ít hơn.
    internal static SushiDishOrder Select(IReadOnlyList<SushiDishOrder> dishes, PoolType supplied,
        Predicate<SushiDishOrder> canReceive, Func<PoolType, int> available, int start,
        Func<SushiDishOrder, SushiDishOrder> foundationSource = null)
    {
        SushiDishOrder best = null;
        int bestMissing = int.MaxValue, bestRemaining = int.MaxValue;
        for (int offset = 0; offset < dishes.Count; offset++)
        {
            SushiDishOrder dish = dishes[(start + offset) % dishes.Count];
            if (!canReceive(dish)) continue;
            int missing = 0, remaining = 0;
            SushiDishOrder shared = foundationSource?.Invoke(dish);
            foreach (OrderItem item in dish.Ingredients)
            {
                OrderItem progress = dish.ProjectedIngredient(item.typeFood, shared);
                int suppliedAmount = item.typeFood == supplied ? 1 : 0;
                int left = Math.Max(0, item.requiredAmount - progress.receivedAmount - progress.incomingAmount - suppliedAmount);
                remaining += left;
                missing += Math.Max(0, left - Math.Max(0, available(item.typeFood) - suppliedAmount));
            }
            if (missing > bestMissing || (missing == bestMissing && remaining > bestRemaining)) continue;
            if (missing == bestMissing && remaining == bestRemaining && best != null &&
                (dish.Recipe.Key != best.Recipe.Key || dish.Index >= best.Index)) continue;
            best = dish;
            bestMissing = missing;
            bestRemaining = remaining;
        }
        return best;
    }
}
