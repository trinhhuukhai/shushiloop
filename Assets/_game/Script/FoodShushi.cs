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
    RigidTray = 4
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
    private Vector2 platedSize;
    private int platedSortingOrder;
    private void Awake()
    {
        if (cover != null) coverScale = cover.transform.localScale;
        if (quantityLabel != null) quantityPosition = quantityLabel.transform.localPosition;
        if (stateLabel != null)
        {
            statePosition = stateLabel.transform.localPosition;
            stateFontSize = stateLabel.fontSize;
        }
    }

    private void OnEnable()
    {
        if (Type != PoolType.None) ApplyIngredientArt();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        if (Application.isPlaying) return;
        if (poolType == PoolType.None) poolType = PoolType.SushiRice;
        QueueEditorArtRefresh();
    }

    private void OnValidate() { QueueEditorArtRefresh(); }

    private void QueueEditorArtRefresh()
    {
        // OnValidate có thể chạy khi import trên luồng phụ; đổi Sprite ở delayCall của Editor.
        UnityEditor.EditorApplication.delayCall -= RefreshEditorArt;
        UnityEditor.EditorApplication.delayCall += RefreshEditorArt;
    }

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

    public void SetupFromData(FoodData data)
    {
        transform.DOKill();
        revealTween?.Kill();
        revealTween = null;
        foodLocation = FoodLocation.Board;
        platedSprite = null;
        Owner = null;
        poolType = data.typeFood;
        amount = Mathf.Max(0, data.amount);
        hidden = data.hidden || data.foodState == FoodState.HoiCham;
        iceLayers = data.foodState == FoodState.Ice ? Mathf.Max(1, data.iceLayers) : Mathf.Max(0, data.iceLayers);
        linkId = data.linkId > 0 ? data.linkId : data.lockId;
        trayId = data.trayId;
        boardVisible = true;
        ApplyIngredientArt();
        cover.transform.localScale = coverScale;
        RefreshVisual();
    }

    public void Setup(PoolType type, FoodState state = FoodState.Normal)
    {
        SetupFromData(new FoodData { typeFood = type, foodState = state });
    }

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
        float scale = platedSprite != null
            ? Mathf.Min(Mathf.Max(0.01f, platedSize.x) / Mathf.Max(0.01f, bounds.size.x),
                Mathf.Max(0.01f, platedSize.y) / Mathf.Max(0.01f, bounds.size.y))
            : 1.05f / Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.y));
        foodRenderer.transform.localScale = Vector3.one * scale;
        foodRenderer.transform.localPosition = -bounds.center * scale;
    }

    public void SetPlatedArtwork(Sprite sprite, Vector2 size, int sortingOrder)
    {
        platedSprite = sprite;
        platedSize = size;
        platedSortingOrder = sortingOrder;
        ApplyIngredientArt();
        RefreshVisual();
    }

    public bool CanPick()
    {
        return foodLocation == FoodLocation.Board && !hidden && iceLayers == 0 &&
            amount > 0 && !IsRevealing;
    }

    public void Reveal()
    {
        if (!hidden || IsRevealing) return;
        revealTween = DOTween.Sequence()
            .Append(cover.transform.DOScaleX(0f, 0.125f))
            .AppendCallback(() => { hidden = false; RefreshVisual(); })
            .AppendInterval(0.125f)
            .OnComplete(() => { revealTween = null; cover.transform.localScale = coverScale; });
    }

    public void BreakIce(int damage = 1)
    {
        if (damage <= 0 || iceLayers == 0) return;
        iceLayers = Mathf.Max(0, iceLayers - damage);
        if (iceLayers == 0) SushiWorldVisual.BurstIce(Owner != null ? Owner.Board?.transform : null, transform.position);
        RefreshVisual();
    }

    public void Unlock(int keyId)
    {
        if (linkId != keyId) return;
        BreakLink();
    }

    public void BreakLink() { linkId = 0; RefreshVisual(); }
    internal void SetAmount(int value) { amount = Mathf.Max(0, value); RefreshVisual(); }
    internal void SetBoardVisible(bool value)
    {
        if (boardVisible == value) return;
        boardVisible = value;
        RefreshVisual();
    }

    internal void CopyBoardBase(Transform parent)
    {
        if (tileBackground == null) return;
        GameObject view = Instantiate(tileBackground.gameObject, parent, true);
        view.name = "Empty base";
        view.SetActive(boardVisible);
    }

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
        quantityLabel.gameObject.SetActive(visible && onBoard && !hidden && amount > 1);
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

    public void RefreshArtwork()
    {
        ApplyIngredientArt();
        if (Application.isPlaying && quantityLabel != null) RefreshVisual();
    }

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

    public bool TryMoveToConveyor()
    {
        if (!CanPick() || linkId > 0 || trayId > 0 || amount != 1) return false;
        BeginConveyorMove(false);
        return true;
    }

    public void ArriveAtConveyor()
    {
        if (foodLocation != FoodLocation.MovingToConveyor) return;
        foodLocation = FoodLocation.Conveyor;
        RefreshVisual();
    }

    public bool TryMoveToCustomerPlate(bool allowPickupFlight = false)
    {
        if (foodLocation != FoodLocation.Conveyor && !(allowPickupFlight && foodLocation == FoodLocation.MovingToConveyor)) return false;
        foodLocation = FoodLocation.MovingToCustomerPlate;
        RefreshVisual();
        return true;
    }

    public void ArriveAtCustomerPlate()
    {
        if (foodLocation != FoodLocation.MovingToCustomerPlate) return;
        foodLocation = FoodLocation.CustomerPlate;
        RefreshVisual();
    }

    public void CancelMoveToCustomerPlate(FoodLocation previousLocation = FoodLocation.Conveyor)
    {
        if (foodLocation != FoodLocation.MovingToCustomerPlate) return;
        foodLocation = previousLocation;
        RefreshVisual();
    }

    internal void RevealForMagnet()
    {
        revealTween?.Kill();
        revealTween = null;
        hidden = false;
        if (cover != null) cover.transform.localScale = coverScale;
        RefreshVisual();
    }

    private void OnDisable()
    {
        transform.DOKill();
        revealTween?.Kill();
        revealTween = null;
    }

}
