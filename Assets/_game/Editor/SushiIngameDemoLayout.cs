using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Bố cục ingame theo đúng ảnh Art/UI/update_ingaem/demo.png (1080x2340).
// Quy ước: 120 px của demo = 1 đơn vị thế giới; tâm màn hình (y = 1170 px) là gốc toạ độ.
// Camera khớp vùng chơi 9 x 19.5 nên trên máy màn dài bố cục trùng demo; máy 16:9 thu nhỏ vừa khung.
public static class SushiIngameDemoLayout
{
    private const string NewArt = "Assets/_game/Art/UI/update_ingaem/";
    private const string Prefabs = "Assets/_game/Prefabs/";
    private const string ScenePath = "Assets/_game/Scenes/GamePlay.unity";
    private const float PixelsPerUnit = 120f;
    private static readonly StringBuilder Log = new StringBuilder();

    // Toạ độ demo (px) -> thế giới.
    private static Vector3 D(float px, float py) => new Vector3((px - 540f) / PixelsPerUnit, (1170f - py) / PixelsPerUnit, 0f);
    private static float U(float px) => px / PixelsPerUnit;

    // ---- Số liệu đo trên demo.png
    private const float CounterTopPx = 615f;            // mép trên bàn phục vụ
    private static readonly float[] ServingPx = { 180f, 420f, 657f, 895f };
    private const float ServingPy = 705f;
    private const float ServingPlatePx = 197f;          // bề ngang đĩa phục vụ
    private const float ConveyorCenterPy = 1157f;       // tâm khung băng chuyền
    private const float ConveyorWidthPx = 1020f;
    private const float ConveyorTablePy = 830f;         // mép trên bàn băng chuyền
    // Tâm rãnh xám đo trực tiếp trên conveyor.png để hố/đĩa không chạm viền khung.
    private const float BeltTopPy = 958f, BeltBottomPy = 1347f, BeltSidePx = 152.5f;
    private const float BeltPlatePx = 190f;             // bề ngang đĩa trên băng
    private const float FirstTrayPy = 1625f;            // tâm hàng khay đầu
    private const float BottomFramePy = 2085f;          // mép trên nền xanh dưới đáy
    private const float Seat1AvatarPx = 190f, Seat2AvatarPx = 660f;
    private const float WaitingPanelPx = 118f;
    private const float BoosterLiftCanvas = 130f;

    // ---- Kích thước customer prefab (đơn vị canvas * 0.013)
    private const float AvatarOffset = 92f * 0.013f;    // ảnh nhân vật lệch trái so với gốc
    private const float PanelOffset = 52f * 0.013f;     // bảng order lệch phải so với gốc
    private const float VisibleBelowRoot = 1.35f;       // phần thân hiện ra dưới gốc tới mép bàn
    private const float WaitingScale = 0.62f;
    // Ảnh bàn/nền kéo ngang tới bề rộng này để màn 16:9 (rộng ~11 đơn vị) không hở hai bên.
    private const float CoverWidth = 11.2f;

    [MenuItem("SushiKhai/Art/Apply Ingame Demo Layout")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Thoát Play Mode trước khi sửa prefab/scene.");
        // Camera giờ cố định orthographicSize = 8 (cao 16 đơn vị), nên các root thế giới trong GamePlay.unity
        // (Ingame art, Bangchuyen, BangchuyenManager, CustomerManager, TutControl, BoardManager) đã được scale 16/19.5
        // và đường chạy băng chuyền nhân theo. Tool này vẫn tính theo 120 px/đơn vị với root scale 1,
        // chạy lại sẽ làm lệch bố cục đó nên phải xác nhận trước.
        if (!EditorUtility.DisplayDialog("Apply Ingame Demo Layout",
            "Bố cục hiện tại đã được scale 16/19.5 cho camera orthographicSize = 8. Tool này tính theo camera cũ (cao 19.5 đơn vị) và sẽ ghi đè làm lệch bố cục. Vẫn chạy?",
            "Chạy", "Huỷ")) return;
        Log.Clear();
        SetPixelsPerUnit("conveyor.png", PixelsPerUnit * 1040f / ConveyorWidthPx);
        SetPixelsPerUnit("conveyor_table.png", PixelsPerUnit);
        SetPixelsPerUnit("serve_table.png", PixelsPerUnit);
        SetPixelsPerUnit("fr_bottom.png", PixelsPerUnit);
        EditPrefab("ServingCounter", LayoutServingCounter);
        EditPrefab("Dia", LayoutBeltSlot);
        EditBoosters();
        EditScene();
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Temp");
        File.WriteAllText("Temp/IngameDemoLayout.txt", Log.ToString());
        Debug.Log("[Ingame demo layout] " + Log);
    }

