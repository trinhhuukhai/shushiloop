using UnityEngine;

// Only world-space scenery. Buttons, progress and popups belong to SushiGameplayHud.
public sealed class SushiRestaurantView : MonoBehaviour
{
    private void Awake()
    {
        Shape("Restaurant wall", new Vector2(0, 4.7f), new Vector2(10, 7), new Color(0.30f, 0.17f, 0.12f), -50);
        for (int i = -4; i <= 4; i++)
        {
            Shape("Wall panel", new Vector2(i * 1.12f, 5.3f), new Vector2(1.04f, 4.6f),
                new Color(0.37f + (i % 2 == 0 ? 0.025f : 0), 0.22f, 0.15f), -49);
            Shape("Panel inset", new Vector2(i * 1.12f, 5.25f), new Vector2(0.84f, 3.7f),
                new Color(0.32f, 0.185f, 0.125f), -48);
        }
        Shape("Upper rail", new Vector2(0, 6.65f), new Vector2(9.4f, 0.11f), new Color(0.68f, 0.43f, 0.24f), -47);
        Shape("Service counter shadow", new Vector2(0, 2.20f), new Vector2(9.4f, 0.36f), new Color(0.16f, 0.08f, 0.055f), -30);
        Shape("Service counter", new Vector2(0, 2.44f), new Vector2(9.4f, 0.42f), new Color(0.74f, 0.42f, 0.19f), -29);
        Shape("Counter lip", new Vector2(0, 2.60f), new Vector2(9.4f, 0.07f), new Color(0.98f, 0.71f, 0.36f), -28);
        Shape("Kitchen worktop", new Vector2(0, -2.6f), new Vector2(9.4f, 9.5f), new Color(0.67f, 0.34f, 0.16f), -45);
        for (int i = 0; i < 10; i++)
            Shape("Worktop join", new Vector2(0, 1.7f - i), new Vector2(9.4f, 0.025f), new Color(0.59f, 0.28f, 0.13f), -44);
        Shape("Board cabinet shadow", new Vector2(0, -4.68f), new Vector2(8.15f, 5.65f), new Color(0.25f, 0.125f, 0.075f), -20);
        Shape("Board cabinet rim", new Vector2(0, -4.55f), new Vector2(8.12f, 5.55f), new Color(0.94f, 0.65f, 0.32f), -19);
        Shape("Board cabinet", new Vector2(0, -4.55f), new Vector2(7.98f, 5.4f), new Color(0.37f, 0.205f, 0.12f), -18);
        Shape("Booster shelf", new Vector2(0, -7.52f), new Vector2(9.4f, 1.0f), new Color(0.22f, 0.12f, 0.085f), -17);
    }

    private void Shape(string name, Vector2 position, Vector2 size, Color color, int order)
    {
        var renderer = SushiWorldVisual.Panel(transform, name, order);
        renderer.transform.localPosition = position;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        renderer.color = color;
    }
}
