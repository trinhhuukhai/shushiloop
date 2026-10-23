using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasGamePlay : UICanvas
{
    public RectTransform refreshButtonRect;
    public RectTransform rootPanel;

    public RectTransform TopGroup;
    public Vector2 posTopGroupOr;

    public TMP_Text txtLevvel;
    public TMP_Text txtGold;

    private void Awake()
    {
        rootPanel.SafeAreaParentAdoption(GetComponent<Canvas>());
        posTopGroupOr = TopGroup.anchoredPosition;
    }
    public override void Open()
    {
        base.Open();
        checkShowBanner();
        TopGroup.anchoredPosition = posTopGroupOr;
        setTxtGold();
        showTxtLevel();
    }

    public void OnClickChangeSpeed()
    {
    }

    public void ChangeSpeed(bool value)
    {

    }

    public void FlyIconBooster(RectTransform iconFly, RectTransform target, int id)
    {
        if (iconFly == null || target == null)
            return;
        BlockUI.Ins.Block();
        iconFly.DOKill();

        iconFly.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.Join(
            iconFly.DOMove(target.position, 0.8f).SetEase(Ease.InOutQuad)
        );

        seq.Join(
            iconFly.DOScale(Vector3.one, 0.8f).SetEase(Ease.OutQuad)
        );

        seq.OnComplete(() =>
        {
            iconFly.gameObject.SetActive(false);
            switch (id) {
                case 1:
                    break;
                case 2:
                    break;
                 case 3:
                    break;
            }
            BlockUI.Ins.UnBlock();
        });
    }

    public void ShowAnim(TypeLevel type)
    {
        BlockUI.Ins.Block();
        DOVirtual.DelayedCall(2f, () =>
        {
            BlockUI.Ins.UnBlock();
        });

    }

    [SerializeField] private Transform boosterClearTf;
    [SerializeField] private Transform boosterShuffleTf;   // MỚI: kéo icon shuffle vào đây
    [SerializeField] private Transform boosterMagnetTf;

    private readonly List<Tween> _boosterHintTweens = new List<Tween>();
    //private readonly Dictionary<Transform, Vector3> _boosterOriginalScales = new Dictionary<Transform, Vector3>();

    public void ShowRandomBoosterHand() 
    {
        HideBoosterHands();
        if (GameDataFragment.cur == null ||
            GameDataFragment.cur.dataPlayer == null)
            return;

        var data = GameDataFragment.cur.dataPlayer;

        // Điều kiện: đã click tutorial và còn số lượng.
        bool hasClear = data.boosterClick > 0 && data.isClickBucketTutorialClear;
        bool hasMagnet = data.boosterHut > 0 && data.isClickBucketTutorialMagnet;

        // Shuffle: thêm điều kiện slot chưa full và còn xáo được.
        bool hasShuffle = data.boosterSuffle > 0
                          && data.fisrtUseBoosterSuffle;

        if (hasClear) PlayBoosterHint(boosterClearTf);
        if (hasShuffle) PlayBoosterHint(boosterShuffleTf);
        if (hasMagnet) PlayBoosterHint(boosterMagnetTf);
    }

    private readonly Dictionary<Transform, Vector3> _boosterOriginalScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Quaternion> _boosterOriginalRotations = new Dictionary<Transform, Quaternion>();

    private void PlayBoosterHint(Transform target)
    {
        if (target == null)
            return;

        _boosterOriginalScales[target] = target.localScale;
        Quaternion r = target.localRotation;
        _boosterOriginalRotations[target] = r;

        float angle = 12f; 

        Tween t = DOTween.Sequence()
            // Lắc trái - phải, biên độ giảm dần
            .Append(target.DOLocalRotate(new Vector3(0f, 0f, angle), 0.08f).SetEase(Ease.OutQuad))
            .Append(target.DOLocalRotate(new Vector3(0f, 0f, -angle), 0.16f).SetEase(Ease.InOutQuad))
            .Append(target.DOLocalRotate(new Vector3(0f, 0f, angle * 0.6f), 0.14f).SetEase(Ease.InOutQuad))
            .Append(target.DOLocalRotate(new Vector3(0f, 0f, -angle * 0.6f), 0.12f).SetEase(Ease.InOutQuad))
            .Append(target.DOLocalRotate(Vector3.zero, 0.08f).SetEase(Ease.InQuad))
            // Nghỉ rồi lặp
            .AppendInterval(1.2f)
            .SetLoops(-1, LoopType.Restart);

        _boosterHintTweens.Add(t);
    }
    public void HideBoosterHands()
    {
        for (int i = 0; i < _boosterHintTweens.Count; i++)
            _boosterHintTweens[i]?.Kill();
        _boosterHintTweens.Clear();

        foreach (var kv in _boosterOriginalScales)
        {
            if (kv.Key != null)
                kv.Key.localScale = kv.Value;
        }
        _boosterOriginalScales.Clear();

        foreach (var kv in _boosterOriginalRotations)
        {
            if (kv.Key != null)
                kv.Key.localRotation = kv.Value;
        }
        _boosterOriginalRotations.Clear();
    }


    public void showTxtLevel()
    {
        txtLevvel.text = "Level " + (GameDataFragment.cur.dataPlayer.cur_level + 1).ToString();

    }
    public void checkShowBanner()
    {
     
    }

    public void setTxtGold()
    {
        txtGold.text = GameDataFragment.cur.dataPlayer.money.ToString("N0");
    }


    public void AnimTopGroupX2()
    {
        TopGroup.DOAnchorPosY(TopGroup.anchoredPosition.y + 300, 0.6f).SetEase(Ease.OutSine);
    }

    public void ResfeshX2()
    {
        TopGroup.anchoredPosition = posTopGroupOr;
    }

    public void AnimBoosterGroup()
    {
        BlockUI.Ins.Block();
        UIManager.Ins.GetUI<CanvasFloating>().AnimTopGroup();
        AnimTopGroupX2();
    }

    public void Refresh()
    {
        txtLevvel.text = "Level " + (GameDataFragment.cur.dataPlayer.cur_level + 1).ToString();
    }

    public void btnSettingIngame()
    {
        UIManager.Ins.GetUI<CanvasFloating>().OnSetting();
    }

    public void ButtonBoosterPick()
    {
       

    }

    public void ButtonBoosterHoanDoi()
    {
     
    }

    public void ButtonBoosterHut()
    {
     
    }

    public void Close()
    {
        base.Close(0f);
    }

    public void RefeshCoountAllBooster()
    {
        UpdateBoosterClick();
        UpdateBoosterSuffle();
        UpdateBoosterHut();
    }

    public void UpdateBoosterClick()
    {
        int boosterClick = GameDataFragment.cur.dataPlayer.boosterClick;    
    }

    public void UpdateBoosterSuffle() 
    {
        int boosterSuffle = GameDataFragment.cur.dataPlayer.boosterSuffle;
        if (boosterSuffle > 0)
        {
           

        }
        else
        {
          
        }
    }
    public void UpdateBoosterHut()
    {
        int boosterHut = GameDataFragment.cur.dataPlayer.boosterHut;
        if (boosterHut > 0)
        {
          
        }
        else
        {
           
        }
    }


    public void BoosterClick()
    {
        HideBoosterHands();
        if (!GameDataFragment.cur.dataPlayer.fisrtUseBoosterClear)
        {
            ButtonBoosterPick();
           


        }
        else
        {
            int countClick = GameDataFragment.cur.dataPlayer.boosterClick;
            if (countClick > 0)
            {
            }
            else
            {
            }
        }
    }

    public void BoosterSuffle()
    {

        HideBoosterHands();
       
        if (!GameDataFragment.cur.dataPlayer.fisrtUseBoosterSuffle)
        {

            ButtonBoosterHoanDoi();
         

        }
        else
        {
            int countClick = GameDataFragment.cur.dataPlayer.boosterSuffle;
            if (countClick > 0)
            {



            }
            else
            {
            }
        }
    }

    public void BoosterHut()
    {
    
        HideBoosterHands();
        if (!GameDataFragment.cur.dataPlayer.fisrtUseBoosterMagnet)
        {
            ButtonBoosterHut();
         
        }
        else
        {
            int countClick = GameDataFragment.cur.dataPlayer.boosterHut;
            if (countClick > 0)
            {
                GameDataFragment.cur.ChangeBoosterCount(BoosterType.Magnet, -1, "GamePlay");
                UpdateBoosterHut();
                ButtonBoosterHut();
            }
            else
            {
                UIManager.Ins.OpenUI<CanvasMoreBooster>().Innit(BoosterType.Magnet);
            }
        }
    }

    public RectTransform textNotiTf;
    
    public void showNoTiWrboosterMagnet()
    {
        DOTween.Kill(textNotiTf, true);
        textNotiTf.gameObject.SetActive(true);

        Vector3 oldPos = textNotiTf.localPosition;
        textNotiTf.DOLocalMoveY(oldPos.y + 50f, 0.6f).SetEase(Ease.OutSine).OnComplete(() =>
        {

            textNotiTf.gameObject.SetActive(false);
            textNotiTf.localPosition = oldPos;
        });
    }


    public RectTransform textNotiShuffleTf;

    public void showNoTiWrboosterShuffle()
    {
        DOTween.Kill(textNotiShuffleTf, true);
        textNotiShuffleTf.gameObject.SetActive(true);

        Vector3 oldPos = textNotiShuffleTf.localPosition;
        textNotiShuffleTf.DOLocalMoveY(oldPos.y + 50f, 0.6f).SetEase(Ease.OutSine).OnComplete(() =>
        {

            textNotiShuffleTf.gameObject.SetActive(false);
            textNotiShuffleTf.localPosition = oldPos;
        });
    }

}
