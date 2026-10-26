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

    private bool CanInteract => !SushiLevelOneTutorial.IsInputLocked && (LevelManager.Ins == null || LevelManager.Ins.CanInteract);
    public void SelectChopsticks() { if (CanInteract && BoosterCount(BoosterType.Click) != 0) PickMode = FoodPickMode.Chopsticks; }
    public void SelectMagnet() { if (CanInteract && BoosterCount(BoosterType.Magnet) != 0) PickMode = FoodPickMode.Magnet; }
    public void CancelBooster() { PickMode = FoodPickMode.Tap; }
    public void UseSwapRow()
    {
        if (SushiLevelOneTutorial.IsInputLocked || LevelManager.Ins == null || !LevelManager.Ins.CanInteract || BoosterCount(BoosterType.Shuffle) == 0) return;
        if (!LevelManager.Ins.Board.SwapRow()) return;
        ConsumeBooster(BoosterType.Shuffle);
        CancelBooster();
    }

    // Tests and editor fixtures without a loaded profile keep unlimited boosters.
    public static int BoosterCount(BoosterType type)
    {
        if (DataManager.ins == null || !DataManager.ins.isLoaded || GameDataFragment.cur == null ||
            GameDataFragment.cur.dataPlayer == null) return -1;
        var player = GameDataFragment.cur.dataPlayer;
        return type == BoosterType.Click ? player.boosterClick : type == BoosterType.Shuffle ? player.boosterSuffle : player.boosterHut;
    }

    private static void ConsumeBooster(BoosterType type)
    {
        if (BoosterCount(type) >= 0) GameDataFragment.cur.ChangeBoosterCount(type, -1, "GamePlay");
    }

    private void Awake() { if (gameCamera == null) gameCamera = Camera.main; }

    public bool TryUseChopsticks(FoodShushi food)
    {
        if (!CanInteract || BoosterCount(BoosterType.Click) == 0 || PickMode != FoodPickMode.Chopsticks || conveyorManager == null ||
            !conveyorManager.TryTakeChopsticks(food)) return false;
        ConsumeBooster(BoosterType.Click);
        CancelBooster();
        return true;
    }

    public bool TryUseMagnet(Customer customer, SushiDishOrder dish)
    {
        if (!CanInteract || BoosterCount(BoosterType.Magnet) == 0 || PickMode != FoodPickMode.Magnet || conveyorManager == null || LevelManager.Ins == null ||
            !conveyorManager.TryTakeMagnet(LevelManager.Ins.Board, customer, dish)) return false;
        ConsumeBooster(BoosterType.Magnet);
        CancelBooster();
        return true;
    }

    private void LateUpdate()
    {
        if (!CanInteract) CancelBooster();
        if (conveyorManager == null || conveyorManager.Customers == null) return;
        foreach (Customer customer in conveyorManager.Customers.ActiveCustomers)
            if (customer != null) customer.GetComponent<CustomerRecipeView>()?.SetMagnetTargeting(PickMode == FoodPickMode.Magnet);
    }

    private void OnDisable()
    {
        CancelBooster();
        if (conveyorManager == null || conveyorManager.Customers == null) return;
        foreach (Customer customer in conveyorManager.Customers.ActiveCustomers)
            if (customer != null) customer.GetComponent<CustomerRecipeView>()?.SetMagnetTargeting(false);
    }

    private void Update()
    {
        if (!TryGetPressPosition(out Vector2 screenPosition)) return;
        if (!CanInteract) return;
        if (gameCamera == null || conveyorManager == null) return;
        // Kiểm tra UI trực tiếp, kể cả touch mới xuất hiện trong frame hiện tại.
        if (EventSystem.current != null)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
            var uiHits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, uiHits);
            if (uiHits.Count > 0) return;
        }
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
