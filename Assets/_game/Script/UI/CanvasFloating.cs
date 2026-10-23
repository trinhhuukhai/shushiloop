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
    private void Awake()
    {
        rootPanel.SafeAreaParentAdoption(GetComponent<Canvas>());
        posTopGroupOr = TopGroup.anchoredPosition;

    }


    public void HideUIFloat(bool ck)
    {
        if (ck)
        {
            UiFloat.alpha = 0;
        }
        else UiFloat.alpha = 1;
    }


    private void OnDisable()
    {
        if (coLives != null)
            StopCoroutine(coLives);
        if (coLivesUnlimited != null)
            StopCoroutine(coLivesUnlimited);
    }




    public override void Open()
    {
        base.Open();
        //ShowGold_Heart();
        //showHeart();
        //StartCoroutine(IE_LivesUnlimited());
        //ShowGold();
        ////ShowLives();
        //TopGroup.anchoredPosition = posTopGroupOr;
        //ShowSettingButton(true);
        //StartCoroutine(IE_Lives());
    }

    public void HideGold_Heart()
    {
        //FrmHeartUnlimited.SetActive(false);
        HeartFr.SetActive(false);
        GoldFr.SetActive(false);
    }

    public void ShowGoldAndHeart()
    {
        //FrmHeartUnlimited.SetActive(false);
        HeartFr.SetActive(true);
        GoldFr.SetActive(true);
    }
    public void ShowGold_Heart()
    {
        GoldFr.SetActive(true);
    }
    //public void showHeart()
    //{
    //    HeartFr.SetActive(true);

    //}

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
    public void HideHeart()
    {
        HeartFr.SetActive(false);
    }


    public void showBtnSettingInGame(bool check)
    {
        btnSettingIngame.SetActive(check);
        settingBt.gameObject.SetActive(!check);
    }


    public void AnimTopGroup()
    {
        TopGroup.DOAnchorPosY(TopGroup.anchoredPosition.y + 300, 0.6f).SetEase(Ease.OutSine);
    }

    public void Resfesh()
    {
        TopGroup.anchoredPosition = posTopGroupOr;
    }

    private Coroutine coLives;
    private Coroutine coLivesUnlimited;

    IEnumerator IE_Lives()
    {
        while (true)
        {
            ShowLives();

            yield return Yielders.Get(1f);
        }
    }

    IEnumerator IE_LivesUnlimited()
    {
        while (true)
        {
            ShowLivesUnlimited();
            yield return Yielders.Get(1f);
        }
    }

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

    public void showIconLives(bool check)
    {
        HeartFr.SetActive(check);
    }

    public void PlayEffGold()
    {
        effGold.Play();
        //SoundManager.PlayEfx(SoundID.sfx_ui_coin_claim);
    }

    private Tween goldTween;

    public void AnimGold(int deltaGold, bool victory)
    {
        goldTween?.Kill();
       
        int current = GameDataFragment.cur.dataPlayer.money;
        int start = current - deltaGold;
        int end = current;
        //SoundManager.PlayEfx(SoundID.sfx_ui_coin_fly);
        DOVirtual.DelayedCall(0.3f, () => pI.Play());
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
                BlockUI.Ins.UnBlock();

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

    protected override void OnInit()
    {
        base.OnInit();
    }

    public void ShowGold()
    {
        txtGold.text = GameDataFragment.cur.dataPlayer.money.ToString("N0");
    }

    public void MoveToPosition(Transform tf)
    {
        transform.position = tf.position;
    }


    public void ShowSettingButton(bool check)
    {
        settingBt.gameObject.SetActive(check);
        btnSettingIngame.SetActive(!check);
    }

    public void HideAllSetting()
    {
        settingBt.gameObject.SetActive(false);
        btnSettingIngame.SetActive(false);
    }

    //------------------------
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

    public void OnSetting()
    {
        //if (BoosterManager.Ins.usingFreePickBooster) return;
        //if(BoosterManager.Ins.usingShuffleBooster) return;
        //if (BoosterManager.Ins.usingHutBooster) return;
        //SoundManager.PlayEfx(SoundID.sfx_click);

        //UIManager.Ins.OpenUI<CanvasSetting>();
        //LevelManager.Ins.OnSetting();
    }

    public void OnMoreLives()
    {
        //if (UIManager.Ins.IsOpened<CanvasGetMoreLives>())
        //{
        //    return;
        //}
        //SoundManager.PlayEfx(SoundID.sfx_click);
        //UIManager.Ins.OpenUI<CanvasGetMoreLives>();
    }

    public void Close()
    {

        base.Close(0.26f);
    }

    public void openShop()
    {
        //SoundManager.PlayEfx(SoundID.sfx_click);

        //UIManager.Ins.OpenUI<CanvasShop>();
        //UIManager.Ins.CloseUI<CanvasFloating>();
    }


    #region SPAM nhiều particle
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