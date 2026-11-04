using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class NoffDailyLogin : MonoBehaviour
{
    public static NoffDailyLogin cur;

    public GameObject noffObject;
    public RectTransform noffRect;
    private Tween noffTween;
    private Vector3 noffRootScale;

    // Lưu đối tượng thông báo hiện tại và kích thước gốc của biểu tượng.
    private void Awake()
    {
        cur = this;
        noffRootScale = noffRect.localScale;
    }

    // Cập nhật dấu thông báo và bắt đầu theo dõi ngày mới khi được bật.
    private void OnEnable()
    {
        UpdateNoff();

        CheckTime();
    }

    // Dừng hiệu ứng phóng to thu nhỏ của dấu thông báo.
    private void OnDisable()
    {
        noffTween?.Kill();
    }

    // Khởi chạy coroutine theo dõi mốc ngày mới cho quà đăng nhập.
    private void CheckTime()
    {
        StartCoroutine(ie_Check());
        return;

        // Chờ nguồn giờ hợp lệ rồi kiểm tra ngày mới mỗi giây để cập nhật thông báo.
        IEnumerator ie_Check()
        {
            yield return new WaitUntil(() => UnbiasedTime.IsValidTime);
            while (true)
            {
                if (DailyLoginDataFragment.cur.CheckNewDay(out _))
                {
                    UpdateNoff();
                }

                yield return Yielders.Get(1);
            }
        }
    }

    // Hiện và tạo nhịp thông báo khi quà đăng nhập hôm nay chưa được nhận.
    private void UpdateNoff()
    {
        var isOn = !DailyLoginDataFragment.cur.gameData.isClaimedToday;

        noffObject.SetActive(isOn);
        if (isOn) NoffBubble();
        else noffTween?.Kill();
    }

    // Cho thành phần khác yêu cầu làm mới dấu thông báo đang tồn tại.
    public static void UpdateNoffStatic()
    {
        if (cur == null) return;
        cur.UpdateNoff();
    }

    // Khởi động nhịp phóng to thu nhỏ lặp cho biểu tượng thông báo.
    private void NoffBubble()
    {
        noffTween?.Kill();
        noffRect.localScale = noffRootScale;
        noffTween = noffRect.DOScale(noffRootScale * 1.12f, .72f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }
}