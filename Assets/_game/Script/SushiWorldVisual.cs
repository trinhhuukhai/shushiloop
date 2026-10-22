using TMPro;
using UnityEngine;
using DG.Tweening;

// Material URP dùng chung, nền ô và các hiệu ứng element.
public static class SushiWorldVisual
{
    private static Sprite panelSprite;
    private static Sprite roundedSprite;
    private static Material lineMaterial;
    private static Material spriteMaterial;

    public static Material SpriteMaterial
    {
        get
        {
            if (spriteMaterial == null)
            {
                spriteMaterial = Resources.Load<Material>("SushiSpriteMaterial");
                if (spriteMaterial == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    if (shader == null) shader = Shader.Find("Sprites/Default");
                    spriteMaterial = new Material(shader);
                }
            }
            return spriteMaterial;
        }
    }

    public static Sprite WhiteSprite
    {
        get
        {
            if (panelSprite == null)
                panelSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width,
                    Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
            return panelSprite;
        }
    }

    // A neutral nine-slice shape, shared by world panels and Canvas controls.
    public static Sprite RoundedSprite
    {
        get
        {
            if (roundedSprite != null) return roundedSprite;
            const int size = 64;
            const float radius = 12f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Rounded panel";
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - radius - 1), 0);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - radius - 1), 0);
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy)));
                }
            texture.SetPixels(pixels);
            texture.Apply();
            roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f,
                size, 0, SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14));
            return roundedSprite;
        }
    }

    public static void FitSprite(SpriteRenderer renderer, Sprite sprite, Vector2 size)
    {
        renderer.sprite = sprite;
        renderer.sharedMaterial = SpriteMaterial;
        renderer.transform.localScale = new Vector3(size.x / Mathf.Max(0.01f, sprite.bounds.size.x),
            size.y / Mathf.Max(0.01f, sprite.bounds.size.y), 1);
    }

    public static void BurstIce(Transform parent, Vector3 position)
    {
        for (int i = 0; i < 6; i++)
        {
            var shard = Panel(parent, "Ice shard", 6);
            shard.transform.position = position;
            shard.transform.localScale = Vector3.one * 0.1f;
            Vector3 direction = new Vector3(Mathf.Cos(i * Mathf.PI / 3), Mathf.Sin(i * Mathf.PI / 3), 0);
            DG.Tweening.DOVirtual.Float(0, 1, 0.3f, t =>
            {
                if (shard == null) return;
                shard.transform.position = position + direction * t * 0.6f;
                shard.color = new Color(0.3f, 0.8f, 1, 1 - t);
            }).OnComplete(() => { if (shard != null) Object.Destroy(shard.gameObject); });
        }
    }

    public static Material LineMaterial => lineMaterial != null ? lineMaterial :
        (lineMaterial = new Material(SpriteMaterial) { mainTexture = Texture2D.whiteTexture });

    public static TextMeshPro Label(Transform parent, string name, Vector3 position, float fontSize, int order)
    {
        var label = new GameObject(name).AddComponent<TextMeshPro>();
        label.transform.SetParent(parent, false);
        label.transform.localPosition = position;
        label.rectTransform.sizeDelta = new Vector2(1.1f, 0.5f);
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.2f, 0.1f, 0.04f);
        label.GetComponent<MeshRenderer>().sortingOrder = order;
        return label;
    }

    public static SpriteRenderer Panel(Transform parent, string name, int order)
    {
        var panel = new GameObject(name).AddComponent<SpriteRenderer>();
        panel.transform.SetParent(parent, false);
        panel.sprite = RoundedSprite;
        panel.sharedMaterial = SpriteMaterial;
        panel.sortingOrder = order;
        return panel;
    }

    public static SpriteRenderer BoardBase(Transform parent, string name = "Board tile")
    {
        var rim = Panel(parent, name, 0);
        Sprite tile = SushiArtCatalog.Default != null ? SushiArtCatalog.Default.boardTile : null;
        if (tile != null)
        {
            FitSprite(rim, tile, new Vector2(1.34f, 1.22f));
            return rim;
        }
        rim.transform.localScale = new Vector3(1.34f, 1.22f, 1);
        rim.color = new Color(0.91f, 0.61f, 0.28f);
        var recess = Panel(rim.transform, "Recess", 0);
        recess.transform.localPosition = new Vector3(0, -0.015f, -0.01f);
        recess.transform.localScale = new Vector3(0.90f, 0.90f, 1);
        recess.color = new Color(0.38f, 0.19f, 0.10f);
        var floor = Panel(recess.transform, "Base", 0);
        floor.transform.localPosition = new Vector3(0, -0.025f, -0.01f);
        floor.transform.localScale = new Vector3(0.88f, 0.86f, 1);
        floor.color = new Color(0.58f, 0.31f, 0.15f);
        return rim;
    }
}
