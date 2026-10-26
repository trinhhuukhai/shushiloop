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

    public OrderItem FindAvailableIngredient(PoolType type)
    {
        if (IsComplete) return null;
        return ingredients.Find(item => item.typeFood == type && item.CanReceive);
    }

    internal bool HasAllIngredients()
    {
        foreach (var ingredient in ingredients) if (!ingredient.IsComplete) return false;
        return ingredients.Count > 0;
    }
}
