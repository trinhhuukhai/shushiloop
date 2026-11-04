using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FoodInputController : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    [SerializeField] private ConveyorManager conveyorManager;
    [SerializeField] private LayerMask columnMask = ~0;
    public FoodPickMode PickMode { get; private set; }

    // Hoán đổi hàng: cờ shuffle giữ true tới khi board hết chuyển động.
    private bool shuffleInProgress;

    private bool CanInteract => (!SushiLevelOneTutorial.IsInputLocked || ControlTutorial.IsConfirmingPickup) &&
        (LevelManager.Ins == null || LevelManager.Ins.CanInteract);
    // Bật chế độ gắp bằng đũa khi được phép tương tác và còn quyền dùng booster.
    public void SelectChopsticks() { if (CanInteract && CanUseClickBooster) SetPickMode(FoodPickMode.Chopsticks); }
    // Bật chế độ chọn món cho nam châm khi được phép tương tác và dùng booster.
    public void SelectMagnet() { if (CanInteract && CanUseMagnetBooster) SetPickMode(FoodPickMode.Magnet); }
    // Đưa thao tác chọn nguyên liệu về chế độ chạm thông thường.
    public void CancelBooster() { SetPickMode(FoodPickMode.Tap); }

    // Đổi chế độ chọn, đồng bộ dừng băng chuyền, lớp che hàng dưới và cờ booster.
    private void SetPickMode(FoodPickMode mode)
    {
        if (PickMode != mode)
        {
            PickMode = mode;
            if (conveyorManager != null) conveyorManager.SetMagnetPaused(mode == FoodPickMode.Magnet);
            BoardShishiManager board = BoardShishiManager.Ins;
            if (board != null)
                foreach (FoodQueue column in board.FoodQueues)
                    if (column != null) column.SetLowerRowsShadeVisible(mode != FoodPickMode.Chopsticks);
        }
        // Luôn đồng bộ cờ, kể cả khi mode không đổi (tránh cờ bị kẹt true).
        SyncBoosterFlags();
    }

    // Đánh dấu booster đang dùng vào BoosterManager; về Tap thì trả lại false.
    private void SyncBoosterFlags()
    {
        // Cancelling also runs during scene teardown, after the manager may already be destroyed.
        BoosterManager manager = PickMode == FoodPickMode.Tap && !shuffleInProgress
            ? BoosterManager.Existing : BoosterManager.Ins;
        if (manager == null) return;
        manager.isUsingBoosterClick = PickMode == FoodPickMode.Chopsticks;
        manager.isUsingBoosterMagnet = PickMode == FoodPickMode.Magnet;
        manager.isUsingBoosterShuffle = shuffleInProgress;
    }

    // Thử hoán đổi hàng, chỉ trừ booster khi bàn thay đổi và giữ cờ dùng đến hết chuyển động.
    public void UseSwapRow()
    {
        if (SushiLevelOneTutorial.IsInputLocked || LevelManager.Ins == null || !LevelManager.Ins.CanInteract || BoosterCount(BoosterType.Shuffle) == 0) return;
        shuffleInProgress = true;
        SyncBoosterFlags();
        if (!LevelManager.Ins.Board.SwapRow())
        {
            // Không hoán đổi được: trả cờ ngay.
            shuffleInProgress = false;
            SyncBoosterFlags();
            return;
        }
        ConsumeBooster(BoosterType.Shuffle);
        CancelBooster();
    }

    // Tests and editor fixtures without a loaded profile keep unlimited boosters.
    // Đọc số booster trong hồ sơ; trả về -1 để dùng không giới hạn khi chưa nạp hồ sơ.
    public static int BoosterCount(BoosterType type)
    {
        if (DataManager.ins == null || !DataManager.ins.isLoaded || GameDataFragment.cur == null ||
            GameDataFragment.cur.dataPlayer == null) return -1;
        var player = GameDataFragment.cur.dataPlayer;
        return type == BoosterType.Click ? player.boosterClick : type == BoosterType.Shuffle ? player.boosterSuffle : player.boosterHut;
    }

    // Trừ một lượt booster khi đang dùng dữ liệu người chơi thực tế.
    private static void ConsumeBooster(BoosterType type)
    {
        if (BoosterCount(type) >= 0) GameDataFragment.cur.ChangeBoosterCount(type, -1, "GamePlay");
    }

    private static bool CanUseClickBooster => BoosterCount(BoosterType.Click) < 0 ||
        (GameDataFragment.cur != null && GameDataFragment.cur.CanUseBooster(BoosterType.Click));
    public static bool CanUseMagnetBooster => BoosterCount(BoosterType.Magnet) < 0 ||
        (GameDataFragment.cur != null && GameDataFragment.cur.CanUseBooster(BoosterType.Magnet));

    // Lấy camera chính nếu chưa gán camera xử lý thao tác.
    private void Awake() { if (gameCamera == null) gameCamera = Camera.main; }

    // Thử gắp nguyên liệu bằng đũa, trừ lượt và thoát chế độ booster nếu thành công.
    public bool TryUseChopsticks(FoodShushi food)
    {
        if (!CanInteract || !CanUseClickBooster || PickMode != FoodPickMode.Chopsticks || conveyorManager == null ||
            !conveyorManager.TryTakeChopsticks(food)) return false;
        ConsumeBooster(BoosterType.Click);
        CancelBooster();
        return true;
    }

    // Thử hút đủ nguyên liệu cho món đã chọn, trừ lượt và thoát chế độ booster nếu thành công.
    public bool TryUseMagnet(Customer customer, SushiDishOrder dish)
    {
        if (!CanInteract || !CanUseMagnetBooster || PickMode != FoodPickMode.Magnet || conveyorManager == null || LevelManager.Ins == null ||
            !conveyorManager.TryTakeMagnet(LevelManager.Ins.Board, customer, dish)) return false;
        ConsumeBooster(BoosterType.Magnet);
        CancelBooster();
        return true;
    }

    // Kết thúc hoặc hủy trạng thái booster theo diễn biến màn chơi và cập nhật các món có thể hút.
    private void LateUpdate()
    {
        if (!CanInteract)
        {
            shuffleInProgress = false;
            if (!ControlTutorial.KeepsBoosterSelected) CancelBooster();
        }
        else if (shuffleInProgress)
        {
            // Hết chuyển động của board thì coi như dùng xong shuffle.
            LevelManager level = LevelManager.Ins;
            if (level == null || level.Board == null || !level.Board.HasPendingMovement)
            {
                shuffleInProgress = false;
                SyncBoosterFlags();
            }
        }
        if (conveyorManager == null || conveyorManager.Customers == null) return;
        foreach (Customer customer in conveyorManager.Customers.ActiveCustomers)
            if (customer != null) customer.GetComponent<CustomerRecipeView>()?.SetMagnetTargeting(PickMode == FoodPickMode.Magnet);
    }

    // Hủy booster và bỏ đánh dấu các mục tiêu nam châm khi bộ điều khiển bị tắt.
    private void OnDisable()
    {
        shuffleInProgress = false;
        CancelBooster();
        if (conveyorManager == null || conveyorManager.Customers == null) return;
        foreach (Customer customer in conveyorManager.Customers.ActiveCustomers)
            if (customer != null) customer.GetComponent<CustomerRecipeView>()?.SetMagnetTargeting(false);
    }

    // Nhận thao tác chạm hoặc chuột, ưu tiên UI và tutorial rồi xử lý xem món, gắp hoặc hút nguyên liệu.
    private void Update()
    {
        if (!TryGetPressPosition(out Vector2 screenPosition)) return;
        if (gameCamera == null || conveyorManager == null) return;
        // Kiểm tra UI trực tiếp, kể cả touch mới xuất hiện trong frame hiện tại.
        if (EventSystem.current != null)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
            var uiHits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, uiHits);
            if (uiHits.Count > 0) return;
        }
        if (ControlTutorial.IsInputLocked)
        {
            ControlTutorial.Ins.TryHandlePress(screenPosition, gameCamera);
            return;
        }
        if (!CanInteract) return;
        Ray ray = gameCamera.ScreenPointToRay(screenPosition);
        if (PickMode == FoodPickMode.Tap && conveyorManager.Customers != null)
        {
            foreach (Customer customer in conveyorManager.Customers.ActiveCustomers)
            {
                if (customer == null) continue;
                var plane = new Plane(Vector3.forward, customer.transform.position);
                if (!plane.Raycast(ray, out float distance)) continue;
                if (customer.GetComponent<CustomerRecipeView>()?.ToggleRecipeDetailsAtWorldPosition(ray.GetPoint(distance)) == true)
                    return;
            }
        }
        if (PickMode == FoodPickMode.Magnet)
        {
            if (conveyorManager.Customers == null) return;
            foreach (Customer customer in conveyorManager.Customers.ActiveCustomers)
            {
                if (customer == null) continue;
                var plane = new Plane(Vector3.forward, customer.transform.position);
                if (!plane.Raycast(ray, out float distance)) continue;
                SushiDishOrder dish = customer.GetComponent<CustomerRecipeView>()?.DishAtWorldPosition(ray.GetPoint(distance));
                if (dish == null) continue;
                TryUseMagnet(customer, dish);
                return;
            }
            return;
        }
        foreach (RaycastHit2D hit in Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity, columnMask))
        {
            FoodQueue column = hit.collider.GetComponentInParent<FoodQueue>();
            if (column == null) continue;
            column.SetConveyor(conveyorManager);
            if (PickMode == FoodPickMode.Tap) column.OnClickColumn();
            else
            {
                FoodShushi food = column.FoodAtWorldPosition(hit.point);
                if (!TryUseChopsticks(food)) column.Shake();
            }
            return;
        }
    }

    // Đọc vị trí bắt đầu chạm hoặc nhấn chuột từ hệ thống input đang được bật.
    private bool TryGetPressPosition(out Vector2 position)
    {
        position = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) return false;
            position = touch.position;
            return true;
        }
        if (Input.GetMouseButtonDown(0)) { position = Input.mousePosition; return true; }
#endif
        return false;
    }
}
