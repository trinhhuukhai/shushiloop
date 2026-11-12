using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Lưu reference tới Sprite thực do Unity import, kể cả PNG được slice ở chế độ Multiple.
[InitializeOnLoad]
public sealed class SushiArtSetup : AssetPostprocessor
{
    private const string ArtRoot = "Assets/_game/Art/";
    private const string CatalogPath = "Assets/_game/Resources/SushiArtCatalog.asset";
    internal const string SourceArtRoot = ArtRoot + "nguyenLieu_monAn/asset-20261102T044738Z-1-001/asset/";
    internal const string FixArtRoot = ArtRoot + "nguyenLieu_monAn/fix/";
    private static readonly string[] SourceToppings = { "ca_ngu", "ca_hoi", "ca_bon", "ca_thu", "tom", "bach_tuoc", "bo", "trung" };
    private static bool queued;
    private static bool repairing;

    static SushiArtSetup()
    {
        Queue();
        EditorApplication.playModeStateChanged += _ => Queue();
    }

    private void OnPreprocessTexture()
    {
        if (!IsArtwork(assetPath) || assetPath.Contains("Preview")) return;
        var importer = (TextureImporter)assetImporter;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
        }
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
    }

    private static bool IsArtwork(string path) => path.StartsWith(ArtRoot + "Ingredients/", StringComparison.Ordinal) ||
        path.StartsWith(ArtRoot + "Dishes/", StringComparison.Ordinal) || path.StartsWith(ArtRoot + "Elements/", StringComparison.Ordinal) ||
        path.StartsWith(SourceArtRoot, StringComparison.Ordinal);

    internal static bool HasSourceArtwork(string path) => path.StartsWith(SourceArtRoot, StringComparison.Ordinal) && File.Exists(path);

    internal static string IngredientPath(PoolType type)
    {
        string source;
        switch (type)
        {
            case PoolType.SushiRice: source = "nguyen_lieu/rice"; break;
            case PoolType.Tuna: source = "nguyen_lieu/tuna"; break;
            case PoolType.Salmon: source = "nguyen_lieu/salmon"; break;
            case PoolType.Flounder: source = "nguyen_lieu/hirame"; break;
            case PoolType.Mackerel: source = "nguyen_lieu/mackerel"; break;
            case PoolType.Shrimp: source = "nguyen_lieu/shrimp"; break;
            case PoolType.Octopus: source = "nguyen_lieu/octopus"; break;
            case PoolType.Avocado: source = "nguyen_lieu/avocado"; break;
            case PoolType.Egg: source = "nguyen_lieu/eggroll"; break;
            case PoolType.Mayonnaise: source = "nguyen_lieu/mayo"; break;
            case PoolType.FishRoe: source = "nguyen_lieu/roe_orange"; break;
            case PoolType.BlackFishRoe: source = "nguyen_lieu/roe_black"; break;
            case PoolType.GreenFishRoe: source = "nguyen_lieu/roe_green"; break;
            case PoolType.Seaweed: source = "nguyen_lieu/nori"; break;
            case PoolType.TeaPot: source = "tea_pot/am_tra"; break;
            case PoolType.EmptyCup: source = "tea_pot/tach_tra"; break;
            case PoolType.IceCube: source = "tea_pot/da"; break;
            // Ba nguyên liệu trang trí nằm trong bộ ảnh fix.
            case PoolType.Lemon: return FixArtRoot + "nguyen_lieu/raw_lemon.png";
            case PoolType.Wasabi: return FixArtRoot + "nguyen_lieu/raw_wasabi.png";
            case PoolType.Perilla: return FixArtRoot + "nguyen_lieu/raw_perilla_leaf.png";
            default: return ArtRoot + "Ingredients/" + type + ".png";
        }
        return SourceArtRoot + source + ".png";
    }

    internal static string DishPath(SushiDishId dish)
    {
        int id = (int)dish;
        string source;
        if (id >= 1 && id <= 8)
            source = "nigiri/nigiri_img/nigiri_" + (id == 6 ? "muc" : SourceToppings[id - 1]);
        else if (id >= 13 && id <= 20)
            source = "maki/" + (id == 19 ? "miaki_" : "maiki_") + SourceToppings[id - 13];
        else if (id >= 21 && id <= 28)
            source = "gunkan/gunkan_" + SourceToppings[id - 21];
        else if (id >= 29 && id <= 31)
            source = "gunkan/gunkan_roe_" + new[] { "vang", "den", "xanh" }[id - 29];
        else if (id >= 32 && id <= 46)
            source = "sashimi_x2/sashimi_x2_img/" + id;
        else if (id == 47 || id == 48)
            source = "tea_pot/" + (id == 47 ? "tra_nong" : "tra_da");
        else if (id >= 49 && id <= 68)
            source = "sashimi_x3/sashimi_x3_img/" + id;
        else return ArtRoot + "Dishes/" + dish + ".png";
        return SourceArtRoot + source + ".png";
    }

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous)
    {
        foreach (string path in imported)
            if (IsArtwork(path) || path == CatalogPath) { Queue(); break; }
    }

    private static void Queue()
    {
        if (queued) return;
        queued = true;
        EditorApplication.delayCall += Repair;
    }

    [MenuItem("SushiKhai/Art/Rebuild Art References")]
    public static void Repair()
    {
        queued = false;
        if (repairing) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Queue(); return; }
        repairing = true;
        try
        {
            var ingredients = LoadArtAsset<SushiIngredientArtLibrary>(ArtRoot + "Ingredients/SushiIngredientArt.asset");
            var dishes = LoadArtAsset<SushiDishArtLibrary>(ArtRoot + "Dishes/SushiDishArt.asset");
            var plated = LoadArtAsset<SushiPlatedIngredientArtLibrary>(ArtRoot + "Ingredients/SushiPlatedIngredientArt.asset");
            var catalog = LoadArtAsset<SushiArtCatalog>(CatalogPath);
            if (ingredients == null || dishes == null || plated == null || catalog == null)
            {
                Debug.LogError("Sushi art: cannot load art data. Check the script import errors above; no ingredient data was deleted.");
                return;
            }
            var missing = new List<string>();
            bool changed = false;
            var ingredientData = new SerializedObject(ingredients);
            SerializedProperty ingredientList = ingredientData.FindProperty("ingredients");
            int ingredientCount = Enum.GetValues(typeof(PoolType)).Length - 1;
            foreach (PoolType type in Enum.GetValues(typeof(PoolType)))
            {
                if (type == PoolType.None) continue;
                BindPreferredOrMissing(ingredientList, "type", (int)type, IngredientPath(type), missing);
            }
            changed |= ingredientData.ApplyModifiedPropertiesWithoutUndo();
            var dishData = new SerializedObject(dishes);
            SerializedProperty dishList = dishData.FindProperty("dishes");
            for (int i = 0; i < SushiRecipeCatalog.All.Count; i++)
            {
                SushiDishId type = SushiRecipeCatalog.All[i].Id;
                BindPreferredOrMissing(dishList, "dish", (int)type, DishPath(type), missing);
            }
            changed |= dishData.ApplyModifiedPropertiesWithoutUndo();
            var platedData = new SerializedObject(plated);
            BindSashimiPlate(platedData.FindProperty("sashimiTwoIngredientPlate"), "sashimi_x2", missing);
            BindSashimiPlate(platedData.FindProperty("sashimiThreeIngredientPlate"), "sashimi_x3", missing);
            changed |= platedData.ApplyModifiedPropertiesWithoutUndo();
            changed |= AssignMissing(catalog, "ingredientImages", ingredients);
            changed |= AssignMissing(catalog, "platedIngredientImages", plated);
            changed |= AssignMissing(catalog, "dishImages", dishes);
            changed |= BindElement(catalog, "hiddenTile", "HiddenTile", missing);
            changed |= BindElement(catalog, "frozenTile", "FrozenTile", missing);
            changed |= BindElement(catalog, "linkedTile", "LinkedTile", missing);
            changed |= BindElement(catalog, "rigidTray", "RigidTray", missing);
            changed |= FixPrefab("Assets/_game/Prefabs/Ingredient.prefab", ingredients);
            changed |= FixPrefab("Assets/_game/Prefabs/Customer.prefab", ingredients);
            if (changed) AssetDatabase.SaveAssets();
            foreach (FoodShushi food in UnityEngine.Object.FindObjectsByType<FoodShushi>(FindObjectsSortMode.None)) food.RefreshArtwork();
            foreach (CustomerRecipeView view in UnityEngine.Object.FindObjectsByType<CustomerRecipeView>(FindObjectsSortMode.None)) view.RefreshArtwork();
            string reportDirectory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp");
            Directory.CreateDirectory(reportDirectory);
            File.WriteAllText(Path.Combine(reportDirectory, "SushiArtBindingReport.txt"), "Ingredients: " + ingredientCount + "\nDishes: " + dishList.arraySize +
                "\nElements: 4\nMissing: " + missing.Count + "\n" + string.Join("\n", missing));
            if (changed && missing.Count == 0) Debug.Log("Sushi art: bound " + ingredientCount + " ingredient sprites, " +
                dishList.arraySize + " dish sprites and 4 element sprites.");
        }
        finally { repairing = false; }
    }

    private static T LoadArtAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            MonoScript boundScript = MonoScript.FromScriptableObject(asset);
            if (boundScript != null && boundScript.GetClass() == typeof(T)) return asset;
        }

        string scriptPath = "Assets/_game/Script/" + typeof(T).Name + ".cs";
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        if (script == null || script.GetClass() != typeof(T))
        {
            AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        }
        if (script == null || script.GetClass() != typeof(T))
        {
            Debug.LogError("Sushi art: " + scriptPath + " does not resolve to " + typeof(T).FullName + ". Fix script compilation/import first.");
            return null;
        }

        if (!File.Exists(path))
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // Missing Script vẫn có thể còn đầy đủ data. Chỉ sửa liên kết script, giữ GUID asset và danh sách ảnh.
        string yaml = File.ReadAllText(path);
        if (!yaml.StartsWith("%YAML", StringComparison.Ordinal) || !Regex.IsMatch(yaml, @"(?m)^  m_Script:"))
        {
            Debug.LogError("Sushi art: cannot repair non-YAML art data at " + path);
            return null;
        }
        string guid = AssetDatabase.AssetPathToGUID(scriptPath);
        yaml = Regex.Replace(yaml, @"(?m)^  m_Script:.*$", "  m_Script: {fileID: 11500000, guid: " + guid + ", type: 3}");
        yaml = Regex.Replace(yaml, @"(?m)^  m_EditorClassIdentifier:.*$", "  m_EditorClassIdentifier: " +
            typeof(T).Assembly.GetName().Name + "::" + typeof(T).FullName);
        File.WriteAllText(path, yaml);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) Debug.Log("Sushi art: restored script reference on " + path);
        return asset;
    }

    // Ưu tiên bộ ảnh mới thay atlas cũ; giữ các chỉnh sửa trong bộ nguồn và ảnh ngoài khi nguồn không có.
    private static void BindPreferredOrMissing(SerializedProperty list, string key, int id, string path, List<string> missing)
    {
        SerializedProperty entry = null;
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).FindPropertyRelative(key).intValue == id)
            { entry = list.GetArrayElementAtIndex(i); break; }
        if (entry == null)
        {
            int index = list.arraySize++;
            entry = list.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative(key).intValue = id;
            entry.FindPropertyRelative("sprite").objectReferenceValue = null;
        }
        var sprite = entry.FindPropertyRelative("sprite");
        bool replaceLegacy = HasSourceArtwork(path) && sprite.objectReferenceValue != null &&
            !AssetDatabase.GetAssetPath(sprite.objectReferenceValue).StartsWith(SourceArtRoot, StringComparison.Ordinal);
        if (sprite.objectReferenceValue == null || replaceLegacy)
        {
            Sprite preferred = LoadSprite(path, missing);
            if (preferred != null) sprite.objectReferenceValue = preferred;
        }
    }

    private static bool BindElement(SushiArtCatalog catalog, string field, string name, List<string> missing)
    {
        if (new SerializedObject(catalog).FindProperty(field).objectReferenceValue != null) return false;
        return Assign(catalog, field, LoadSprite(ArtRoot + "Elements/" + name + ".png", missing));
    }

    private static void BindSashimiPlate(SerializedProperty entry, string folder, List<string> missing)
    {
        var image = entry.FindPropertyRelative("sprite");
        if (image.objectReferenceValue != null) return;
        Sprite sprite = LoadSprite(SourceArtRoot + folder + "/dia_setup.png", missing);
        if (sprite == null) return;
        image.objectReferenceValue = sprite;
        // Match the source canvas used by the ingredient layers, including the sprite crop.
        Vector2 canvasSize = new Vector2(sprite.texture.width, sprite.texture.height);
        Rect rect = sprite.rect;
        entry.FindPropertyRelative("position").vector2Value = new Vector2(
            (rect.center.x / canvasSize.x - 0.5f) * 0.86f, (rect.center.y / canvasSize.y - 0.5f) * 0.7f);
        entry.FindPropertyRelative("size").vector2Value = new Vector2(
            rect.width / canvasSize.x * 0.86f, rect.height / canvasSize.y * 0.7f);
    }

    private static Sprite LoadSprite(string path, List<string> missing)
    {
        Sprite first = null;
        string name = Path.GetFileNameWithoutExtension(path);
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (!(asset is Sprite sprite)) continue;
            if (sprite.name == name || sprite.name == name + "_0") return sprite;
            if (first == null) first = sprite;
        }
        if (first == null && File.Exists(path) && AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            first = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        if (first == null) missing.Add(path);
        return first;
    }

    private static bool Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(target);
        var property = data.FindProperty(field);
        if (property == null || property.objectReferenceValue == value) return false;
        property.objectReferenceValue = value;
        return data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool AssignMissing(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        if (new SerializedObject(target).FindProperty(field).objectReferenceValue != null) return false;
        return Assign(target, field, value);
    }

    private static bool FixPrefab(string path, SushiIngredientArtLibrary ingredients)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return false;
        bool changed = false;
        FoodShushi food = prefab.GetComponent<FoodShushi>();
        Customer customer = prefab.GetComponent<Customer>();
        if (food != null) changed |= AssignMissing(food, "ingredientArt", ingredients);
        if (customer != null) changed |= AssignMissing(customer, "ingredientArt", ingredients);
        foreach (SpriteRenderer renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.sharedMaterial == null)
            {
                renderer.sharedMaterial = SushiWorldVisual.SpriteMaterial;
                EditorUtility.SetDirty(renderer); changed = true;
            }
            if (food != null && renderer == new SerializedObject(food).FindProperty("foodRenderer").objectReferenceValue)
            {
                var library = new SerializedObject(food).FindProperty("ingredientArt").objectReferenceValue as SushiIngredientArtLibrary;
                Sprite sprite = library != null ? library.GetSprite(food.Type) : null;
                if (renderer.sprite != sprite) { renderer.sprite = sprite; EditorUtility.SetDirty(renderer); changed = true; }
            }
        }
        if (changed) PrefabUtility.SavePrefabAsset(prefab);
        return changed;
    }
}
