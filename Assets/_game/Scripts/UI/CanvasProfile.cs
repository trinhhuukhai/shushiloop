using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayerPrefs = UnityEngine.PlayerPrefs;

// Popup hồ sơ (art Home/profile): bảng xem (avatar, tên, ngày, ID, level, thống kê, Google Play Games)
// và bảng sửa (đổi tên + chọn 1 trong các avatar, bấm Save để lưu). Bố cục dựng sẵn bằng SushiHomeArtSetup.
public class CanvasProfile : UICanvas
{
    public static CanvasProfile cur;
    public RectTransform mainPanelRect;
    private Tween popTween;

    [Header("Bảng xem")]
    public GameObject mainPanelObject;
    public Image avatarImage;
    public Image frameImage;
    public TMP_Text userNameText;
    public TMP_Text joinDateText;
    public TMP_Text idText;
    public TMP_Text levelText;
    public TMP_Text firstTryText;
    public TMP_Text starText;
    public TMP_Text restaurantText;
    public Image googleButton;
    public Image googleLabel;
    public Sprite googleLinkButton;
    public Sprite googleLinkedButton;
    public Sprite googleLinkLabel;
    public Sprite googleLinkedLabel;

    [Header("Bảng sửa")]
    public GameObject editPanelObject;
    public RectTransform editPanelRect;
    public UIChooseAvatar[] uIChooseAvatars;
    public Image img_avatar;
    public Image img_frame;
    public TMP_InputField nameInput;
    [Min(1)] public int maxNameLength = 12;

    private int pendingAvatar;
    private bool googleLinked;

    private void Awake() => cur = this;

    // Lần đầu mở: chuyển sang popup đặt tên; còn lại mở bảng xem với hiệu ứng phóng.
    public override void Open()
    {
        bool isFirstOpen = PlayerPrefs.GetInt(DataManager.FIRST_OPEN_PROFILE, 0) == 0;
        if (isFirstOpen)
        {
            PlayerPrefs.SetInt(DataManager.FIRST_OPEN_PROFILE, 1);
            PlayerPrefs.Save();
            UIManager.Ins.OpenUI<CanvasFirstOpenProfile>();
            base.CloseDirectly();
            return;
        }
        base.Open();
        SetShowEdit(false);
    }

    // Hiệu ứng phóng nhẹ cho bảng đang hiện.
    private void Pop(RectTransform panel)
    {
        if (panel == null) return;
        popTween?.Kill();
        panel.localScale = Vector3.one * 0.6f;
        popTween = panel.DOScale(Vector3.one, 0.24f).SetUpdate(true).SetEase(Ease.OutBack);
    }

    // Đồng bộ avatar, tên, ngày tham gia, ID, level và thống kê.
    public void UpdateMainPanel()
    {
        var profile = ProfileDataFragment.cur;
        var player = GameDataFragment.cur != null ? GameDataFragment.cur.dataPlayer : null;
        if (profile != null)
        {
            if (avatarImage != null) avatarImage.sprite = AvatarSprite(profile.gameData.idAvatar);
            if (userNameText != null) userNameText.text = profile.gameData.userName;
            var join = profile.gameData.joinTime != default ? profile.gameData.joinTime : System.DateTime.Now;
            if (joinDateText != null) joinDateText.text = join.ToString("MM/yyyy");
            if (firstTryText != null) firstTryText.text = profile.gameData.firstTryWinMax.ToString();
        }
        if (idText != null) idText.text = "ID: " + PlayerId;
        if (levelText != null) levelText.text = "Level\n" + ((player?.cur_level ?? 0) + 1);
        var decoration = SushiDecorationProgress.Catalog;
        if (starText != null) starText.text = SushiDecorationProgress.AvailableStars(player, decoration).ToString();
        if (restaurantText != null)
        {
            int built = SushiDecorationProgress.Built(player, decoration, SushiRestaurantCatalog.FirstRestaurantId);
            int total = decoration != null ? decoration.items.Length : 12;
            restaurantText.text = built.ToString("00") + "/" + total.ToString("00");
        }
        RefreshGoogle();
    }

