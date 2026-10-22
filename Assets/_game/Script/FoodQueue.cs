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
    private Transform beltView;
    private readonly List<Transform> emptyBases = new List<Transform>();

    public BoardShishiManager Board { get; internal set; }
    public IReadOnlyList<FoodShushi> Foods => foods;
    public int Count => foods.Count;
    public bool IsPickupPending { get; private set; }
    public void SetConveyor(ConveyorManager manager) { conveyor = manager; }
    public FoodShushi GetTopFood() => foods.Count > 0 ? foods[0] : null;
    public bool IsEmpty() => foods.Count == 0;
    public bool CanTakeTopFood() => isActiveAndEnabled && !IsPickupPending && GetTopFood() != null && GetTopFood().CanPick();

    public void OnClickColumn()
    {
        if (conveyor != null) conveyor.TryTakeFromQueue(this);
    }

    // Input được xử lý một lần bởi FoodInputController. Không dùng thêm OnMouseDown.
    public void OnInit(FoodQueueData data)
    {
        ClearFoods();
        if (tf == null) tf = transform;
        EnsureBeltView();
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

    public int IndexOf(FoodShushi food) => foods.IndexOf(food);
    internal int VisibleRowOf(FoodShushi food) => Mathf.RoundToInt(-food.transform.localPosition.y / rowSpace);
    internal FoodShushi FoodAtVisibleRow(int row)
    {
        foreach (FoodShushi food in foods)
            if (VisibleRowOf(food) == row) return food;
        return null;
    }

    internal void Consume(FoodShushi food, int count, bool preserveTray)
    {
        food.SetAmount(food.Amount - count);
        if (food.Amount > 0 || (preserveTray && food.TrayId > 0)) return;
        food.CopyBoardBase(EnsureDepartingTray());
        foods.Remove(food);
        food.Owner = null;
        food.transform.SetParent(null, true);
        // Nguyên liệu bay và hiệu ứng khay rỗng dùng các view riêng.
        SimplePool.Despawn(food);
    }

    private Transform EnsureDepartingTray()
    {
        if (departingTray == null)
        {
            departingTray = new GameObject("Picked empty tray");
            departingTray.transform.SetParent(tf, false);
        }
        return departingTray.transform;
    }

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

    private void RemoveDepartingTray()
    {
        if (departingTray == null) return;
        departingTray.SetActive(false);
        Destroy(departingTray);
        departingTray = null;
    }

    public void RefreshRows()
    {
        if (IsPickupPending) return;
        DOTween.Kill(rowTweenId);
        for (int i = 0; i < foods.Count; i++)
        {
            FoodShushi food = foods[i];
            food.SetBoardVisible(i < VisibleRows);
            food.transform.DOLocalMove(GetRowPosition(i), Mathf.Max(0.01f, moveDuration))
                .SetEase(Ease.OutCubic).SetId(rowTweenId);
        }
        RevealTopFood();
        RefreshEmptyBasesImmediate();
        RefreshCollider();
    }

    internal void AdvanceAfterPickup(int removedRows, float wait)
    {
        columnMoveTween?.Kill();
        DOTween.Kill(rowTweenId);
        IsPickupPending = true;
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

    private void CancelColumnMovement()
    {
        columnMoveTween?.Kill();
        columnMoveTween = null;
        RemoveDepartingTray();
        IsPickupPending = false;
    }

    internal void CompleteColumnMovement()
    {
        columnMoveTween?.Complete(true);
        DOTween.Complete(rowTweenId);
    }

    private void EnsureBeltView()
    {
        if (beltView != null) return;
        beltView = new GameObject("Column conveyor").transform;
        beltView.SetParent(tf, false);
        float height = VisibleRows * rowSpace;
        float center = -(VisibleRows - 1) * rowSpace * 0.5f;
        var bed = SushiWorldVisual.Panel(beltView, "Belt", -4);
        bed.transform.localPosition = Vector3.up * center;
        bed.transform.localScale = new Vector3(1.42f, height, 1);
        bed.color = new Color(0.28f, 0.18f, 0.12f);
        for (int side = -1; side <= 1; side += 2)
        {
            var rail = SushiWorldVisual.Panel(beltView, "Rail", -3);
            rail.transform.localPosition = new Vector3(side * 0.69f, center, -0.01f);
            rail.transform.localScale = new Vector3(0.09f, height, 1);
            rail.color = new Color(0.9f, 0.61f, 0.28f);
        }
        for (int i = 0; i < VisibleRows; i++)
            emptyBases.Add(SushiWorldVisual.BoardBase(tf, "Empty belt base").transform);
    }

    private void RefreshEmptyBasesImmediate()
    {
        for (int i = 0; i < emptyBases.Count; i++)
        {
            int row = foods.Count + i;
            emptyBases[i].localPosition = GetRowPosition(row);
            emptyBases[i].gameObject.SetActive(row < VisibleRows);
        }
    }

    private void RefreshMovingVisibility()
    {
        foreach (FoodShushi food in foods) food.SetBoardVisible(VisibleRowOf(food) < VisibleRows);
        foreach (Transform emptyBase in emptyBases)
            emptyBase.gameObject.SetActive(-emptyBase.localPosition.y / rowSpace < VisibleRows - 0.5f);
    }

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
    }

    private void RefreshCollider()
    {
        BoxCollider2D hitArea = GetComponent<BoxCollider2D>();
        if (hitArea == null) return;
        int rows = Mathf.Min(VisibleRows, foods.Count);
        hitArea.enabled = rows > 0;
        hitArea.size = new Vector2(1.3f, Mathf.Max(1, rows) * rowSpace);
        hitArea.offset = Vector2.down * rowSpace * Mathf.Max(0, rows - 1) / 2f;
    }

    private Vector3 GetRowPosition(int row) => Vector3.down * rowSpace * row;
    private void RevealTopFood() { GetTopFood()?.Reveal(); }

    public FoodShushi FoodAtWorldPosition(Vector3 position)
    {
        if (IsPickupPending || foods.Count == 0) return null;
        float row = -tf.InverseTransformPoint(position).y / rowSpace;
        int index = Mathf.RoundToInt(row);
        return index >= 0 && index < Mathf.Min(4, foods.Count) ? foods[index] : null;
    }

    public void SwapVisibleRows()
    {
        if (IsPickupPending || foods.Count < 2) return;
        int firstSize = 1;
        int tray = foods[0].TrayId;
        if (tray > 0)
            while (firstSize < foods.Count && foods[firstSize].TrayId == tray) firstSize++;
        int window = Mathf.Min(4, foods.Count);
        int lastTray = foods[window - 1].TrayId;
        if (lastTray > 0)
            while (window < foods.Count && foods[window].TrayId == lastTray) window++;
        if (firstSize >= window) return;
        List<FoodShushi> firstBlock = foods.GetRange(0, firstSize);
        foods.RemoveRange(0, firstSize);
        foods.InsertRange(window - firstSize, firstBlock);
        RefreshRows();
    }

    public void Shake()
    {
        if (shakeTween != null && shakeTween.IsActive()) return;
        Vector3 origin = transform.localPosition;
        shakeTween = transform.DOShakePosition(0.22f, new Vector3(0.08f, 0, 0), 12)
            .OnComplete(() => transform.localPosition = origin);
    }

    public void ClearFoods()
    {
        CancelColumnMovement();
        DOTween.Kill(rowTweenId);
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

    private void OnDisable()
    {
        bool wasPending = IsPickupPending;
        CancelColumnMovement();
        DOTween.Kill(rowTweenId);
        shakeTween?.Complete();
        if (wasPending && tf != null) RefreshRowsImmediate();
    }
}
