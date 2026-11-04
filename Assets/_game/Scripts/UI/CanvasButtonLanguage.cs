using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CanvasButtonLanguage : MonoBehaviour
{
    public LanguageCode LanguageCode;
    public List<GameObject> imagebutton;
    public TMP_Text textt1;
    public TMP_Text textt2;

    // Đổi hình và nhãn sang trạng thái được chọn rồi gọi điểm xử lý chọn ngôn ngữ.
    public void OnClick()
    {
        if (imagebutton != null)
        {
            textt2.gameObject.SetActive(true);
            textt1.gameObject.SetActive(false);
            imagebutton[1].SetActive(true);
            imagebutton[0].SetActive(false);
        }
        OnSelectLanguage();
    }
    // Điểm áp dụng ngôn ngữ được chọn; hiện chưa có xử lý đang hoạt động.
    public void OnSelectLanguage()
    {
        //LocalizationManager.CurrentLanguageCode = LanguageCode.ToString();

        //ExtensionPrime.SetLanguage(langCode);
        //SoundManager.PlayEfx(SoundID.sfx_ui_button_click);
        //UIManager.Ins.GetUI<CanvasSetting>().ChoosenLanguage();
    }
    // Đồng bộ hai bộ hình và nhãn của nút ngôn ngữ theo trạng thái yêu cầu.
    public void SetActiveImage(bool ck)
    {
        if (imagebutton != null)
        {
            imagebutton[1].SetActive(!ck);
            imagebutton[0].SetActive(ck);
            textt2.gameObject.SetActive(!ck);
            textt1.gameObject.SetActive(ck);
        }
    }
}
