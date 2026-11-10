using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Áp art ingame mới (Art/UI/update_ingaem) theo ảnh demo.png:
// - Đổi sprite cũ -> mới ở prefab/scene/catalog, chỉnh Pixels Per Unit để kích thước trên màn hình giữ nguyên.
// - Ô băng chuyền: hố tối khi trống, đĩa trắng chỉ hiện khi nguyên liệu đáp (SushiConveyorSlotView trên Dia.prefab).
// - Bàn phục vụ: 4 ô cố định dùng chung (SushiServingCounter trên ServingCounter.prefab).
// - Board: 4 cột cố định, phóng to cho vừa bề ngang như demo.
// Khung băng chuyền / bàn băng chuyền giữ ảnh cũ vì ảnh mới khác tỉ lệ (sẽ lệch đường chạy).
public static class SushiIngameArtUpdate
{
    private const string NewArt = "Assets/_game/Art/UI/update_ingaem/";
    private const string Prefabs = "Assets/_game/Prefabs/";
    private const string Scene = "Assets/_game/Scenes/GamePlay.unity";
    private const string Mark = "sushi-ingame-art-v1";

    // Ảnh cũ -> ảnh mới.
    private static readonly Dictionary<string, string> SpriteMap = new Dictionary<string, string>
    {
        { "Assets/_game/Art/ingame/tray.png", NewArt + "tray.png" },
        { "Assets/_game/Art/ingame/tray_selecct.png", NewArt + "tray_selecct.png" },
        { "Assets/_game/Art/ingame/line2.png", NewArt + "line2.png" },
        { "Assets/_game/Art/ingame/serve_table.png", NewArt + "serve_table.png" },
        { "Assets/_game/Art/ingame/serve_empty_slot.png", NewArt + "serve_empty_slot.png" },
        { "Assets/_game/Art/ingame/serve_plate.png", NewArt + "serve_plate.png" },
        { "Assets/_game/Art/Elements/HiddenTile.png", NewArt + "lid.png" },
        { "Assets/_game/Art/Elements/FrozenTile.png", NewArt + "ice.png" },
    };

    // Vị trí 4 ô bàn phục vụ (world, lấy theo demo: chia đều bề ngang bàn).
    private static readonly float[] ServingX = { -3f, -1f, 1f, 3f };
    private const float ServingY = 2.82f;
    private const float ServingSlotWidth = 1.12f;
    // Board 4 cột vừa bề ngang như demo.
    private const float BoardScale = 1.25f;
    private const float BoardColumnWidth = 1.5f;

    private static readonly StringBuilder Log = new StringBuilder();

    [MenuItem("SushiKhai/Art/Apply Ingame Art Update (demo)")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Thoát Play Mode trước khi sửa prefab/scene.");
        Log.Clear();
        ImportNewArt();
        var targets = new[] { "FoodQueue", "Ingredient", "ServingCounter", "PreparedDish", "Dia", "Customer" };
        var ppu = new Dictionary<string, float>();
        // Lượt 1: đo kích thước ảnh cũ để ảnh mới giữ đúng kích thước trên màn hình.
        foreach (string prefab in targets) EditPrefab(prefab, root => Measure(root, ppu));
        var catalog = AssetDatabase.LoadAssetAtPath<SushiArtCatalog>("Assets/_game/Resources/SushiArtCatalog.asset");
        if (catalog != null) MeasureObject(catalog, ppu);
        // Viền vàng board: ảnh mới khác tỉ lệ, khớp theo chiều cao (919px cũ ở PPU 100) để dài bằng cột.
        ppu[NewArt + "line2.png"] = 816f * 100f / 919f;
        ApplyPixelsPerUnit(ppu);
        // Lượt 2: thay reference.
        foreach (string prefab in targets) EditPrefab(prefab, root => Replace(root));
        if (catalog != null) { ReplaceObject(catalog); EditorUtility.SetDirty(catalog); }

        EditPrefab("Dia", SetupConveyorSlot);
        EditPrefab("ServingCounter", SetupServingCounter);
        EditPrefab("Customer", root =>
        {
            var view = root.GetComponentInChildren<CustomerRecipeView>(true);
            if (view == null) return;
            var data = new SerializedObject(view);
            data.FindProperty("oneColumnThreeRowsBoard").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_game/Art/ingame/order_board_1x3.png");
            data.ApplyModifiedPropertiesWithoutUndo();
            Log.AppendLine("Customer: bảng order 1x3");
        });
        EditPrefab("DishCard", root =>
        {
            var card = root.GetComponent<SushiDishCardView>();
            if (card == null) return;
            var data = new SerializedObject(card);
            data.FindProperty("recipeCellSprite").objectReferenceValue = NewSprite("fr_order_inside.png");
            data.ApplyModifiedPropertiesWithoutUndo();
            Log.AppendLine("DishCard: ô chi tiết fr_order_inside");
        });
        EditScene();
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Temp");
        File.WriteAllText("Temp/IngameArtReport.txt", Log.ToString());
        Debug.Log("[Ingame art] " + Log);
    }

