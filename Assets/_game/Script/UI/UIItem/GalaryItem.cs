using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.UI;

public class GalaryItem : MonoBehaviour
{
    public Image imgPreview;
    public GameObject lockObj;
    public GameObject obMain;

    // Gán ảnh xem trước và chọn trạng thái mở khóa theo việc level đã hoàn thành.
    public void Setup(int levelId, Sprite preview)
    {
        bool unlocked = GameDataFragment.cur.dataPlayer.completedLevels.Contains(levelId);
        obMain.SetActive(unlocked);
        imgPreview.sprite = preview;
        lockObj.SetActive(!unlocked);

    }

    // Kiểm tra level có trong danh sách màn người chơi đã hoàn thành.
    public bool IsLevelCompleted(int level)
    {
        return GameDataFragment.cur.dataPlayer.completedLevels.Contains(level);
    }
}
