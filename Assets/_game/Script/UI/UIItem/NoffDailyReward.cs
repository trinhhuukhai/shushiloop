using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class NoffDailyReward : MonoBehaviour
{
    public static NoffDailyReward cur;

    public GameObject noffObject;
    public TMP_Text noffText;
    public RectTransform noffRect;
    private Tween noffTween;
    private Vector3 noffRootScale;

    private bool isNoffOn;

    private void Awake()
    {
        cur = this;
        noffRootScale = noffRect.localScale;
    }

    private void OnEnable()
    {
        UpdateNoff();

        Check();
    }

    private void OnDisable()
    {
        noffTween?.Kill();
    }

    private void Check()
    {
        StartCoroutine(ie_Check());
        return;

        IEnumerator ie_Check()
        {
            while (true)
            {
                UpdateNoff();

                yield return Yielders.Get(4);
            }
        }
    }

    private void UpdateNoff()
    {
        if (DailyRewardDataFragment.cur == null)
            return;

        int dailyNum = 0;

        // Reward free 6 giờ
        if (DailyRewardDataFragment.cur.CheckIfItsTimeToFree(out _))
        {
            dailyNum++;
        }

        //Các reward Ads chưa claim
        var dailyData = DailyRewardDataFragment.cur.gameData;

        if (dailyData.itemFlags != null)
        {
            for (int i = 0; i < dailyData.itemFlags.Count; i++)
            {
                // false = chưa claim
                if (!dailyData.itemFlags[i])
                {
                    dailyNum++;
                }
            }
        }

        // noti
        bool isOn = dailyNum > 0;

        noffObject.SetActive(isOn);
        noffText.text = dailyNum.ToString();

        if (isOn)
        {
            NoffBubble();
        }
        else
        {
            noffTween?.Kill();
            noffTween = null;
            noffRect.localScale = noffRootScale;
        }

        isNoffOn = isOn;
    }

    //private void UpdateNoff()
    //{
    //    int dailyNum = 0;
    //    if (DailyRewardDataFragment.cur.CheckIfItsTimeToFree(out var _)) dailyNum++;

    //    var dailyData = DailyRewardDataFragment.cur.gameData;
    //    //var config = GameManager.Ins.IsIapCamp() ? ConfigHolder.cur.dailyRewardForCampIap.config : new ConfigHolder.DailyRewardForCampIapConfigData();

    //    //for (int i = 0; i < config.adsRewardList.Count; i++)
    //    //{
    //    //    if (!dailyData.itemFlags[i]) dailyNum++;
    //    //}

    //    noffObject.SetActive(dailyNum > 0);
    //    noffText.text = dailyNum.ToString();

    //    var isOn = dailyNum > 0;
    //    if (isOn) NoffBubble();
    //    else noffTween?.Kill();

    //    isNoffOn = isOn;
    //}

    public static void UpdateNoffStatic()
    {
        if (cur == null) return;
        cur.UpdateNoff();
    }

    private void NoffBubble()
    {
        if (isNoffOn) return;
        noffTween?.Kill();
        noffRect.localScale = noffRootScale;
        noffTween = noffRect.DOScale(noffRootScale * 1.12f, .72f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }
}