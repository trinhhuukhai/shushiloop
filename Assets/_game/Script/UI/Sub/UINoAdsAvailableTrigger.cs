using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINoAdsAvailableTrigger : MonoBehaviour
{
    public RectTransform buttonRectTransform;

    // Tự gán RectTransform của nút khi thêm hoặc reset component trong Editor.
    private void Reset()
    {
        buttonRectTransform = GetComponent<RectTransform>();
    }

    // Điểm xử lý nút quảng cáo không khả dụng; hiện chưa có nội dung đang hoạt động.
    public void OnAdsButtonClicked()
    {
        //CanvasNoAdsAvailableNoff.Noff(buttonRectTransform);
    }
}