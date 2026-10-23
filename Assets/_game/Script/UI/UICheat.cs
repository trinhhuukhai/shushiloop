using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UICheat : MonoBehaviour
{
    public static UICheat Ins;
    public GameObject canvasCheat;
    public UIWatch clock;
    public CanvasGroup canvasGroup;

    [Header("Inputs")] public TMP_InputField inputLevel; // ô nhập LevelIndex (0-based)
    public TMP_InputField inputMoney; // ô nhập số tiền muốn set tuyệt đối
    public TMP_InputField inputCrystal; // ô nhập số tiền muốn set tuyệt đối

    public TMP_InputField inputLvAddUnit;
    public TMP_InputField inputLvDamage;
    public TMP_InputField inputLvFirerate;
    public TMP_InputField inputEnergy;
    public TMP_InputField inputKey;
    public TMP_InputField inputbankPass;
    public TMP_InputField inputCampName;

    public Toggle toggleCheat;

    private void Awake()
    {
        Ins = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OpenCheat()
    {
        canvasCheat.SetActive(true);
        canvasGroup.alpha = 1.0f;

        RefreshInputsFromData();

        if (UIManager.Ins != null)
        {
            UIManager.Ins.GetComponent<CanvasGroup>().alpha = 1f;
        }

        if (toggleCheat != null && GameManager.ins != null)
            toggleCheat.isOn = GameManager.ins.isCheat;
    }

    public void OnToggleCheatChanged()
    {
        bool check = toggleCheat.isOn;
        if (GameManager.ins == null) return;
        GameManager.ins.isCheat = check;
        //if (check) AdsManager.Ins.HideBanner();
        //else AdsManager.Ins.ShowBanner();

        // nếu muốn lưu luôn:
        // PlayerPrefs.SetInt("isCheat", isOn ? 1 : 0);
        // PlayerPrefs.Save();
    }

    private void RefreshInputsFromData()
    {
        if (GameDataFragment.cur == null) return;

        var dp = GameDataFragment.cur.dataPlayer;

        // Level: hiển thị 1-based cho dễ nhìn
        if (inputLevel != null)
            inputLevel.text = (dp.cur_level + 1).ToString();

    }

    public void ApplyUpgradeLevels()
    {
        if (GameDataFragment.cur == null) return;
        DataManager.ins.SaveData();
        ToLevel();
        // Refresh lại text để khớp với giá trị đã clamp (nếu có)
        RefreshInputsFromData();
    }

    public void CloseCheat()
    {
        canvasCheat.SetActive(false);
    }

    public void HideUI()
    {
        if (UIManager.Ins != null)
        {
            UIManager.Ins.GetComponent<CanvasGroup>().alpha = 0f;
        }

        canvasCheat.SetActive(false);
        canvasGroup.alpha = 0f;
        //MaxManager.Ins.HideBanner();
    }

    public void Win()
    {
        //LevelManager.Ins.OnWin();
        CloseCheat();
    }

    public void PauseTimer()
    {
        clock.PauseTimer();
    }

    public void ResetTimer()
    {
        clock.ResetTimer();
    }

    public void StartTimer()
    {
        clock.StartTimer();
    }

    public void Lose()
    {
        //LevelManager.Ins.OnLose();
        CloseCheat();
    }

    public void CloseProcessBar()
    {
        canvasCheat.SetActive(false);
        canvasGroup.alpha = 0f;
        //MaxManager.Ins.HideBanner();
    }

    private bool check = false;

    public void HideUIVer2()
    {
        check = !check; 

        canvasCheat.SetActive(false);
        canvasGroup.alpha = 0f;
        UIManager.Ins.GetUI<CanvasFloating>().HideUIFloat(check);
    }

    // ================== CHEAT CORE ======================
    // Nút “Apply (Level + Money) & Reload Main Menu”
    public void ApplyAndReload()
    {
        ApplyLevelFromInput();
        ApplyMoneyFromInput();
        // Gọi OnHome để LevelManager tự LoadData() + Update UI
        ToLevel();
        //LevelManager.Ins.OnHome();
        RefreshInputsFromData();
        CloseCheat();
    }

    // Nút “Chỉ Apply Level & Reload” (nếu bạn muốn tách nút)
    public void ApplyLevelOnly()
    {
        ApplyLevelFromInput();
        ToLevel();
        //LevelManager.Ins.OnHome();
        //CloseCheat();
    }

    public void ToLevel()
    {
        //LevelManager.Ins.OnHome();
        DelayRunner.Delay(0.5f, () =>
        {
            DelayRunner.Delay(0.7f, () =>
            {
                StartCoroutine(ieBackHome());

                IEnumerator ieBackHome()
                {
                    var sync = SceneManager.LoadSceneAsync("GamePlay");
                    yield return new WaitUntil(() => sync.isDone);
                }

                UIManager.Ins.GetUI<CanvasGamePlay>().Close();
                //SceneManager.LoadScene("GamePlay");
                //Debug.LogError(GameDataFragment.cur.dataPlayer.cur_level + "===========");
                //if (UIManager.Ins.IsOpened<CanvasShop>()) UIManager.Ins.CloseUI<CanvasShop>();
                if(UIManager.Ins.IsOpened<CanvasFloating>())
                {
                }
            });
        });
    }

    // Nút “Chỉ Apply Money” (không reload màn, chỉ cập nhật UI tiền)
    public void ApplyMoneyOnly()
    {
        ApplyMoneyFromInput();
        // Không bắt buộc OnHome; UI tiền đã tự cập nhật qua Gold.UpdateCurrency
    }

    // ------- Helpers -------
    private void ApplyLevelFromInput()
    {
        if (GameDataFragment.cur == null) return;

        int targetLv = 0;
        if (inputLevel != null && int.TryParse(inputLevel.text, out targetLv))
        {
            //targetLv = ClampLevelIndex(targetLv);
            GameDataFragment.cur.dataPlayer.cur_level = (targetLv - 1);
            DataManager.ins.SaveData();
            //RefreshInputsFromData();
            //Debug.Log(GameDataFragment.cur.dataPlayer.cur_level + "------");
        }
    }

  
    private void ApplyKeyFromInput()
    {

        //energy
       

        int newMoney = 0;
        if (inputKey != null && int.TryParse(inputKey.text, out newMoney))
        {
            //Debug.LogError(newMoney + "------------");
            if (newMoney < 0) newMoney = 0;
            //BattlePassManager.Ins.AddKey(newMoney);
            //UIManager.Ins.GetUI<CanvasFloating>().UpdateCurrentEnergy();
        }
    }

    private void ApplyMoneyFromInput()
    {
        if (GameDataFragment.cur == null) return;
        //Debug.LogError("-=====");
        int newMoney = 0;
        if (inputMoney != null && int.TryParse(inputMoney.text, out newMoney))
        {
        }
    }
}