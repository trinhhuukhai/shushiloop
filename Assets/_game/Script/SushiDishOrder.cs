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

    // Maki/gunkan: rong biển lên đĩa trước, rồi bát cơm (ghép thành cơm cuộn rong biển), cuối cùng là topping.
    public bool IsSeaweedWrapped => FoundationType == PoolType.SushiRice &&
        (Recipe.Family == SushiDishFamily.Maki || Recipe.Family == SushiDishFamily.Gunkan) &&
        ingredients.Exists(item => item.typeFood == PoolType.Seaweed);

    // Nguyên liệu lên đĩa đầu tiên: rong biển với maki/gunkan, nếu không thì phần nền (cơm, cốc).
    public PoolType LeadType => IsSeaweedWrapped ? PoolType.Seaweed : FoundationType;

    // Bước ghép của nguyên liệu: nguyên liệu bước sau chỉ lên đĩa khi các bước trước đã đủ.
    public int AssemblyStage(PoolType type)
    {
        // Trứng cá phủ trên lát chính (vd. cơm -> cá -> trứng cá) phải đợi lát chính lên đĩa xong.
        int roeStep = IsRoeOverTopping && SushiRecipeCatalog.IsRoe(type) ? 1 : 0;
        if (IsSeaweedWrapped)
            return type == PoolType.Seaweed ? 0 : type == PoolType.SushiRice ? 1 : 2 + roeStep;
        PoolType foundation = FoundationType;
        return foundation == PoolType.None || type == foundation ? 0 : 1 + roeStep;
    }

    // Món nền cơm có cả trứng cá lẫn một lát chính khác (cá, tôm, bơ...): trứng cá là lớp trên cùng.
    public bool IsRoeOverTopping => FoundationType == PoolType.SushiRice &&
        ingredients.Exists(item => SushiRecipeCatalog.IsRoe(item.typeFood)) &&
        ingredients.Exists(item => !SushiRecipeCatalog.IsRoe(item.typeFood) && !IsSharedBaseIngredient(item.typeFood) &&
            !SushiRecipeCatalog.IsGarnish(item.typeFood));

    // Kiểm tra các bước ghép trước đã đủ để ghép nguyên liệu này, tùy chọn tính cả phần đang bay tới.
    public bool CanStartIngredient(PoolType type, bool includeIncomingFoundation = false)
    {
        int stage = AssemblyStage(type);
        foreach (OrderItem ingredient in ingredients)
        {
            if (AssemblyStage(ingredient.typeFood) >= stage) continue;
            int progress = ingredient.receivedAmount + (includeIncomingFoundation ? ingredient.incomingAmount : 0);
            if (progress < ingredient.requiredAmount) return false;
        }
        return true;
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
        // Phần nền dùng chung bắt đầu từ nguyên liệu lên đĩa đầu tiên (rong biển của maki/gunkan, cơm của nigiri).
        PoolType lead = LeadType;
        if (target == null || target == this || lead == PoolType.None || target.LeadType != lead ||
            !IsPrepared || IsComplete || IsOnTable || target.IsComplete || target.IsOnTable) return false;
        OrderItem sourceBase = ingredients.Find(item => item.typeFood == lead);
        OrderItem targetBase = target.ingredients.Find(item => item.typeFood == lead);
        if (sourceBase == null || targetBase == null || !sourceBase.IsComplete || sourceBase.incomingAmount != 0 ||
            sourceBase.requiredAmount != targetBase.requiredAmount) return false;
        // Đĩa đã ghép thành kiểu món (vd. rong biển + cơm đã thành maki) thì không đổi sang kiểu khác (gunkan).
        if (target.Recipe.Family != Recipe.Family && (HasLandedBeyondLead() || target.HasLandedBeyondLead())) return false;
        // Mọi thứ đang có trên đĩa (kể cả wasabi, chanh, topping) phải dùng được cho món kia, và ngược lại khi đổi chỗ.
        if (!target.CanHold(this) || !CanHold(target)) return false;
        int sourceProgress = Progress(), targetProgress = target.Progress();
        if (target.IsPrepared != (targetProgress > 0)) return false;
        return sourceProgress > targetProgress;
    }

    private int Progress()
    {
        int progress = 0;
        foreach (OrderItem item in ingredients) progress += item.receivedAmount + item.incomingAmount;
        return progress;
    }

    // Đã có nguyên liệu ngoài nguyên liệu đầu tiên đáp lên đĩa: đĩa đã ghép thành kiểu món của công thức.
    private bool HasLandedBeyondLead()
    {
        PoolType lead = LeadType;
        return ingredients.Exists(item => item.typeFood != lead && item.receivedAmount > 0);
    }

    // Phần đang có (đã đáp + đang bay tới) của món content đặt được vào món này: đúng nguyên liệu, không thừa,
    // phần nền cùng số lượng và không phá thứ tự ghép của món này.
    private bool CanHold(SushiDishOrder content)
    {
        foreach (OrderItem item in content.ingredients)
        {
            int progress = item.receivedAmount + item.incomingAmount;
            if (progress == 0) continue;
            OrderItem mine = ingredients.Find(other => other.typeFood == item.typeFood);
            if (mine == null || progress > mine.requiredAmount) return false;
            bool sharedBase = IsSharedBaseIngredient(item.typeFood);
            if (sharedBase != content.IsSharedBaseIngredient(item.typeFood) ||
                (sharedBase && mine.requiredAmount != item.requiredAmount)) return false;
            if (!CanStartWith(item.typeFood, content)) return false;
        }
        return true;
    }

    // Sau khi nhận phần đang có của món content, các bước ghép trước nguyên liệu này đã đủ hay chưa (tính cả phần đang bay).
    internal bool CanStartWith(PoolType type, SushiDishOrder content)
    {
        int stage = AssemblyStage(type);
        foreach (OrderItem earlier in ingredients)
        {
            if (AssemblyStage(earlier.typeFood) >= stage) continue;
            OrderItem have = content.ingredients.Find(other => other.typeFood == earlier.typeFood);
            if (have == null || have.receivedAmount + have.incomingAmount < earlier.requiredAmount) return false;
        }
        return true;
    }

    // Xác định nguyên liệu thuộc phần nền dùng chung, gồm cơm và rong biển ở nhóm món phù hợp.
    internal bool IsSharedBaseIngredient(PoolType type) => type == FoundationType ||
        (type == PoolType.Seaweed && IsSeaweedWrapped);

    // Lấy tiến độ nguyên liệu dự kiến sau khi dùng phần nền từ món nguồn.
    internal OrderItem ProjectedIngredient(PoolType type, SushiDishOrder foundationSource)
    {
        if (foundationSource != null)
        {
            OrderItem shared = foundationSource.ingredients.Find(item => item.typeFood == type);
            if (shared != null) return shared;
        }
        return ingredients.Find(item => item.typeFood == type);
    }

    // Trao đổi bộ đếm của mọi nguyên liệu hai món cùng có (phần nền lẫn topping đã lên đĩa) và đồng bộ trạng thái chuẩn bị.
    internal bool TryTransferFoundationTo(SushiDishOrder target)
    {
        if (!CanTransferFoundationTo(target)) return false;
        foreach (OrderItem item in ingredients)
        {
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
            Score(dish, supplied, available, foundationSource?.Invoke(dish), out int missing, out int remaining);
            if (missing > bestMissing || (missing == bestMissing && remaining > bestRemaining)) continue;
            if (missing == bestMissing && remaining == bestRemaining && best != null &&
                (dish.Recipe.Key != best.Recipe.Key || dish.Index >= best.Index)) continue;
            best = dish;
            bestMissing = missing;
            bestRemaining = remaining;
        }
        return best;
    }

    // Chấm điểm một món khi nhận thêm nguyên liệu supplied:
    // missing = phần còn thiếu mà băng chuyền/túi chưa có; remaining = tổng phần còn thiếu.
    internal static void Score(SushiDishOrder dish, PoolType supplied, Func<PoolType, int> available,
        SushiDishOrder shared, out int missing, out int remaining)
    {
        missing = 0;
        remaining = 0;
        foreach (OrderItem item in dish.Ingredients)
        {
            OrderItem progress = dish.ProjectedIngredient(item.typeFood, shared);
            int suppliedAmount = item.typeFood == supplied ? 1 : 0;
            int left = Math.Max(0, item.requiredAmount - progress.receivedAmount - progress.incomingAmount - suppliedAmount);
            remaining += left;
            missing += Math.Max(0, left - Math.Max(0, available(item.typeFood) - suppliedAmount));
        }
    }
}
