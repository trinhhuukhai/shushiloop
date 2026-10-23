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


    private void Awake()
    {
        ins = this;
    }

    private void OnDisable()
    {
        if (imgProgress != null)
        {
            imgProgress.DOKill();
        }
    }
    public void SetPercent(float to, float time)
    {
        imgProgress.DOKill();

        imgProgress.DOFillAmount(to, time)
            .SetEase(Ease.Linear).SetUpdate(true);
    }

    public IEnumerator ie_txtLoading()
    {
        for (int i = 0; i < 4; i++)
        {
            if (txt_loading == null) yield break;
            txt_loading.text = "Loading";
            yield return Cache.GetWFS(0.25f);
            txt_loading.text = "Loading.";
            yield return Cache.GetWFS(0.25f);
            txt_loading.text = "Loading..";
            yield return Cache.GetWFS(0.25f);
            txt_loading.text = "Loading...";
            yield return Cache.GetWFS(0.25f);
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void PopNoti(Action onAcceptUpdate, Action onDeclineUpdate)
    {
        //this.onAcceptUpdate = onAcceptUpdate;
        //this.onDeclineUpdate = onDeclineUpdate;

        //inAppUpdateObject.SetActive(true);
        //inAppUpdateCg.alpha = 0;
        //inAppUpdateCg.DOFade(1, 0.32f);
    }

    public void OnClickAccept()
    {
        //SoundManager.PlayEfx(SoundID.sfx_ui_button_click);
        OpenStore();
        //inAppUpdateObject.SetActive(false);

        //onAcceptUpdate?.Invoke();
    }

    public void OnClickDecline()
    {
        //SoundManager.PlayEfx(SoundID.sfx_ui_button_click);
        //inAppUpdateObject.SetActive(false);

        //onDeclineUpdate?.Invoke();
    }

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