using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CustomerRecipeView : MonoBehaviour
{
    private Customer customer;
    private SushiDishArtLibrary art;
    private readonly List<DishCard> cards = new List<DishCard>();
    private bool magnetTargeting;

    private sealed class DishCard
    {
        public GameObject root;
        public SpriteRenderer background;
        public SpriteRenderer finished;
        public LineRenderer check;
        public readonly List<SpriteRenderer> ingredients = new List<SpriteRenderer>();
        public readonly List<TextMeshPro> progress = new List<TextMeshPro>();
    }

    private static int Rows(int count) => Mathf.Max(1, (count + 1) / 2);
    private static float CardHeight(int count) => Mathf.Min(1.38f, 2.12f / Rows(count) - 0.08f);
    private static float CardWidth(int count) => count == 1 ? 3.16f : 1.53f;
    public static float IngredientSize(int count) => Mathf.Min(count == 1 ? 0.42f : 0.34f, CardHeight(count) * 0.30f);

    public static Vector3 CardPosition(int index, int count)
    {
        float x = count == 1 ? 0 : (index % 2 - 0.5f) * 1.63f;
        float height = CardHeight(count);
        return new Vector3(x, 0.11f - height / 2 - (index / 2) * (height + 0.08f), -0.2f);
    }

    // Cùng vị trí cho icon yêu cầu và điểm đáp của nguyên liệu thật.
    public static Vector3 IngredientPosition(int dishIndex, int dishCount, int ingredientIndex, int ingredientCount)
    {
        float spacing = dishCount == 1 ? 0.78f : 0.44f;
        return CardPosition(dishIndex, dishCount) + new Vector3(
            (ingredientIndex - (ingredientCount - 1) * 0.5f) * spacing,
            -0.30f * CardHeight(dishCount) / 1.38f, -0.3f);
    }

    public static Rect OrderBounds(int count)
    {
        float bottom = 0.11f - Rows(count) * (CardHeight(count) + 0.08f) + 0.08f;
        return Rect.MinMaxRect(-1.62f, bottom - 0.04f, 1.62f, 0.15f);
    }

    public void Setup(Customer owner, SushiDishArtLibrary library)
    {
        if (customer != null) customer.OnOrderChanged -= Refresh;
        customer = owner; art = library != null ? library : SushiArtCatalog.Default != null ? SushiArtCatalog.Default.dishImages : null;
        foreach (var card in cards) if (card.root != null) Destroy(card.root);
        cards.Clear();
        int count = customer.Dishes.Count;
        foreach (var dish in customer.Dishes)
        {
            var card = new DishCard { root = new GameObject(dish.Recipe.Name) };
            Transform root = card.root.transform;
            root.SetParent(transform, false);
            root.localPosition = CardPosition(dish.Index, count);
            float height = CardHeight(count), width = CardWidth(count), vertical = height / 1.38f;
            card.background = SushiWorldVisual.Panel(root, "Order card", 5);
            card.background.transform.localScale = new Vector3(width, height, 1);
            var name = SushiWorldVisual.Label(root, "Dish", new Vector3(0, 0.54f * vertical, -0.1f), 1.8f, 20);
            name.rectTransform.sizeDelta = new Vector2(width - 0.16f, 0.26f * vertical);
            name.enableAutoSizing = true;
            name.fontSizeMin = 0.8f * vertical;
            name.fontSizeMax = (count == 1 ? 2.2f : 1.8f) * vertical;
            name.text = dish.Recipe.Name;
            for (int i = 0; i < dish.Ingredients.Count; i++)
            {
                var ingredient = dish.Ingredients[i];
                SpriteRenderer icon = MakeIcon(root, ingredient.typeFood.ToString(),
                    owner.GetIngredientSprite(ingredient.typeFood), IngredientSize(count));
                icon.transform.localPosition = IngredientPosition(dish.Index, count, i, dish.Ingredients.Count) - root.localPosition;
                card.ingredients.Add(icon);
                var text = SushiWorldVisual.Label(root, "Progress", icon.transform.localPosition +
                    new Vector3(0, -0.23f * vertical, -0.1f), 1.6f * vertical, 21);
                text.rectTransform.sizeDelta = new Vector2(count == 1 ? 0.72f : 0.43f, 0.22f * vertical);
                text.enableAutoSizing = true;
                text.fontSizeMin = 1.0f * vertical;
                text.fontSizeMax = 1.6f * vertical;
                card.progress.Add(text);
            }
            card.finished = MakeIcon(root, "Order dish", art != null ? art.GetSprite(dish.Recipe.Id) : null, height * 0.48f);
            card.finished.transform.localPosition = new Vector3(0, 0.13f * vertical, -0.3f);
            card.check = new GameObject("Completed check").AddComponent<LineRenderer>();
            card.check.transform.SetParent(root, false);
            card.check.material = SushiWorldVisual.LineMaterial;
            card.check.useWorldSpace = false;
            card.check.startWidth = card.check.endWidth = 0.035f;
            card.check.startColor = card.check.endColor = new Color(0.1f, 0.55f, 0.22f);
            card.check.sortingOrder = 22;
            card.check.positionCount = 3;
            float right = width / 2 - 0.1f;
            card.check.SetPositions(new[] { new Vector3(right - 0.15f, 0.3f * vertical, -0.4f),
                new Vector3(right - 0.1f, 0.25f * vertical, -0.4f), new Vector3(right, 0.38f * vertical, -0.4f) });
            cards.Add(card);
        }
        customer.OnOrderChanged += Refresh;
        Refresh(customer);
    }

    private static SpriteRenderer MakeIcon(Transform parent, string name, Sprite sprite, float size)
    {
        var icon = new GameObject(name).AddComponent<SpriteRenderer>();
        icon.transform.SetParent(parent, false);
        icon.sharedMaterial = SushiWorldVisual.SpriteMaterial;
        icon.sortingOrder = 11;
        SetIcon(icon, sprite, size);
        return icon;
    }

    private static void SetIcon(SpriteRenderer icon, Sprite sprite, float size)
    {
        icon.sprite = sprite;
        if (sprite != null)
            icon.transform.localScale = Vector3.one * size / Mathf.Max(0.01f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
    }

    public void SetSeated(bool seated)
    {
        foreach (DishCard card in cards) card.root.SetActive(seated);
    }

    public SushiDishOrder DishAtWorldPosition(Vector3 position)
    {
        if (customer == null || !customer.IsSeated || customer.IsCompleted || customer.RushExpired) return null;
        Vector3 local = transform.InverseTransformPoint(position);
        int count = customer.Dishes.Count;
        for (int i = 0; i < cards.Count; i++)
        {
            Vector3 center = cards[i].root.transform.localPosition;
            if (cards[i].root.activeSelf && !customer.Dishes[i].IsComplete &&
                Mathf.Abs(local.x - center.x) <= CardWidth(count) * 0.5f &&
                Mathf.Abs(local.y - center.y) <= CardHeight(count) * 0.5f) return customer.Dishes[i];
        }
        return null;
    }

    public void SetMagnetTargeting(bool active)
    {
        if (magnetTargeting == active) return;
        magnetTargeting = active;
        if (customer != null) Refresh(customer);
    }

    public void RefreshArtwork()
    {
        if (customer == null) return;
        if (art == null && SushiArtCatalog.Default != null) art = SushiArtCatalog.Default.dishImages;
        for (int i = 0; i < cards.Count && i < customer.Dishes.Count; i++)
        {
            SushiDishOrder dish = customer.Dishes[i];
            SetIcon(cards[i].finished, art != null ? art.GetSprite(dish.Recipe.Id) : null, CardHeight(customer.Dishes.Count) * 0.48f);
            foreach (SpriteRenderer icon in cards[i].ingredients)
                if (System.Enum.TryParse(icon.name, out PoolType type))
                    SetIcon(icon, customer.GetIngredientSprite(type), IngredientSize(customer.Dishes.Count));
        }
        Refresh(customer);
    }

    private void Refresh(Customer owner)
    {
        SetSeated(owner.IsSeated);
        for (int i = 0; i < owner.Dishes.Count && i < cards.Count; i++)
        {
            SushiDishOrder dish = owner.Dishes[i];
            DishCard card = cards[i];
            card.background.color = dish.IsComplete ? new Color(0.79f, 0.94f, 0.75f) : new Color(1, 0.92f, 0.76f);
            if (magnetTargeting && !dish.IsComplete) card.background.color = new Color(1f, 0.76f, 0.3f);
            card.check.enabled = dish.IsComplete;
            card.finished.enabled = card.finished.sprite != null;
            for (int j = 0; j < dish.Ingredients.Count; j++)
            {
                OrderItem ingredient = dish.Ingredients[j];
                card.ingredients[j].enabled = true;
                card.ingredients[j].color = new Color(1, 1, 1, ingredient.IsComplete ? 1f : ingredient.incomingAmount > 0 ? 0.85f : 0.65f);
                card.progress[j].gameObject.SetActive(true);
                card.progress[j].text = ingredient.receivedAmount + "/" + ingredient.requiredAmount +
                    (ingredient.incomingAmount > 0 ? " +" + ingredient.incomingAmount : "");
                card.progress[j].color = ingredient.IsComplete ? new Color(0.1f, 0.55f, 0.22f) :
                    ingredient.incomingAmount > 0 ? new Color(0.68f, 0.36f, 0.05f) : new Color(0.3f, 0.24f, 0.18f);
            }
        }
    }

    private void OnDestroy() { if (customer != null) customer.OnOrderChanged -= Refresh; }
}
