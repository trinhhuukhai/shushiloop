using DG.Tweening;
using TMPro;
using UnityEngine;

// Giữ các giá trị enum cũ để không làm mất dữ liệu prefab/level.
public enum FoodState
{
    Normal = 0,
    HoiCham = 1, // hidden_tile
    Ice = 2,     // frozen_tile
    Lock = 3,    // linked_tile (lockId cũ là linkId)
    RigidTray = 4,
    Time = 5,
}

public enum FoodLocation
{
    Board, MovingToConveyor, Conveyor, MovingToCustomerPlate, CustomerPlate
}

public class FoodShushi : GameUnit
{
    [SerializeField] private FoodState foodState;
    [SerializeField] private SpriteRenderer foodRenderer;
    [SerializeField] private SushiIngredientArtLibrary ingredientArt;
    [SerializeField] private FoodLocation foodLocation;
    [SerializeField, Range(1, 3)] private int amount = 1;
    [SerializeField] private int iceLayers;
    [SerializeField] private int linkId;
    [SerializeField] private int trayId;
    [SerializeField] private bool hidden;

    [Header("Prefab visuals")]
    [SerializeField] private TextMeshPro quantityLabel;
    [SerializeField] private SpriteRenderer quantityBadge;
    [SerializeField] private Sprite quantityTwo;
    [SerializeField] private Sprite quantityThree;
    [SerializeField] private TextMeshPro stateLabel;
    [SerializeField] private SpriteRenderer cover;
    [SerializeField] private SpriteRenderer tileBackground;
    [SerializeField] private Vector3 frozenQuantityPosition = new Vector3(-0.28f, 0.32f, -0.1f);
    [SerializeField] private Vector3 frozenStatePosition = new Vector3(0.25f, -0.31f, -0.3f);
    [SerializeField] private float frozenStateFontSize = 2.6f;
    private Vector3 quantityPosition;
    private Vector3 statePosition;
    private float stateFontSize;
    private Vector3 coverScale = Vector3.one;
    private bool boardVisible = true;
    private Tween revealTween;
    private Sprite platedSprite;
    private Sprite defaultBoardBase;
    private Vector2 platedSize;
    private int platedSortingOrder;
    // Lưu thông số hình ảnh ban đầu của nguyên liệu để phục hồi khi đổi trạng thái.
    private void Awake()
    {
        if (tileBackground != null) defaultBoardBase = tileBackground.sprite;
        if (cover != null) coverScale = cover.transform.localScale;
        if (quantityLabel != null) quantityPosition = quantityLabel.transform.localPosition;
        if (stateLabel != null)
        {
            statePosition = stateLabel.transform.localPosition;
            stateFontSize = stateLabel.fontSize;
        }
    }

    // Cập nhật hình ảnh nguyên liệu khi đối tượng được kích hoạt.
    private void OnEnable()
    {
        if (Type != PoolType.None) ApplyIngredientArt();
    }

#if UNITY_EDITOR
    // Chọn loại cơm mặc định nếu chưa có loại nguyên liệu và lên lịch làm mới hình ảnh trong Editor.
    private void Reset()
    {
        if (Application.isPlaying) return;
        if (poolType == PoolType.None) poolType = PoolType.SushiRice;
        QueueEditorArtRefresh();
    }

    // Lên lịch làm mới hình ảnh khi chỉnh dữ liệu nguyên liệu trong Inspector.
    private void OnValidate() { QueueEditorArtRefresh(); }

    // Đăng ký cập nhật hình ảnh trễ trong Editor để tránh sửa đối tượng lúc đang kiểm tra dữ liệu.
    private void QueueEditorArtRefresh()
    {
        // OnValidate có thể chạy khi import trên luồng phụ; đổi Sprite ở delayCall của Editor.
        UnityEditor.EditorApplication.delayCall -= RefreshEditorArt;
        UnityEditor.EditorApplication.delayCall += RefreshEditorArt;
    }