    // Chuyển giữa bảng xem và bảng sửa.
    public void SetShowEdit(bool isEditOn)
    {
        if (editPanelObject != null) editPanelObject.SetActive(isEditOn);
        if (mainPanelObject != null) mainPanelObject.SetActive(!isEditOn);
        if (isEditOn)
        {
            var profile = ProfileDataFragment.cur;
            pendingAvatar = profile != null ? profile.gameData.idAvatar : 0;
            if (nameInput != null)
            {
                nameInput.characterLimit = maxNameLength;
                nameInput.text = profile != null ? profile.gameData.userName : "";
            }
            for (int i = 0; i < uIChooseAvatars.Length; i++)
            {
                uIChooseAvatars[i].id = i;
                uIChooseAvatars[i].idAvatar = i;
                uIChooseAvatars[i].SetUp();
            }
            UpdateAvatar();
            Pop(editPanelRect);
        }
        else
        {
            UpdateMainPanel();
            Pop(mainPanelRect);
        }
    }

    // Ô avatar được bấm trong bảng sửa (chưa lưu tới khi bấm Save).
    public void ChooseAvatar(int avatarIndex)
    {
        pendingAvatar = avatarIndex;
        UpdateAvatar();
    }

    // Làm mới ảnh xem trước và dấu chọn trong bảng sửa.
    public void UpdateAvatar()
    {
        if (img_avatar != null) img_avatar.sprite = AvatarSprite(pendingAvatar);
        foreach (UIChooseAvatar item in uIChooseAvatars)
            if (item != null) item.SetSelected(item.idAvatar == pendingAvatar);
    }

    // Nút Save: lưu tên (không rỗng) và avatar, cập nhật avatar trang chủ rồi về bảng xem.
    public void OnSave()
    {
        var profile = ProfileDataFragment.cur;
        if (profile != null)
        {
            string name = nameInput != null ? nameInput.text.Trim() : "";
            if (name.Length > 0) profile.gameData.userName = name;
            profile.gameData.idAvatar = pendingAvatar;
            profile.gameData.idChoose = pendingAvatar;
            profile.gameData.idFrame = 0;
            profile.Save();
        }
        if (ProfileHome.cur != null) ProfileHome.cur.UpdateAvatar();
        SetShowEdit(false);
    }

    // Nút bút chì cạnh ô tên: đưa con trỏ vào ô nhập.
    public void FocusName()
    {
        if (nameInput == null) return;
        nameInput.Select();
        nameInput.ActivateInputField();
    }

    // Nút copy cạnh ID.
    public void CopyId() => GUIUtility.systemCopyBuffer = PlayerId;

    // Nút Google Play Games (chưa tích hợp GPGS: chỉ đổi trạng thái hiển thị).
    public void OnGoogleButton()
    {
        Debug.Log("[Profile] Chưa tích hợp Google Play Games.", this);
        RefreshGoogle();
    }

    private void RefreshGoogle()
    {
        if (googleButton != null && googleLinkButton != null)
            googleButton.sprite = googleLinked ? googleLinkedButton : googleLinkButton;
        if (googleLabel != null && googleLinkLabel != null)
        {
            googleLabel.sprite = googleLinked ? googleLinkedLabel : googleLinkLabel;
            googleLabel.SetNativeSize();
        }
    }

    // Nút X: ở bảng sửa thì quay về bảng xem, ở bảng xem thì đóng popup.
    public void OnClose()
    {
        if (editPanelObject != null && editPanelObject.activeSelf) SetShowEdit(false);
        else base.CloseDirectly();
    }

    public void OnOpenEditProfile() => SetShowEdit(true);
    public void OnCloseProfile() => SetShowEdit(false);
    public override void BackKey() => OnClose();

    private static Sprite AvatarSprite(int index)
    {
        var profile = ProfileDataFragment.cur;
        if (profile == null || profile.spr_avatars == null || profile.spr_avatars.Length == 0) return null;
        return profile.spr_avatars[Mathf.Clamp(index, 0, profile.spr_avatars.Length - 1)];
    }

    // ID ngắn cố định theo thiết bị (9 ký tự).
    private static string PlayerId
    {
        get
        {
            const string chars = "0123456789abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
            unchecked
            {
                ulong hash = 1469598103934665603UL;
                foreach (char c in SystemInfo.deviceUniqueIdentifier) hash = (hash ^ c) * 1099511628211UL;
                var id = new char[9];
                for (int i = 0; i < id.Length; i++) { id[i] = chars[(int)(hash % (ulong)chars.Length)]; hash /= (ulong)chars.Length; if (hash == 0) hash = 7919UL * (ulong)(i + 3); }
                return new string(id);
            }
        }
    }
}
