using UnityEngine;

public enum FoodState
{
    Normal,
    HoiCham,
    Ice,
    Lock
}

// cơm, tôm, trứng cuộn, hạt mè, cá hồi, thanh cua, dưa leo, bạch tuộc, trứng cá, cá ngừ, lươn nướng
//public enum TypeFood
//{

//}

public enum FoodLocation
{
    Board,
    MovingToConveyor,
    Conveyor,
    MovingToCustomerPlate,
    CustomerPlate
}

public class FoodShushi : GameUnit
{
    [Header("Thông tin món ăn")]
    [SerializeField] private FoodState foodState;

    //[Header("Trạng thái đặc biệt")]
    //[Min(0)]
    //[SerializeField] private int iceLayers;
    //[SerializeField] private int lockId;

    [Header("Hiển thị")]
    [Header("Hình ảnh món ăn")]
    [SerializeField] private Sprite rawSprite;
    [SerializeField] private Sprite cookedSprite;
    [SerializeField] private SpriteRenderer foodRenderer;
    //[SerializeField] private GameObject questionMarkObject;
    //[SerializeField] private GameObject iceObject;
    //[SerializeField] private GameObject lockObject;

    [Header("Vị trí hiện tại")]
    [SerializeField] private FoodLocation foodLocation = FoodLocation.Board;
    public FoodLocation Location => foodLocation;




    public PoolType Type => poolType;
    public FoodState State => foodState;
    //public int IceLayers => iceLayers;
    //public int LockId => lockId;

    private void Awake()
    {
        RefreshVisual();
    }
    public void SetupFromData(FoodData data)
    {
        Setup(
            data.typeFood,
            rawSprite,
            data.foodState
        );
    }
    public void Setup(PoolType type, Sprite sprite, FoodState state = FoodState.Normal)
    {
        poolType = type;
        foodState = state;
        //iceLayers = state == FoodState.Ice? Mathf.Max(1, initialIceLayers) : 0;
        //lockId = state == FoodState.Lock ? initialLockId : 0;

        if (foodRenderer != null) foodRenderer.sprite = sprite;

        RefreshVisual();
    }

    // Chỉ kiểm tra trạng thái của Food.
    // FoodQueue kiểm tra món đầu cột và băng chuyền còn chỗ.
    public bool CanPick()
    {
        return foodLocation == FoodLocation.Board
            && foodState == FoodState.Normal;
    }

    public void Reveal()
    {
        if (foodState != FoodState.HoiCham) return;
        foodState = FoodState.Normal;
        RefreshVisual();
    }

    public void BreakIce(int damage = 1)
    {
        if (foodState != FoodState.Ice || damage <= 0) return;
        //iceLayers = Mathf.Max(0, iceLayers - damage);
        //if (iceLayers == 0) foodState = FoodState.Normal;
        RefreshVisual();
    }

    public void Unlock(int keyId)
    {
        //if (foodState != FoodState.Lock || keyId != lockId)  return;
        foodState = FoodState.Normal;
        //lockId = 0;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        bool isHidden = foodState == FoodState.HoiCham;

        // Trong lúc bay từ board, vẫn giữ hình sống.
        bool isCooked =
            foodLocation == FoodLocation.Conveyor ||
            foodLocation == FoodLocation.MovingToCustomerPlate ||
            foodLocation == FoodLocation.CustomerPlate;

        if (foodRenderer != null)
        {
            foodRenderer.sprite = isCooked ? cookedSprite : rawSprite;
            foodRenderer.enabled = !isHidden;
        }

        //if (questionMarkObject != null)
        //    questionMarkObject.SetActive(isHidden);

        //if (iceObject != null)
        //    iceObject.SetActive(foodState == FoodState.Ice);

        //if (lockObject != null)
        //    lockObject.SetActive(foodState == FoodState.Lock);
    }

    public bool TryMoveToConveyor()
    {
        if (!CanPick()) return false;
        foodLocation = FoodLocation.MovingToConveyor;
        return true;
    }

    // Gọi khi animation bay lên băng chuyền hoàn tất.
    public void ArriveAtConveyor()
    {
        if (foodLocation != FoodLocation.MovingToConveyor) return;
        foodLocation = FoodLocation.Conveyor;
    }

    public bool TryMoveToCustomerPlate()
    {
        if (foodLocation != FoodLocation.Conveyor) return false;
        foodLocation = FoodLocation.MovingToCustomerPlate;
        return true;
    }

    // Gọi khi animation bay tới đĩa khách hoàn tất.
    public void ArriveAtCustomerPlate()
    {
        if (foodLocation != FoodLocation.MovingToCustomerPlate) return;
        foodLocation = FoodLocation.CustomerPlate;
    }

    public void CancelMoveToCustomerPlate()
    {
        if (foodLocation != FoodLocation.MovingToCustomerPlate)  return;

        foodLocation = FoodLocation.Conveyor;
        RefreshVisual();
    }
}
