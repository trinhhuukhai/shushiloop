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

    // Lưu tham chiếu popup hồ sơ hiện tại cho các ô chọn avatar.
    private void Awake()
    {
        cur = this;
    }

    // Khởi tạo các lựa chọn avatar, khung và điền hình ảnh cùng tên đang lưu.
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

    // Chuyển sang đặt tên khi mở lần đầu, hoặc mở hồ sơ chính với hiệu ứng và tắt chế độ sửa.
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

    // Đồng bộ avatar, tên, level, thành tích và ngày tham gia trên bảng hồ sơ.
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

    // Chuyển giữa bảng xem hồ sơ và bảng chỉnh sửa, đồng bộ dấu chọn avatar.
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

    // Đóng trực tiếp popup hồ sơ.
    public void OnClose()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        base.CloseDirectly();
    }

    // Bật chế độ chỉnh sửa hồ sơ.
    public void OnOpenEditProfile()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        SetShowEdit(true);
    }

    // Thoát chế độ chỉnh sửa và hiển thị lại bảng hồ sơ chính.
    public void OnCloseProfile()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        SetShowEdit(false);
    }

    // Làm mới avatar, khung cùng dấu chọn trong bảng chỉnh sửa theo dữ liệu hồ sơ.
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

    // Cập nhật tên không rỗng hoặc phục hồi tên đang lưu nếu người chơi xóa hết.
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

    // Cắt tên về 10 ký tự khi nội dung ô nhập vượt quá 14 ký tự.
    public void OnValueChange()
    {
        if (inputField_name.text.Length > 14)
        {
            inputField_name.text = inputField_name.text.Substring(0, 10);
        }
    }

    // Điểm mở cheat từ hồ sơ trên bản production; hiện chưa có xử lý đang hoạt động.
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