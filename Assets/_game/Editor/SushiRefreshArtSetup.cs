using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Import actual sprites, then bind the same libraries used by gameplay and the Inspector.
[InitializeOnLoad]
public static class SushiRefreshArtSetup
{
    private const string Root = "Assets/_game/Art/Refresh/";
    private const string Marker = "sushi-refresh-v1";
    [Serializable] public class Layout { public Atlas[] atlases; }
    [Serializable] public class Atlas { public string file; public Slice[] sprites; }
    [Serializable] public class Slice { public string name; public int x, y, width, height; }
    static SushiRefreshArtSetup() => EditorApplication.delayCall += ApplyIfNeeded;

    private static void ApplyIfNeeded()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += ApplyIfNeeded; return; }
        if (File.Exists(Root + "AtlasLayout.json")) Apply(false);
    }

    [MenuItem("SushiKhai/Art/Apply Refreshed Sprite Art")]
    public static void Apply() => Apply(true);

    private static void Apply(bool force)
    {
        if (!File.Exists(Root + "AtlasLayout.json")) return;
        Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Root + "AtlasLayout.json"));
        if (layout?.atlases == null) return;
        var sprites = new Dictionary<string, Sprite>();
        bool updated = false;
        foreach (Atlas atlas in layout.atlases)
        {
            string path = Root + atlas.file;
            if (!File.Exists(path)) continue;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { AssetDatabase.ImportAsset(path); importer = AssetImporter.GetAtPath(path) as TextureImporter; }
            if (importer == null) continue;
            if (force || importer.userData != Marker)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 256;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                var factory = new SpriteDataProviderFactories();
                factory.Init();
                var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var old = provider.GetSpriteRects();
                var rects = new List<SpriteRect>();
                foreach (Slice slice in atlas.sprites)
                {
                    SpriteRect previous = Array.Find(old, item => item.name == slice.name);
                    rects.Add(new SpriteRect { name = slice.name, rect = new Rect(slice.x, slice.y, slice.width, slice.height),
                        pivot = new Vector2(0.5f, 0.5f), alignment = SpriteAlignment.Center,
                        spriteID = previous != null ? previous.spriteID : GUID.Generate() });
                }
                provider.SetSpriteRects(rects.ToArray());
                var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
                ids?.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
                provider.Apply();
                importer.userData = Marker;
                importer.SaveAndReimport();
                updated = true;
            }
            foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                sprites[sprite.name] = sprite;
        }
        if (sprites.Count == 0) return;
        var catalog = AssetDatabase.LoadAssetAtPath<SushiArtCatalog>("Assets/_game/Resources/SushiArtCatalog.asset");
        if (catalog == null) return;
        if (!updated && !force && catalog.boardTile != null && catalog.customerAvatars.Length > 0) return;
        foreach (var entry in catalog.ingredientImages.ingredients)
            if (sprites.TryGetValue(entry.type.ToString(), out Sprite sprite)) entry.sprite = sprite;
        foreach (var entry in catalog.dishImages.dishes)
            if (sprites.TryGetValue(entry.dish.ToString(), out Sprite sprite)) entry.sprite = sprite;
        if (sprites.TryGetValue("BoardTile", out Sprite tile)) catalog.boardTile = tile;
        if (sprites.TryGetValue("Plate", out Sprite plate)) catalog.plate = plate;
        if (sprites.TryGetValue("OrderCard", out Sprite card)) catalog.orderCard = card;
        if (sprites.TryGetValue("Conveyor", out Sprite belt)) catalog.conveyor = belt;
        catalog.customerAvatars = sprites.Where(pair => pair.Key.StartsWith("Customer", StringComparison.Ordinal))
            .OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
        EditorUtility.SetDirty(catalog);
        EditorUtility.SetDirty(catalog.ingredientImages);
        EditorUtility.SetDirty(catalog.dishImages);
        AssetDatabase.SaveAssets();
        SushiArtSetup.Repair();
        Debug.Log("Sushi refresh: imported and bound " + sprites.Count + " world sprites.");
    }
}

public sealed class SushiRefreshArtWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous)
    {
        foreach (string path in imported)
            if (path == "Assets/_game/Art/Refresh/AtlasLayout.json")
            { EditorApplication.delayCall += SushiRefreshArtSetup.Apply; break; }
    }
}
