using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class FoodQueue : MonoBehaviour
{
    public Transform tf;
    public int columnIndex;
    public PoolType foodType;
    [SerializeField] private List<FoodShushi> foods = new List<FoodShushi>();
    [SerializeField] private float rowSpace = 1.3f;
    [SerializeField] private float moveDuration = 0.28f;
    [Header("Hiệu ứng khay rỗng")]
    [SerializeField, Min(0)] private float emptyTraySlideDistance = 0.24f;
    [SerializeField, Min(0.01f)] private float emptyTraySlideDuration = 0.28f;
    private readonly object rowTweenId = new object();
    private ConveyorManager conveyor;
    private Tween shakeTween;
    private Tween columnMoveTween;
    private GameObject departingTray;
    private const int VisibleRows = 4;
    [Header("Prefab visuals")]
    [SerializeField] private Transform beltView;
    [SerializeField] private List<Transform> emptyBases = new List<Transform>();
    [SerializeField] private SpriteRenderer lowerRowsShade;

    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite topSprite;

    public BoardShishiManager Board { get; internal set; }
    public IReadOnlyList<FoodShushi> Foods => foods;
    public int Count => foods.Count;
    public bool IsPickupPending { get; private set; }
    // Gán băng chuyền nhận nguyên liệu từ cột này.
    public void SetConveyor(ConveyorManager manager) { conveyor = manager; }
    // Bật hoặc tắt lớp che các hàng dưới theo chế độ chọn nguyên liệu.
    internal void SetLowerRowsShadeVisible(bool visible)
    {
        if (lowerRowsShade != null) lowerRowsShade.gameObject.SetActive(visible);
    }

    // Hiện thanh viền trái hoặc phải cho các cột ở mép bàn.
    internal void SetOuterRails(bool showLeft, bool showRight)
    {
        Transform leftRail = beltView != null ? beltView.Find("Left rail") : null;
        Transform rightRail = beltView != null ? beltView.Find("Right rail") : null;
        if (leftRail == null || rightRail == null)
        {
            Debug.LogError("FoodQueue cần có Left rail và Right rail trong beltView.", this);
            return;
        }
        leftRail.gameObject.SetActive(showLeft);
        rightRail.gameObject.SetActive(showRight);
    }

    // Lấy nguyên liệu đầu cột, trả về null khi cột trống.
    public FoodShushi GetTopFood() => foods.Count > 0 ? foods[0] : null;
    // Kiểm tra danh sách nguyên liệu của cột đã trống.
    public bool IsEmpty() => foods.Count == 0;
    // Kiểm tra cột đang hoạt động, đã đứng yên và có nguyên liệu đầu cột lấy được.
    public bool CanTakeTopFood() => isActiveAndEnabled && !IsPickupPending && GetTopFood() != null && GetTopFood().CanPick();

    // Chuyển thao tác chạm cột sang băng chuyền để thử lấy nguyên liệu.
    public void OnClickColumn()
    {
        if (conveyor != null) conveyor.TryTakeFromQueue(this);
    }

    // Input được xử lý một lần bởi FoodInputController. Không dùng thêm OnMouseDown.
    // Tạo nguyên liệu từ dữ liệu cột, sắp hàng và lật ô đầu tiên nếu đang bị che.
    public void OnInit(FoodQueueData data)
    {
        ClearFoods();
        if (tf == null) tf = transform;
        if (lowerRowsShade == null)
        {
            Debug.LogError("FoodQueue prefab needs an authored Lower rows shade SpriteRenderer.", this);
            return;
        }
        lowerRowsShade.transform.localPosition = Vector3.down * rowSpace * 2f;
        lowerRowsShade.transform.localScale = new Vector3(1.34f, rowSpace * (VisibleRows - 1), 1f);
        SetLowerRowsShadeVisible(true);
        foreach (FoodData item in data.foods)
        {
            FoodShushi food = SimplePool.Spawn<FoodShushi>(item.typeFood);
            food.transform.SetParent(tf, false);
            food.transform.localRotation = Quaternion.identity;
            food.transform.localScale = Vector3.one;
            food.SetupFromData(item);
            food.Owner = this;
            foods.Add(food);
        }
        RefreshRowsImmediate();
        RevealTopFood();
    }

    // Lấy chỉ số của nguyên liệu trong thứ tự dữ liệu của cột.
    public int IndexOf(FoodShushi food) => foods.IndexOf(food);
    // Tính hàng đang hiển thị của nguyên liệu dựa trên vị trí thực tế trong cột.
    internal int VisibleRowOf(FoodShushi food) => Mathf.RoundToInt(-food.transform.localPosition.y / rowSpace);
    // Tìm nguyên liệu đang nằm ở hàng hiển thị được yêu cầu.
    internal FoodShushi FoodAtVisibleRow(int row)
    {
        foreach (FoodShushi food in foods)
            if (VisibleRowOf(food) == row) return food;
        return null;
    }

    // Trừ số lượng trong ô, giữ ô khay nếu cần hoặc trả ô đã hết về pool.
    internal void Consume(FoodShushi food, int count, bool preserveTray)
    {
        food.SetAmount(food.Amount - count);
        if (food.Amount > 0 || (preserveTray && food.TrayId > 0)) return;
        food.SetBoardBaseSprite(normalSprite);
        food.CopyBoardBase(EnsureDepartingTray());
        foods.Remove(food);
        SetTopVisual(true);
        food.Owner = null;
        food.transform.SetParent(null, true);
        // Nguyên liệu bay và hiệu ứng khay rỗng dùng các view riêng.
        SimplePool.Despawn(food);
    }

    // Tạo hoặc lấy nhóm chứa hình ảnh khay rỗng đang rời bàn.
    private Transform EnsureDepartingTray()
    {
        if (departingTray == null)
        {
            departingTray = new GameObject("Picked empty tray");
            departingTray.transform.SetParent(tf, false);
        }
        return departingTray.transform;
    }

    // Sao chép khung khay và đổi tọa độ đường viền để khung di chuyển cùng nhóm khay.
    internal void CopyTrayFrame(GameObject view)
    {
        if (view == null || !view.activeSelf) return;
        GameObject copy = Instantiate(view, EnsureDepartingTray(), true);
        LineRenderer line = copy.GetComponent<LineRenderer>();
        if (line == null || !line.useWorldSpace) return;
        var positions = new Vector3[line.positionCount];
        line.GetPositions(positions);
        for (int i = 0; i < positions.Length; i++) positions[i] = line.transform.InverseTransformPoint(positions[i]);
        line.useWorldSpace = false;
        line.SetPositions(positions);
    }

    // Ẩn và hủy nhóm hình ảnh khay rỗng đã rời cột.
    private void RemoveDepartingTray()
    {
        if (departingTray == null) return;
        departingTray.SetActive(false);
        Destroy(departingTray);
        departingTray = null;
    }

    // Chạy chuyển động sắp lại nguyên liệu và cập nhật ô đầu, đế trống, vùng nhận chạm.
    public void RefreshRows()
    {
        if (IsPickupPending) return;
        DOTween.Kill(rowTweenId);
        for (int i = 0; i < foods.Count; i++)
        {
            FoodShushi food = foods[i];
            food.SetBoardVisible(i < VisibleRows);
            Tween move = food.transform.DOLocalMove(GetRowPosition(i), Mathf.Max(0.01f, moveDuration))
                .SetEase(Ease.OutCubic).SetId(rowTweenId);
            if (i == 0) move.OnComplete(() => SetTopVisual(true));
        }
        RevealTopFood();
        RefreshEmptyBasesImmediate();
        RefreshCollider();
    }

    // Khóa lượt lấy trong lúc khay rời đi và các hàng còn lại trượt lên lấp chỗ trống.
    internal void AdvanceAfterPickup(int removedRows, float wait)
    {
        columnMoveTween?.Kill();
        DOTween.Kill(rowTweenId);
        IsPickupPending = true;
        SetTopVisual(true);
        float delay = Mathf.Max(0, wait);
        float duration = Mathf.Max(0.01f, moveDuration);
        Sequence sequence = DOTween.Sequence().AppendInterval(delay + duration);
        if (departingTray != null)
        {
            Transform tray = departingTray.transform;
            Vector3 origin = tray.localPosition;
            float slideDuration = Mathf.Max(0.01f, emptyTraySlideDuration);
            Vector3 offset = Vector3.up * Mathf.Max(0, emptyTraySlideDistance);
            SpriteRenderer[] sprites = tray.GetComponentsInChildren<SpriteRenderer>(true);
            var colors = new Color[sprites.Length];
            for (int i = 0; i < sprites.Length; i++) colors[i] = sprites[i].color;
            LineRenderer[] lines = tray.GetComponentsInChildren<LineRenderer>(true);
            var startColors = new Color[lines.Length];
            var endColors = new Color[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                startColors[i] = lines[i].startColor;
                endColors[i] = lines[i].endColor;
            }
            // Cùng một nhịp cho vị trí và alpha, giữ nguyên scale của toàn bộ khay.
            sequence.Insert(delay, DOVirtual.Float(0, 1, slideDuration, t =>
            {
                if (tray == null) return;
                tray.localPosition = origin + offset * t;
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] == null) continue;
                    Color color = colors[i];
                    color.a *= 1 - t;
                    sprites[i].color = color;
                }
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i] == null) continue;
                    Color start = startColors[i], end = endColors[i];
                    start.a *= 1 - t;
                    end.a *= 1 - t;
                    lines[i].startColor = start;
                    lines[i].endColor = end;
                }
            }).SetEase(Ease.InOutSine));
            sequence.InsertCallback(delay + slideDuration, RemoveDepartingTray);
        }
        for (int i = 0; i < foods.Count; i++)
            sequence.Insert(delay, foods[i].transform.DOLocalMove(GetRowPosition(i), duration).SetEase(Ease.InOutSine));
        // Đế nền bổ sung từ cuối cột, tách khỏi hiệu ứng khay vừa pick.
        for (int i = 0; i < emptyBases.Count; i++)
        {
            emptyBases[i].localPosition = GetRowPosition(foods.Count + removedRows + i);
            sequence.Insert(delay, emptyBases[i].DOLocalMove(GetRowPosition(foods.Count + i), duration).SetEase(Ease.InOutSine));
        }
        columnMoveTween = sequence
            .OnUpdate(RefreshMovingVisibility)
            .OnComplete(() =>
            {
                columnMoveTween = null;
                RemoveDepartingTray();
                IsPickupPending = false;
                RefreshRowsImmediate();
                RevealTopFood();
            });
    }

    // Hủy chuyển động dồn cột, dọn khay rời và bỏ trạng thái đang chờ lấy.
    private void CancelColumnMovement()
    {
        columnMoveTween?.Kill();
        columnMoveTween = null;
        RemoveDepartingTray();
        IsPickupPending = false;
    }

    // Hoàn tất ngay chuyển động cột và các tween sắp hàng còn chạy.
    internal void CompleteColumnMovement()
    {
        columnMoveTween?.Complete(true);
        DOTween.Complete(rowTweenId);
    }

    // Đặt các đế trống sau nguyên liệu và chỉ hiện đế trong cửa sổ hiển thị.
    private void RefreshEmptyBasesImmediate()
    {
        for (int i = 0; i < emptyBases.Count; i++)
        {
            int row = foods.Count + i;
            emptyBases[i].localPosition = GetRowPosition(row);
            emptyBases[i].gameObject.SetActive(row < VisibleRows);
        }
    }

    // Cập nhật ẩn hiện nguyên liệu và đế trống theo vị trí trong lúc cột di chuyển.
    private void RefreshMovingVisibility()
    {
        foreach (FoodShushi food in foods) food.SetBoardVisible(VisibleRowOf(food) < VisibleRows);
        foreach (Transform emptyBase in emptyBases)
            emptyBase.gameObject.SetActive(-emptyBase.localPosition.y / rowSpace < VisibleRows - 0.5f);
    }

    // Đặt ngay nguyên liệu về đúng hàng, đồng bộ đế trống, vùng chạm và ô đầu cột.
    private void RefreshRowsImmediate()
    {
        DOTween.Kill(rowTweenId);
        for (int i = 0; i < foods.Count; i++)
        {
            foods[i].transform.localPosition = GetRowPosition(i);
            foods[i].SetBoardVisible(i < VisibleRows);
        }
        RefreshEmptyBasesImmediate();
        RefreshCollider();
        SetTopVisual(true);
    }

    // Cập nhật kích thước và trạng thái collider theo số hàng nguyên liệu đang hiển thị.
    private void RefreshCollider()
    {
        BoxCollider2D hitArea = GetComponent<BoxCollider2D>();
        if (hitArea == null) return;
        int rows = Mathf.Min(VisibleRows, foods.Count);
        hitArea.enabled = rows > 0;
        hitArea.size = new Vector2(1.3f, Mathf.Max(1, rows) * rowSpace);
        hitArea.offset = Vector2.down * rowSpace * Mathf.Max(0, rows - 1) / 2f;
    }

    // Tính vị trí cục bộ của một hàng theo khoảng cách giữa các ô.
    private Vector3 GetRowPosition(int row) => Vector3.down * rowSpace * row;
    // Lật nguyên liệu bị che ở đầu cột và cập nhật hình ảnh đế đầu cột.
    private void RevealTopFood()
    {
        GetTopFood()?.Reveal();
        SetTopVisual(true);
    }

    // Chọn hình ảnh đế đầu cột khi nguyên liệu đầu tiên đã nằm đúng vị trí.
    public void SetTopVisual(bool isTop)
    {
        // A new head receives its selected sprite only after reaching the front.
        FoodShushi head = GetTopFood();
        bool showHead = isTop && head != null && head.transform.localPosition == GetRowPosition(0);
        for (int i = 0; i < foods.Count; i++)
            if (foods[i] != null) foods[i].SetBoardBaseSprite(showHead && i == 0 ? topSprite : normalSprite);
    }

    // Tìm nguyên liệu trong bốn hàng đầu tại vị trí chạm trong thế giới game.
    public FoodShushi FoodAtWorldPosition(Vector3 position)
    {
        if (IsPickupPending || foods.Count == 0) return null;
        float row = -tf.InverseTransformPoint(position).y / rowSpace;
        int index = Mathf.RoundToInt(row);
        return index >= 0 && index < Mathf.Min(4, foods.Count) ? foods[index] : null;
    }

    // Build a proposal without moving anything: linked columns must be checked together.
    // Tạo thứ tự xoay các hàng đang thấy, giữ nguyên các ô thuộc cùng một khay.
    internal List<FoodShushi> BuildSwapRows()
    {
        var result = new List<FoodShushi>(foods);
        if (IsPickupPending || foods.Count < 2) return result;
        int firstSize = 1;
        int tray = foods[0].TrayId;
        if (tray > 0)
            while (firstSize < foods.Count && foods[firstSize].TrayId == tray) firstSize++;
        int window = Mathf.Min(VisibleRows, foods.Count);
        int lastTray = foods[window - 1].TrayId;
        if (lastTray > 0)
            while (window < foods.Count && foods[window].TrayId == lastTray) window++;
        if (firstSize >= window) return result;
        List<FoodShushi> firstBlock = foods.GetRange(0, firstSize);
        result.RemoveRange(0, firstSize);
        result.InsertRange(window - firstSize, firstBlock);
        return result;
    }

    // Áp dụng thứ tự hàng mới và chạy hiệu ứng sắp lại khi có thay đổi.
    internal bool ApplySwapRows(List<FoodShushi> rows)
    {
        bool changed = false;
        for (int i = 0; i < foods.Count; i++) changed |= foods[i] != rows[i];
        if (!changed) return false;
        foods.Clear();
        foods.AddRange(rows);
        RefreshRows();
        return true;
    }

    // Rung nhẹ cột để báo thao tác không hợp lệ, tránh chồng nhiều hiệu ứng rung.
    public void Shake()
    {
        if (shakeTween != null && shakeTween.IsActive()) return;
        Vector3 origin = transform.localPosition;
        shakeTween = transform.DOShakePosition(0.22f, new Vector3(0.08f, 0, 0), 12)
            .OnComplete(() => transform.localPosition = origin);
    }

    // Dừng chuyển động, trả toàn bộ nguyên liệu về pool và ẩn các đế trống.
    public void ClearFoods()
    {
        CancelColumnMovement();
        DOTween.Kill(rowTweenId);
        SetTopVisual(false);
        foreach (FoodShushi food in foods)
        {
            if (food == null) continue;
            food.Owner = null;
            food.transform.SetParent(null, true);
            SimplePool.Despawn(food);
        }
        foods.Clear();
        foreach (Transform emptyBase in emptyBases) emptyBase.gameObject.SetActive(false);
    }

    // Dừng các hiệu ứng của cột và khôi phục bố cục nếu bị tắt giữa lượt lấy.
    private void OnDisable()
    {
        bool wasPending = IsPickupPending;
        CancelColumnMovement();
        DOTween.Kill(rowTweenId);
        shakeTween?.Complete();
        if (wasPending && tf != null) RefreshRowsImmediate();
    }
}
