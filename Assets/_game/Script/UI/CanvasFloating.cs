using AssetKits.ParticleImage;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasFloating : UICanvas
{
    protected override bool BlocksGameplay => false;
    private int displayedGold = -1;
    // A Cường-----------------------------------
    public List<TripleValue<ItemT, ParticleImage, float>> listEffects;
    TripleValue<ItemT, ParticleImage, float> cur_Fx = new TripleValue<ItemT, ParticleImage, float>();
    public Coroutine coroutine;


    //------------------------------
    [Header("UI References")]
    //[SerializeField] private TextMeshProUGUI goldText;
    private RectTransform rewardPos;
    public Button settingBt;

    public RectTransform rootPanel;
    public CanvasGroup UiFloat;
    public TMP_Text txtGold;
    public ParticleImage effGold;

    [Header("Lives")]
    public TMP_Text txtLives;
    public TMP_Text txtLivesTime;
    //public ParticleImage effLives;
    public RectTransform goldRect;

    public GameObject GoldFr;
    public GameObject HeartFr;
    public GameObject btnPlus;
    public Button addPlus;
    public GameObject FrmHeartUnlimited;
    public GameObject FrmHeartNormal;
    public TMP_Text txtLivesTimeUnlimited;

    public RectTransform TopGroup;
    public Vector2 posTopGroupOr;
    public GameObject btnSettingIngame;

    //
    public ParticleImage pI;
    // Căn vùng an toàn và lưu vị trí gốc của nhóm UI phía trên.
    private void Awake()
    {
        if (rootPanel != null) rootPanel.SafeAreaParentAdoption(GetComponent<Canvas>());
        if (TopGroup != null) posTopGroupOr = TopGroup.anchoredPosition;

    }


    // Ẩn hoặc hiện nhóm UI nổi, đồng bộ khả năng nhận và chặn thao tác.
    public void HideUIFloat(bool ck)
    {
        UiFloat.interactable = UiFloat.blocksRaycasts = !ck;
        if (ck)
        {
            UiFloat.alpha = 0;
        }
        else UiFloat.alpha = 1;
    }


    // Dừng tween tiền và các coroutine cập nhật mạng khi UI nổi bị tắt.
    protected override void OnDisable()
    {
        base.OnDisable();
        goldTween?.Kill();
        if (coLives != null)
            StopCoroutine(coLives);
        if (coLivesUnlimited != null)
            StopCoroutine(coLivesUnlimited);
    }




    // Mở UI nổi, nhận tiền tạm, cập nhật ví và chọn các nhóm hiển thị cho gameplay.
    public override void Open()
    {
        base.Open();
        CheckMoneyTemp();
        ShowGold();
        ShowGold_Heart();
        HideHeart();
        showBtnSettingInGame(true);

        //ShowGold_Heart();
        //showHeart();
        //StartCoroutine(IE_LivesUnlimited());
        //ShowGold();
        ////ShowLives();
        //TopGroup.anchoredPosition = posTopGroupOr;
        //ShowSettingButton(true);
        //StartCoroutine(IE_Lives());
    }
    // Cộng tiền thưởng tạm còn tồn vào ví rồi xóa khoản tạm.
    public void CheckMoneyTemp()
    {
        var data = GameDataFragment.cur;
        if (data?.dataPlayer == null || data.dataPlayer.moneyTemp <= 0) return;
        int reward = data.dataPlayer.moneyTemp;
        data.dataPlayer.moneyTemp = 0;
        data.ChangeMoney(reward, "home");
    }


    // Ẩn cả khung mạng và khung tiền.
    public void HideGold_Heart()
    {
        //FrmHeartUnlimited.SetActive(false);
        HeartFr.SetActive(false);
        GoldFr.SetActive(false);
    }

    // Hiện cả khung tiền và khung mạng.
    public void ShowGoldAndHeart()
    {
        //FrmHeartUnlimited.SetActive(false);
        HeartFr.SetActive(true);
        GoldFr.SetActive(true);
    }
    // Hiện khung tiền; hàm hiện không đổi trạng thái khung mạng.
    public void ShowGold_Heart()
    {
        GoldFr.SetActive(true);
    }
    //public void showHeart()
    //{
    //    HeartFr.SetActive(true);

    //}

    // Điểm gọi hiển thị mạng được giữ lại; hiện chưa có xử lý đang hoạt động.
    public void showHeart()
    {
        //HeartFr.SetActive(true);
        //var data = LivesDataFragment.cur;
        //if (data.IsUnlimitedLives())
        //{
        //    FrmHeartUnlimited.SetActive(true);
        //    FrmHeartNormal.SetActive(false);
        //}
        //else
        //{
        //    FrmHeartUnlimited.SetActive(false);
        //    FrmHeartNormal.SetActive(true);

        //}
    }
    // Ẩn khung mạng trên UI nổi.
    public void HideHeart()
    {
        HeartFr.SetActive(false);
    }


    // Chọn hiển thị nút cài đặt trong game hoặc nút cài đặt thông thường.
    public void showBtnSettingInGame(bool check)
    {
        btnSettingIngame.SetActive(check);
        settingBt.gameObject.SetActive(!check);
    }


    // Trượt nhóm UI phía trên lên thêm 300 đơn vị bằng tween.
    public void AnimTopGroup()
    {
        TopGroup.DOAnchorPosY(TopGroup.anchoredPosition.y + 300, 0.6f).SetEase(Ease.OutSine);
    }

    // Đặt nhóm UI phía trên về vị trí gốc đã lưu.
    public void Resfesh()
    {
        TopGroup.anchoredPosition = posTopGroupOr;
    }

    private Coroutine coLives;
    private Coroutine coLivesUnlimited;

    // Gọi cập nhật hiển thị mạng theo nhịp một giây.
    IEnumerator IE_Lives()
    {
        while (true)
        {
            ShowLives();

            yield return Yielders.Get(1f);
        }
    }

    // Gọi cập nhật mạng vô hạn theo nhịp một giây.
    IEnumerator IE_LivesUnlimited()
    {
        while (true)
        {
            ShowLivesUnlimited();
            yield return Yielders.Get(1f);
        }
    }

    // Điểm cập nhật UI mạng thường; phần xử lý hiện đang được để trống.
    public void ShowLives()
    {
        //var data = LivesDataFragment.cur;

        //data.RefreshLives();

        //txtLives.text = data.gameData.lives.ToString();

        //if (data.gameData.lives >= LivesDataFragment.MAX_LIVES)
        //{
        //    txtLivesTime.text = "FULL";
        //    btnPlus.SetActive(false);
        //    addPlus.interactable = false;
        //}
        //else
        //{
        //    btnPlus.SetActive(true);
        //    txtLivesTime.gameObject.SetActive(true);
        //    addPlus.interactable = true;

        //    txtLivesTime.text =
        //        data.GetRemainTime().ToTimeFormat_H_M_S();
        //}
    }

    // Điểm cập nhật UI vô hạn mạng; phần xử lý hiện đang được để trống.
    public void ShowLivesUnlimited()
    {
        //var data = LivesDataFragment.cur;

        //if (data == null)
        //    return;

        //bool isUnlimited = data.IsUnlimitedLives();

        //if (isUnlimited)
        //{
        //    FrmHeartUnlimited.SetActive(true);
        //    FrmHeartNormal.SetActive(false);

        //    txtLivesTimeUnlimited.gameObject.SetActive(true);

        //   // Không dùng ToTimeFormat_H_M_S()
        //    // vì extension đó có thể reset Hours sau 24h.
        //    txtLivesTimeUnlimited.text =
        //        data.GetUnlimitedRemainTimeText();
        //}
        //else
        //{
        //    FrmHeartUnlimited.SetActive(false);
        //    FrmHeartNormal.SetActive(true);

        //    txtLivesTimeUnlimited.gameObject.SetActive(false);
        //}
    }

    // Bật hoặc tắt khung biểu tượng mạng theo yêu cầu.
    public void showIconLives(bool check)
    {
        HeartFr.SetActive(check);
    }

    // Phát hiệu ứng tiền khi đã gán particle tương ứng.
    public void PlayEffGold()
    {
        if (effGold != null) effGold.Play();
        //SoundManager.PlayEfx(SoundID.sfx_ui_coin_claim);
    }

    private Tween goldTween;

    // Chạy bộ đếm từ tiền trước thay đổi tới tiền hiện tại và mở khóa UI khi cần.
    public void AnimGold(int deltaGold, bool victory)
    {
        goldTween?.Kill();
       
        int current = GameDataFragment.cur.dataPlayer.money;
        int start = current - deltaGold;
        int end = current;
        //SoundManager.PlayEfx(SoundID.sfx_ui_coin_fly);
        DOVirtual.DelayedCall(0.3f, () => { if (pI != null) pI.Play(); }).SetUpdate(true);
        goldTween = DOTween.To(
            () => start,
            x =>
            {
                start = x;
                txtGold.text = x.ToString("N0");
            },
            end,
            0.5f
        )
        .SetEase(deltaGold >= 0 ? Ease.OutCubic : Ease.InCubic)
        .OnComplete(() =>
        {
            txtGold.text = end.ToString("N0");
            if(!victory)
            {
                if (BlockUI.Ins != null) BlockUI.Ins.UnBlock();

            }
        });
    }

    //public void AnimGold(int addGold)
    //{
    //    int start = GameDataFragment.cur.dataPlayer.money - addGold;
    //    int end = GameDataFragment.cur.dataPlayer.money;

    //    DOTween.To(
    //        () => start,
    //        x =>
    //        {
    //            start = x;
    //            txtGold.text = x.ToString();
    //        },
    //        end,
    //        0.8f
    //    ).SetEase(Ease.OutCubic).OnComplete(()=>
    //    {
    //        BlockUI.Ins.UnBlock();
    //    });
    //}

    // Chuyển xử lý khởi tạo UI nổi về lớp canvas cơ sở.
    protected override void OnInit()
    {
        base.OnInit();
    }

    // Hiển thị số tiền hiện có và lưu giá trị để phát hiện thay đổi.
    public void ShowGold()
    {
        displayedGold = GameDataFragment.cur != null && GameDataFragment.cur.dataPlayer != null ? GameDataFragment.cur.dataPlayer.money : 0;
        if (txtGold != null) txtGold.text = displayedGold.ToString("N0");
    }

    // Đặt UI nổi tại vị trí của Transform được chỉ định.
    public void MoveToPosition(Transform tf)
    {
        transform.position = tf.position;
    }


    // Chọn nút cài đặt thường hoặc nút cài đặt gameplay theo giá trị yêu cầu.
    public void ShowSettingButton(bool check)
    {
        settingBt.gameObject.SetActive(check);
        btnSettingIngame.SetActive(!check);
    }

    // Ẩn cả hai nút cài đặt trên UI nổi.
    public void HideAllSetting()
    {
        settingBt.gameObject.SetActive(false);
        btnSettingIngame.SetActive(false);
    }

    //------------------------
    // Tìm hiệu ứng theo loại phần thưởng và chạy coroutine hiển thị tại vị trí nhận.
    public void ShowItemEffect(ItemT itemT, int amount = 100, Vector3? wpos = null)
    {
        //Debug.Log("---------------");
        cur_Fx = new TripleValue<ItemT, ParticleImage, float>();
        for (int i = 0; i < listEffects.Count; i++)
        {
            if (listEffects[i].value1 == itemT)
            {
                cur_Fx = listEffects[i];
                break;
            }
        }

        if (cur_Fx != null) StartCoroutine(ie_ShowItemEffect(itemT, cur_Fx, wpos));

        // Chờ hiệu ứng dùng chung rảnh, phát tại vị trí yêu cầu rồi ẩn và phục hồi vị trí UI.
        IEnumerator ie_ShowItemEffect(ItemT _itemT, TripleValue<ItemT, ParticleImage, float> fx, Vector3? worldPos)
        {
            if (fx.value2.gameObject.activeSelf)
                yield return new WaitUntil(() => !fx.value2.gameObject.activeSelf);

            for (int i = 0; i < listEffects.Count; i++)
            {
                if (listEffects[i].value2.gameObject.activeSelf)
                    yield return Yielders.Get(0.3f);
            }

            RectTransform fxRt = fx.value2.GetComponent<RectTransform>();
            Vector3 oldWorldPos = fxRt.position;
            Vector2 oldAnchored = fxRt.anchoredPosition;

            if (worldPos.HasValue)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos.Value);
                {
                    fxRt.position = screen;
                }
            }
            else if (rewardPos != null)
            {
                fxRt.position = rewardPos.position;
            }

            fx.value2.gameObject.SetActive(true);
            yield return Yielders.Get(fx.value3);
            fx.value2.gameObject.SetActive(false);

            fxRt.anchoredPosition = oldAnchored; // restore
        }
    }

    // Mở cài đặt nếu màn hiện tại đang cho phép tương tác.
    public void OnSetting()
    {
        if (UIManager.Ins != null && LevelManager.Ins != null && LevelManager.Ins.CanInteract)
            UIManager.Ins.OpenUI<CanvasSetting>();
    }

    // Đồng bộ tiền khi không chạy tween và khóa nút cài đặt theo trạng thái tương tác của màn.
    private void Update()
    {
        if (GameDataFragment.cur == null || GameDataFragment.cur.dataPlayer == null) return;
        if (displayedGold != GameDataFragment.cur.dataPlayer.money && (goldTween == null || !goldTween.IsActive())) ShowGold();
        bool available = LevelManager.Ins != null && LevelManager.Ins.CanInteract;
        if (settingBt != null) settingBt.interactable = available;
        if (btnSettingIngame != null && btnSettingIngame.TryGetComponent(out Button button)) button.interactable = available;
    }

    // Điểm gọi mở thêm mạng; hiện chưa có xử lý đang hoạt động.
    public void OnMoreLives()
    {
        //if (UIManager.Ins.IsOpened<CanvasGetMoreLives>())
        //{
        //    return;
        //}
        //SoundManager.PlayEfx(SoundID.sfx_click);
        //UIManager.Ins.OpenUI<CanvasGetMoreLives>();
    }

    // Đóng UI nổi với thời gian trễ 0,26 giây qua lớp cơ sở.
    public void Close()
    {

        base.Close(0.26f);
    }

    // Điểm gọi mở cửa hàng từ UI nổi; hiện chưa có xử lý đang hoạt động.
    public void openShop()
    {
        //SoundManager.PlayEfx(SoundID.sfx_click);

        //UIManager.Ins.OpenUI<CanvasShop>();
        //UIManager.Ins.CloseUI<CanvasFloating>();
    }


    #region SPAM nhiều particle
    // Tìm hiệu ứng phần thưởng phù hợp và tạo một lượt phát độc lập.
    public void ShowItemEffectMulti(ItemT itemT, int amount = 100, Vector3? wpos = null)
    {
        // Tìm effect template tương ứng item
        TripleValue<ItemT, ParticleImage, float> fx = null;
        for (int i = 1; i < listEffects.Count; i++)
        {
            if (listEffects[i].value1 == itemT)
            {
                fx = listEffects[i];
                break;
            }
        }

        // Không có cấu hình effect thì thôi
        if (fx == null || fx.value2 == null) return;

        StartCoroutine(IE_ShowItemEffectMulti(fx, wpos));
    }

    // Nhân bản particle, phát tại vị trí nhận thưởng rồi hủy bản sao khi hết thời lượng.
    private IEnumerator IE_ShowItemEffectMulti(TripleValue<ItemT, ParticleImage, float> fx, Vector3? worldPos)
    {
        // fx.value2 đóng vai trò prefab template, nên tốt nhất để inactive sẵn trong prefab
        ParticleImage fxInstance = Instantiate(fx.value2, fx.value2.transform.parent);
        RectTransform fxRt = fxInstance.GetComponent<RectTransform>();

        // Đặt vị trí bắt đầu
        if (worldPos.HasValue)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos.Value);
            fxRt.position = screen;
        }
        else if (rewardPos != null)
        {
            fxRt.position = rewardPos.position;
        }

        fxInstance.gameObject.SetActive(true);

        // Thời lượng chạy effect lấy từ value3
        yield return Yielders.Get(fx.value3);

        // Hết hiệu ứng thì hủy instance
        Destroy(fxInstance.gameObject);
    }


    #endregion
}