    private static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(NewArt + name);

    private static void SetPixelsPerUnit(string name, float ppu)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(NewArt + name);
        if (importer == null || Mathf.Approximately(importer.spritePixelsPerUnit, ppu)) return;
        importer.spritePixelsPerUnit = ppu;
        importer.SaveAndReimport();
        Log.AppendLine($"PPU {name} = {ppu:F1}");
    }

    private static void EditPrefab(string name, Action<GameObject> edit)
    {
        string path = Prefabs + name + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Gốc ServingCounter đặt sao cho mép trên bàn trùng demo; 4 ô đĩa to bằng đĩa demo.
    private static float CounterRootY => D(0, CounterTopPx).y - CounterArtTopLocal;
    private const float CounterArtTopLocal = 0.3f;

    private static void LayoutServingCounter(GameObject root)
    {
        Transform art = root.transform.Find("Serving table art");
        var table = art != null ? art.GetComponent<SpriteRenderer>() : null;
        if (table != null)
        {
            table.sprite = Art("serve_table.png");
            art.localScale = new Vector3(CoverWidth / 9f, 1f, 1f);
            art.localPosition = new Vector3(0f, CounterArtTopLocal - table.sprite.bounds.size.y * 0.5f, 0f);
        }
        var counter = root.GetComponent<SushiServingCounter>();
        if (counter == null) return;
        var data = new SerializedObject(counter);
        var slots = data.FindProperty("slots");
        var arts = data.FindProperty("emptyArts");
        // PreparedDish có đĩa rộng ~1.03 đơn vị; phóng ô để đĩa bằng đĩa demo.
        float slotScale = U(ServingPlatePx) / 1.03f;
        for (int i = 0; i < slots.arraySize && i < ServingPx.Length; i++)
        {
            var slot = slots.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            if (slot == null) continue;
            Vector3 world = D(ServingPx[i], ServingPy);
            slot.localPosition = new Vector3(world.x, world.y - CounterRootY, 0f);
            slot.localScale = new Vector3(slotScale, slotScale, 1f);
            var dashed = i < arts.arraySize ? arts.GetArrayElementAtIndex(i).objectReferenceValue as SpriteRenderer : null;
            if (dashed != null && dashed.sprite != null)
            {
                float scale = U(193f) / slotScale / dashed.sprite.bounds.size.x;
                dashed.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
        Log.AppendLine($"ServingCounter: ô đĩa scale {slotScale:F2}, bàn PPU 120");
    }

    // Đĩa và hố trên băng to bằng demo (nguyên liệu trên đĩa phóng theo).
    private static void LayoutBeltSlot(GameObject root)
    {
        var plate = root.transform.Find("Ceramic plate")?.GetComponent<SpriteRenderer>();
        if (plate == null || plate.sprite == null) return;
        float plateWidth = plate.sprite.bounds.size.x * plate.transform.localScale.x;
        float scale = U(BeltPlatePx) / Mathf.Max(0.01f, plateWidth);
        root.transform.localScale = new Vector3(scale, scale, 1f);
        Log.AppendLine($"Dia: scale {scale:F2}");
    }

    // Nâng nhóm booster lên vùng nền xanh như demo (chừa chỗ banner dưới đáy).
    private static void EditBoosters()
    {
        const string path = "Assets/_game/Resources/UI/UICGamePlay.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hud = root.GetComponent<CanvasGamePlay>();
            if (hud != null && hud.BoosterGroup != null && !root.name.Contains("lifted"))
            {
                const string mark = "DemoBoosterLift";
                // Chỉ nâng một lần: đánh dấu bằng tên child rỗng.
                if (hud.BoosterGroup.Find(mark) == null)
                {
                    hud.BoosterGroup.anchoredPosition += new Vector2(0f, BoosterLiftCanvas);
                    var marker = new GameObject(mark, typeof(RectTransform));
                    marker.transform.SetParent(hud.BoosterGroup, false);
                    marker.SetActive(false);
                    Log.AppendLine("UICGamePlay: nâng booster " + BoosterLiftCanvas);
                }
                // Nền dưới nhóm booster: đổi mảng nâu tối thành nền xanh fr_bottom như demo
                // (mép trên ngay trên tâm nút booster).
                if (hud.BoosterGroup.Find("Image (1)") is RectTransform panel &&
                    panel.GetComponent<UnityEngine.UI.Image>() is UnityEngine.UI.Image image)
                {
                    Sprite wave = Art("fr_bottom.png");
                    image.sprite = wave;
                    image.type = UnityEngine.UI.Image.Type.Simple;
                    image.preserveAspect = false;
                    image.color = Color.white;
                    panel.sizeDelta = new Vector2(wave.rect.width, wave.rect.height);
                    panel.anchoredPosition = new Vector2(0f, 45f - wave.rect.height * 0.5f);
                    Log.AppendLine("UICGamePlay: nền booster fr_bottom");
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void EditScene()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                switch (root.name)
                {
                    case "Main Camera": LayoutCamera(root); break;
                    case "Ingame art": LayoutArt(root); break;
                    case "Bangchuyen": LayoutBelt(root); break;
                    case "CustomerManager": LayoutCustomers(root); break;
                    case "BoardManager":
                        // Hàng khay đầu ở đúng vị trí demo (cột: tâm hàng đầu lệch ~0.04 * scale).
                        root.transform.position = new Vector3(0f, D(0, FirstTrayPy).y - 0.04f, 0f);
                        Log.AppendLine("Board: y " + root.transform.position.y.ToString("F2"));
                        break;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void LayoutCamera(GameObject root)
    {
        var control = root.GetComponent<CameraControl>();
        if (control == null) return;
        var data = new SerializedObject(control);
        data.FindProperty("playfieldSize").vector2Value = new Vector2(9f, 2340f / PixelsPerUnit);
        data.FindProperty("playfieldCenter").vector2Value = Vector2.zero;
        data.ApplyModifiedPropertiesWithoutUndo();
        Log.AppendLine("Camera: vùng chơi 9 x 19.5");
    }

    private static void LayoutArt(GameObject root)
    {
        float halfHeight = 2340f / PixelsPerUnit * 0.5f;
        foreach (Transform child in root.transform)
        {
            var renderer = child.GetComponent<SpriteRenderer>();
            switch (child.name)
            {
                case "Background":
                    Cover(child, renderer, CoverWidth, halfHeight * 2f + 0.4f, 0f);
                    break;
                case "Restaurant backdrop":
                    // Phủ từ đỉnh màn hình xuống dưới mép bàn phục vụ.
                    if (renderer != null && renderer.sprite != null)
                    {
                        float lowest = D(0, CounterTopPx).y - 0.6f;
                        float highest = halfHeight + 0.2f;
                        Vector2 size = renderer.sprite.bounds.size;
                        float scale = Mathf.Max(CoverWidth / size.x, (highest - lowest) / size.y);
                        child.localScale = new Vector3(scale, scale, 1f);
                        child.position = new Vector3(0f, lowest + size.y * scale * 0.5f, 0f);
                    }
                    break;
                case "Conveyor table":
                    if (renderer != null)
                    {
                        renderer.sprite = Art("conveyor_table.png");
                        child.localScale = new Vector3(CoverWidth / 9f, 1f, 1f);
                        child.position = new Vector3(0f, D(0, ConveyorTablePy).y - renderer.sprite.bounds.size.y * 0.5f, 0f);
                    }
                    break;
                case "Conveyor frame":
                    if (renderer != null)
                    {
                        renderer.sprite = Art("conveyor.png");
                        child.localScale = Vector3.one;
                        child.position = D(540f, ConveyorCenterPy);
                    }
                    break;
                case "Conveyor frame (1)":
                case "Conveyor frame (2)":
                    // Ảnh băng chuyền mới đã gồm cả mép trước; tắt các mảnh cũ.
                    child.gameObject.SetActive(false);
                    break;
            }
        }
        // Nền xanh dưới đáy là ảnh UI dưới nhóm booster (UICGamePlay), bản đặt trong scene không cần nữa.
        Transform oldBottom = root.transform.Find("Bottom frame");
        if (oldBottom != null) Object.DestroyImmediate(oldBottom.gameObject);
        Log.AppendLine("Art: băng chuyền + bàn băng chuyền mới");
    }

    // Phóng đều để phủ kín khung width x height quanh tâm.
    private static void Cover(Transform target, SpriteRenderer renderer, float width, float height, float centerY)
    {
        if (renderer == null || renderer.sprite == null) return;
        Vector2 size = renderer.sprite.bounds.size;
        float scale = Mathf.Max(width / size.x, height / size.y);
        target.localScale = new Vector3(Mathf.Max(target.localScale.x, scale), Mathf.Max(target.localScale.y, scale), 1f);
        target.position = new Vector3(0f, centerY, target.position.z);
    }

    // Đường chạy của đĩa khớp giữa rãnh xám của khung mới.
    private static void LayoutBelt(GameObject root)
    {
        var spline = root.GetComponentInChildren<ConveyorSpline>(true);
        if (spline == null) return;
        float top = D(0, BeltTopPy).y, bottom = D(0, BeltBottomPy).y;
        Vector2 half = new Vector2(Mathf.Abs(D(BeltSidePx, 0).x), (top - bottom) * 0.5f);
        spline.Configure(new Vector2(0f, (top + bottom) * 0.5f), half, 1.1f);
        EditorUtility.SetDirty(spline);
        Log.AppendLine($"Belt: center y {(top + bottom) * 0.5f:F2}, half {half}");
    }

    private static void LayoutCustomers(GameObject root)
    {
        var manager = root.GetComponent<CustomerManager>();
        if (manager == null) return;
        var data = new SerializedObject(manager);
        float counterTop = D(0, CounterTopPx).y;
        float seatY = counterTop + VisibleBelowRoot;
        if (data.FindProperty("firstSeat").objectReferenceValue is Transform first)
            first.position = new Vector3(D(Seat1AvatarPx, 0).x + AvatarOffset, seatY, 0f);
        if (data.FindProperty("secondSeat").objectReferenceValue is Transform second)
            second.position = new Vector3(D(Seat2AvatarPx, 0).x + AvatarOffset, seatY, 0f);
        data.FindProperty("waitingScale").floatValue = WaitingScale;
        // Khách chờ đứng sát bàn như khách ngồi: phần thân dưới chạm mép bàn.
        float waitingY = counterTop + VisibleBelowRoot * WaitingScale;
        float firstWaitingX = D(WaitingPanelPx, 0).x - PanelOffset * WaitingScale;
        var list = data.FindProperty("waitingPositions");
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue is Transform point)
                point.position = new Vector3(firstWaitingX - 1.2f * i, waitingY, 0f);
        data.ApplyModifiedPropertiesWithoutUndo();
        // Bàn phục vụ (instance prefab) đặt theo mép bàn demo.
        Transform counter = FindDeep(root.scene, "Serving counter");
        if (counter != null) counter.position = new Vector3(0f, CounterRootY, counter.position.z);
        Log.AppendLine($"Customers: ghế y {seatY:F2}, khách chờ y {waitingY:F2} x {firstWaitingX:F2}");
    }

    private static Transform FindDeep(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
        return null;
    }
}
