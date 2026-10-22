using TMPro;
using UnityEngine;
using DG.Tweening;

// Material URP dùng chung, nền ô và các hiệu ứng element.
public static class SushiWorldVisual
{
    private static Sprite panelSprite;
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
        panel.sprite = WhiteSprite;
        panel.sharedMaterial = SpriteMaterial;
        panel.sortingOrder = order;
        return panel;
    }

    public static SpriteRenderer BoardBase(Transform parent, string name = "Board tile")
    {
        var rim = Panel(parent, name, 0);
        rim.transform.localScale = new Vector3(1.08f, 1.08f, 1);
        rim.color = new Color(0.94f, 0.88f, 0.73f);
        var recess = Panel(rim.transform, "Recess", 0);
        recess.transform.localPosition = new Vector3(0, -0.015f, -0.01f);
        recess.transform.localScale = new Vector3(0.87f, 0.87f, 1);
        recess.color = new Color(0.49f, 0.44f, 0.35f);
        var floor = Panel(recess.transform, "Base", 0);
        floor.transform.localPosition = new Vector3(0, -0.025f, -0.01f);
        floor.transform.localScale = new Vector3(0.88f, 0.86f, 1);
        floor.color = new Color(0.68f, 0.63f, 0.53f);
        return rim;
    }
}
