using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class NoffSpin : MonoBehaviour
{
    public static NoffSpin cur;

    public GameObject noffObject;
    public RectTransform noffRect;
    private Tween noffTween;
    private Vector3 noffRootScale;

    private Coroutine timeCor;

    //public Image noffIconImage;
    //[SerializeField] private Sprite normIconSprite;
    //[SerializeField] private Sprite adsIconSprite;

    // Lưu đối tượng thông báo vòng quay và kích thước biểu tượng ban đầu.
    private void Awake()
    {
        cur = this;
        noffRootScale = noffRect.localScale;
    }

    // Cập nhật thông báo lượt quay khi đối tượng được bật.
    private void OnEnable()
    {
        UpdateNoff();
    }

    // Dừng hiệu ứng nhịp của biểu tượng thông báo.
    private void OnDisable()
    {
        noffTween?.Kill();
    }

    // Bật thông báo khi đủ tiến trình quay hoặc có lượt quảng cáo và cập nhật bộ chờ chu kỳ.
    private void UpdateNoff()
    {
        var require = SpinDataFragment.cur.GetRequiredPassedLevel();
        var passed = Mathf.Min(SpinDataFragment.cur.gameData.levelPassedNum, require);


        var isNorm = passed >= require;
        var isAds = SpinDataFragment.cur.IsAdsAble();
        var isOn = isNorm || isAds;
        if (isOn) NoffBubble();
        else noffTween?.Kill();
        noffObject.SetActive(isOn);

        //noffIconImage.sprite = isNorm ? normIconSprite : adsIconSprite;

        CheckTimeLeftTillFree();
    }

    // Cho thành phần khác yêu cầu làm mới thông báo vòng quay hiện tại.
    public static void UpdateNoffStatic()
    {
        if (cur == null) return;
        cur.UpdateNoff();
    }

    // Chạy hiệu ứng phóng to thu nhỏ lặp cho biểu tượng lượt quay.
    private void NoffBubble()
    {
        noffTween?.Kill();
        noffRect.localScale = noffRootScale;
        noffTween = noffRect.DOScale(noffRootScale * 1.12f, .72f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    private static WaitUntil waitForValidTime = new WaitUntil(() => UnbiasedTime.IsValidTime);

    // Khởi động lại coroutine chờ chu kỳ lượt quay quảng cáo mới.
    public void CheckTimeLeftTillFree()
    {
        if (timeCor != null) StopCoroutine(timeCor);
        timeCor = StartCoroutine(ie_Check());
        return;

        // Chờ giờ hợp lệ và làm mới thông báo khi đến chu kỳ lượt quay tiếp theo.
        IEnumerator ie_Check()
        {
            if (SpinDataFragment.cur.IsAdsAble()) yield break;
            while (true)
            {
                yield return waitForValidTime;
                if (SpinDataFragment.cur.CheckNewTimeSegment(out _))
                {
                    UpdateNoff();
                    yield break;
                }

                yield return Yielders.Get(1);
            }
        }
    }
}