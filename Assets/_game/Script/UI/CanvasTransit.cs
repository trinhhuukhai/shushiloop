using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasTransit : UICanvas
{

    public List<GameObject> tutorials;
    public TMP_Text txtLevel;

    private void Awake()
    {

        var ratio = ((float)Screen.width / Screen.height) / (1080f / 1920);
    }

    public override void Open()
    {
        base.Open();
        txtLevel.text = "LEVEL " + (GameDataFragment.cur.dataPlayer.cur_level + 1).ToString();
    }

    private int UCLN(int a, int b)
    {
        int maxNum = 100;
        while (a != b)
        {
            if (a > b)
                a = a - b;
            else
                b = b - a;

            maxNum--;
            if (maxNum < 0) return 0;
        }

        return a; // or return b; a = b
    }
    private void SelectTut(int currentLevel)
    {
        if (tutorials == null || tutorials.Count == 0) return;
        tutorials.ForEach(tut => { if (tut != null) tut.SetActive(false); });

        int maxIndex = tutorials.Count - 1;
        int chosenIndex;

        // Rule:
        // level 0 -> 4 : tut 0
        // level 5 : tut 1
        // level 6 -> 9 : random 0 1
        // level 10 : tut 2
        // level 11 -> 14 : random 0 1 2
        // level 15 : tut 3
        // còn lại : random 0 1 2 3
        if (currentLevel <= 4)
        {
            chosenIndex = 0;
        }
        else if (currentLevel == 5)
        {
            chosenIndex = 1;
        }
        else if (currentLevel < 10)
        {
            chosenIndex = UnityEngine.Random.Range(0, 2); // 0..1
        }
        else if (currentLevel == 10)
        {
            chosenIndex = 2;
        }
        else if (currentLevel < 15)
        {
            chosenIndex = UnityEngine.Random.Range(0, 3); // 0..2
        }
        else if (currentLevel == 15)
        {
            chosenIndex = 3;
        }
        else
        {
            chosenIndex = UnityEngine.Random.Range(0, 4); // 0..3
        }

        chosenIndex = Mathf.Clamp(chosenIndex, 0, maxIndex);
        if (tutorials[chosenIndex] != null)
            tutorials[chosenIndex].SetActive(true);

    }

#if UNITY_EDITOR
    [ContextMenu("AAA")]
    public void Trns()
    {
        MakeTransit();
    }
#endif
    public void MakeTransit(Action onDone = null)
    {

        onDone?.Invoke();
        base.CloseDirectly();
    }

    public Image fill;
    public void MakeDelayTransit(Action action, float delay = 1.44f)
    {
        //SelectTut(LevelDataFragment.cur.gameData.level);
        fill.DOKill();
        fill.gameObject.SetActive(true);
        fill.fillAmount = 0f;
        fill.DOFillAmount(1f, delay).SetEase(Ease.Linear);
        SoundManager.PlayEfx(SoundID.sfx_game_loading);
        Timer.ScheduleSupreme(delay, () =>
        {
            fill.DOKill();
            fill.fillAmount = 0f;
            fill.gameObject.SetActive(false);

            action?.Invoke();
            Time.timeScale = 1f;
            base.CloseDirectly();
        });
    }
}