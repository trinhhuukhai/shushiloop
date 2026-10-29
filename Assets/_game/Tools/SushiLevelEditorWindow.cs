#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Đặt file này trong thư mục Editor. Menu: SushiKhai/Level Editor (hoặc double-click asset SushiLevelData).
/// - Trái: cài đặt level + danh sách khách (thêm/sửa/xóa/đổi thứ tự/nhân bản), hiển thị icon món và nguyên liệu.
/// - Phải: board đúng hàng/cột, kéo thả để đổi chỗ, chỉnh element (Ice/Hidden/Link/Tray), mỗi ô có icon nguyên liệu.
/// - Thêm/sửa/xóa khách hoặc món => board tự thêm/bớt nguyên liệu tương ứng.
/// Icon lấy từ SushiArtCatalog (dishImages cho món, ingredientImages cho nguyên liệu). Nếu catalog không có thì tìm theo TÊN FILE trùng tên enum:
///   1) Sprite/Texture có tên file trùng (bỏ qua hoa/thường, dấu _ và tiền tố icon/ic/spr/sprite/img/ui)
///   2) Prefab trùng tên: lấy SpriteRenderer trong prefab, nếu không có thì dùng ảnh preview của prefab.
/// </summary>
public class SushiLevelEditorWindow : EditorWindow
{
    private enum Element { Normal, Ice, Hidden, Link, Tray }
    private enum IconKind { Dish, Ingredient }

    private class Icon { public Texture tex; public Rect uv; }

    private class IconIndex
    {
        public readonly Dictionary<string, string> textures = new Dictionary<string, string>();
        public readonly Dictionary<string, string> prefabs = new Dictionary<string, string>();
    }

    private const float CellW = 100f, CellH = 56f, Gap = 4f, HeaderW = 40f, HeaderH = 22f;
    private const float CellIcon = 44f;

    private const string PrefShowIcons = "SushiLevelEditor.ShowIcons";
    private const string PrefDishFolder = "SushiLevelEditor.IconFolder.Dish";
    private const string PrefIngredientFolder = "SushiLevelEditor.IconFolder.Ingredient";

    private SushiLevelData level;
    private Vector2 leftScroll, boardScroll;
    private int selCustomer = -1;
    private int selCol = -1, selRow = -1;
    private int dragCol = -1, dragRow = -1;
    private bool linkMode;
    private bool showSettings = true;
    private int traySize = 2;
    private int kValue = 5;
    private int simSeats = 3;
    private int annealIterations = 1500;
    private string diffReport = "";
    private MessageType diffReportType = MessageType.None;
    private readonly DishOrderData newOrder = new DishOrderData { recipeKind = SushiRecipeKind.TwoWithRice };
    private static readonly string[] RecipeKindLabels = { "Món có sẵn", "2 nguyên liệu — có cơm", "2 nguyên liệu — không cơm", "3 nguyên liệu — cơm đầu tiên" };
    private static readonly PoolType[] ToppingTypes = Enum.GetValues(typeof(PoolType)).Cast<PoolType>().Where(SushiRecipeCatalog.IsFoodTopping).ToArray();
    private static readonly string[] ToppingNames = ToppingTypes.Select(SushiRecipeCatalog.IngredientName).ToArray();

    private bool showIcons = true;
    private string dishFolder = "";
    private string ingredientFolder = "";
    private readonly Dictionary<string, Icon> iconCache = new Dictionary<string, Icon>();
    private readonly Dictionary<IconKind, IconIndex> iconIndex = new Dictionary<IconKind, IconIndex>();
    private bool previewPending;
    private string tileWarning;

    private readonly List<Action> pending = new List<Action>();
    private bool dirty;
    private GUIStyle cellStyle, leftButton, countStyle;

    [MenuItem("SushiKhai/Level Editor")]
    private static void Open()
    {
        var w = GetWindow<SushiLevelEditorWindow>("Sushi Level Editor");
        w.minSize = new Vector2(1020, 560);
    }

    [UnityEditor.Callbacks.OnOpenAsset(0)]
    private static bool OnOpenAsset(int instanceId, int line)
    {
        var data = EditorUtility.InstanceIDToObject(instanceId) as SushiLevelData;
        if (data == null) return false;
        var w = GetWindow<SushiLevelEditorWindow>("Sushi Level Editor");
        w.minSize = new Vector2(1020, 560);
        w.level = data;
        w.ResetSelection();
        w.LoadK();
        w.Show();
        return true;
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += Repaint;
        showIcons = EditorPrefs.GetBool(PrefShowIcons, true);
        dishFolder = EditorPrefs.GetString(PrefDishFolder, "");
        ingredientFolder = EditorPrefs.GetString(PrefIngredientFolder, "");
    }

    private void OnDisable() { Undo.undoRedoPerformed -= Repaint; }

    private void OnProjectChange() { ClearIcons(); Repaint(); }

    // ───────────────────────── Icons ─────────────────────────

    private void ClearIcons()
    {
        iconCache.Clear();
        iconIndex.Clear();
        artCatalogLoaded = false;
        artCatalog = null;
    }

    private static string Norm(string s)
    {
        var chars = s.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray();
        return new string(chars);
    }

    private static readonly string[] IconPrefixes = { "icon", "ic", "spr", "sprite", "img", "ui" };

    private static void AddKey(Dictionary<string, string> map, string fileName, string path)
    {
        string n = Norm(fileName);
        if (n.Length == 0) return;
        map.TryAdd(n, path);
        foreach (var p in IconPrefixes)
            if (n.StartsWith(p) && n.Length > p.Length) map.TryAdd(n.Substring(p.Length), path);
    }

