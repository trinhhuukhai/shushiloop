using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICanvas : MonoBehaviour
{
    private LevelManager pausedLevel;
    private Coroutine pendingClose;
    protected virtual bool BlocksGameplay => true;
    //public bool IsAvoidBackKey = false;
    public bool IsDestroyOnClose = false;

    protected RectTransform m_RectTransform;
    private Animator m_Animator;
    //private float m_OffsetY = 0;

    private void Start()
    {
        OnInit();
    }

    //Init default Canvas
    //khoi tao gia tri canvas
    protected virtual void OnInit()
    {
        m_RectTransform = GetComponent<RectTransform>();
        m_Animator = GetComponent<Animator>();

        // xu ly tai tho
        //float ratio = (float)Screen.height / (float)Screen.width;
        //if (ratio > 2.1f)
        //{
        //    Vector2 leftBottom = m_RectTransform.offsetMin;
        //    Vector2 rightTop = m_RectTransform.offsetMax;
        //    rightTop.y = -100f;
        //    m_RectTransform.offsetMax = rightTop;
        //    leftBottom.y = 0f;
        //    m_RectTransform.offsetMin = leftBottom;
        //    m_OffsetY = 100f;
        //}
    }

    //Setup canvas to avoid flash UI
    //set up mac dinh cho UI de tranh truong hop bi nhay' hinh
    public virtual void Setup()
    {
        UIManager.Ins.AddBackUI(this);
        UIManager.Ins.PushBackAction(this, BackKey);
    }


    //back key in android device
    //back key danh cho android
    public virtual void BackKey()
    {

    }

    //Open canvas
    //mo canvas
    public virtual void Open()
    {
        CancelInvoke(nameof(CloseDirectly));
        if (pendingClose != null) { StopCoroutine(pendingClose); pendingClose = null; }
        gameObject.SetActive(true);
        if (BlocksGameplay && pausedLevel == null && LevelManager.Ins != null)
        {
            pausedLevel = LevelManager.Ins;
            pausedLevel.SetPopupOpen(true);
        }
    }

    //close canvas directly
    //dong truc tiep, ngay lap tuc
    public virtual void CloseDirectly()
    {
        CancelInvoke(nameof(CloseDirectly));
        if (pendingClose != null) { StopCoroutine(pendingClose); pendingClose = null; }
        if (UIManager.Ins != null) UIManager.Ins.RemoveBackUI(this);
        gameObject.SetActive(false);
        if (IsDestroyOnClose)
        {
            Destroy(gameObject);
        }

    }

    protected virtual void OnDisable()
    {
        if (pausedLevel == null) return;
        LevelManager owner = pausedLevel;
        pausedLevel = null;
        owner.SetPopupOpen(false);
    }

    //close canvas with delay time, used to anim UI action
    //dong canvas sau mot khoang thoi gian delay
    public virtual void Close(float delayTime)
    {
        if (delayTime <= 0) CloseDirectly();
        else
        {
            if (pendingClose != null) StopCoroutine(pendingClose);
            pendingClose = StartCoroutine(CloseAfterDelay(delayTime));
        }
    }

    private System.Collections.IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        pendingClose = null;
        CloseDirectly();
    }

}
