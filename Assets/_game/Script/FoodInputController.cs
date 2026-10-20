using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FoodInputController : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    [SerializeField] private ConveyorManager conveyorManager;
    [SerializeField] private LayerMask columnMask = ~0;

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    private void Update()
    {
        if (!TryGetPressPosition(out Vector2 screenPosition))
            return;

        if (gameCamera == null || conveyorManager == null)
        {
            Debug.LogError(
                "Input chưa được gán Camera hoặc ConveyorManager.",
                this
            );
            return;
        }

        Ray ray = gameCamera.ScreenPointToRay(screenPosition);

        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(
            ray,
            Mathf.Infinity,
            columnMask
        );

        foreach (RaycastHit2D hit in hits)
        {
            FoodQueue column =
                hit.collider.GetComponentInParent<FoodQueue>();

            if (column == null)
                continue;

            Debug.Log($"Click trúng cột {column.columnIndex}", column);

            // Gán trực tiếp, tránh cột spawn mới chưa có tham chiếu.
            column.SetConveyor(conveyorManager);
            column.OnClickColumn();

            // Một lần bấm chỉ xử lý một cột.
            return;
        }

        Debug.Log("Đã nhận click nhưng không trúng collider của cột.");
    }

    private bool TryGetPressPosition(out Vector2 position)
    {
        position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                position = touch.position;
                return true;
            }

            return false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif

        return false;
    }
}