using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINoAdsAvailableTrigger : MonoBehaviour
{
    public RectTransform buttonRectTransform;

    private void Reset()
    {
        buttonRectTransform = GetComponent<RectTransform>();
    }

    public void OnAdsButtonClicked()
    {
        //CanvasNoAdsAvailableNoff.Noff(buttonRectTransform);
    }
}