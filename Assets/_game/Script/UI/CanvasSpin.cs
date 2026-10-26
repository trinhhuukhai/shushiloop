using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;
using AssetKits.ParticleImage;

public class CanvasSpin : UICanvas
{
    [System.Serializable]
    public struct SpinWheelLight
    {
        public GameObject onObject;
        public GameObject offObject;
    }

    public static CanvasSpin cur;
    //public RectTransform canvasRect;

    //public CanvasGroup canvasGroup;

    public Transform wheelTrans;

    [Header("Wheel lights (11 slots: on = lit, off = dim; blink xen kẽ chẵn/lẻ)")]
    [SerializeField] private SpinWheelLight[] spinWheelLights;
    [SerializeField] private float spinLightStepSeconds = 0.22f;
    private DG.Tweening.Sequence spinLightsSequence;

    public Image progressBarImage;

    public float[] prizeAngle;
    [SerializeField] private float prizeOffsetAngle = 20;

    private int curPrizeIndex;

    public TMP_Text passedLevelText;
    public TMP_Text passLevelDescriptionText;
    //public MonoBehaviour textLocalizeAutoSwitchContent;

    public GameObject spinButtonObject;
    public GameObject lockSpinButtonObject;
    public GameObject adsSpinButtonObject;

    private bool isSpinable;
    //end stuff

    private Tween spinTween;
    private bool isSpinning;

    public static bool IsSpinning => cur != null && cur.isSpinning;

    private float timer;
    private const float TICK_OFFSET = 45f;
    private float lastAngle;

    private Tween pointerPopTween;
    public RectTransform pointerRect;
    [SerializeField] private Vector3 pointerNormAngle;
    [SerializeField] private Vector3 pointerHitAngle;
    [SerializeField] private float popTime;

    public RectTransform mainPanelRect;
    ////private static AnimationCurve PopCurve => UIManager.Ins.popCanvasCurve;
    private Tween popTween;

    public GameObject timeObject;
    public TMP_Text timeLeftText;
    private Coroutine timeCor;
    public List<ParticleImage> rewardImages;

    // private const string AAA = "Next free in: ";

    //

    public CanvasGroup canvasGroup;

    //public GameObject mainPanelObject;
    //public RectTransform mainPanelRect;

    //private Vector2 rootPos;
    //private Vector2 upperPos;
    //private Vector2 bottomPos;

    private void Awake()
    {
        cur = this;
        //rootPos = mainPanelRect.anchoredPosition;
        //upperPos = rootPos + Vector2.up * 220;
        //bottomPos = rootPos - Vector2.up * 180;
    }

    public override void Open()
    {
        base.Open();
        //SoundManager.PlayEfx(SoundID.sfx_ui_popup_show);

        //mainPanelObject.SetActive(false);
        //mainPanelRect.anchoredPosition = upperPos;

        //canvasGroup.alpha = 0;
        //canvasGroup.interactable = true;
        //if (UIManager.Ins.IsOpened<CanvasFloating>())
        //{
        //    UIManager.Ins.GetUI<CanvasFloating>().HideGold_Heart();
        //    UIManager.Ins.GetUI<CanvasFloating>().HideAllSetting();
        //    UIManager.Ins.GetUI<CanvasFloating>().ShowGold_Heart();
        //}
        Pop();

        timeObject.SetActive(false);
        CheckSpinButtonAndMore();
        SetupWheel();

        isSpinning = false;
        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);

