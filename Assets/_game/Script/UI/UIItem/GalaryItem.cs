using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.UI;

public class GalaryItem : MonoBehaviour
{
    public Image imgPreview;
    public GameObject lockObj;
    public GameObject obMain;

    public void Setup(int levelId, Sprite preview)
    {
        bool unlocked = GameDataFragment.cur.dataPlayer.completedLevels.Contains(levelId);
        obMain.SetActive(unlocked);
        imgPreview.sprite = preview;
        lockObj.SetActive(!unlocked);

    }

    public bool IsLevelCompleted(int level)
    {
        return GameDataFragment.cur.dataPlayer.completedLevels.Contains(level);
    }
}
