using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit editor migration only. Existing authored visuals are never rebuilt on import/play.
public static class SushiPrefabVisualSetup
{
    private const string Root = "Assets/_game/Prefabs/";
    private const string RoundedPath = "Assets/_game/Resources/SushiRoundedPanel.png";
    private const string LinePath = "Assets/_game/Resources/SushiLineMaterial.mat";
    private static Sprite rounded;
    private static Material lineMaterial;

    [MenuItem("SushiKhai/Prefabs/Add Missing Visuals")]
    public static void Bake()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing prefabs.");
        PrepareAssets();
        Edit("Ingredient", BuildIngredient);
        Edit("FoodQueue", BuildQueue);
        Edit("Dia", BuildPlate);
        SushiCustomerPrefabSetup.EnsurePrefabs();
        AssetDatabase.SaveAssets();
        Debug.Log("SUSHI_PREFABS_OK: Ingredient, FoodQueue, Dia, Customer and DishCard have authored visual references; OrderView already uses authored children.");
    }

    public static void RunBatch()
    {
        try { Bake(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static void PrepareAssets()
    {
        if (!File.Exists(RoundedPath))
        {
            File.WriteAllBytes(RoundedPath, SushiWorldVisual.RoundedSprite.texture.EncodeToPNG());
            AssetDatabase.ImportAsset(RoundedPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(RoundedPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64;
            importer.spriteBorder = new Vector4(14, 14, 14, 14);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPath);
        lineMaterial = AssetDatabase.LoadAssetAtPath<Material>(LinePath);
        if (lineMaterial == null)
        {
            lineMaterial = new Material(SushiWorldVisual.SpriteMaterial) { name = "SushiLineMaterial", mainTexture = Texture2D.whiteTexture };
            AssetDatabase.CreateAsset(lineMaterial, LinePath);
        }
    }

    private static void Edit(string name, Action<GameObject> build)
    {
        string path = Root + name + ".prefab";
        bool exists = File.Exists(path);
        GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
        try
        {
            build(root);
            foreach (SpriteRenderer image in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (image.sprite != null && !AssetDatabase.Contains(image.sprite)) image.sprite = rounded;
            foreach (LineRenderer line in root.GetComponentsInChildren<LineRenderer>(true))
                if (line.sharedMaterial == null || !AssetDatabase.Contains(line.sharedMaterial)) line.sharedMaterial = lineMaterial;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    private static T Component<T>(GameObject root) where T : UnityEngine.Component
    {
        T component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
    }

    private static void Bind(Object owner, string field, Object value)
    {
        var data = new SerializedObject(owner);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool Has(Object owner, string field) => new SerializedObject(owner).FindProperty(field).objectReferenceValue != null;

    private static Transform Node(Transform parent, string name)
    {
        var node = new GameObject(name).transform;
        node.SetParent(parent, false);
        return node;
    }

    private static SpriteRenderer Panel(Transform parent, string name, int order) => SushiWorldVisual.Panel(parent, name, order);
    private static TextMeshPro Label(Transform parent, string name, Vector3 position, float size, int order) => SushiWorldVisual.Label(parent, name, position, size, order);

    private static void BuildIngredient(GameObject root)
    {
        FoodShushi food = root.GetComponent<FoodShushi>();
        if (!Has(food, "quantityLabel"))
        {
            var label = Label(root.transform, "Quantity", new Vector3(0.32f, -0.4f, -0.1f), 2.6f, 4);
            label.text = "×2";
            Bind(food, "quantityLabel", label);
            label.gameObject.SetActive(false);
        }
        if (!Has(food, "tileBackground")) Bind(food, "tileBackground", SushiWorldVisual.BoardBase(root.transform));
        if (!Has(food, "stateLabel"))
        {
            var label = Label(root.transform, "Element", new Vector3(0, 0, -0.2f), 3.5f, 5);
            label.text = "?";
            Bind(food, "stateLabel", label);
            label.gameObject.SetActive(false);
        }
        if (!Has(food, "cover"))
        {
            var cover = Panel(root.transform, "Cover", 3);
            cover.sprite = SushiArtCatalog.Default.hiddenTile;
            Bind(food, "cover", cover);
            cover.gameObject.SetActive(false);
        }
    }

    private static void BuildQueue(GameObject root)
    {
        FoodQueue queue = root.GetComponent<FoodQueue>();
        float spacing = new SerializedObject(queue).FindProperty("rowSpace").floatValue;
        if (!Has(queue, "beltView"))
        {
            Transform belt = Node(root.transform, "Column conveyor");
            float center = -1.5f * spacing, height = 4 * spacing;
            var bed = Panel(belt, "Belt", -4);
            bed.transform.localPosition = Vector3.up * center;
            bed.transform.localScale = new Vector3(1.42f, height, 1);
            bed.color = new Color(0.28f, 0.18f, 0.12f);
            for (int side = -1; side <= 1; side += 2)
            {
                var rail = Panel(belt, side < 0 ? "Left rail" : "Right rail", -3);
                rail.transform.localPosition = new Vector3(side * 0.83f, center, -0.01f);
                rail.transform.localScale = new Vector3(0.8f, height, 1);
                rail.color = new Color(0.9f, 0.61f, 0.28f);
            }
            Bind(queue, "beltView", belt);
        }
        var data = new SerializedObject(queue);
        var bases = data.FindProperty("emptyBases");
        if (bases.arraySize > 0) return;
        bases.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            var tile = SushiWorldVisual.BoardBase(root.transform, "Empty belt base " + (i + 1));
            tile.transform.localPosition = Vector3.down * spacing * i;
            bases.GetArrayElementAtIndex(i).objectReferenceValue = tile.transform;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildPlate(GameObject root)
    {
        if (root.transform.Find("Ceramic plate") != null) return;
        SpriteRenderer old = root.GetComponent<SpriteRenderer>();
        if (old != null) Object.DestroyImmediate(old);
        var plate = Panel(root.transform, "Ceramic plate", 0);
        SushiWorldVisual.FitSprite(plate, SushiArtCatalog.Default.plate, new Vector2(1.03f, 0.82f));
    }

}
