using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasFirstOpenProfile : UICanvas
{
    public InputField inputField;
    public RectTransform mainPanelRect;
    private Tween popTween;

    // Mở popup đặt tên lần đầu với hiệu ứng và điền tên hiện tại.
    public override void Open()
    {
        base.Open();
        popTween?.Kill();
        mainPanelRect.localScale = Vector3.zero;
        popTween = mainPanelRect.DOScale(Vector3.one, .24f).SetUpdate(true).SetEase(Ease.OutSine);
        inputField.text = ProfileDataFragment.cur.gameData.userName;
    }

    // Ghi tên nhập không rỗng vào hồ sơ hoặc phục hồi tên cũ nếu để trống.
    public void OnEndEdit()
    {
        if (inputField.text.Length > 0)
        {
            ProfileDataFragment.cur.gameData.userName = inputField.text;
        }
        else
        {
            inputField.text = ProfileDataFragment.cur.gameData.userName;
        }
    }

    // Mở popup hồ sơ chính rồi đóng popup đặt tên lần đầu.
    public void OnClickSave()
    {
        //AudioManager.ins.PlaySound(SoundType.UIClick);
        UIManager.Ins.OpenUI<CanvasProfile>();
        base.CloseDirectly();
    }

    // Cắt nội dung còn 10 ký tự khi độ dài tên nhập vượt quá 14 ký tự.
    public void OnValueChange()
    {
        if (inputField.text.Length > 14)
        {
            inputField.text = inputField.text.Substring(0, 10);
        }
    }
}