    private IconIndex GetIndex(IconKind kind)
    {
        if (iconIndex.TryGetValue(kind, out var idx)) return idx;
        idx = new IconIndex();
        string folder = kind == IconKind.Dish ? dishFolder : ingredientFolder;
        string[] roots = !string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder)
            ? new[] { folder } : new[] { "Assets" };

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", roots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AddKey(idx.textures, System.IO.Path.GetFileNameWithoutExtension(path), path);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", roots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AddKey(idx.prefabs, System.IO.Path.GetFileNameWithoutExtension(path), path);
        }
        iconIndex[kind] = idx;
        return idx;
    }

    private Icon GetIcon(string name, IconKind kind)
    {
        string key = (int)kind + ":" + name;
        if (iconCache.TryGetValue(key, out var icon)) return icon;
        icon = ResolveIcon(name, kind, out bool final);
        if (final) iconCache[key] = icon;
        else previewPending = true;
        return icon;
    }

    private static Icon FromSprite(Sprite s)
    {
        var tex = s.texture;
        var r = s.textureRect;
        return new Icon { tex = tex, uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height) };
    }

    private Icon ResolveIcon(string name, IconKind kind, out bool final)
    {
        final = true;
        string n = Norm(name);

        // Ưu tiên lấy từ SushiArtCatalog (dishImages / ingredientImages).
        var fromCatalog = ResolveFromCatalog(name, kind);
        if (fromCatalog != null) return fromCatalog;

        var idx = GetIndex(kind);

        if (idx.textures.TryGetValue(n, out string path))
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
            Sprite s = sprites.FirstOrDefault(x => Norm(x.name) == n) ?? sprites.FirstOrDefault();
            if (s != null) return FromSprite(s);
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t != null) return new Icon { tex = t, uv = new Rect(0, 0, 1, 1) };
        }

        if (idx.prefabs.TryGetValue(n, out string prefabPath))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (go != null)
            {
                var sr = go.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null && sr.sprite != null) return FromSprite(sr.sprite);

                var preview = AssetPreview.GetAssetPreview(go);
                if (preview != null) return new Icon { tex = preview, uv = new Rect(0, 0, 1, 1) };
                if (AssetPreview.IsLoadingAssetPreview(go.GetInstanceID())) final = false;
            }
        }
        return null;
    }

    // ── SushiArtCatalog: đọc dishImages / ingredientImages bằng SerializedObject (không phụ thuộc API của catalog) ──

    private ScriptableObject artCatalog;
    private bool artCatalogLoaded;

    private ScriptableObject GetArtCatalog()
    {
        if (artCatalogLoaded) return artCatalog;
        artCatalogLoaded = true;
        foreach (string guid in AssetDatabase.FindAssets("t:SushiArtCatalog"))
        {
            artCatalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (artCatalog != null) break;
        }
        return artCatalog;
    }

    private Icon ResolveFromCatalog(string name, IconKind kind)
    {
        var catalog = GetArtCatalog();
        if (catalog == null) return null;
        var prop = new SerializedObject(catalog).FindProperty(kind == IconKind.Dish ? "dishImages" : "ingredientImages");
        if (prop == null || prop.objectReferenceValue == null) return null;

        Type enumType = kind == IconKind.Dish ? typeof(SushiDishId) : typeof(PoolType);
        if (!Enum.TryParse(enumType, name, out object parsed)) return null;
        int value = Convert.ToInt32(parsed);

        UnityEngine.Object found = FindImageInMap(prop.objectReferenceValue, name, value, enumType);
        if (found is Sprite sp) return FromSprite(sp);
        if (found is Texture2D tex) return new Icon { tex = tex, uv = new Rect(0, 0, 1, 1) };
        return null;
    }

    private static bool KeyMatches(SerializedProperty e, string name, int value)
    {
        switch (e.propertyType)
        {
            case SerializedPropertyType.Enum:
                int i = e.enumValueIndex;
                return i >= 0 && i < e.enumNames.Length && Norm(e.enumNames[i]) == Norm(name);
            case SerializedPropertyType.String:
                return Norm(e.stringValue ?? "") == Norm(name);
            case SerializedPropertyType.Integer:
                return e.intValue == value;
        }
        return false;
    }

    private static UnityEngine.Object AsImage(SerializedProperty p)
    {
        if (p.propertyType != SerializedPropertyType.ObjectReference) return null;
        var o = p.objectReferenceValue;
        return o is Sprite || o is Texture2D ? o : null;
    }

    private static UnityEngine.Object FindImageInMap(UnityEngine.Object map, string name, int value, Type enumType)
    {
        var so = new SerializedObject(map);
        var it = so.GetIterator();
        if (!it.NextVisible(true)) return null;

        var imageArrays = new List<SerializedProperty>();
        var keyArrays = new List<SerializedProperty>();
        string n = Norm(name);

        do
        {
            if (it.propertyPath == "m_Script" || !it.isArray || it.propertyType != SerializedPropertyType.Generic) continue;
            if (it.arraySize == 0) continue;
            var first = it.GetArrayElementAtIndex(0);

            if (first.propertyType == SerializedPropertyType.ObjectReference) imageArrays.Add(it.Copy());
            else if (first.propertyType == SerializedPropertyType.Enum || first.propertyType == SerializedPropertyType.String
                     || first.propertyType == SerializedPropertyType.Integer) keyArrays.Add(it.Copy());
            else if (first.propertyType == SerializedPropertyType.Generic)
            {
                // Mảng struct: mỗi phần tử có key (enum/string/int) + sprite.
                for (int i = 0; i < it.arraySize; i++)
                {
                    var el = it.GetArrayElementAtIndex(i);
                    var child = el.Copy();
                    var end = el.GetEndProperty();
                    bool match = false;
                    UnityEngine.Object img = null;
                    bool enter = child.NextVisible(true);
                    while (enter && !SerializedProperty.EqualContents(child, end))
                    {
                        if (child.propertyType == SerializedPropertyType.Integer)
                        {
                            string p = child.name.ToLowerInvariant();
                            if ((p.Contains("type") || p.Contains("id") || p.Contains("key") || p.Contains("food")
                                 || p.Contains("dish") || p.Contains("ingredient")) && child.intValue == value) match = true;
                        }
                        else if (KeyMatches(child, name, value)) match = true;
                        if (img == null) img = AsImage(child);
                        enter = child.NextVisible(false);
                    }
                    if (match && img != null) return img;
                }
            }
        } while (it.NextVisible(false));

        if (imageArrays.Count == 0) return null;
        var images = imageArrays[0];

        // Mảng key song song với mảng ảnh.
        foreach (var keys in keyArrays)
        {
            if (keys.arraySize != images.arraySize) continue;
            for (int i = 0; i < keys.arraySize; i++)
                if (KeyMatches(keys.GetArrayElementAtIndex(i), name, value))
                    return AsImage(images.GetArrayElementAtIndex(i));
        }

        // Chỉ có mảng ảnh: ưu tiên khớp theo tên sprite, sau đó theo index enum.
        for (int i = 0; i < images.arraySize; i++)
        {
            var img = AsImage(images.GetArrayElementAtIndex(i));
            if (img != null && Norm(img.name) == n) return img;
        }
        if (value >= 0 && value < images.arraySize)
        {
            var img = AsImage(images.GetArrayElementAtIndex(value));
            if (img != null) return img;
        }
        int pos = Array.IndexOf(Enum.GetValues(enumType), Enum.ToObject(enumType, value));
        if (pos >= 0 && pos < images.arraySize) return AsImage(images.GetArrayElementAtIndex(pos));
        return null;
    }

    /// Vẽ icon (giữ tỉ lệ). Trả về true nếu có icon để vẽ.
    private bool DrawIcon(Rect r, string name, IconKind kind)
    {
        if (!showIcons || Event.current.type != EventType.Repaint) return false;
        var ic = GetIcon(name, kind);
        if (ic == null || ic.tex == null) return false;

        float pw = ic.uv.width * ic.tex.width, ph = ic.uv.height * ic.tex.height;
        if (pw <= 0 || ph <= 0) return false;
        float scale = Mathf.Min(r.width / pw, r.height / ph);
        var fit = new Rect(r.x + (r.width - pw * scale) * 0.5f, r.y + (r.height - ph * scale) * 0.5f, pw * scale, ph * scale);
        GUI.DrawTextureWithTexCoords(fit, ic.tex, ic.uv, true);
        return true;
    }

    private void DrawCount(Rect r, int amount)
    {
        if (Event.current.type != EventType.Repaint || amount <= 1) return;
        string t = "×" + amount;
        var shadow = new Rect(r.x + 1, r.y + 1, r.width, r.height);
        var old = countStyle.normal.textColor;
        countStyle.normal.textColor = Color.black;
        GUI.Label(shadow, t, countStyle);
        countStyle.normal.textColor = Color.white;
        GUI.Label(r, t, countStyle);
        countStyle.normal.textColor = old;
    }

    /// Icon + (nếu thiếu icon thì vẽ tên rút gọn) trong ô vuông size×size ở layout hiện tại.
    private void LayoutIcon(string name, IconKind kind, float size, int amount = 1)
    {
        Rect r = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
        if (!showIcons || Event.current.type != EventType.Repaint) return;
        if (!DrawIcon(r, name, kind))
        {
            EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.15f));
            GUI.Label(r, name.Length > 4 ? name.Substring(0, 4) : name, EditorStyles.centeredGreyMiniLabel);
        }
        DrawCount(r, amount);
    }

    private void DrawIconSettings()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Hình ảnh (icon)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        bool show = EditorGUILayout.Toggle("Hiển thị icon", showIcons);
        string dish = FolderField("Thư mục icon món (trống = cả project)", dishFolder);
        string ing = FolderField("Thư mục icon nguyên liệu (trống = cả project)", ingredientFolder);
        if (EditorGUI.EndChangeCheck())
        {
            showIcons = show; dishFolder = dish; ingredientFolder = ing;
            EditorPrefs.SetBool(PrefShowIcons, showIcons);
            EditorPrefs.SetString(PrefDishFolder, dishFolder);
            EditorPrefs.SetString(PrefIngredientFolder, ingredientFolder);
            ClearIcons();
        }

        if (GUILayout.Button("Làm mới icon")) ClearIcons();
        EditorGUILayout.LabelField("Icon lấy từ SushiArtCatalog; nếu thiếu thì tìm theo tên file trùng tên enum (thư mục bên trên).", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();
    }

    private static string FolderField(string label, string current)
    {
        var obj = string.IsNullOrEmpty(current) ? null : AssetDatabase.LoadAssetAtPath<DefaultAsset>(current);
        var picked = (DefaultAsset)EditorGUILayout.ObjectField(label, obj, typeof(DefaultAsset), false);
        if (picked == null) return "";
        string path = AssetDatabase.GetAssetPath(picked);
        return AssetDatabase.IsValidFolder(path) ? path : current;
    }

    // ───────────────────────── GUI ─────────────────────────

    private void OnGUI()
    {
        if (cellStyle == null)
        {
            cellStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 9,
                clipping = TextClipping.Clip
            };
            cellStyle.normal.textColor = Color.black;
            leftButton = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft };
            countStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.LowerRight, fontSize = 10 };
        }

        Event e = Event.current;
        pending.Clear();
        dirty = false;

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUI.BeginChangeCheck();
        var picked = (SushiLevelData)EditorGUILayout.ObjectField(level, typeof(SushiLevelData), false, GUILayout.Width(300));
        if (EditorGUI.EndChangeCheck()) { level = picked; ResetSelection(); LoadK(); }
        GUILayout.FlexibleSpace();
        if (level != null)
        {
            if (GUILayout.Button("Đồng bộ board", EditorStyles.toolbarButton)) pending.Add(SyncBoard);
            if (GUILayout.Button("Tạo lại board (seed)", EditorStyles.toolbarButton)) pending.Add(RebuildBoard);
        }
        EditorGUILayout.EndHorizontal();

        if (level == null)
        {
            EditorGUILayout.HelpBox("Chọn một SushiLevelData (hoặc double-click asset) để chỉnh sửa.", MessageType.Info);
            return;
        }

        if (e.type != EventType.Repaint && e.type != EventType.Layout)
            Undo.RecordObject(level, "Sushi Level Edit");
        if (level.customers == null) level.customers = new List<CustomerOrderData>();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(600));
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
        DrawSettings();
        EditorGUILayout.Space(6);
        DrawCustomers();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        DrawBoard();
        EditorGUILayout.Space(4);
        DrawTileInspector();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        DrawValidation();

        if (EditorGUI.EndChangeCheck()) dirty = true;

        if (e.rawType == EventType.MouseUp && dragCol >= 0)
        {
            dragCol = dragRow = -1;
            Repaint();
        }

        if (pending.Count > 0)
        {
            dirty = true;
            foreach (var a in pending.ToArray()) a();
            pending.Clear();
            Repaint();
        }

        if (dirty) EditorUtility.SetDirty(level);

        // Preview prefab được Unity nạp bất đồng bộ -> vẽ lại cho đến khi xong.
        if (previewPending && e.type == EventType.Repaint) { previewPending = false; Repaint(); }
    }

    private void DrawSettings()
    {
        showSettings = EditorGUILayout.Foldout(showSettings, "Cài đặt level", true);
        if (!showSettings) return;

        EditorGUILayout.BeginVertical("box");
        level.TypeLevel = (TypeLevel)EditorGUILayout.EnumPopup("Type Level", level.TypeLevel);
        level.number = Mathf.Max(1, EditorGUILayout.IntField("Số level", level.number));
        level.levelName = EditorGUILayout.TextField("Tên level", level.levelName);
        EditorGUILayout.LabelField("Hint");
        level.hint = EditorGUILayout.TextArea(level.hint, GUILayout.MinHeight(34));
        level.beltSlots = Mathf.Max(6, EditorGUILayout.IntField("Belt slots", level.beltSlots));
        level.loopsPerSecond = EditorGUILayout.Slider("Vòng/giây", level.loopsPerSecond, 0.02f, 0.3f);

        EditorGUI.BeginChangeCheck();
        level.numberColumns = EditorGUILayout.IntSlider("Số cột board", level.numberColumns, 1, 6);
        if (EditorGUI.EndChangeCheck()) pending.Add(EnsureColumns);

        level.boardSeed = EditorGUILayout.IntField("Board seed", level.boardSeed);
        level.shuffleFoods = EditorGUILayout.Toggle("Shuffle foods", level.shuffleFoods);
        level.maxIngredientsPerTile = EditorGUILayout.IntSlider("Max ×/ô (ô mới)", level.maxIngredientsPerTile, 1, 3);
        EditorGUILayout.EndVertical();

        DrawIconSettings();
        DrawDifficulty();
    }

    private void DrawDifficulty()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Độ khó theo hệ số K", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        kValue = EditorGUILayout.IntSlider("Hệ số K (1 dễ – 10 khó)", kValue, 1, 10);
        if (EditorGUI.EndChangeCheck()) SaveK();

        simSeats = EditorGUILayout.IntSlider("Khách phục vụ cùng lúc (mô phỏng)", simSeats, 1, 6);
        annealIterations = EditorGUILayout.IntSlider("Số vòng tối ưu", annealIterations, 200, 5000);
        EditorGUILayout.LabelField($"Mục tiêu: người chơi hay nhầm ~{SushiDifficulty.PlayerNoise:P0} thua ≈ {SushiDifficulty.TargetLoss(kValue):P0}, nhưng vẫn giải được.", EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sắp xếp board theo K")) pending.Add(ArrangeByK);
        if (GUILayout.Button("Chấm độ khó hiện tại")) pending.Add(EvaluateCurrent);
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(diffReport)) EditorGUILayout.HelpBox(diffReport, diffReportType);
        EditorGUILayout.EndVertical();
    }

    private string KKey() => "SushiLevelEditor.K." + AssetDatabase.GetAssetPath(level);
    private void LoadK() { if (level != null) kValue = Mathf.Clamp(EditorPrefs.GetInt(KKey(), 5), 1, 10); diffReport = ""; }
    private void SaveK() { if (level != null) EditorPrefs.SetInt(KKey(), kValue); }

    private void ArrangeByK()
    {
        if (!BoardReady()) { SetReport("Board chưa khởi tạo. Bấm 'Tạo lại board (seed)' trước.", MessageType.Warning); return; }
        SushiDifficulty.Result r;
        try
        {
            r = SushiDifficulty.Arrange(level, kValue, simSeats, annealIterations,
                p => EditorUtility.DisplayCancelableProgressBar("Sushi Level", "Đang sắp xếp theo K...", p));
        }
        finally { EditorUtility.ClearProgressBar(); }

        if (r.ok) ResetSelection(keepCustomer: true);
        SetReport(FormatReport(r), r.ok ? MessageType.Info : MessageType.Error);
    }

    private void EvaluateCurrent()
    {
        if (!BoardReady()) { SetReport("Board chưa khởi tạo.", MessageType.Warning); return; }
        var r = SushiDifficulty.Evaluate(level, kValue, simSeats);
        SetReport(FormatReport(r), r.ok ? MessageType.Info : MessageType.Error);
    }

    private void SetReport(string text, MessageType type) { diffReport = text; diffReportType = type; }

    private string FormatReport(SushiDifficulty.Result r)
    {
        if (!r.ok) return r.message;
        string belt = r.minBelt >= 0 ? $"{r.minBelt}/{level.beltSlots}" : $">{level.beltSlots}";
        return $"{r.message}\nGiải được: {(r.solvable ? "có" : "KHÔNG")}   Tỉ lệ thua ước lượng: {r.estimatedLoss:P0} (mục tiêu K={kValue}: {r.targetLoss:P0})" +
               $"\nBelt tối thiểu để thắng: {belt}" + (r.iterations > 0 ? $"   Số vòng: {r.iterations}" : "");
    }

    // ───────────────────────── Customers ─────────────────────────

    private void DrawCustomers()
    {
        EditorGUILayout.LabelField($"Khách ({level.customers.Count}) – thứ tự từ trên xuống", EditorStyles.boldLabel);

        for (int i = 0; i < level.customers.Count; i++)
        {
            int index = i;
            if (level.customers[i] == null) level.customers[i] = new CustomerOrderData();
            CustomerOrderData cu = level.customers[i];
            if (cu.dishes == null) cu.dishes = new List<DishOrderData>();
            bool sel = selCustomer == i;

            Color old = GUI.backgroundColor;
            if (sel) GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = old;

            EditorGUILayout.BeginHorizontal();
            if (showIcons)
            {
                foreach (var d in cu.dishes)
                    if (d != null) DrawDishIcon(d);
            }
            if (GUILayout.Button($"#{i + 1}{(cu.rushCustomer ? " [RUSH]" : "")}  {Summary(cu)}", leftButton, GUILayout.Height(28)))
                selCustomer = sel ? -1 : i;
            GUI.enabled = i > 0;
            if (GUILayout.Button("↑", EditorStyles.miniButtonLeft, GUILayout.Width(22), GUILayout.Height(28))) pending.Add(() => MoveCustomer(index, -1));
            GUI.enabled = i < level.customers.Count - 1;
            if (GUILayout.Button("↓", EditorStyles.miniButtonMid, GUILayout.Width(22), GUILayout.Height(28))) pending.Add(() => MoveCustomer(index, 1));
            GUI.enabled = true;
            if (GUILayout.Button("C", EditorStyles.miniButtonMid, GUILayout.Width(22), GUILayout.Height(28))) pending.Add(() => DuplicateCustomer(index));
            if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(22), GUILayout.Height(28))) pending.Add(() => RemoveCustomer(index));
            EditorGUILayout.EndHorizontal();

            if (sel)
            {
                cu.rushCustomer = EditorGUILayout.Toggle("Rush", cu.rushCustomer);
                if (cu.rushCustomer)
                    cu.rushSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("Rush giây (0 = mặc định)", cu.rushSeconds));

                for (int j = 0; j < cu.dishes.Count; j++)
                {
                    int dj = j;
                    if (cu.dishes[j] == null) cu.dishes[j] = new DishOrderData();
                    DishOrderData d = cu.dishes[j];
                    EditorGUILayout.BeginVertical("box");
                    EditorGUI.BeginChangeCheck();
                    DrawRecipeInputs(d);
                    EditorGUILayout.BeginHorizontal();
                    d.amount = Mathf.Max(1, EditorGUILayout.IntField("Số phần", d.amount));
                    GUI.enabled = cu.dishes.Count > 1;
                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(28)))
                        pending.Add(() => { cu.dishes.RemoveAt(dj); SyncBoard(); });
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck() && cu.ValidateOrders() == null) pending.Add(SyncBoard);
                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("+ Món", EditorStyles.miniButton))
                    pending.Add(() => { cu.dishes.Add(newOrder.Copy()); if (cu.ValidateOrders() == null) SyncBoard(); });

                DrawIngredientIcons(cu);
                EditorGUILayout.LabelField("Nguyên liệu: " + IngredientText(cu), EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(2);
        EditorGUILayout.BeginVertical("box");
        DrawRecipeInputs(newOrder);
        newOrder.amount = Mathf.Max(1, EditorGUILayout.IntField("Số phần", newOrder.amount));
        GUI.enabled = newOrder.TryGetRecipe(out _, out _);
        if (GUILayout.Button("+ Thêm khách", GUILayout.Width(100), GUILayout.Height(28)))
        {
            DishOrderData order = newOrder.Copy();
            pending.Add(() =>
            {
                level.customers.Add(new CustomerOrderData
                {
                    dishes = new List<DishOrderData> { order }
                });
                selCustomer = level.customers.Count - 1;
                SyncBoard();
            });
        }
        GUI.enabled = true;
        EditorGUILayout.EndVertical();
    }

    private static void DrawRecipeInputs(DishOrderData order)
    {
        order.recipeKind = (SushiRecipeKind)EditorGUILayout.Popup("Công thức", (int)order.recipeKind, RecipeKindLabels);
        if (order.recipeKind == SushiRecipeKind.Catalog)
            order.dish = (SushiDishId)EditorGUILayout.EnumPopup("Món", order.dish);
        else
        {
            if (order.recipeKind != SushiRecipeKind.TwoWithoutRice)
                EditorGUILayout.LabelField("Nguyên liệu 1", "Cơm (nền)");
            order.firstIngredient = ToppingPopup(order.recipeKind == SushiRecipeKind.TwoWithoutRice ? "Nguyên liệu 1" : "Nguyên liệu 2", order.firstIngredient);
            if (order.recipeKind != SushiRecipeKind.TwoWithRice)
                order.secondIngredient = ToppingPopup(order.recipeKind == SushiRecipeKind.TwoWithoutRice ? "Nguyên liệu 2" : "Nguyên liệu 3", order.secondIngredient);
        }
        if (!order.TryGetRecipe(out _, out string error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
    }

    private static PoolType ToppingPopup(string label, PoolType value)
    {
        int index = Array.IndexOf(ToppingTypes, value);
        int selected = EditorGUILayout.Popup(label, index, ToppingNames);
        return selected >= 0 && selected < ToppingTypes.Length ? ToppingTypes[selected] : value;
    }

    private void DrawDishIcon(DishOrderData order)
    {
        if (order.recipeKind == SushiRecipeKind.Catalog) LayoutIcon(order.dish.ToString(), IconKind.Dish, 28f, order.amount);
        else if (order.TryGetRecipe(out var recipe, out _))
            foreach (PoolType type in recipe.Ingredients) LayoutIcon(type.ToString(), IconKind.Ingredient, 22f);
    }

    private static string DishLabel(DishOrderData order) => order.TryGetRecipe(out var recipe, out _) ? recipe.Name : "Công thức chưa hợp lệ";

    private void DrawIngredientIcons(CustomerOrderData cu)
    {
        if (!showIcons) return;
        var counts = new Dictionary<PoolType, int>();
        var order = new List<PoolType>();
        AccumulateIngredients(cu, counts, order);
        if (order.Count == 0) return;

        EditorGUILayout.BeginHorizontal();
        foreach (var t in order) LayoutIcon(t.ToString(), IconKind.Ingredient, 26f, counts[t]);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private static string Summary(CustomerOrderData cu)
    {
        if (cu.dishes == null || cu.dishes.Count == 0) return "(trống)";
        return string.Join(", ", cu.dishes.Where(d => d != null).Select(d => d.amount > 1 ? $"{DishLabel(d)} ×{d.amount}" : DishLabel(d)));
    }

    private static string IngredientText(CustomerOrderData cu)
    {
        var counts = new Dictionary<PoolType, int>();
        var order = new List<PoolType>();
        AccumulateIngredients(cu, counts, order);
        return counts.Count == 0 ? "-" : string.Join(", ", order.Select(t => counts[t] > 1 ? $"{t}×{counts[t]}" : t.ToString()));
    }

    private void MoveCustomer(int i, int dir)
    {
        int j = i + dir;
        if (j < 0 || j >= level.customers.Count) return;
        var t = level.customers[i]; level.customers[i] = level.customers[j]; level.customers[j] = t;
        if (selCustomer == i) selCustomer = j; else if (selCustomer == j) selCustomer = i;
    }

    private void DuplicateCustomer(int i)
    {
        var src = level.customers[i];
        var copy = new CustomerOrderData
        {
            rushCustomer = src.rushCustomer,
            rushSeconds = src.rushSeconds,
            dishes = src.dishes.Select(d => d?.Copy()).ToList()
        };
        level.customers.Insert(i + 1, copy);
        selCustomer = i + 1;
        SyncBoard();
    }

    private void RemoveCustomer(int i)
    {
        level.customers.RemoveAt(i);
        if (selCustomer >= level.customers.Count) selCustomer = -1;
        SyncBoard();
    }

    // ───────────────────────── Board drawing ─────────────────────────

    private bool BoardReady()
    {
        int n = Mathf.Clamp(level.numberColumns, 1, 6);
        if (level.boardColumns == null || level.boardColumns.Count != n) return false;
        int tiles = 0;
        foreach (var c in level.boardColumns)
        {
            if (c == null || c.foods == null) return false;
            tiles += c.foods.Count;
        }
        return !(tiles == 0 && level.customers.Count > 0);
    }

    private void DrawBoard()
    {
        EditorGUILayout.LabelField("Board – kéo thả ô để đổi chỗ (thả vào ô trống = chuyển xuống cuối cột)", EditorStyles.boldLabel);

        if (!BoardReady())
        {
            EditorGUILayout.HelpBox("Board chưa khởi tạo / lệch số cột.", MessageType.Warning);
            if (GUILayout.Button("Khởi tạo board từ khách")) pending.Add(RebuildBoard);
            return;
        }

        var board = level.boardColumns;
        int cols = board.Count;
        int rows = 1;
        foreach (var c in board) rows = Mathf.Max(rows, c.foods.Count + 1);

        HashSet<PoolType> highlight = null;
        if (selCustomer >= 0 && selCustomer < level.customers.Count)
        {
            var cnt = new Dictionary<PoolType, int>();
            AccumulateIngredients(level.customers[selCustomer], cnt, new List<PoolType>());
            highlight = new HashSet<PoolType>(cnt.Keys);
        }

        float w = HeaderW + cols * (CellW + Gap);
        float h = HeaderH + rows * (CellH + Gap);

        boardScroll = EditorGUILayout.BeginScrollView(boardScroll, GUILayout.Height(Mathf.Max(220, position.height * 0.52f)));
        Rect area = GUILayoutUtility.GetRect(w, h);
        Event e = Event.current;

        for (int c = 0; c < cols; c++)
        {
            var hr = new Rect(area.x + HeaderW + c * (CellW + Gap), area.y, CellW, HeaderH - 2);
            if (e.type == EventType.Repaint)
                GUI.Label(hr, $"Cột {c + 1} ({board[c].foods.Count})", EditorStyles.centeredGreyMiniLabel);
        }
        for (int r = 0; r < rows; r++)
        {
            var rr = new Rect(area.x, area.y + HeaderH + r * (CellH + Gap), HeaderW - 4, CellH);
            if (e.type == EventType.Repaint) GUI.Label(rr, $"H{r + 1}", EditorStyles.centeredGreyMiniLabel);
        }

        int dropC = -1, dropR = -1;
        for (int c = 0; c < cols; c++)
        {
            var foods = board[c].foods;
            for (int r = 0; r < rows; r++)
            {
                var rc = new Rect(area.x + HeaderW + c * (CellW + Gap), area.y + HeaderH + r * (CellH + Gap), CellW, CellH);
                bool has = r < foods.Count;

                switch (e.type)
                {
                    case EventType.Repaint:
                        DrawCell(rc, has ? foods[r] : null, c == selCol && r == selRow, highlight);
                        break;
                    case EventType.MouseDown:
                        if (e.button == 0 && has && rc.Contains(e.mousePosition))
                        {
                            OnCellDown(c, r);
                            e.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (dragCol >= 0 && rc.Contains(e.mousePosition) && (c != dragCol || r != dragRow) && r <= foods.Count)
                        { dropC = c; dropR = r; }
                        break;
                }
            }
        }

        if (e.type == EventType.MouseDrag && dragCol >= 0) { Repaint(); }
        if (e.type == EventType.Repaint && dragCol >= 0)
        {
            var g = new Rect(e.mousePosition.x - CellW / 2, e.mousePosition.y - CellH / 2, CellW, CellH);
            var f = FoodAt(dragCol, dragRow);
            if (f != null)
            {
                Color col = TypeColor(f.typeFood); col.a = 0.75f;
                EditorGUI.DrawRect(g, col);
                var gi = new Rect(g.x + 3, g.y + (CellH - CellIcon) * 0.5f, CellIcon, CellIcon);
                if (DrawIcon(gi, f.typeFood.ToString(), IconKind.Ingredient))
                    GUI.Label(new Rect(g.x + CellIcon + 6, g.y, g.width - CellIcon - 8, g.height), $"{f.typeFood}\n×{f.amount}", cellStyle);
                else
                    GUI.Label(g, f.typeFood.ToString(), cellStyle);
            }
        }

        if (dropC >= 0)
        {
            int fc = dragCol, fr = dragRow, tc = dropC, tr = dropR;
            pending.Add(() => MoveTile(fc, fr, tc, tr));
            dragCol = dragRow = -1;
        }

        EditorGUILayout.EndScrollView();
    }

    private void OnCellDown(int c, int r)
    {
        if (linkMode && Selected() != null && !(c == selCol && r == selRow))
        {
            int fc = selCol, fr = selRow;
            pending.Add(() => LinkTiles(fc, fr, c, r));
            linkMode = false;
            return;
        }
        selCol = c; selRow = r;
        tileWarning = null;
        dragCol = c; dragRow = r;
        Repaint();
    }

    private void DrawCell(Rect rc, FoodData f, bool selected, HashSet<PoolType> highlight)
    {
        if (f == null)
        {
            EditorGUI.DrawRect(rc, new Color(0f, 0f, 0f, 0.12f));
            return;
        }

        EditorGUI.DrawRect(rc, TypeColor(f.typeFood));
        Element el = GetElement(f);
        string badge = "";
        Color border = Color.clear;
        switch (el)
        {
            case Element.Ice: badge = $"ICE{f.iceLayers}"; border = new Color(0.1f, 0.7f, 1f); break;
            case Element.Hidden: badge = "HIDDEN"; border = Color.gray; break;
            case Element.Link: badge = $"LINK{(f.linkId > 0 ? f.linkId : f.lockId)}"; border = new Color(0.9f, 0.2f, 0.9f); break;
            case Element.Tray: badge = $"TRAY{f.trayId}"; border = new Color(1f, 0.55f, 0.1f); break;
        }

        // Icon nguyên liệu (nằm dưới lớp phủ Hidden để vẫn nhìn thấy mờ mờ).
        var iconRect = new Rect(rc.x + 3, rc.y + (rc.height - CellIcon) * 0.5f, CellIcon, CellIcon);
        bool hasIcon = DrawIcon(iconRect, f.typeFood.ToString(), IconKind.Ingredient);
        if (hasIcon) DrawCount(iconRect, f.amount);

        if (el == Element.Hidden) EditorGUI.DrawRect(rc, new Color(0.3f, 0.3f, 0.3f, 0.45f));

        if (highlight != null && highlight.Contains(f.typeFood)) DrawBorder(rc, new Color(0.1f, 0.8f, 0.1f), 1f);
        if (border.a > 0) DrawBorder(rc, border, 3f);
        if (selected) DrawBorder(rc, Color.yellow, 3f);

        if (hasIcon)
        {
            var textRect = new Rect(rc.x + CellIcon + 6, rc.y, rc.width - CellIcon - 8, rc.height);
            GUI.Label(textRect, $"{f.typeFood}\n{badge}", cellStyle);
        }
        else
        {
            GUI.Label(rc, $"{f.typeFood}\n×{f.amount}  {badge}", cellStyle);
        }
    }

    private static void DrawBorder(Rect r, Color c, float t)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), c);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
        EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), c);
        EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
    }

    private static Color TypeColor(PoolType t)
    {
        float hue = (Convert.ToInt32(t) * 0.61803f) % 1f;
        return Color.HSVToRGB(hue, 0.35f, 0.97f);
    }

    // ───────────────────────── Tile inspector ─────────────────────────

    private void DrawTileInspector()
    {
        EditorGUILayout.BeginVertical("box");
        FoodData f = Selected();
        if (f == null)
        {
            EditorGUILayout.LabelField("Click một ô trên board để chỉnh nguyên liệu / element.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if (showIcons) LayoutIcon(f.typeFood.ToString(), IconKind.Ingredient, 32f);
        EditorGUILayout.LabelField($"Ô đang chọn: Cột {selCol + 1}, Hàng {selRow + 1}", EditorStyles.boldLabel, GUILayout.Height(32));
        EditorGUILayout.EndHorizontal();

        f.typeFood = (PoolType)EditorGUILayout.EnumPopup("Nguyên liệu", f.typeFood);
        int wanted = EditorGUILayout.IntSlider("Số lượng (×)", f.amount, 1, 3);
        if (wanted != f.amount)
        {
            int wc = selCol, wr = selRow;
            pending.Add(() => SetTileAmount(wc, wr, wanted));
        }
        {
            var exp = Expected(new List<PoolType>());
            exp.TryGetValue(f.typeFood, out int want);
            CountBoard().TryGetValue(f.typeFood, out int have);
            EditorGUILayout.LabelField($"Tổng {f.typeFood}: board {have} / đơn {want}", EditorStyles.miniLabel);
        }
        if (!string.IsNullOrEmpty(tileWarning)) EditorGUILayout.HelpBox(tileWarning, MessageType.Warning);

        Element cur = GetElement(f);
        var next = (Element)EditorGUILayout.EnumPopup("Element", cur);
        if (next != cur)
        {
            if (next == Element.Tray) MakeTray(selCol, selRow, traySize);
            else ApplyElement(f, next);
            cur = GetElement(f);
        }

        switch (cur)
        {
            case Element.Ice:
                f.iceLayers = EditorGUILayout.IntSlider("Số lớp băng", Mathf.Max(1, f.iceLayers), 1, SushiLevelData.MaximumIceLayers);
                break;
            case Element.Link:
                EditorGUILayout.LabelField("Link ID", (f.linkId > 0 ? f.linkId : f.lockId).ToString());
                linkMode = GUILayout.Toggle(linkMode, linkMode ? "Đang chờ: click ô muốn nối..." : "Nối với ô khác", "Button");
                break;
            case Element.Tray:
                traySize = EditorGUILayout.IntSlider("Số ngăn khay", traySize, 2, 3);
                if (GUILayout.Button($"Tạo lại khay {traySize} ngăn từ ô này xuống")) MakeTray(selCol, selRow, traySize);
                EditorGUILayout.LabelField("Tray ID", f.trayId.ToString());
                break;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("← Cột")) pending.Add(() => NudgeSelected(-1, 0));
        if (GUILayout.Button("→ Cột")) pending.Add(() => NudgeSelected(1, 0));
        if (GUILayout.Button("↑ Hàng")) pending.Add(() => NudgeSelected(0, -1));
        if (GUILayout.Button("↓ Hàng")) pending.Add(() => NudgeSelected(0, 1));
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Xóa ô này (sau đó bấm 'Đồng bộ board' để sinh lại nếu cần)"))
            pending.Add(DeleteSelected);

        EditorGUILayout.EndVertical();
    }

    // ───────────────────────── Tile operations ─────────────────────────

    private FoodData Selected() => FoodAt(selCol, selRow);

    private FoodData FoodAt(int c, int r)
    {
        var b = level != null ? level.boardColumns : null;
        if (b == null || c < 0 || c >= b.Count || b[c] == null || b[c].foods == null) return null;
        return r >= 0 && r < b[c].foods.Count ? b[c].foods[r] : null;
    }

    private IEnumerable<FoodData> AllTiles()
    {
        foreach (var c in level.boardColumns)
            foreach (var f in c.foods)
                if (f != null) yield return f;
    }

    private void Warn(string msg)
    {
        tileWarning = msg;
        ShowNotification(new GUIContent(msg));
    }

    /// Đổi số lượng (×) của một ô và bù/trừ vào các ô cùng loại để tổng nguyên liệu vẫn khớp đơn khách.
    private void SetTileAmount(int c, int r, int wanted)
    {
        var f = FoodAt(c, r);
        if (f == null) return;
        wanted = Mathf.Clamp(wanted, 1, 3);
        if (wanted == f.amount) return;

        var expectedMap = Expected(new List<PoolType>());
        expectedMap.TryGetValue(f.typeFood, out int expected);

        if (wanted > expected)
        {
            Warn($"Không đủ nguyên liệu: đơn khách chỉ có {expected} {f.typeFood}, không thể ghi ×{wanted}.");
            return;
        }

        var others = new List<(int col, int row, FoodData tile)>();
        int othersSum = 0;
        for (int cc = 0; cc < level.boardColumns.Count; cc++)
        {
            var foods = level.boardColumns[cc].foods;
            for (int rr = 0; rr < foods.Count; rr++)
            {
                var t = foods[rr];
                if (t == null || t == f || t.typeFood != f.typeFood) continue;
                others.Add((cc, rr, t));
                othersSum += t.amount;
            }
        }

        int diff = (expected - wanted) - othersSum; // <0: cần bớt ở ô khác, >0: cần thêm vào ô khác
        if (diff < 0)
        {
            int need = -diff;
            int removable = others.Sum(x => GetElement(x.tile) == Element.Normal ? x.tile.amount : x.tile.amount - 1);
            if (need > removable)
            {
                Warn($"Không thể ghi ×{wanted}: cần bớt {need} {f.typeFood} ở ô khác nhưng chỉ bù được {removable} (ô có element không bị xóa).");
                return;
            }
            // Bước 1: giảm số lượng các ô khác (nhiều nhất trước) xuống tối thiểu ×1.
            foreach (var o in others.OrderByDescending(x => x.tile.amount).ToList())
            {
                if (need <= 0) break;
                int take = Mathf.Min(o.tile.amount - 1, need);
                o.tile.amount -= take;
                need -= take;
            }
            // Bước 2: nếu vẫn dư thì xóa các ô thường (từ cuối lên).
            for (int i = others.Count - 1; i >= 0 && need > 0; i--)
            {
                var o = others[i];
                if (GetElement(o.tile) != Element.Normal) continue;
                need -= o.tile.amount;
                level.boardColumns[o.col].foods.Remove(o.tile);
            }
        }
        else if (diff > 0)
        {
            int add = diff;
            // Bước 1: cộng vào các ô thường cùng loại (tối đa ×3).
            foreach (var o in others)
            {
                if (add <= 0) break;
                if (GetElement(o.tile) != Element.Normal) continue;
                int take = Mathf.Min(3 - o.tile.amount, add);
                o.tile.amount += take;
                add -= take;
            }
            // Bước 2: còn dư thì sinh ô mới vào cột ngắn nhất.
            int per = Mathf.Clamp(level.maxIngredientsPerTile, 1, 3);
            while (add > 0)
            {
                int amt = Mathf.Min(per, add);
                AddToShortest(new FoodData { typeFood = f.typeFood, amount = amt });
                add -= amt;
            }
        }

        f.amount = wanted;
        tileWarning = null;

        // Vị trí ô đang chọn có thể đổi nếu có ô bị xóa.
        selCol = selRow = -1;
        for (int cc = 0; cc < level.boardColumns.Count; cc++)
        {
            int idx = level.boardColumns[cc].foods.IndexOf(f);
            if (idx >= 0) { selCol = cc; selRow = idx; break; }
        }
    }

    private void MoveTile(int c1, int r1, int c2, int r2)
    {
        var a = FoodAt(c1, r1);
        if (a == null || c2 < 0 || c2 >= level.boardColumns.Count) return;
        var listA = level.boardColumns[c1].foods;
        var listB = level.boardColumns[c2].foods;
        if (r2 < listB.Count)
        {
            var b = listB[r2];
            listA[r1] = b; listB[r2] = a;
            selCol = c2; selRow = r2;
        }
        else
        {
            listA.RemoveAt(r1);
            listB.Add(a);
            selCol = c2; selRow = listB.Count - 1;
        }
    }

    private void NudgeSelected(int dc, int dr)
    {
        if (Selected() == null) return;
        if (dr != 0)
        {
            int r2 = selRow + dr;
            if (r2 < 0 || r2 >= level.boardColumns[selCol].foods.Count) return;
            MoveTile(selCol, selRow, selCol, r2);
        }
        else
        {
            int c2 = selCol + dc;
            if (c2 < 0 || c2 >= level.boardColumns.Count) return;
            MoveTile(selCol, selRow, c2, selRow);
        }
    }

    private void DeleteSelected()
    {
        var f = Selected();
        if (f == null) return;
        ClearElement(f);
        level.boardColumns[selCol].foods.RemoveAt(selRow);
        selCol = selRow = -1;
    }

    private static Element GetElement(FoodData f)
    {
        if (f.trayId > 0 || f.foodState == FoodState.RigidTray) return Element.Tray;
        if (f.linkId > 0 || f.lockId > 0 || f.foodState == FoodState.Lock) return Element.Link;
        if (f.iceLayers > 0 || f.foodState == FoodState.Ice) return Element.Ice;
        if (f.hidden || f.foodState == FoodState.HoiCham) return Element.Hidden;
        return Element.Normal;
    }

    private static void ResetTile(FoodData f)
    {
        f.foodState = FoodState.Normal;
        f.hidden = false;
        f.iceLayers = 0;
        f.linkId = 0;
        f.lockId = 0;
        f.trayId = 0;
    }

    private void ClearGroupPartners(FoodData f)
    {
        int link = f.linkId > 0 ? f.linkId : f.lockId;
        foreach (var t in AllTiles().ToList())
        {
            if (t == f) continue;
            if ((link > 0 && (t.linkId > 0 ? t.linkId : t.lockId) == link) || (f.trayId > 0 && t.trayId == f.trayId))
                ResetTile(t);
        }
    }

    private void ClearElement(FoodData f)
    {
        ClearGroupPartners(f);
        ResetTile(f);
    }

    private void ApplyElement(FoodData f, Element e)
    {
        ClearElement(f);
        switch (e)
        {
            case Element.Ice: f.foodState = FoodState.Ice; f.iceLayers = 1; break;
            case Element.Hidden: f.hidden = true; f.foodState = FoodState.HoiCham; break;
            case Element.Link: f.foodState = FoodState.Lock; f.linkId = NextId(true); break;
        }
    }

    private void MakeTray(int c, int r, int size)
    {
        var foods = level.boardColumns[c].foods;
        size = Mathf.Min(size, foods.Count - r);
        if (size < 2)
        {
            ShowNotification(new GUIContent("Khay cần ≥ 2 ô liên tiếp trong cột."));
            return;
        }
        var tiles = foods.GetRange(r, size);
        foreach (var t in tiles) ClearElement(t);
        int id = NextId(false);
        foreach (var t in tiles) { t.foodState = FoodState.RigidTray; t.trayId = id; }
    }

    private void LinkTiles(int c1, int r1, int c2, int r2)
    {
        var a = FoodAt(c1, r1);
        var b = FoodAt(c2, r2);
        if (a == null || b == null) return;
        int id = a.linkId > 0 ? a.linkId : a.lockId;
        if (id == 0) id = NextId(true);
        ClearGroupPartners(a);
        ClearElement(b);
        a.foodState = FoodState.Lock; a.linkId = id; a.lockId = 0;
        b.foodState = FoodState.Lock; b.linkId = id;
    }

    private int NextId(bool link)
    {
        int max = 0;
        foreach (var t in AllTiles())
            max = Mathf.Max(max, link ? Mathf.Max(t.linkId, t.lockId) : t.trayId);
        return max + 1;
    }

    // ───────────────────────── Board sync from customers ─────────────────────────

    private static void AccumulateIngredients(CustomerOrderData cu, Dictionary<PoolType, int> counts, List<PoolType> order)
    {
        if (cu == null || cu.dishes == null) return;
        foreach (var o in cu.dishes)
        {
            if (o == null || o.amount < 1 || !o.TryGetRecipe(out var recipe, out _)) continue;
            foreach (PoolType t in recipe.Ingredients)
            {
                if (!counts.ContainsKey(t)) { counts[t] = 0; order.Add(t); }
                counts[t] += o.amount;
            }
        }
    }

    private Dictionary<PoolType, int> Expected(List<PoolType> order)
    {
        var d = new Dictionary<PoolType, int>();
        foreach (var cu in level.customers) AccumulateIngredients(cu, d, order);
        return d;
    }

    private Dictionary<PoolType, int> CountBoard()
    {
        var d = new Dictionary<PoolType, int>();
        foreach (var t in AllTiles())
        {
            d.TryGetValue(t.typeFood, out int v);
            d[t.typeFood] = v + t.amount;
        }
        return d;
    }

    /// Đảm bảo số cột = numberColumns, dồn ô của cột thừa sang cột khác.
    private void EnsureColumns()
    {
        int n = Mathf.Clamp(level.numberColumns, 1, 6);
        if (level.boardColumns == null) level.boardColumns = new List<FoodQueueData>();
        for (int i = 0; i < level.boardColumns.Count; i++)
        {
            if (level.boardColumns[i] == null) level.boardColumns[i] = new FoodQueueData();
            if (level.boardColumns[i].foods == null) level.boardColumns[i].foods = new List<FoodData>();
            level.boardColumns[i].foods.RemoveAll(f => f == null);
        }
        while (level.boardColumns.Count < n) level.boardColumns.Add(new FoodQueueData());
        while (level.boardColumns.Count > n)
        {
            var last = level.boardColumns[level.boardColumns.Count - 1];
            level.boardColumns.RemoveAt(level.boardColumns.Count - 1);
            foreach (var t in last.foods) AddToShortest(t);
        }
        ClampSelection();
    }

    private void AddToShortest(FoodData tile)
    {
        int best = 0;
        for (int i = 1; i < level.boardColumns.Count; i++)
            if (level.boardColumns[i].foods.Count < level.boardColumns[best].foods.Count) best = i;
        level.boardColumns[best].foods.Add(tile);
    }

    /// Thêm nguyên liệu còn thiếu / bớt nguyên liệu thừa để khớp các món khách gọi.
    private void SyncBoard()
    {
        // Do not remove tiles while a recipe is only half-edited or invalid.
        if (level.customers.Any(customer => customer == null || customer.ValidateOrders() != null)) return;
        EnsureColumns();
        var order = new List<PoolType>();
        var expected = Expected(order);

        foreach (var kv in CountBoard().ToList())
        {
            expected.TryGetValue(kv.Key, out int want);
            int extra = kv.Value - want;
            if (extra > 0) RemoveFood(kv.Key, extra);
        }

        var current = CountBoard();
        foreach (var t in order)
        {
            current.TryGetValue(t, out int have);
            int need = expected[t] - have;
            while (need > 0)
            {
                int amt = Mathf.Min(Mathf.Clamp(level.maxIngredientsPerTile, 1, 3), need);
                AddToShortest(new FoodData { typeFood = t, amount = amt });
                need -= amt;
            }
        }
        ClampSelection();
    }

    private void RemoveFood(PoolType type, int extra)
    {
        for (int pass = 0; pass < 2 && extra > 0; pass++)
        {
            for (int c = level.boardColumns.Count - 1; c >= 0 && extra > 0; c--)
            {
                var foods = level.boardColumns[c].foods;
                for (int r = foods.Count - 1; r >= 0 && extra > 0; r--)
                {
                    var t = foods[r];
                    if (t.typeFood != type) continue;
                    if (pass == 0 && GetElement(t) != Element.Normal) continue;
                    if (t.amount > extra) { t.amount -= extra; extra = 0; }
                    else
                    {
                        extra -= t.amount;
                        ClearElement(t);
                        foods.RemoveAt(r);
                    }
                }
            }
        }
    }

    /// Xóa board và sinh lại từ khách (shuffle theo boardSeed, chia đều theo cột như SushiLevelData).
    private void RebuildBoard()
    {
        level.boardColumns = new List<FoodQueueData>();
        EnsureColumns();

        var order = new List<PoolType>();
        var expected = Expected(order);
        var tiles = new List<FoodData>();
        int per = Mathf.Clamp(level.maxIngredientsPerTile, 1, 3);
        foreach (var t in order)
        {
            int remaining = expected[t];
            while (remaining > 0)
            {
                int amt = Mathf.Min(per, remaining);
                tiles.Add(new FoodData { typeFood = t, amount = amt });
                remaining -= amt;
            }
        }
        if (level.shuffleFoods)
        {
            var rnd = new System.Random(level.boardSeed);
            for (int i = tiles.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var tmp = tiles[i]; tiles[i] = tiles[j]; tiles[j] = tmp;
            }
        }
        for (int i = 0; i < tiles.Count; i++)
            level.boardColumns[i % level.boardColumns.Count].foods.Add(tiles[i]);
        ResetSelection(keepCustomer: true);
    }

    // ───────────────────────── Misc ─────────────────────────

    private void DrawValidation()
    {
        string error;
        try { error = level.ValidateData(); }
        catch (Exception ex) { error = ex.Message; }

        int tiles = 0, ingredients = 0;
        if (level.boardColumns != null)
            foreach (var c in level.boardColumns)
                if (c != null && c.foods != null)
                    foreach (var f in c.foods) if (f != null) { tiles++; ingredients += f.amount; }
        int expected = 0;
        try { expected = level.GetTotalFoodCount(); } catch { }

        EditorGUILayout.LabelField($"Khách: {level.customers.Count}   Ô board: {tiles}   Nguyên liệu board/đơn: {ingredients}/{expected}", EditorStyles.miniLabel);
        if (error == null) EditorGUILayout.HelpBox("Level hợp lệ.", MessageType.Info);
        else EditorGUILayout.HelpBox(error, MessageType.Error);
    }

    private void ClampSelection()
    {
        if (Selected() == null) selCol = selRow = -1;
    }

    private void ResetSelection(bool keepCustomer = false)
    {
        selCol = selRow = dragCol = dragRow = -1;
        tileWarning = null;
        linkMode = false;
        if (!keepCustomer) selCustomer = -1;
    }
}
#endif