    // Làm mới hình ảnh nguyên liệu trong Editor khi đối tượng còn hợp lệ.
    private void RefreshEditorArt()
    {
        if (this == null || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        bool assignedReferences = false;
        if (foodRenderer == null)
        {
            foodRenderer = GetComponentInChildren<SpriteRenderer>(true);
            assignedReferences = foodRenderer != null;
        }
        if (ingredientArt == null)
        {
            ingredientArt = UnityEditor.AssetDatabase.LoadAssetAtPath<SushiIngredientArtLibrary>(
                "Assets/_game/Art/Ingredients/SushiIngredientArt.asset");
            assignedReferences |= ingredientArt != null;
        }
        if (assignedReferences) UnityEditor.EditorUtility.SetDirty(this);
        if (foodRenderer == null || ingredientArt == null || Type == PoolType.None) return;
        Sprite previous = foodRenderer.sprite;
        Vector3 previousScale = foodRenderer.transform.localScale;
        Vector3 previousPosition = foodRenderer.transform.localPosition;
        Color previousColor = foodRenderer.color;
        ApplyIngredientArt();
        if (previous == foodRenderer.sprite && previousScale == foodRenderer.transform.localScale &&
            previousPosition == foodRenderer.transform.localPosition && previousColor == foodRenderer.color) return;
        UnityEditor.EditorUtility.SetDirty(foodRenderer);
        UnityEditor.EditorUtility.SetDirty(foodRenderer.transform);
        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(foodRenderer))
        {
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(foodRenderer);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(foodRenderer.transform);
        }
    }
#endif

    public FoodQueue Owner { get; internal set; }
    public PoolType Type => poolType;
    public TypeTutFood TutorialType { get; private set; }
    public FoodState State => foodState;
    public FoodLocation Location => foodLocation;
    public int Amount => amount;
    public int IceLayers => iceLayers;
    public int LinkId => linkId;
    public int TrayId => trayId;
    public bool IsHidden => hidden;
    public bool IsRevealing => revealTween != null && revealTween.IsActive();
    public bool CanUseChopsticks => foodLocation == FoodLocation.Board &&
        iceLayers == 0 && amount > 0 && !IsRevealing;

    // Khôi phục trạng thái ô trên bàn từ dữ liệu, gồm loại, số lượng, lớp che, băng, liên kết và khay.
    public void SetupFromData(FoodData data)
    {
        transform.DOKill();
        revealTween?.Kill();
        revealTween = null;
        foodLocation = FoodLocation.Board;
        platedSprite = null;
        Owner = null;
        poolType = data.typeFood;
        TutorialType = data.tutFood;
        amount = Mathf.Max(0, data.amount);
        hidden = data.hidden || data.foodState == FoodState.HoiCham;
        iceLayers = data.foodState == FoodState.Ice ? Mathf.Max(1, data.iceLayers) : Mathf.Max(0, data.iceLayers);
        linkId = data.linkId > 0 ? data.linkId : data.lockId;
        trayId = data.trayId;
        boardVisible = true;
        SetBoardBaseSprite(null);
        ApplyIngredientArt();
        cover.transform.localScale = coverScale;
        RefreshVisual();
    }

    // Khởi tạo nhanh nguyên liệu từ loại thực phẩm và trạng thái ô.
    public void Setup(PoolType type, FoodState state = FoodState.Normal)
    {
        SetupFromData(new FoodData { typeFood = type, foodState = state });
    }

