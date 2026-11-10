using UnityEngine;
using UnityEngine.UI;

// Một ô chọn avatar trong bảng sửa hồ sơ: khung xám khi chưa chọn, khung xanh + dấu tick khi đang chọn.
public class UIChooseAvatar : MonoBehaviour
{
    public int id;
    public Image img_avatar;
    public Image img_frame;
    public Image img_selecting;
    public Sprite idleFrame;
    public Sprite selectedFrame;

    public int idAvatar;
    public int idFrame;

    // Gán sprite avatar theo idAvatar (giữ nguyên kích thước ô đã dựng sẵn).
    public void SetUp()
    {
        var profile = ProfileDataFragment.cur;
        if (profile != null && profile.spr_avatars != null && profile.spr_avatars.Length > 0 && img_avatar != null)
            img_avatar.sprite = profile.spr_avatars[Mathf.Clamp(idAvatar, 0, profile.spr_avatars.Length - 1)];
    }

    // Đổi khung và dấu tick theo trạng thái chọn.
    public void SetSelected(bool selected)
    {
        if (img_frame != null && idleFrame != null && selectedFrame != null) img_frame.sprite = selected ? selectedFrame : idleFrame;
        if (img_selecting != null) img_selecting.gameObject.SetActive(selected);
    }

    // Bấm ô: chọn avatar này (lưu khi bấm Save).
    public void Btn_clickChooseThis()
    {
        if (CanvasProfile.cur != null) CanvasProfile.cur.ChooseAvatar(idAvatar);
    }
}
