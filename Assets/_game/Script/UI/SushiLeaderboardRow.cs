using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Một dòng trong bảng xếp hạng: hạng (số hoặc huy chương), avatar, tên và điểm.
public sealed class SushiLeaderboardRow : MonoBehaviour
{
    public TMP_Text rankText;
    public Image medal;
    public Image avatar;
    public TMP_Text nameText;
    public TMP_Text scoreText;

    // Hiển thị dữ liệu dòng; top 3 dùng huy chương thay cho số hạng nếu có ảnh.
    public void Show(string rankLabel, Sprite medalSprite, Sprite avatarSprite, string playerName, int score)
    {
        bool hasMedal = medal != null && medalSprite != null;
        if (medal != null)
        {
            medal.gameObject.SetActive(hasMedal);
            if (hasMedal) medal.sprite = medalSprite;
        }
        if (rankText != null)
        {
            rankText.gameObject.SetActive(!hasMedal);
            rankText.text = rankLabel;
        }
        if (avatar != null && avatarSprite != null) avatar.sprite = avatarSprite;
        if (nameText != null) nameText.text = playerName;
        if (scoreText != null) scoreText.text = "Score\n" + score;
    }
}
