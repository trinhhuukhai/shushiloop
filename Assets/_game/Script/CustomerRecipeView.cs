using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomerRecipeView : MonoBehaviour
{
    [Tooltip("Inactive nested DishCard prefab, editable on Customer.prefab.")]
    [SerializeField] private SushiDishCardView cardTemplate;
    [SerializeField] private RectTransform orderRoot;
    [SerializeField] private Transform leftColumn;
    [SerializeField] private Transform rightColumn;
    [SerializeField] private Sprite oneColumnTwoRowsBoard;
    [Tooltip("Bảng 1 cột 3 hàng (order thường có 3 món trở lên).")]
    [SerializeField] private Sprite oneColumnThreeRowsBoard;
    [SerializeField] private Sprite twoColumnTwoRowsBoard;
    [SerializeField] private Sprite twoColumnThreeRowsBoard;
    [Tooltip("Bảng order dùng native size của sprite. Chỉ khi bảng cao hơn mức này (bảng 3 hàng) mới thu nhỏ đều để không đè lên HUD.")]
    [SerializeField, Min(1f)] private float maxBoardHeight = 252f;
    [Tooltip("Bề ngang bảng 2 cột (3 món trở lên, hoặc xem chi tiết). 0 = dùng native size của sprite (307).")]
    [SerializeField, Min(0f)] private float wideBoardWidth = 210f;
    [Tooltip("Lề phải của bảng khi khách vội: chừa chỗ cho thanh thời gian nằm trong bảng, hình món dồn sang trái.")]
    [SerializeField, Min(0)] private int rushRightPadding = 32;
    [Tooltip("Xem chi tiết món từ 4 nguyên liệu trở lên: bề ngang tối thiểu của mỗi ô món (2 cột nguyên liệu). Bảng tự nới rộng cho đủ. 0 = không nới.")]
    [SerializeField, Min(0f)] private float detailGridCellWidth = 130f;
    private Customer customer;
    private SushiDishArtLibrary art;
    private readonly List<SushiDishCardView> cards = new List<SushiDishCardView>();
    private readonly List<SushiDishCardView> dishCards = new List<SushiDishCardView>();
    private readonly List<List<SushiDishOrder>> groups = new List<List<SushiDishOrder>>();
    private bool magnetTargeting;
    private float collapsedOrderHeight;
    private bool hasCachedOrderHeight;
    private Vector2 defaultOrderSize;
    private Vector2 defaultOrderPosition;
    private Sprite defaultOrderBoard;
    private Image.Type defaultOrderBoardType;
    private Image orderBoardImage;
    private bool hasCachedOrderBoard;
    private HorizontalLayoutGroup orderLayout;
    private int defaultRightPadding;
    private float cellHeight;
    private float boardInnerHeight;
    private bool wideBoard;
    private float columnSpacing;
    private readonly Vector3[] boardCorners = new Vector3[4];

    // Lấy thẻ hiển thị tương ứng với chỉ số đơn món, kể cả các phần dùng chung thẻ.
    public SushiDishCardView CardAt(int index) => dishCards[index];

    // Bỏ đăng ký sự kiện khách, phục hồi bảng đơn gốc và hủy các thẻ món đã tạo.
    public void Clear()
    {
        if (customer != null) customer.OnOrderChanged -= Refresh;
        RestoreOrderBoard();
        customer = null;
        magnetTargeting = false;
        foreach (var card in cards)
            if (card != null) { card.gameObject.SetActive(false); Destroy(card.gameObject); }
        cards.Clear();
        dishCards.Clear(); groups.Clear();
    }

    // Gom các phần cùng công thức, dựng thẻ món theo cột và đăng ký cập nhật đơn khách.
    public void Setup(Customer owner, SushiDishArtLibrary library)
    {
        Clear();
        customer = owner;
        art = library != null ? library : SushiArtCatalog.Default != null ? SushiArtCatalog.Default.dishImages : null;
        if (!hasCachedOrderHeight && orderRoot != null)
        {
            CacheDefaultOrderBoard();
        }
        foreach (var dish in customer.Dishes)
        {
            var group = groups.Find(g => g[0].Recipe.Key == dish.Recipe.Key);
            if (group == null) { group = new List<SushiDishOrder>(); groups.Add(group); }
            group.Add(dish);
        }
        // Order thường: 1–2 món xếp 1 cột dọc; từ 3 món dùng bảng 2 cột (2x2, 2x3) như khi xem chi tiết.
        ConfigureOrderBoard(groups.Count, false);
        for (int i = 0; i < groups.Count; i++)
        {
            SushiDishCardView card = Instantiate(cardTemplate, leftColumn, false);
            card.name = groups[i][0].Recipe.Name;
            card.BindGroup(owner, groups[i], art);
            card.SetCellHeight(cellHeight);
            card.gameObject.SetActive(true);
            cards.Add(card);
        }
        foreach (var dish in customer.Dishes)
            dishCards.Add(cards[groups.FindIndex(g => g[0].Recipe.Key == dish.Recipe.Key)]);
        ArrangeColumns(UsesTwoColumns(cards.Count, false));
        LayoutRebuilder.ForceRebuildLayoutImmediate(orderRoot);
        RefreshOrderLayout();
        customer.OnOrderChanged += Refresh;
        Refresh(customer);
    }

    // Lấy điểm nhận nguyên liệu trên thẻ của một món.
    public Transform DeliverySlot(int dishIndex, int ingredientIndex) => dishCards[dishIndex].DeliverySlot(ingredientIndex);

    // Khách chờ vẫn hiện order (mờ hơn, do Customer.SetQueueVisual); chỉ khách đang ngồi mới mở được chi tiết.
    public void SetSeated(bool seated)
    {
        if (orderRoot != null) orderRoot.gameObject.SetActive(true);
        bool changed = ApplyRushPadding();
        foreach (var card in cards)
            if (!card.gameObject.activeSelf) { card.gameObject.SetActive(true); changed = true; }
        if (!seated && AnyExpanded) { SetAllExpanded(false); return; }
        if (changed) RefreshOrderLayout();
    }

    // Khách đã nhận đủ món và rời bàn: ẩn bảng order để chỉ còn khách đi ra.
    public void HideOrder()
    {
        if (orderRoot != null) orderRoot.gameObject.SetActive(false);
    }

    // Tìm phần món chưa hoàn thành tại vị trí chạm, ưu tiên phần chưa bắt đầu chuẩn bị.
    public SushiDishOrder DishAtWorldPosition(Vector3 position)
    {
        if (customer == null || !customer.IsSeated || customer.IsCompleted || customer.RushExpired) return null;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].gameObject.activeInHierarchy && cards[i].Contains(position))
            {
                var available = groups[i].Find(d => !d.IsComplete && !d.IsPrepared);
                return available ?? groups[i].Find(d => !d.IsComplete);
            }
        }
        return null;
    }

    // Đóng hoặc mở chi tiết thẻ món được chạm và thu gọn các thẻ khác.
    public bool ToggleRecipeDetailsAtWorldPosition(Vector3 position)
    {
        if (customer == null || !customer.IsSeated || customer.IsCompleted || customer.RushExpired) return false;
        foreach (var card in cards)
        {
            if (!card.gameObject.activeInHierarchy || !card.Contains(position)) continue;
            if (card.IsOrderComplete && !AnyExpanded) return false;
            // Mở/đóng chi tiết cả bảng: mỗi món chưa xong là một ô nguyên liệu xếp dọc.
            SetAllExpanded(!AnyExpanded);
            return true;
        }
        return false;
    }

    // Mở chi tiết cả bảng order (thẻ mục tiêu phải là món chưa xong) rồi tính lại bố cục.
    public bool ShowRecipeDetails(SushiDishCardView target)
    {
        if (customer == null || !customer.IsSeated || target == null || target.IsOrderComplete || !cards.Contains(target))
            return false;
        SetAllExpanded(true);
        return true;
    }

    private bool AnyExpanded
    {
        get
        {
            foreach (SushiDishCardView card in cards) if (card.IsRecipeExpanded) return true;
            return false;
        }
    }

    // Bảng 2 cột khi: xem chi tiết từ 2 món, hoặc order thường từ 3 món. Còn lại dùng bảng 1 cột.
    private static bool UsesTwoColumns(int orderCount, bool detail) => orderCount > (detail ? 1 : 2);

    // Chia món vào cột theo thứ tự đọc từ trái sang phải, trên xuống dưới (món 1,3,5 bên trái; 2,4,6 bên phải).
    private void ArrangeColumns(bool twoColumns)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Transform column = twoColumns && i % 2 == 1 ? rightColumn : leftColumn;
            if (cards[i].transform.parent != column) cards[i].transform.SetParent(column, false);
        }
        // Giữ đúng thứ tự món trong từng cột.
        for (int i = 0; i < cards.Count; i++) cards[i].transform.SetAsLastSibling();
        rightColumn.gameObject.SetActive(twoColumns);
    }

    // Chi tiết: mỗi món một ô nguyên liệu xếp dọc. Thu gọn: hình món, 1 cột (1–2 món) hoặc 2 cột (từ 3 món).
    private void SetAllExpanded(bool expanded)
    {
        bool twoColumns = UsesTwoColumns(cards.Count, expanded);
        ArrangeColumns(twoColumns);
        ConfigureOrderBoard(cards.Count, expanded);
        if (expanded)
        {
            FitDetailCells(leftColumn);
            if (twoColumns) FitDetailCells(rightColumn);
        }
        foreach (SushiDishCardView card in cards) card.SetRecipeExpanded(expanded && !card.IsOrderComplete);
        RefreshOrderLayout();
    }

    // Xem chi tiết: các ô trong một cột chia đều trọn chiều cao bảng (cột 1 món thì ô cao hết bảng),
    // nên ô luôn khít bảng và bảng không phải dài ra theo số nguyên liệu.
    private void FitDetailCells(Transform column)
    {
        int count = 0;
        foreach (SushiDishCardView card in cards) if (card.transform.parent == column) count++;
        if (count == 0) return;
        float height = (boardInnerHeight - columnSpacing * (count - 1)) / count;
        foreach (SushiDishCardView card in cards)
            if (card.transform.parent == column) card.SetCellHeight(height);
    }

    // Đổi trạng thái chọn mục tiêu nam châm và cập nhật màu các thẻ món.
    public void SetMagnetTargeting(bool active)
    {
        if (magnetTargeting == active) return;
        magnetTargeting = active;
        if (customer != null) Refresh(customer);
    }

    // Làm mới hình món, nguyên liệu và trạng thái thẻ theo thư viện hình ảnh hiện tại.
    public void RefreshArtwork()
    {
        if (customer == null) return;
        if (art == null && SushiArtCatalog.Default != null) art = SushiArtCatalog.Default.dishImages;
        for (int i = 0; i < cards.Count && i < customer.Dishes.Count; i++)
            cards[i].RefreshArtwork(customer, groups[i][0], art);
        Refresh(customer);
    }

    // Đồng bộ thẻ món theo đơn khách và cập nhật bố cục khi một thẻ tự thu gọn.
    private void Refresh(Customer owner)
    {
        SetSeated(owner.IsSeated);
        bool layoutChanged = false;
        for (int i = 0; i < cards.Count && i < owner.Dishes.Count; i++)
        {
            bool wasExpanded = cards[i].IsRecipeExpanded;
            cards[i].Refresh(groups[i][0], magnetTargeting);
            layoutChanged |= wasExpanded && !cards[i].IsRecipeExpanded;
        }
        if (layoutChanged) RefreshOrderLayout();
    }

    // Tính chiều cao bảng đơn từ cột cao nhất và dựng lại bố cục UI.
    private void RefreshOrderLayout()
    {
        if (orderRoot == null) return;
        float requiredHeight = Mathf.Max(GetColumnHeight(leftColumn), GetColumnHeight(rightColumn));
        HorizontalLayoutGroup layout = orderLayout != null ? orderLayout : orderRoot.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) requiredHeight += layout.padding.top + layout.padding.bottom;
        float targetHeight = Mathf.Max(collapsedOrderHeight, requiredHeight);
        orderRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(orderRoot);
    }

    // Tính chiều cao các thẻ đang bật trong cột, gồm khoảng cách và lề bố cục.
    private float GetColumnHeight(Transform column)
    {
        if (column == null) return 0;
        VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
        float height = layout != null ? layout.padding.top + layout.padding.bottom : 0;
        int cardCount = 0;
        foreach (Transform child in column)
        {
            if (!child.gameObject.activeSelf) continue;
            LayoutElement element = child.GetComponent<LayoutElement>();
            height += element != null && element.preferredHeight > 0
                ? element.preferredHeight : ((RectTransform)child).rect.height;
            if (cardCount++ > 0 && layout != null) height += layout.spacing;
        }
        return height;
    }

    // Lưu sprite, vị trí và kích thước bảng đơn gốc để có thể phục hồi.
    private void CacheDefaultOrderBoard()
    {
        if (orderRoot == null)
        {
            Debug.LogError("Customer recipe view requires an order panel RectTransform.", this);
            return;
        }
        orderBoardImage = orderRoot.GetComponent<Image>();
        if (orderBoardImage == null)
        {
            Debug.LogError("Customer order panel requires an Image for its board artwork.", this);
            return;
        }
        defaultOrderSize = orderRoot.sizeDelta;
        defaultOrderPosition = orderRoot.anchoredPosition;
        defaultOrderBoard = orderBoardImage.sprite;
        defaultOrderBoardType = orderBoardImage.type;
        orderLayout = orderRoot.GetComponent<HorizontalLayoutGroup>();
        defaultRightPadding = orderLayout != null ? orderLayout.padding.right : 0;
        hasCachedOrderBoard = true;
    }

    // Chọn hình bảng theo số món: 1 cột (1x2) cho 1–2 món, 2 cột (2x2, từ 5 món là 2x3) cho 3 món trở lên
    // và cho mọi bảng chi tiết từ 2 món.
    private void ConfigureOrderBoard(int orderCount, bool detail)
    {
        if (!hasCachedOrderBoard)
        {
            CacheDefaultOrderBoard();
            if (!hasCachedOrderBoard) return;
        }

        Sprite board;
        int rows;
        wideBoard = UsesTwoColumns(orderCount, detail);
        if (!wideBoard)
        {
            board = oneColumnTwoRowsBoard;
            // Một món thì ô chiếm trọn bảng để hình món nằm giữa và to nhất có thể.
            rows = Mathf.Clamp(orderCount, 1, 2);
        }
        else if (orderCount <= 4)
        {
            board = twoColumnTwoRowsBoard;
            rows = 2;
        }
        else
        {
            board = twoColumnThreeRowsBoard;
            rows = 3;
        }

        if (board == null)
        {
            Debug.LogError("Customer order board artwork is not assigned for " + orderCount + " orders.", this);
            return;
        }
        orderBoardImage.sprite = board;
        orderBoardImage.type = Image.Type.Simple;
        // Native size của sprite; mép dưới cố định (pivot đáy trong prefab) nên bảng cao hơn chỉ dài lên trên.
        orderBoardImage.SetNativeSize();
        Vector2 size = orderRoot.sizeDelta;
        if (wideBoard && wideBoardWidth > 0)
        {
            // Bảng 2 cột ở native size (307) che mất khách ghế bên cạnh: thu bề ngang cho vừa khoảng giữa hai khách,
            // giữ chiều cao bằng bảng 1 cột. Sprite có viền 9-slice nên góc bo không bị méo.
            if (board.border.sqrMagnitude > 0) orderBoardImage.type = Image.Type.Sliced;
            size = new Vector2(wideBoardWidth, Mathf.Min(size.y, maxBoardHeight));
        }
        else if (size.y > maxBoardHeight) size *= maxBoardHeight / size.y;
        // Xem chi tiết món nhiều nguyên liệu (xếp lưới 2 cột trong mỗi ô): nới bảng theo bề ngang để ảnh nguyên liệu đủ to.
        if (detail && detailGridCellWidth > 0)
        {
            bool grid = false;
            foreach (SushiDishCardView card in cards)
                grid |= !card.IsOrderComplete && card.RecipeIngredientCount > 3;
            int columns = wideBoard ? 2 : 1;
            float padding = orderLayout != null
                ? orderLayout.padding.left + Mathf.Max(orderLayout.padding.left, customer != null && customer.IsRush ? rushRightPadding : 0) +
                    orderLayout.spacing * (columns - 1)
                : 0f;
            float wanted = columns * detailGridCellWidth + padding;
            if (grid && wanted > size.x)
            {
                if (board.border.sqrMagnitude > 0) orderBoardImage.type = Image.Type.Sliced;
                size.x = wanted;
                // Bảng rộng hơn bình thường: giữ trong màn hình như bảng 2 cột.
                wideBoard = true;
            }
        }
        orderRoot.sizeDelta = size;
        orderRoot.anchoredPosition = defaultOrderPosition;
        collapsedOrderHeight = size.y;
        hasCachedOrderHeight = true;

        float spacing = 0;
        VerticalLayoutGroup column = leftColumn != null ? leftColumn.GetComponent<VerticalLayoutGroup>() : null;
        if (column != null) spacing = column.spacing;
        float inner = size.y;
        ApplyRushPadding();
        if (orderLayout != null) inner -= orderLayout.padding.top + orderLayout.padding.bottom;
        // Chia đều chiều cao bảng cho số hàng để thẻ món (và hình món) lấp đầy bảng.
        boardInnerHeight = inner;
        columnSpacing = spacing;
        cellHeight = (inner - spacing * (rows - 1)) / rows;
        foreach (SushiDishCardView card in cards) card.SetCellHeight(cellHeight);
        KeepBoardOnScreen();
    }

    // Khách có thể đổi ghế hoặc còn đang đi vào, nên bảng 2 cột được giữ trong màn hình theo từng khung hình.
    private void LateUpdate() => KeepBoardOnScreen();

    // Khách thường: lề hai bên cân nhau, hình món nằm giữa bảng. Khách vội: lề phải rộng hơn chừa thanh thời gian.
    // Trả về true khi lề đổi (trạng thái vội được gán sau khi dựng bảng).
    private bool ApplyRushPadding()
    {
        if (orderLayout == null) return false;
        int right = customer != null && customer.IsRush ? rushRightPadding : orderLayout.padding.left;
        if (orderLayout.padding.right == right) return false;
        orderLayout.padding.right = right;
        return true;
    }

    // Bảng 2 cột rộng của khách đang ngồi: đẩy ngang vào trong nếu tràn mép màn hình, trả về chỗ cũ khi hết tràn.
    private void KeepBoardOnScreen()
    {
        if (!hasCachedOrderBoard || orderRoot == null) return;
        float targetX = defaultOrderPosition.x;
        Transform parent = orderRoot.parent;
        Camera view = wideBoard && customer != null && customer.IsSeated ? Camera.main : null;
        float scale = parent != null ? parent.lossyScale.x : 0f;
        if (view != null && view.orthographic && Mathf.Abs(scale) > 0.0001f)
        {
            float halfWidth = view.orthographicSize * view.aspect - 0.1f;
            float left = view.transform.position.x - halfWidth, right = view.transform.position.x + halfWidth;
            // Tính mép bảng như khi đang ở vị trí gốc để không cộng dồn độ lệch qua từng khung hình.
            float current = (orderRoot.anchoredPosition.x - defaultOrderPosition.x) * scale;
            orderRoot.GetWorldCorners(boardCorners);
            float boardLeft = boardCorners[0].x - current, boardRight = boardCorners[2].x - current;
            if (boardRight > right) targetX += (right - boardRight) / scale;
            else if (boardLeft < left) targetX += (left - boardLeft) / scale;
        }
        if (Mathf.Abs(orderRoot.anchoredPosition.x - targetX) > 0.01f)
            orderRoot.anchoredPosition = new Vector2(targetX, orderRoot.anchoredPosition.y);
    }

    // Phục hồi hình ảnh và bố cục bảng đơn về cấu hình ban đầu.
    private void RestoreOrderBoard()
    {
        if (!hasCachedOrderBoard || orderRoot == null) return;
        orderRoot.sizeDelta = defaultOrderSize;
        orderRoot.anchoredPosition = defaultOrderPosition;
        orderBoardImage.sprite = defaultOrderBoard;
        orderBoardImage.type = defaultOrderBoardType;
        if (orderLayout != null) orderLayout.padding.right = defaultRightPadding;
        collapsedOrderHeight = defaultOrderSize.y;
        LayoutRebuilder.ForceRebuildLayoutImmediate(orderRoot);
    }

    // Bỏ đăng ký cập nhật đơn khách khi thành phần hiển thị bị hủy.
    private void OnDestroy() { if (customer != null) customer.OnOrderChanged -= Refresh; }
}
