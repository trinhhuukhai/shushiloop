using UnityEngine;
using UnityEngine.UI;

public class ProfileHome : MonoBehaviour
{
    public static ProfileHome cur;
    public Image img_avatar;
    public Image img_frame;

    // Lưu tham chiếu thành phần hồ sơ trang chủ hiện tại.
    private void Awake()
    {
        cur = this;
    }

    // Cập nhật avatar và khung hồ sơ khi bắt đầu.
    private void Start()
    {
        UpdateAvatar();
    }

    // Mở popup hồ sơ khi người chơi nhấn avatar ở trang chủ.
    public void Btn_OpenPopupProfile()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //FirebaseManager.Ins.button_click("BUTTON_PROFILE");
        //SoundManager.PlayEfx(SoundID.sfx_click);

        UIManager.Ins.OpenUI<CanvasProfile>();
    }

    // Gán avatar và khung đang chọn rồi đặt kích thước ảnh theo sprite gốc.
    public void UpdateAvatar()
    {
        img_avatar.sprite = ProfileDataFragment.cur.spr_avatars[ProfileDataFragment.cur.gameData.idAvatar];
        img_frame.sprite = ProfileDataFragment.cur.spr_frames[ProfileDataFragment.cur.gameData.idFrame];
        img_frame.SetNativeSize();
        img_avatar.SetNativeSize();
    }
}