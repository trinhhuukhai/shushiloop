#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

// Author the new bag prefab once. Runtime only instantiates it and binds its contents.
public static class SushiReviveBagSetup
{
    public const string PrefabPath = "Assets/_game/Prefabs/ReviveBag.prefab";

    [MenuItem("SushiKhai/Art/Create Revive Bag Prefab")]
    public static void CreatePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
        var root = new GameObject("ReviveBag");
        try
        {
            var view = root.AddComponent<SushiReviveBag>();
            var art = new GameObject("Pouch", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            art.transform.SetParent(root.transform, false);
            art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_game/Art/ReviveBag.png");
            if (art.sprite == null) throw new System.InvalidOperationException("Missing ReviveBag sprite.");
            art.sharedMaterial = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Prefabs/Ingredient.prefab")
                .GetComponentInChildren<SpriteRenderer>(true).sharedMaterial;
            art.sortingOrder = 5;
            var opening = new GameObject("Opening").transform;
            opening.SetParent(root.transform, false);
            opening.localPosition = new Vector3(0, 0.3f, -0.05f);
            var label = new GameObject("Quantity", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0.3f, -0.3f, -0.1f);
            label.rectTransform.sizeDelta = new Vector2(0.55f, 0.3f);
            var textTemplate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_game/Prefabs/Ingredient.prefab")
                .GetComponentInChildren<TextMeshPro>(true);
            label.font = textTemplate.font;
            label.fontSharedMaterial = textTemplate.fontSharedMaterial;
            label.fontSize = 2.4f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.text = "×5";
            label.color = Color.white;
            label.GetComponent<MeshRenderer>().sortingOrder = 6;
            var serialized = new SerializedObject(view);
            serialized.FindProperty("opening").objectReferenceValue = opening;
            serialized.FindProperty("quantity").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(root); }
    }
}
#endif