    // Chọn sprite nguyên liệu hoặc sprite trên đĩa, rồi căn tâm và kích thước hiển thị.
    private void ApplyIngredientArt()
    {
        if (foodRenderer == null) foodRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (foodRenderer == null) return;
        if (ingredientArt == null && SushiArtCatalog.Default != null) ingredientArt = SushiArtCatalog.Default.ingredientImages;
        Sprite sprite = platedSprite != null ? platedSprite : ingredientArt != null ? ingredientArt.GetSprite(Type) : null;
        if (sprite == null && SushiArtCatalog.Default != null) sprite = SushiArtCatalog.Default.GetIngredient(Type);
        foodRenderer.sprite = sprite;
        if (sprite == null) return;
        if (foodRenderer.transform == transform) return;
        Bounds bounds = foodRenderer.sprite.bounds;
        Vector3 scale;
        if (platedSprite != null)
        {
            float widthScale = Mathf.Max(0.01f, platedSize.x) / Mathf.Max(0.01f, bounds.size.x);
            float heightScale = Mathf.Max(0.01f, platedSize.y) / Mathf.Max(0.01f, bounds.size.y);
            scale = new Vector3(widthScale, heightScale, 1);
        }
        else scale = Vector3.one * (1.05f / Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.y)));
        foodRenderer.transform.localScale = scale;
        foodRenderer.transform.localPosition = -Vector3.Scale(bounds.center, scale);
    }

    // Gán hình ảnh, kích thước và thứ tự vẽ của nguyên liệu khi đặt lên đĩa.
    public void SetPlatedArtwork(Sprite sprite, Vector2 size, int sortingOrder)
    {
        platedSprite = sprite;
        platedSize = size;
        platedSortingOrder = sortingOrder;
        ApplyIngredientArt();
        RefreshVisual();
    }

    // Kiểm tra nguyên liệu trên bàn có thể lấy thường: còn số lượng, không bị che, đóng băng hay đang lật.
    public bool CanPick()
    {
        return foodLocation == FoodLocation.Board && !hidden && iceLayers == 0 &&
            amount > 0 && !IsRevealing;
    }

    // Chạy hiệu ứng lật lớp che để lộ nguyên liệu.
    public void Reveal()
    {
        if (!hidden || IsRevealing) return;
        revealTween = DOTween.Sequence()
            .Append(cover.transform.DOScaleX(0f, 0.125f))
            .AppendCallback(() => { hidden = false; RefreshVisual(); })
            .AppendInterval(0.125f)
            .OnComplete(() => { revealTween = null; cover.transform.localScale = coverScale; });
    }

    // Giảm lớp băng, phát hiệu ứng khi băng vỡ hết và cập nhật hình ảnh ô.
    public void BreakIce(int damage = 1)
    {
        if (damage <= 0 || iceLayers == 0) return;
        iceLayers = Mathf.Max(0, iceLayers - damage);
        if (iceLayers == 0 && boardVisible) SushiWorldVisual.BurstIce(Owner != null ? Owner.Board?.transform : null, transform.position);
        RefreshVisual();
    }

    // Gỡ liên kết khi mã mở khóa khớp với mã của nguyên liệu.
    public void Unlock(int keyId)
    {
        if (linkId != keyId) return;
        BreakLink();
    }

    // Xóa mã liên kết và cập nhật trạng thái hiển thị.
    public void BreakLink() { linkId = 0; RefreshVisual(); }
    // Cập nhật số lượng nguyên liệu, chặn giá trị âm và làm mới nhãn số lượng.
    internal void SetAmount(int value) { amount = Mathf.Max(0, value); RefreshVisual(); }
    // Đổi sprite đế ô, dùng đế mặc định khi không có sprite thay thế.
    internal void SetBoardBaseSprite(Sprite sprite)
    {
        if (tileBackground != null) tileBackground.sprite = sprite != null ? sprite : defaultBoardBase;
    }

    // Đổi trạng thái hiển thị ô trên bàn và đồng bộ các thành phần hình ảnh.
    internal void SetBoardVisible(bool value)
    {
        if (boardVisible == value) return;
        boardVisible = value;
        RefreshVisual();
    }

    // Sao chép đế ô sang nhóm cha để giữ hình ảnh khay trống khi nguyên liệu rời đi.
    internal void CopyBoardBase(Transform parent)
    {
        if (tileBackground == null) return;
        GameObject view = Instantiate(tileBackground.gameObject, parent, true);
        view.name = "Empty base";
        view.SetActive(boardVisible);
    }

    // Đồng bộ sprite, lớp che, số lượng và thứ tự vẽ theo vị trí cùng trạng thái nguyên liệu.
    private void RefreshVisual()
    {
        foodState = hidden ? FoodState.HoiCham : iceLayers > 0 ? FoodState.Ice :
            linkId > 0 ? FoodState.Lock : trayId > 0 ? FoodState.RigidTray : FoodState.Normal;
        bool onBoard = foodLocation == FoodLocation.Board;
        bool visible = !onBoard || boardVisible;
        if (foodRenderer != null)
        {
            foodRenderer.enabled = visible && !hidden && amount > 0;
            foodRenderer.sortingOrder = platedSprite != null ? platedSortingOrder :
                foodLocation == FoodLocation.MovingToCustomerPlate || foodLocation == FoodLocation.MovingToConveyor ? 30 :
                foodLocation == FoodLocation.CustomerPlate ? 18 : 1;
        }
        if (quantityLabel == null) return;
        quantityLabel.text = "×" + amount;
        quantityLabel.transform.localPosition = iceLayers > 0 ? frozenQuantityPosition : quantityPosition;
        tileBackground.gameObject.SetActive(visible && onBoard);
        bool showQuantity = visible && onBoard && !hidden && amount > 1;
        Sprite badgeSprite = amount == 2 ? quantityTwo : amount == 3 ? quantityThree : null;
        if (quantityBadge != null)
        {
            quantityBadge.sprite = badgeSprite;
            quantityBadge.transform.localPosition = quantityLabel.transform.localPosition;
            quantityBadge.gameObject.SetActive(showQuantity && badgeSprite != null);
        }
        quantityLabel.gameObject.SetActive(showQuantity && (quantityBadge == null || badgeSprite == null));
        bool showCover = visible && (hidden || (onBoard && iceLayers > 0));
        cover.gameObject.SetActive(showCover);
        SushiArtCatalog catalog = SushiArtCatalog.Default;
        Sprite element = catalog != null ? (hidden ? catalog.hiddenTile : catalog.frozenTile) : null;
        cover.sprite = element != null ? element : SushiWorldVisual.WhiteSprite;
        cover.color = element != null ? Color.white : hidden ? new Color(0.68f, 0.37f, 0.14f) : new Color(0.3f, 0.8f, 1f, 0.55f);
        stateLabel.text = hidden ? (element == null ? "?" : "") : iceLayers > 0 ? iceLayers.ToString() : "";
        stateLabel.transform.localPosition = !hidden && element != null ? frozenStatePosition : statePosition;
        stateLabel.fontSize = !hidden && element != null ? frozenStateFontSize : stateFontSize;
        stateLabel.color = !hidden && element != null ? Color.white : new Color(0.2f, 0.1f, 0.04f);
        stateLabel.gameObject.SetActive(showCover && stateLabel.text.Length > 0);
    }

    // Làm mới sprite nguyên liệu và cập nhật hình ảnh trạng thái khi đang chơi.
    public void RefreshArtwork()
    {
        ApplyIngredientArt();
        if (Application.isPlaying && quantityLabel != null) RefreshVisual();
    }

    // Tách nguyên liệu khỏi cột, chuẩn hóa thành một phần và chuyển sang trạng thái bay tới băng chuyền.
    internal void BeginConveyorMove(bool revealHidden)
    {
        Owner = null;
        amount = 1;
        hidden = revealHidden;
        iceLayers = linkId = trayId = 0;
        foodLocation = FoodLocation.MovingToConveyor;
        RefreshVisual();
        if (hidden) Reveal();
    }

    // Thử đưa ô đơn lấy được lên băng chuyền khi không thuộc liên kết hoặc khay.
    public bool TryMoveToConveyor()
    {
        if (!CanPick() || linkId > 0 || trayId > 0 || amount != 1) return false;
        BeginConveyorMove(false);
        return true;
    }

    // Xác nhận nguyên liệu đã đáp xuống băng chuyền và cập nhật hình ảnh.
    public void ArriveAtConveyor()
    {
        if (foodLocation != FoodLocation.MovingToConveyor) return;
        foodLocation = FoodLocation.Conveyor;
        RefreshVisual();
    }

    // Thử chuyển nguyên liệu sang trạng thái bay tới đĩa khách từ vị trí được phép.
    public bool TryMoveToCustomerPlate(bool allowPickupFlight = false)
    {
        if (foodLocation != FoodLocation.Conveyor && !(allowPickupFlight && foodLocation == FoodLocation.MovingToConveyor)) return false;
        foodLocation = FoodLocation.MovingToCustomerPlate;
        RefreshVisual();
        return true;
    }

    // Xác nhận nguyên liệu đã đến đĩa khách và cập nhật thứ tự hiển thị.
    public void ArriveAtCustomerPlate()
    {
        if (foodLocation != FoodLocation.MovingToCustomerPlate) return;
        foodLocation = FoodLocation.CustomerPlate;
        RefreshVisual();
    }

    // Hủy chuyến giao tới khách và khôi phục trạng thái vị trí trước đó.
    public void CancelMoveToCustomerPlate(FoodLocation previousLocation = FoodLocation.Conveyor)
    {
        if (foodLocation != FoodLocation.MovingToCustomerPlate) return;
        foodLocation = previousLocation;
        RefreshVisual();
    }

    // Bỏ lớp che ngay khi dùng nam châm và dừng hiệu ứng lật còn chạy.
    internal void RevealForMagnet()
    {
        revealTween?.Kill();
        revealTween = null;
        hidden = false;
        if (cover != null) cover.transform.localScale = coverScale;
        RefreshVisual();
    }

    // Hủy tween di chuyển và lật ô khi nguyên liệu bị tắt hoặc trả về pool.
    private void OnDisable()
    {
        transform.DOKill();
        revealTween?.Kill();
        revealTween = null;
    }

}
