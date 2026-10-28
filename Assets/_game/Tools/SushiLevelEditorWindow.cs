#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Đặt file này trong thư mục Editor. Menu: SushiKhai/Level Editor (hoặc double-click asset SushiLevelData).
/// - Trái: cài đặt level + danh sách khách (thêm/sửa/xóa/đổi thứ tự/nhân bản).
/// - Phải: board đúng hàng/cột, kéo thả để đổi chỗ, chỉnh element (Ice/Hidden/Link/Tray).
/// - Thêm/sửa/xóa khách hoặc món => board tự thêm/bớt nguyên liệu tương ứng.
/// </summary>
public class SushiLevelEditorWindow : EditorWindow
{
    private enum Element { Normal, Ice, Hidden, Link, Tray }

    private const float CellW = 80f, CellH = 50f, Gap = 4f, HeaderW = 40f, HeaderH = 22f;

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
    private SushiDishId newDish = SushiDishId.NigiriShrimp;
    private int newAmount = 1;

    private readonly List<Action> pending = new List<Action>();
    private bool dirty;
    private GUIStyle cellStyle, leftButton;

    [MenuItem("SushiKhai/Level Editor")]
    private static void Open()
    {
        var w = GetWindow<SushiLevelEditorWindow>("Sushi Level Editor");
        w.minSize = new Vector2(980, 560);
    }

    [UnityEditor.Callbacks.OnOpenAsset(0)]
    private static bool OnOpenAsset(int instanceId, int line)
    {
        var data = EditorUtility.InstanceIDToObject(instanceId) as SushiLevelData;
        if (data == null) return false;
        var w = GetWindow<SushiLevelEditorWindow>("Sushi Level Editor");
        w.minSize = new Vector2(980, 560);
        w.level = data;
        w.ResetSelection();
        w.LoadK();
        w.Show();
        return true;
    }

    private void OnEnable() { Undo.undoRedoPerformed += Repaint; }
    private void OnDisable() { Undo.undoRedoPerformed -= Repaint; }

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
            if (GUILayout.Button($"#{i + 1}{(cu.rushCustomer ? " [RUSH]" : "")}  {Summary(cu)}", leftButton))
                selCustomer = sel ? -1 : i;
            GUI.enabled = i > 0;
            if (GUILayout.Button("↑", EditorStyles.miniButtonLeft, GUILayout.Width(22))) pending.Add(() => MoveCustomer(index, -1));
            GUI.enabled = i < level.customers.Count - 1;
            if (GUILayout.Button("↓", EditorStyles.miniButtonMid, GUILayout.Width(22))) pending.Add(() => MoveCustomer(index, 1));
            GUI.enabled = true;
            if (GUILayout.Button("C", EditorStyles.miniButtonMid, GUILayout.Width(22))) pending.Add(() => DuplicateCustomer(index));
            if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(22))) pending.Add(() => RemoveCustomer(index));
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
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    d.dish = (SushiDishId)EditorGUILayout.EnumPopup(d.dish);
                    d.amount = Mathf.Max(1, EditorGUILayout.IntField(d.amount, GUILayout.Width(40)));
                    if (EditorGUI.EndChangeCheck()) pending.Add(SyncBoard);
                    GUI.enabled = cu.dishes.Count > 1;
                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22)))
                        pending.Add(() => { cu.dishes.RemoveAt(dj); SyncBoard(); });
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("+ Món", EditorStyles.miniButton))
                    pending.Add(() => { cu.dishes.Add(new DishOrderData { dish = newDish, amount = 1 }); SyncBoard(); });

                EditorGUILayout.LabelField("Nguyên liệu: " + IngredientText(cu), EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(2);
        EditorGUILayout.BeginHorizontal("box");
        newDish = (SushiDishId)EditorGUILayout.EnumPopup(newDish);
        newAmount = Mathf.Max(1, EditorGUILayout.IntField(newAmount, GUILayout.Width(40)));
        if (GUILayout.Button("+ Thêm khách", GUILayout.Width(100)))
        {
            int amount = newAmount; SushiDishId dish = newDish;
            pending.Add(() =>
            {
                level.customers.Add(new CustomerOrderData
                {
                    dishes = new List<DishOrderData> { new DishOrderData { dish = dish, amount = amount } }
                });
                selCustomer = level.customers.Count - 1;
                SyncBoard();
            });
        }
        EditorGUILayout.EndHorizontal();
    }

    private static string Summary(CustomerOrderData cu)
    {
        if (cu.dishes == null || cu.dishes.Count == 0) return "(trống)";
        return string.Join(", ", cu.dishes.Where(d => d != null).Select(d => d.amount > 1 ? $"{d.dish} ×{d.amount}" : d.dish.ToString()));
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
            dishes = src.dishes.Select(d => new DishOrderData { dish = d.dish, amount = d.amount }).ToList()
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
            case Element.Hidden: badge = "HIDDEN"; border = Color.gray; EditorGUI.DrawRect(rc, new Color(0.3f, 0.3f, 0.3f, 0.45f)); break;
            case Element.Link: badge = $"LINK{(f.linkId > 0 ? f.linkId : f.lockId)}"; border = new Color(0.9f, 0.2f, 0.9f); break;
            case Element.Tray: badge = $"TRAY{f.trayId}"; border = new Color(1f, 0.55f, 0.1f); break;
        }

        if (highlight != null && highlight.Contains(f.typeFood)) DrawBorder(rc, new Color(0.1f, 0.8f, 0.1f), 1f);
        if (border.a > 0) DrawBorder(rc, border, 3f);
        if (selected) DrawBorder(rc, Color.yellow, 3f);

        GUI.Label(rc, $"{f.typeFood}\n×{f.amount}  {badge}", cellStyle);
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

        EditorGUILayout.LabelField($"Ô đang chọn: Cột {selCol + 1}, Hàng {selRow + 1}", EditorStyles.boldLabel);
        f.typeFood = (PoolType)EditorGUILayout.EnumPopup("Nguyên liệu", f.typeFood);
        f.amount = EditorGUILayout.IntSlider("Số lượng (×)", f.amount, 1, 3);

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
            if (o == null || o.amount < 1 || !SushiRecipeCatalog.TryGet(o.dish, out var recipe)) continue;
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
        linkMode = false;
        if (!keepCustomer) selCustomer = -1;
    }
}
#endif