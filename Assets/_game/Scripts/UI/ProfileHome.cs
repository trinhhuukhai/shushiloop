using UnityEngine;
using UnityEngine.UI;

public class ProfileHome : MonoBehaviour
{
    public static ProfileHome cur;
    public Image img_avatar;
    public Image img_frame;

    private void Awake()
    {
        cur = this;
    }

    private void Start()
    {
        UpdateAvatar();
    }

    public void Btn_OpenPopupProfile()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        //FirebaseManager.Ins.button_click("BUTTON_PROFILE");
        //SoundManager.PlayEfx(SoundID.sfx_click);

        UIManager.Ins.OpenUI<CanvasProfile>();
    }

    public void UpdateAvatar()
    {
        img_avatar.sprite = ProfileDataFragment.cur.spr_avatars[ProfileDataFragment.cur.gameData.idAvatar];
        img_frame.sprite = ProfileDataFragment.cur.spr_frames[ProfileDataFragment.cur.gameData.idFrame];
        img_frame.SetNativeSize();
        img_avatar.SetNativeSize();
    }
}