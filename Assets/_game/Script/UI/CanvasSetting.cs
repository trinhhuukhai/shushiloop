using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasSetting : UICanvas
{
    public Button homeBt;
    public Button retry;
    public Button close;

    public Button backbuttonLanguage;
    public GameObject panelSettingLanguage;
    public RectTransform mainPanelRect;
    private Tween popTween;


    // Mở cài đặt với hiệu ứng, tạm dừng thời gian và chọn các nút phù hợp với ngữ cảnh màn chơi.
    public override void Open()
    {
        base.Open();
        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

        Time.timeScale = 0f;
        bool inGame = LevelManager.Ins != null && LevelManager.Ins.CurrentLevel != null;
        bool allowHome = inGame && GameManager.ins != null && GameManager.ins.CanReturnHome;
        if (homeBt != null) homeBt.gameObject.SetActive(inGame);
        if (retry != null) retry.gameObject.SetActive(inGame);
        if (close != null) close.gameObject.SetActive(!inGame);
        if (homeBt != null)
        {
            homeBt.interactable = allowHome;
            homeBt.gameObject.SetActive(allowHome);
        }

    }
    // Khôi phục tốc độ thời gian bình thường và đóng cài đặt.
    public void OnResume()
    {

        Time.timeScale = 1f;
        CloseDirectly();
    }
    // Chuyển yêu cầu chơi lại từ nút UI sang xử lý kiểm tra mạng.
    public void OnReplayLevel() => OnReplay();
    // Chơi lại ngay khi vô hạn mạng hoặc mở popup mạng, cảnh báo tùy số mạng còn lại.
    public void OnReplay()
    {


        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        //ObjLive.SetActive(!isUnlimited);
        if (isUnlimited)
        {
            Time.timeScale = 1f;
            if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel()) CloseDirectly();
        }
        else
        {
            if(LivesDataFragment.cur.gameData.lives <= 0)
            {
                UIManager.Ins.OpenUI<CanvasLive>();

            }
            else
            {
                UIManager.Ins.OpenUI<CanvasWarningLiveSetting>().Setupp(false);

            }
        }

    }
    // Đóng cài đặt và yêu cầu kết thúc màn với kết quả thua.
    public void OnGiveUp() { CloseDirectly(); LevelManager.Ins?.LoseLevel(); }
    // Về trang chủ khi vô hạn mạng và được phép, hoặc mở cảnh báo mất mạng.
    public void OnBackHome()
    {


        bool isUnlimited = LivesDataFragment.cur != null && LivesDataFragment.cur.IsUnlimitedLives();
        //ObjLive.SetActive(!isUnlimited);
        if (isUnlimited)
        {
            if (GameManager.ins == null || !GameManager.ins.CanReturnHome || GameManager.ins.IsLoadingLevel) return;
            Time.timeScale = 1f;
            GameManager.ins.ReturnToMainMenu();
        }
        else
        {
            UIManager.Ins.OpenUI<CanvasWarningLiveSetting>().Setupp(true);

        }

    }
    // Mở panel chọn ngôn ngữ trong cài đặt.
    public void OnSettingLanguage() { if (panelSettingLanguage != null) panelSettingLanguage.SetActive(true); }
    // Ẩn panel chọn ngôn ngữ.
    public void CloseSettingLanguage() { if (panelSettingLanguage != null) panelSettingLanguage.SetActive(false); }
    // Khôi phục thời gian và đóng cài đặt khi đang ở trang chủ.
    public void BackInHome()
    {
        Time.timeScale = 1f;CloseDirectly();
    }
    // Đóng trực tiếp popup cài đặt.
    public void Close() => CloseDirectly();
    // Đóng cài đặt khi nhận phím quay lại.
    public override void BackKey() => CloseDirectly();
}