        StartSpinWheelLightsBlink();
    }

    private void Pop()
    {
        //canvasGroup.DOFade(1, .28f);

        //mainPanelObject.SetActive(true);
        //mainPanelRect.DOAnchorPos(rootPos, .36f).SetEase(Ease.OutSine);
    }

    protected void Close()
    {
        CloseDirectly();

        spinTween.Kill();
        StopSpinWheelLightsBlink();

        //CanvasFloatingStuff.cur.SetHighLightGold(false);
        //CanvasFloatingStuff.SetPlusIconDefault();
        //CanvasFloatingStuff.cur.ChangeGoldRectInHome();

        isSpinning = false;
    }

    public override void CloseDirectly()
    {
        base.CloseDirectly();


        //canvasGroup.interactable = false;
        //canvasGroup.DOFade(0, .28f);
        //mainPanelRect.DOAnchorPos(bottomPos, .28f).SetEase(Ease.Linear)
        //    .OnComplete(() =>
        //    {
        //        //UIManager.Ins.OpenUI<CanvasFloating>();
        //        base.CloseDirectly();

        //    });
    }

    private void SetBar()
    {
        var require = SpinDataFragment.cur.GetRequiredPassedLevel();
        var passed = Mathf.Min(SpinDataFragment.cur.gameData.levelPassedNum, require);

        var per = (float)passed / require;
        progressBarImage.fillAmount = per;
    }

    public void OnCLickExit()
    {
        if (isSpinning) return;
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_ui_popup_disappear);
        BlockUI.Ins.UnBlock();
        UIManager.Ins.GetUI<CanvasFloating>().ShowGoldAndHeart();
        UIManager.Ins.GetUI<CanvasFloating>().ShowSettingButton(true);
        Close();
    }

    public void OnClickSpin()
    {
        if (isSpinning || !isSpinable) return;
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        Spinning();

        SpinDataFragment.cur.ConsumePassedLevel();

        CheckSpinButtonAndMore();
    }

    public void OnClickAdsSpin()
    {
        if (isSpinning || !SpinDataFragment.cur.IsAdsAble()) return;
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_click);


        canvasGroup.interactable = false;

        //AdsManager.Ins.ShowRewardedAd(
        //               GameDataFragment.cur.dataPlayer.cur_level,
        //               "Spin",
        //               "Spin",
        //               "Spin",
        //               1,
        //               "popup_spin",
        //               onClose,
        //               onFall
        //           );
        onClose();
        //AdsManager.Ins.ShowRewardedAd("SPIN", () =>
        //{
        void onClose()
        {
            SpinDataFragment.cur.gameData.isAdsAble = false;
            SpinDataFragment.cur.MarkTime();
            Spinning();
            CheckSpinButtonAndMore();
            canvasGroup.interactable = true;
        }

        void onFall()
        {
            canvasGroup.interactable = true;
            BlockUI.Ins.UnBlock();

        }

        //}, () => { canvasGroup.interactable = true; });
    }

    private void CheckSpinButtonAndMore()
    {
        var require = SpinDataFragment.cur.GetRequiredPassedLevel();
        var passed = Mathf.Min(SpinDataFragment.cur.gameData.levelPassedNum, require);
        passedLevelText.text = passed + "/" + require;
        passLevelDescriptionText.text = ("Win " + require + " levels to spin the Wheel").ToUpper();

        isSpinable = passed >= require;
        var isAdsAble = SpinDataFragment.cur.IsAdsAble();

        SetBar();

        spinButtonObject.SetActive(isSpinable);
        adsSpinButtonObject.SetActive(!isSpinable && isAdsAble);
        lockSpinButtonObject.SetActive(!isSpinable && !isAdsAble);

        CheckTimeLeftTillFree();

        //CanvasHome.CheckSpinNumStatic();
    }

    private static WaitUntil waitForValidTime = new WaitUntil(() => UnbiasedTime.IsValidTime);

    public void CheckTimeLeftTillFree()
    {
        if (timeCor != null) StopCoroutine(timeCor);
        timeCor = StartCoroutine(ie_Check());
        return;

        IEnumerator ie_Check()
        {
            while (!SpinDataFragment.cur.IsAdsAble())
            {
                yield return waitForValidTime;
                if (!SpinDataFragment.cur.CheckNewTimeSegment(out var timeLeft))
                {
                    timeObject.SetActive(true);
                    timeLeftText.text = timeLeft.ToTimeFormat_H_M_S();
                }
                else
                {
                    timeObject.SetActive(false);
                    CheckSpinButtonAndMore();
                    yield break;
                }

                yield return Yielders.Get(1);
            }

            timeLeftText.text = "";
            timeObject.SetActive(false);
        }
    }

    private void SetupWheel()
    {
        wheelTrans.eulerAngles = new Vector3(0, 0, Random.Range(0, 360f));
    }

    private void StartSpinWheelLightsBlink()
    {
        StopSpinWheelLightsBlink();
        if (spinWheelLights == null || spinWheelLights.Length == 0) return;

        spinLightsSequence = DOTween.Sequence().SetUpdate(true).SetLoops(-1, LoopType.Restart);
        var step = Mathf.Max(0.05f, spinLightStepSeconds);

        // Pha 1: đèn index chẵn sáng, lẻ tối
        spinLightsSequence.AppendCallback(() => SetSpinWheelLightsAlternating(oddIndicesLit: false));
        spinLightsSequence.AppendInterval(step);
        // Pha 2: đèn index lẻ sáng, chẵn tối
        spinLightsSequence.AppendCallback(() => SetSpinWheelLightsAlternating(oddIndicesLit: true));
        spinLightsSequence.AppendInterval(step);
    }

    private void StopSpinWheelLightsBlink()
    {
        spinLightsSequence?.Kill();
        spinLightsSequence = null;
        if (spinWheelLights == null) return;
        for (var i = 0; i < spinWheelLights.Length; i++)
            SetSpinWheelLightState(i, false);
    }

    private void SetSpinWheelLightsAlternating(bool oddIndicesLit)
    {
        if (spinWheelLights == null) return;
        for (var i = 0; i < spinWheelLights.Length; i++)
        {
            var isLit = oddIndicesLit ? (i % 2 == 1) : (i % 2 == 0);
            SetSpinWheelLightState(i, isLit);
        }
    }

    private void SetSpinWheelLightState(int index, bool isOn)
    {
        if (spinWheelLights == null || index < 0 || index >= spinWheelLights.Length) return;
        var pair = spinWheelLights[index];
        if (pair.onObject != null) pair.onObject.SetActive(isOn);
        if (pair.offObject != null) pair.offObject.SetActive(!isOn);
    }

    private void Spinning()
    {
        // Kiểm tra dữ liệu trước, tránh exception làm kẹt trạng thái
        if (prizeAngle == null || prizeAngle.Length == 0)
        {
            Debug.LogError("[CanvasSpin] prizeAngle chưa được gán trong Inspector");
            return;
        }

        curPrizeIndex = GetPossiblityIndex();
        if (curPrizeIndex < 0 || curPrizeIndex >= prizeAngle.Length)
        {
            Debug.LogError($"[CanvasSpin] prizeAngle thiếu phần tử index {curPrizeIndex}, hiện có {prizeAngle.Length}");
            return;
        }

        NoffSpin.UpdateNoffStatic();

        isSpinning = true;
        spinTween?.Kill();
        timer = TICK_OFFSET / 2;
        lastAngle = 0;

        float startAngle = wheelTrans.eulerAngles.z;
        float endAngle = 360f * 10 + prizeAngle[curPrizeIndex] + Random.Range(-prizeOffsetAngle, prizeOffsetAngle);

        spinTween = DOVirtual.Float(startAngle, endAngle, 5.32f, (x) =>
        {
            wheelTrans.eulerAngles = Vector3Pot.GetVector3(0, 0, x);
            float diff = Mathf.Max(wheelTrans.eulerAngles.z - lastAngle, 0);
            lastAngle = wheelTrans.eulerAngles.z;
            timer += diff;
            if (timer > TICK_OFFSET)
            {
                timer = 0;
                PopPointer();
            }
        })
            .SetEase(Ease.InOutSine)
            .SetUpdate(true) // chạy kể cả khi timeScale = 0
            .OnComplete(() =>
            {
                SetPrize(curPrizeIndex);
                isSpinning = false;
            });
    }

    private void PopPointer()
    {
        pointerPopTween?.Complete();
        pointerPopTween = pointerRect.DORotate(pointerHitAngle, popTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                pointerPopTween = pointerRect.DORotate(pointerNormAngle, popTime)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true);
            });
    }

    private int GetPossiblityIndex()
    {
        if (SpinDataFragment.cur.gameData.spinRank == 0)
        {
            return 4;
        }

        float i = Random.Range(0, 100f);
        return i switch
        {
            < 40 => 0,
            < 60 => 4,
            < 70 => 1,
            < 80 => 2,
            < 90 => 3,
            _ => 5
        };
    }

    private void SetPrize(int index)
    {
        // var pos = starRect.position.ToFullScreenCanvasPosFromOverLayCanvasPos(canvasRect);
        var pos = Vector2.zero;
        // Debug.Log("POS " + pos);
        BlockUI.Ins.Block();
        switch (index)
        {
            case 0:
                GameDataFragment.cur.ChangeMoney(50, "Spin");
                UIManager.Ins.GetUI<CanvasFloating>().ShowGold();

                rewardImages[0].Play();
                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.GoldSplash, pos);
                //FirebaseManager.Ins.reward_claim("SPIN", "GOLD_15");
                //ResourcesDataFragment.cur.AddGold(15, "SPIN");
                break;
            case 1:
                GameDataFragment.cur.ChangeBoosterCount(BoosterType.Click, 1, "Spin");

                rewardImages[1].Play();

                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.ClearSplash, pos);
                //FirebaseManager.Ins.reward_claim("DAILY_REWARD", "CLEAR");
                //ResourcesDataFragment.cur.AddClear(1, "SPIN");
                break;
            case 2:
                GameDataFragment.cur.ChangeMoney(300, "Spin");
                UIManager.Ins.GetUI<CanvasFloating>().ShowGold();

                rewardImages[2].Play();

                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.GoldSplash, pos);
                //FirebaseManager.Ins.reward_claim("DAILY_REWARD", "GOLD_50");
                //ResourcesDataFragment.cur.AddGold(50, "SPIN");
                break;
            case 3:
                GameDataFragment.cur.ChangeBoosterCount(BoosterType.Magnet, 1, "Spin");
                rewardImages[3].Play();


                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.SortSplash, pos);
                //FirebaseManager.Ins.reward_claim("DAILY_REWARD", "SORT");
                //ResourcesDataFragment.cur.AddSort(1, "SPIN");
                break;
            case 4:
                GameDataFragment.cur.ChangeMoney(150, "Spin");
                UIManager.Ins.GetUI<CanvasFloating>().ShowGold();

                rewardImages[4].Play();

                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.GoldSplash, pos);
                //FirebaseManager.Ins.reward_claim("DAILY_REWARD", "GOLD_25");
                //ResourcesDataFragment.cur.AddGold(25, "SPIN");
                break;
            case 5:
                GameDataFragment.cur.ChangeBoosterCount(BoosterType.Shuffle, 1, "Spin");

                rewardImages[5].Play();

                //CanvasFloatingStuff.cur.PopItemStuff(FloatingEffectType.RefreshSplash, pos);
                //FirebaseManager.Ins.reward_claim("DAILY_REWARD", "REFRESH");
                //ResourcesDataFragment.cur.AddRefresh(1, "SPIN");
                break;
        }
        DOVirtual.DelayedCall(1.3f, () =>
        {
            BlockUI.Ins.UnBlock();
        });
    }
}

// 250 Gold 13,5 %
// 500 Gold 13,5 %
// 750 Gold 13,5 %
// 1000 Gold 13,5 %
// 2000 Gold 13,5 %
// 1 Crystal 13,5 %
// 10 Crystal 13,5 %
// 50 Crystal 5,5 %