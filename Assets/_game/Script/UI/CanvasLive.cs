using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

public class CanvasLive : UICanvas
{
    public TMP_Text txtCountLives;
    public GameObject timeObject;
    public TMP_Text txtTime;
    public TMP_Text txtTile;

    public GameObject HeartRed;
    public GameObject HeartBlack;
    public TMP_Text priceTxt;
    public RectTransform mainPanelRect;

    private const int ReviveLivesPrice = 800;
    public TMP_Text txtGold;

    private bool isopen;
    private Tween popTween;
    private Coroutine coLives;
    private static readonly WaitForSecondsRealtime OneSecondRealtime = new WaitForSecondsRealtime(1f);

    // Mở popup mạng một lần, chạy hiệu ứng xuất hiện và cập nhật giá, tiền cùng bộ đếm hồi mạng.
    public override void Open()
    {
        base.Open();               // chỉ gọi một lần (trước đây gọi hai lần)
        if (isopen) return;
        isopen = true;

        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

        setUp();

        if (coLives != null) StopCoroutine(coLives);
        coLives = StartCoroutine(IE_Lives());

        setGold();
    }

    // Hiển thị tiền hiện tại có phân nhóm hàng nghìn.
    public void setGold()
    {
        txtGold.text = GameDataFragment.cur.dataPlayer.money.ToString("N0");
    }

    // Xóa cờ popup đang mở rồi đóng trực tiếp.
    public void Close()
    {
        isopen = false;
        CloseDirectly();
    }

    // Đóng popup mạng qua luồng dọn cờ mở khi nhận phím quay lại.
    public override void BackKey() => Close();


    // Làm mới số mạng và thời gian hồi mỗi giây theo thời gian thực.
    private IEnumerator IE_Lives()
    {
        while (true)
        {
            ShowLives();
            yield return OneSecondRealtime;
        }
    }

    // Đồng bộ số mạng, tiêu đề và vùng đếm thời gian hồi hoặc trạng thái FULL.
    public void ShowLives()
    {
        var data = LivesDataFragment.cur;
        if (data == null) return;

        data.RefreshLives();
        int lives = data.gameData.lives;
        txtCountLives.text = lives.ToString();

        if (lives >= LivesDataFragment.MAX_LIVES)
        {
            timeObject.SetActive(false);
            txtTime.text = "FULL";
            txtTile.text = "Live";
        }
        else
        {
            timeObject.SetActive(true);
            txtTime.gameObject.SetActive(true);
            txtTime.text = data.GetRemainTime().ToTimeFormat_H_M_S();
            txtTile.text = lives <= 0 ? "Out Of Live" : "Live";
        }
    }

    // Cập nhật giá bổ sung mạng, màu thiếu tiền và hình tim theo trạng thái hết mạng.
    public void setUp()
    {
        priceTxt.text = ReviveLivesPrice.ToString();
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        priceTxt.color = player != null && player.money < ReviveLivesPrice ? Color.red : Color.white;

        bool outOfLives = LivesDataFragment.cur != null && LivesDataFragment.cur.gameData.lives <= 0;
        HeartRed.SetActive(!outOfLives);
        HeartBlack.SetActive(outOfLives);
    }

    // Đóng popup qua luồng dọn cờ mở; hiện chưa dùng tham số làm mới tim.
    private void RefreshFloatingAndClose(bool refreshHeartOnly = false)
    {
        //if (!UIManager.Ins.IsOpened<CanvasFail>())
        //{
        //    var floating = UIManager.Ins.GetUI<CanvasFloating>();
        //    if (floating != null)
        //    {
        //        if (refreshHeartOnly) floating.showHeart();
        //        else floating.ShowLives();
        //        floating.ShowGold();
        //    }
        //}
        Close();
    }

    // Trừ tiền mua năm mạng và tiếp tục chơi hoặc chơi lại theo trạng thái game hiện tại.
    public void ButtonUseGold()
    {
        int price = 800;
        bool isEnough = false;
        int gold = 0;
        if (GameDataFragment.cur.dataPlayer.money >= price)
        {
            isEnough = true;
            GameDataFragment.cur.ChangeMoney(-price, "get_more_lives");
            gold = price;

        }


        if (isEnough)
        {
            SoundManager.PlayEfx(SoundID.sfx_click);

            LivesDataFragment.cur.AddLife(5);

            if (GameManager.IsState(GameState.GamePlay))
            {
                Debug.LogError("gameplayyyyyyy----------");

                Time.timeScale = 1f;
                if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel())
                {
                    CloseDirectly();
                    LivesDataFragment.cur.ConsumeLife();
                    UIManager.Ins.CloseUI<CanvasSetting>();
                }
            }
            else if (GameManager.IsState(GameState.Fail))
            {
                Debug.LogError("fallllllllllllll----------");
                if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel())
                {
                    CloseDirectly();
                    LivesDataFragment.cur.ConsumeLife();
                    UIManager.Ins.CloseUI<CanvasFail>();
                }
            }
            else if (GameManager.IsState(GameState.MainMenu))
            {
                Debug.LogError("mainmenuuu----------");
                UIManager.Ins.GetUI<CanvasMainMenu>().Play();
            }



            //if (UIManager.Ins.IsOpened<CanvasFail>())
            //{
            //    Close();
            //}
            //else
            //{
            //    UIManager.Ins.GetUI<CanvasFloating>().ShowLives();
            //    UIManager.Ins.GetUI<CanvasFloating>().ShowGold();
            //    Close();
            //}
            Close();


        }
        else
        {
           
            //UIManager.Ins.OpenUI<CanvasShop>();

        }
    }

    // Gọi nhận thưởng mạng trực tiếp; hiện chưa có bước mở quảng cáo.
    public void BtnWatchAds()
    {
        //AdsManager.Ins.ShowRewardedAd(
        //    GameDataFragment.cur.dataPlayer.cur_level,
        //    "More_lives", "More_lives", "More_lives", 1, "popup_more_live",
        //    OnRewarded, null);

        OnRewarded();
    }

    // Cộng một mạng và tiếp tục luồng chơi hoặc chơi lại theo trạng thái game.
    private void OnRewarded()
    {
        LivesDataFragment.cur.AddLife(1);
        
        RefreshFloatingAndClose();
        if (GameManager.IsState(GameState.GamePlay))
        {
            Debug.LogError("gameplayyyyyyy----------");

            Time.timeScale = 1f;
            if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel())
            {
                CloseDirectly();
                LivesDataFragment.cur.ConsumeLife();
                UIManager.Ins.CloseUI<CanvasSetting>();
            }
        }
        else if (GameManager.IsState(GameState.Fail))
        {
            Debug.LogError("fallllllllllllll----------");
            if (GameManager.ins != null && GameManager.ins.ReplayCurrentLevel())
            {
                CloseDirectly();
                LivesDataFragment.cur.ConsumeLife();
                UIManager.Ins.CloseUI<CanvasFail>();
            }
        }
        else if (GameManager.IsState(GameState.MainMenu))
        {
            Debug.LogError("mainmenuuu----------");
            UIManager.Ins.GetUI<CanvasMainMenu>().Play();

        }

    }

    // Nhận quyền miễn phí khi hết mạng trong ngày, cộng 30 phút vô hạn mạng và đóng popup.
    public void btnClaimFreeInDay()
    {
        if (!LivesDataFragment.cur.TryClaimFirstEmptyLivesToday())
            return;
        SoundManager.PlayEfx(SoundID.sfx_click);

        LivesDataFragment.cur.AddUnlimitedLivesMinutes(30);
        RefreshFloatingAndClose(true);
    }
}