using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen ins;
    public Image imgProgress;

    //public GameObject inAppUpdateObject;
    //public CanvasGroup inAppUpdateCg;
    //private Action onAcceptUpdate;
    //private Action onDeclineUpdate;

    [SerializeField] private string packageName;
    [SerializeField] private TMP_Text txt_loading;


    // Lưu tham chiếu màn hình tải hiện tại cho luồng khởi động game.
    private void Awake()
    {
        ins = this;
    }

    // Xóa tham chiếu màn hình tải nếu đối tượng hiện tại bị hủy.
    private void OnDestroy()
    {
        if (ins == this) ins = null;
    }

    // Hủy tween thanh tiến trình khi màn hình tải bị tắt.
    private void OnDisable()
    {
        if (imgProgress != null)
        {
            imgProgress.DOKill();
        }
    }
    // Dừng tween cũ và chạy thanh tải tới giá trị đã giới hạn trong khoảng 0 đến 1.
    public void SetPercent(float to, float time)
    {
        if (imgProgress == null) return;
        imgProgress.DOKill();

        imgProgress.DOFillAmount(Mathf.Clamp01(to), time)
            .SetEase(Ease.Linear).SetUpdate(true);
    }

    // Luân phiên số dấu chấm của chữ Loading theo thời gian thực trong bốn chu kỳ.
    public IEnumerator ie_txtLoading()
    {
        for (int i = 0; i < 4; i++)
        {
            if (txt_loading == null) yield break;
            txt_loading.text = "Loading";
            yield return new WaitForSecondsRealtime(0.25f);
            txt_loading.text = "Loading.";
            yield return new WaitForSecondsRealtime(0.25f);
            txt_loading.text = "Loading..";
            yield return new WaitForSecondsRealtime(0.25f);
            txt_loading.text = "Loading...";
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    // Ẩn đối tượng màn hình tải.
    public void Close()
    {
        gameObject.SetActive(false);
    }

    // Điểm mở thông báo cập nhật và gán callback; hiện chưa có xử lý đang hoạt động.
    public void PopNoti(Action onAcceptUpdate, Action onDeclineUpdate)
    {
        //this.onAcceptUpdate = onAcceptUpdate;
        //this.onDeclineUpdate = onDeclineUpdate;

        //inAppUpdateObject.SetActive(true);
        //inAppUpdateCg.alpha = 0;
        //inAppUpdateCg.DOFade(1, 0.32f);
    }

    // Mở trang cửa hàng khi người chơi chấp nhận cập nhật.
    public void OnClickAccept()
    {
        //SoundManager.PlayEfx(SoundID.sfx_ui_button_click);
        OpenStore();
        //inAppUpdateObject.SetActive(false);

        //onAcceptUpdate?.Invoke();
    }

    // Điểm xử lý từ chối cập nhật; hiện chưa có nội dung đang hoạt động.
    public void OnClickDecline()
    {
        //SoundManager.PlayEfx(SoundID.sfx_ui_button_click);
        //inAppUpdateObject.SetActive(false);

        //onDeclineUpdate?.Invoke();
    }

    // Mở liên kết ứng dụng trên cửa hàng và dùng liên kết web nếu thao tác phát sinh lỗi.
    void OpenStore()
    {
        try
        {
            Application.OpenURL("market://details?id=" + packageName);
        }
        catch
        {
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + packageName);
        }
    }
}