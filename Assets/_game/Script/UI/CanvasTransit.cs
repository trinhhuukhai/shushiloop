using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UICTransis owns the visual cover. GameManager owns the level-loading coroutine.
public class CanvasTransit : UICanvas
{
    public List<GameObject> tutorials = new List<GameObject>();
    public Image fill;
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField, Min(0.1f)] private float preparationDuration = 0.45f;
    private Coroutine delayedTransition;

    public override void Open()
    {
        base.Open();
        if (fadeGroup != null) { fadeGroup.DOKill(); fadeGroup.alpha = 0; fadeGroup.blocksRaycasts = true; }
        SetProgress(0);
    }

    public IEnumerator CoverLevel(int levelIndex)
    {
        if (levelLabel != null) levelLabel.text = "Level " + (levelIndex + 1);
        if (fadeGroup != null)
            yield return fadeGroup.DOFade(1, 0.16f).SetUpdate(true).WaitForCompletion();
        if (fill != null)
            yield return fill.DOFillAmount(0.75f, preparationDuration).SetEase(Ease.OutSine).SetUpdate(true).WaitForCompletion();
        else yield return new WaitForSecondsRealtime(preparationDuration);
    }

    public void SetProgress(float progress)
    {
        if (fill == null) return;
        fill.DOKill(); fill.gameObject.SetActive(true); fill.fillAmount = Mathf.Clamp01(progress);
    }

    public IEnumerator RevealLevel(bool closeWhenComplete = true)
    {
        SetProgress(1);
        yield return new WaitForSecondsRealtime(0.12f);
        if (fadeGroup != null)
            yield return fadeGroup.DOFade(0, 0.2f).SetUpdate(true).WaitForCompletion();
        if (closeWhenComplete) CloseDirectly();
    }

    public void MakeTransit(Action onDone = null) => MakeDelayTransit(onDone, preparationDuration);

    public void MakeDelayTransit(Action action, float delay = 1.44f)
    {
        if (delayedTransition != null) StopCoroutine(delayedTransition);
        Open();
        delayedTransition = StartCoroutine(DelayedTransition(action, Mathf.Max(0, delay)));
    }

    private IEnumerator DelayedTransition(Action action, float delay)
    {
        if (fadeGroup != null) fadeGroup.alpha = 1;
        if (fill != null) fill.DOFillAmount(0.75f, delay).SetUpdate(true);
        yield return new WaitForSecondsRealtime(delay);
        bool completed = false;
        try { action?.Invoke(); completed = true; }
        finally { delayedTransition = null; if (!completed) CloseDirectly(); }
        yield return RevealLevel();
    }

    protected override void OnDisable()
    {
        if (delayedTransition != null) StopCoroutine(delayedTransition);
        delayedTransition = null;
        if (fill != null) fill.DOKill();
        if (fadeGroup != null) fadeGroup.DOKill();
        base.OnDisable();
    }

    public override void BackKey() { }
}