using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FoodOrderData
{
    public PoolType typeFood;

    [Min(1)]
    public int amount = 1;
}

[Serializable]
public class CustomerOrderData
{
    public List<FoodOrderData> orders =
        new List<FoodOrderData>();

    // Chuyển thành danh sách mà Customer.Setup() đang sử dụng.
    public List<PoolType> CreateFoodList()
    {
        List<PoolType> result = new List<PoolType>();

        foreach (FoodOrderData order in orders)
        {
            for (int i = 0; i < order.amount; i++)
                result.Add(order.typeFood);
        }

        return result;
    }
}

[Serializable]
public class FoodData
{
    public PoolType typeFood;
    public FoodState foodState = FoodState.Normal;

    public int iceLayers;
    public int lockId;
}

[Serializable]
public class FoodQueueData
{
    // Phần tử 0 là Food đầu cột.
    public List<FoodData> foods = new List<FoodData>();
}

[Serializable]
public class FoodModifierData
{
    [Tooltip("Cột bắt đầu từ 0.")]
    [Min(0)]
    public int column;

    [Tooltip("Hàng bắt đầu từ 0, hàng 0 là đầu cột.")]
    [Min(0)]
    public int row;

    public FoodState foodState;

    [Min(1)]
    public int iceLayers = 1;

    [Min(0)]
    public int lockId;
}