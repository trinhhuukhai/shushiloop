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
    public void OnSelectLanguage()
    {
        //LocalizationManager.CurrentLanguageCode = LanguageCode.ToString();

        //ExtensionPrime.SetLanguage(langCode);
        //SoundManager.PlayEfx(SoundID.sfx_ui_button_click);
        //UIManager.Ins.GetUI<CanvasSetting>().ChoosenLanguage();
    }
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
