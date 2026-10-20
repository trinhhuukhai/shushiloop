using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
public class FoodQueue : MonoBehaviour
{
    public Transform tf;
    public int columnIndex;
    public PoolType foodType;

    [SerializeField]
    private List<FoodShushi> foods = new List<FoodShushi>();

    [Header("Khoảng cách và thời gian đẩy món")]
    [SerializeField] private float rowSpace = 1.2f;
    [SerializeField] private float moveDuration = 0.2f;

    // ID riêng để chỉ dừng tween sắp xếp của cột này.
    private readonly object rowTweenId = new object();

    public int Count => foods.Count;

    private ConveyorManager conveyor;

    public void SetConveyor(ConveyorManager conveyorManager)
    {
        conveyor = conveyorManager;
    }

    public void OnClickColumn()
    {
        if (conveyor == null || !CanTakeTopFood())
            return;

        conveyor.TryTakeFromQueue(this);
    }

    private void OnMouseDown()
    {
        OnClickColumn();
    }

    public void OnInit(
        FoodQueueData data,
        List<FoodShushi> foodPrefabs)
    {
        DOTween.Kill(rowTweenId);

        if (tf == null)
            tf = transform;

        // Root này chỉ chứa các Food của cột.
        for (int i = tf.childCount - 1; i >= 0; i--)
        {
            GameObject child = tf.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        foods.Clear();

        foreach (FoodData foodData in data.foods)
        {
            FoodShushi food = SimplePool.Spawn<FoodShushi>(foodData.typeFood);
            food.transform.SetParent(tf, false);
            food.SetupFromData(foodData);
            food.gameObject.SetActive(true);

            foods.Add(food);
        }

        RevealTopFood();
        RefreshRowsImmediate();
    }


    public FoodShushi GetTopFood()
    {
        return foods.Count > 0 ? foods[0] : null;
    }

    public bool IsEmpty()
    {
        return foods.Count == 0;
    }

    public bool CanTakeTopFood()
    {
        FoodShushi food = GetTopFood();

        return isActiveAndEnabled
            && food != null
            && food.CanPick();
    }

    // Chỉ gọi khi băng chuyền đã giữ được một slot.
    public bool TryTakeTopFood(Transform conveyorRoot, out FoodShushi food)
    {
        food = null;
        if (conveyorRoot == null || !CanTakeTopFood())  return false;
        FoodShushi topFood = foods[0];

        if (!topFood.TryMoveToConveyor())  return false;

        // Dừng tween xếp hàng trước khi đổi parent.
        // Không làm ảnh hưởng các tween khác của Food.
        DOTween.Kill(rowTweenId);

        foods.RemoveAt(0);

        // Giữ vị trí hiện tại để bắt đầu animation nhảy.
        topFood.transform.SetParent(conveyorRoot, true);

        food = topFood;

        RevealTopFood();
        RefreshRows();

        return true;
    }

    public void RefreshRows()
    {
        // Khi click liên tục, chạy lại từ vị trí hiện tại.
        DOTween.Kill(rowTweenId);

        for (int i = 0; i < foods.Count; i++)
        {
            FoodShushi food = foods[i];

            if (food == null)
                continue;

            Vector3 targetPosition = GetRowPosition(i);

            if ((food.transform.localPosition - targetPosition)
                .sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            food.transform
                .DOLocalMove(
                    targetPosition,
                    Mathf.Max(0.01f, moveDuration)
                )
                .SetEase(Ease.OutQuad)
                .SetId(rowTweenId);
        }
    }

    private void RefreshRowsImmediate()
    {
        DOTween.Kill(rowTweenId);

        for (int i = 0; i < foods.Count; i++)
        {
            if (foods[i] == null)
                continue;

            foods[i].transform.localPosition =
                GetRowPosition(i);
        }
    }

    private Vector3 GetRowPosition(int row)
    {
        return Vector3.down * rowSpace * row;
    }

    private void RevealTopFood()
    {
        FoodShushi food = GetTopFood();

        if (food != null)
            food.Reveal();
    }

    private void OnDisable()
    {
        DOTween.Kill(rowTweenId);
    }

    private void OnEnable()
    {
        // Đưa về đúng hàng nếu cột bị tắt giữa animation.
        RefreshRowsImmediate();
    }
}