using System.Collections.Generic;
using UnityEngine;

public class BoardShishiManager : SingletonMonoBehaviour<BoardShishiManager>
{
    [SerializeField] private Transform tf;
    [SerializeField] private FoodQueue columnPrefab;

    [Header("Mỗi TypeFood có một prefab")]
    [SerializeField]
    private List<FoodShushi> foodPrefabs =
        new List<FoodShushi>();

    [SerializeField] private float columnWidth = 1.55f;

    private readonly List<FoodQueue> foodQueues =
        new List<FoodQueue>();

    public IReadOnlyList<FoodQueue> FoodQueues => foodQueues;

    [SerializeField] private ConveyorManager conveyorManager;

    public void Init(List<FoodQueueData> boardData)
    {
        ClearBoard();

        int columnCount = boardData.Count;
        float startX = -((columnCount - 1) / 2f) * columnWidth;

        for (int i = 0; i < columnCount; i++)
        {
            FoodQueue column = Instantiate(columnPrefab, tf);

            column.columnIndex = i;
            column.transform.localPosition =
                new Vector3(startX + i * columnWidth, -1f, 0f);

            column.gameObject.SetActive(true);

            foodQueues.Add(column);
            column.OnInit(boardData[i], foodPrefabs);
            column.SetConveyor(conveyorManager);
        }
    }

    public bool IsEmpty()
    {
        foreach (FoodQueue column in foodQueues)
        {
            if (!column.IsEmpty())
                return false;
        }

        return true;
    }

    public void ClearBoard()
    {
        foreach (FoodQueue column in foodQueues)
        {
            if (column == null)
                continue;

            column.gameObject.SetActive(false);
            Destroy(column.gameObject);
        }

        foodQueues.Clear();
    }
}