    private static void ImportNewArt()
    {
        foreach (string file in Directory.GetFiles(NewArt, "*.png"))
        {
            string path = file.Replace('\\', '/');
            if (path.EndsWith("demo.png")) continue;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.userData == Mark) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 100;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            // Ô chi tiết order co giãn theo số nguyên liệu: cắt 9 phần giữ bo góc.
            if (path.EndsWith("fr_order_inside.png")) importer.spriteBorder = new Vector4(36, 36, 36, 36);
            importer.userData = Mark;
            importer.SaveAndReimport();
        }
    }

    private static Sprite NewSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(NewArt + name);

    private static void EditPrefab(string name, Action<GameObject> edit)
    {
        string path = Prefabs + name + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // ---------------------------------------------------------------- Sprite swap

    private static void Measure(GameObject root, Dictionary<string, float> ppu)
    {
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
            if (component != null) MeasureObject(component, ppu);
    }

    private static void MeasureObject(Object target, Dictionary<string, float> ppu)
    {
        var data = new SerializedObject(target);
        var property = data.GetIterator();
        while (property.Next(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
            if (!(property.objectReferenceValue is Sprite old)) continue;
            string oldPath = AssetDatabase.GetAssetPath(old);
            if (!SpriteMap.TryGetValue(oldPath, out string newPath) || ppu.ContainsKey(newPath)) continue;
            var fresh = AssetDatabase.LoadAssetAtPath<Sprite>(newPath);
            if (fresh == null) continue;
            // Cùng chiều rộng trên màn hình: rộng mới / PPU mới = rộng cũ / PPU cũ.
            ppu[newPath] = fresh.rect.width * old.pixelsPerUnit / Mathf.Max(1f, old.rect.width);
        }
    }

    private static void ApplyPixelsPerUnit(Dictionary<string, float> ppu)
    {
        foreach (var pair in ppu)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(pair.Key);
            if (Mathf.Approximately(importer.spritePixelsPerUnit, pair.Value)) continue;
            importer.spritePixelsPerUnit = pair.Value;
            importer.SaveAndReimport();
            Log.AppendLine($"PPU {Path.GetFileName(pair.Key)} = {pair.Value:F1}");
        }
    }

    private static void Replace(GameObject root)
    {
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
            if (component != null) ReplaceObject(component);
    }

    private static void ReplaceObject(Object target)
    {
        var data = new SerializedObject(target);
        var property = data.GetIterator();
        bool changed = false;
        while (property.Next(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
            if (!(property.objectReferenceValue is Sprite old)) continue;
            if (!SpriteMap.TryGetValue(AssetDatabase.GetAssetPath(old), out string newPath)) continue;
            var fresh = AssetDatabase.LoadAssetAtPath<Sprite>(newPath);
            if (fresh == null) continue;
            property.objectReferenceValue = fresh;
            changed = true;
        }
        if (changed)
        {
            data.ApplyModifiedPropertiesWithoutUndo();
            Log.AppendLine("Sprite: " + target.name + " (" + target.GetType().Name + ")");
        }
    }

    // ---------------------------------------------------------------- Conveyor slot

    // Dia.prefab: thêm hố tối "Empty slot" cùng cỡ với đĩa; đĩa chỉ hiện khi có nguyên liệu.
    private static void SetupConveyorSlot(GameObject root)
    {
        var plate = root.transform.Find("Ceramic plate")?.GetComponent<SpriteRenderer>();
        if (plate == null) { Log.AppendLine("Dia: thiếu Ceramic plate"); return; }
        Sprite hole = NewSprite("conveyor_slot.png");
        Transform emptyTransform = root.transform.Find("Empty slot");
        if (emptyTransform == null)
        {
            emptyTransform = new GameObject("Empty slot").transform;
            emptyTransform.SetParent(root.transform, false);
            emptyTransform.SetAsFirstSibling();
        }
        var empty = emptyTransform.GetComponent<SpriteRenderer>();
        if (empty == null) empty = emptyTransform.gameObject.AddComponent<SpriteRenderer>();
        empty.sprite = hole;
        empty.sharedMaterial = plate.sharedMaterial;
        empty.sortingLayerID = plate.sortingLayerID;
        empty.sortingOrder = plate.sortingOrder;
        float plateWidth = plate.sprite.bounds.size.x * plate.transform.localScale.x;
        float scale = plateWidth * 1.02f / Mathf.Max(0.01f, hole.bounds.size.x);
        emptyTransform.localPosition = plate.transform.localPosition;
        emptyTransform.localScale = new Vector3(scale, scale, 1f);
        var view = root.GetComponent<SushiConveyorSlotView>();
        if (view == null) view = root.AddComponent<SushiConveyorSlotView>();
        var data = new SerializedObject(view);
        data.FindProperty("emptyArt").objectReferenceValue = empty;
        data.FindProperty("plate").objectReferenceValue = plate;
        data.ApplyModifiedPropertiesWithoutUndo();
        Log.AppendLine("Dia: thêm Empty slot + SushiConveyorSlotView");
    }

    // ---------------------------------------------------------------- Serving counter

    // ServingCounter.prefab: 4 ô cố định dùng chung, mỗi ô có vòng nét đứt.
    private static void SetupServingCounter(GameObject root)
    {
        Sprite dashed = AssetDatabase.LoadAssetAtPath<Sprite>(NewArt + "serve_empty_slot.png");
        var counter = root.GetComponent<SushiServingCounter>();
        if (counter == null) counter = root.AddComponent<SushiServingCounter>();
        Transform group = root.transform.Find("Serving slots");
        if (group == null)
        {
            group = new GameObject("Serving slots").transform;
            group.SetParent(root.transform, false);
        }
        // Instance trong scene đặt ở y = 3, nên vị trí local = world - 3.
        var slots = new Transform[ServingX.Length];
        var arts = new SpriteRenderer[ServingX.Length];
        for (int i = 0; i < ServingX.Length; i++)
        {
            string name = "Slot " + (i + 1);
            Transform slot = group.Find(name);
            if (slot == null)
            {
                slot = new GameObject(name).transform;
                slot.SetParent(group, false);
            }
            slot.localPosition = new Vector3(ServingX[i], ServingY - 3f, 0f);
            Transform artTransform = slot.Find("Empty slot art");
            if (artTransform == null)
            {
                artTransform = new GameObject("Empty slot art").transform;
                artTransform.SetParent(slot, false);
            }
            var art = artTransform.GetComponent<SpriteRenderer>();
            if (art == null) art = artTransform.gameObject.AddComponent<SpriteRenderer>();
            art.sprite = dashed;
            art.sortingOrder = 2;
            float scale = ServingSlotWidth / Mathf.Max(0.01f, dashed.bounds.size.x);
            artTransform.localScale = new Vector3(scale, scale, 1f);
            slots[i] = slot;
            arts[i] = art;
        }
        var data = new SerializedObject(counter);
        var slotList = data.FindProperty("slots");
        slotList.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++) slotList.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        var artList = data.FindProperty("emptyArts");
        artList.arraySize = arts.Length;
        for (int i = 0; i < arts.Length; i++) artList.GetArrayElementAtIndex(i).objectReferenceValue = arts[i];
        data.ApplyModifiedPropertiesWithoutUndo();
        Log.AppendLine("ServingCounter: 4 ô dùng chung");
    }

    // Hàng chờ như demo: khách chờ đầu tiên ló ở mép trái (cả người + order), các khách sau xếp tiếp ra ngoài mép.
    private const float WaitingX = -3.75f;
    private const float WaitingY = 4.8f;
    private const float WaitingStep = 1.2f;
    private const float WaitingScale = 0.62f;
    // Ghế 1 lùi vào trong để chừa chỗ cho khách chờ ló ở mép trái (như demo).
    private const float FirstSeatX = -1.7f;

    private static void LayoutWaitingLine(GameObject root)
    {
        var manager = root.GetComponent<CustomerManager>();
        if (manager == null) return;
        var data = new SerializedObject(manager);
        data.FindProperty("waitingScale").floatValue = WaitingScale;
        data.ApplyModifiedPropertiesWithoutUndo();
        var firstSeat = data.FindProperty("firstSeat")?.objectReferenceValue as Transform;
        if (firstSeat != null) firstSeat.position = new Vector3(FirstSeatX, firstSeat.position.y, firstSeat.position.z);
        var list = data.FindProperty("waitingPositions");
        for (int i = 0; i < list.arraySize; i++)
        {
            var point = list.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            if (point != null) point.position = new Vector3(WaitingX - WaitingStep * i, WaitingY, point.position.z);
        }
        Log.AppendLine("Scene: hàng chờ " + list.arraySize + " vị trí ở mép trái, scale " + WaitingScale);
    }

    // ---------------------------------------------------------------- Scene

    private static void EditScene()
    {
        Scene scene = SceneManager.GetSceneByPath(Scene);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Additive);
        try
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "BoardManager")
                {
                    root.transform.localScale = new Vector3(BoardScale, BoardScale, 1f);
                    var board = root.GetComponent<BoardShishiManager>();
                    if (board != null)
                    {
                        var data = new SerializedObject(board);
                        data.FindProperty("columnWidth").floatValue = BoardColumnWidth;
                        data.FindProperty("fixedColumns").intValue = 4;
                        data.ApplyModifiedPropertiesWithoutUndo();
                    }
                    Log.AppendLine("Scene: BoardManager scale " + BoardScale + ", columnWidth " + BoardColumnWidth);
                }
                if (root.name == "CustomerManager") LayoutWaitingLine(root);
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                    if (component != null) ReplaceObject(component);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
