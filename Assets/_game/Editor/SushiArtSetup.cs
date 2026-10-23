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
        path.StartsWith(ArtRoot + "Dishes/", StringComparison.Ordinal) || path.StartsWith(ArtRoot + "Elements/", StringComparison.Ordinal);

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
            var catalog = LoadArtAsset<SushiArtCatalog>(CatalogPath);
            if (ingredients == null || dishes == null || catalog == null)
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
                BindMissing(ingredientList, "type", (int)type, ArtRoot + "Ingredients/" + type + ".png", missing);
            }
            changed |= ingredientData.ApplyModifiedPropertiesWithoutUndo();
            var dishData = new SerializedObject(dishes);
            SerializedProperty dishList = dishData.FindProperty("dishes");
            for (int i = 0; i < SushiRecipeCatalog.All.Count; i++)
            {
                SushiDishId type = SushiRecipeCatalog.All[i].Id;
                BindMissing(dishList, "dish", (int)type, ArtRoot + "Dishes/" + type + ".png", missing);
            }
            changed |= dishData.ApplyModifiedPropertiesWithoutUndo();
            changed |= AssignMissing(catalog, "ingredientImages", ingredients);
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
            if (changed && missing.Count == 0) Debug.Log("Sushi art: bound 21 ingredient sprites, 48 dish sprites and 4 element sprites.");
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

    // Giữ ảnh do người dùng gán trong Inspector; chỉ bổ sung type hoặc reference đang thiếu.
    private static void BindMissing(SerializedProperty list, string key, int id, string path, List<string> missing)
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
        if (sprite.objectReferenceValue == null) sprite.objectReferenceValue = LoadSprite(path, missing);
    }

    private static bool BindElement(SushiArtCatalog catalog, string field, string name, List<string> missing)
    {
        if (new SerializedObject(catalog).FindProperty(field).objectReferenceValue != null) return false;
        return Assign(catalog, field, LoadSprite(ArtRoot + "Elements/" + name + ".png", missing));
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
