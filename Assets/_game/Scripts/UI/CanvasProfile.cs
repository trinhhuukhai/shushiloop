using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayerPrefs = UnityEngine.PlayerPrefs;
using Random = UnityEngine.Random;

public class CanvasProfile : UICanvas
{
    public static CanvasProfile cur;
    public RectTransform mainPanelRect;
    //private static AnimationCurve PopCurve => UIManager.ins.popCanvasCurve;
    private Tween popTween;

    public GameObject editPanelObject;
    public UIChooseAvatar[] uIChooseAvatars;
    public Image img_avatar;
    public Image img_frame;
    private UIChooseAvatar choosing;
    public InputField inputField_name;
    private string valueString = "";

    public GameObject mainPanelObject;
    public Image avatarImage;
    public Image frameImage;
    public Text userNameText;
    public TMP_Text joinDateText;
    public TMP_Text levelText;
    public TMP_Text firstTryText;
    public TMP_Text winStreakText;
    public TMP_Text exploredItemText;

    private void Awake()
    {
        cur = this;
    }

    private void Start()
    {
        for (int i = 0; i < uIChooseAvatars.Length; i++)
        {
            uIChooseAvatars[i].id = i;
            uIChooseAvatars[i].idAvatar = i % ProfileDataFragment.cur.spr_avatars.Length;
            uIChooseAvatars[i].idFrame = i % ProfileDataFragment.cur.spr_frames.Length;
            uIChooseAvatars[i].SetUp();
        }

        img_avatar.sprite = ProfileDataFragment.cur.spr_avatars[ProfileDataFragment.cur.gameData.idAvatar];
        img_frame.sprite = ProfileDataFragment.cur.spr_frames[ProfileDataFragment.cur.gameData.idFrame];
        img_frame.SetNativeSize();
        img_avatar.SetNativeSize();

        // name
        inputField_name.text = ProfileDataFragment.cur.gameData.userName;
    }

    public override void Open()
    {
        bool isFirstOpen =
            PlayerPrefs.GetInt(DataManager.FIRST_OPEN_PROFILE, 0) == 0;

        if (isFirstOpen)
        {
            // Đánh dấu đã mở lần đầu
            PlayerPrefs.SetInt(DataManager.FIRST_OPEN_PROFILE, 1);
            PlayerPrefs.Save();

            UIManager.Ins.OpenUI<CanvasFirstOpenProfile>();

            base.CloseDirectly();
            return;
        }

        base.Open();

        popTween?.Kill();

        mainPanelRect.localScale = Vector3.one * .05f;

        popTween = mainPanelRect
            .DOScale(Vector3.one, .24f)
            .SetUpdate(true)
            .SetEase(Ease.OutSine);

        SetShowEdit(false);
    }

    public void UpdateMainPanel()
    {
        avatarImage.sprite = ProfileDataFragment.cur.spr_avatars[ProfileDataFragment.cur.gameData.idAvatar];
        frameImage.sprite = ProfileDataFragment.cur.spr_frames[ProfileDataFragment.cur.gameData.idFrame];
        frameImage.SetNativeSize();
        avatarImage.SetNativeSize();
        userNameText.text = ProfileDataFragment.cur.gameData.userName;
        //joinDateText.text = PlayerRecordDataFragment.cur.GetDateString();
        levelText.text = (GameDataFragment.cur.dataPlayer.cur_level + 1).ToString();
        firstTryText.text = ProfileDataFragment.cur.gameData.firstTryWinMax.ToString();
        winStreakText.text = ProfileDataFragment.cur.gameData.winStreakMax.ToString();
        //exploredItemText.text = PlayerRecordDataFragment.cur.GetExploredItemString();
        // ==============================
        // JOIN DATE
        // ==============================

        if (ProfileDataFragment.cur.gameData.joinTime != default)
        {
            joinDateText.text =
                ProfileDataFragment.cur.gameData.joinTime.ToString("MM/dd/yyyy");
        }
        else
        {
            joinDateText.text =
                System.DateTime.Now.ToString("MM/dd/yyyy");
        }
    }

    public void SetShowEdit(bool isEditOn)
    {
        editPanelObject.SetActive(isEditOn);
        mainPanelObject.SetActive(!isEditOn);

        if (isEditOn)
        {
            choosing = uIChooseAvatars[ProfileDataFragment.cur.gameData.idChoose];
            choosing.img_selecting.gameObject.SetActive(true);
        }
        else
        {
            UpdateMainPanel();
        }
    }

    public void OnClose()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        base.CloseDirectly();
    }

    public void OnOpenEditProfile()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        SetShowEdit(true);
    }

    public void OnCloseProfile()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        SetShowEdit(false);
    }

    public void UpdateAvatar()
    {
        img_avatar.sprite = ProfileDataFragment.cur.spr_avatars[ProfileDataFragment.cur.gameData.idAvatar];
        img_frame.sprite = ProfileDataFragment.cur.spr_frames[ProfileDataFragment.cur.gameData.idFrame];
        img_frame.SetNativeSize();
        img_avatar.SetNativeSize();

        choosing.img_selecting.gameObject.SetActive(false);
        choosing = uIChooseAvatars[ProfileDataFragment.cur.gameData.idChoose];
        choosing.img_selecting.gameObject.SetActive(true);
    }

    public void OnChangeName()
    {
        if (inputField_name.text.Length > 0)
        {
            ProfileDataFragment.cur.gameData.userName = inputField_name.text;
        }
        else
        {
            inputField_name.text = ProfileDataFragment.cur.gameData.userName;
        }
    }

    public void OnValueChange()
    {
        if (inputField_name.text.Length > 14)
        {
            inputField_name.text = inputField_name.text.Substring(0, 10);
        }
    }

    public void OpenCheatOnProduction()
    {
        //if(inputField_name.text == GrandManager.ins.SecretKey && AdsManager.Ins.cheatEnabled && UIManager.ins.FukCanvas==null)
        //{
        //    var newGo = Instantiate(UIManager.ins.fukCanvas);
        //    UIManager.ins.FukCanvas = newGo;
        //    newGo.SetActive(true);
        //    UnbiasedTime.isCheatableTime = true;
        //}
       
    }
}