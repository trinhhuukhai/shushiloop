using UnityEngine;
using UnityEngine.UI;

// Avatar ở header trang chủ: hiện avatar đang chọn, bấm để mở popup hồ sơ.
public class ProfileHome : MonoBehaviour
{
    public static ProfileHome cur;
    public Image img_avatar;
    public Image img_frame;

    private void Awake() => cur = this;

    private void Start() => UpdateAvatar();

    public void Btn_OpenPopupProfile() => UIManager.Ins.OpenUI<CanvasProfile>();

    // Gán avatar đang chọn; giữ kích thước khung đã dựng trong prefab (không SetNativeSize).
    public void UpdateAvatar()
    {
        var profile = ProfileDataFragment.cur;
        if (profile == null || profile.spr_avatars == null || profile.spr_avatars.Length == 0) return;
        if (img_avatar != null)
        {
            img_avatar.sprite = profile.spr_avatars[Mathf.Clamp(profile.gameData.idAvatar, 0, profile.spr_avatars.Length - 1)];
            img_avatar.preserveAspect = true;
        }
        if (img_frame != null && profile.spr_frames != null && profile.spr_frames.Length > 0)
            img_frame.sprite = profile.spr_frames[Mathf.Clamp(profile.gameData.idFrame, 0, profile.spr_frames.Length - 1)];
    }
}
