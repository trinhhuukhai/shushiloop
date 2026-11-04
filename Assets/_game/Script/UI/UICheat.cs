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

    public TMP_InputField inputBsClick;
    public TMP_InputField inputShuffle;
    public TMP_InputField inputMagnet;

    public Toggle toggleCheat;

    // Lưu singleton UI cheat và giữ đối tượng khi đổi scene.
    private void Awake()
    {
        Ins = this;
        DontDestroyOnLoad(gameObject);
    }

    // Hiện bảng cheat, nạp thông tin đầu vào và đồng bộ công tắc bật cheat.
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

    // Ghi giá trị công tắc cheat vào GameManager.
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

    // Điền level hiện tại theo cách đánh số từ 1 vào ô nhập cheat.
    private void RefreshInputsFromData()
    {
        if (GameDataFragment.cur == null) return;

        var dp = GameDataFragment.cur.dataPlayer;

        // Level: hiển thị 1-based cho dễ nhìn
        if (inputLevel != null)
            inputLevel.text = (dp.cur_level + 1).ToString();

    }

    // Lưu dữ liệu, yêu cầu nạp lại cảnh chơi và cập nhật ô nhập.
    public void ApplyUpgradeLevels()
    {
        if (GameDataFragment.cur == null) return;
        DataManager.ins.SaveData();
        ToLevel();
        // Refresh lại text để khớp với giá trị đã clamp (nếu có)
        RefreshInputsFromData();
    }

    // Ẩn bảng điều khiển cheat.
    public void CloseCheat()
    {
        canvasCheat.SetActive(false);
    }

    // Ẩn UIManager cùng bảng cheat bằng trạng thái hiển thị và alpha.
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

    // Buộc màn hiện tại thắng qua LevelManager rồi đóng bảng cheat.
    public void Win()
    {
        LevelManager.Ins.OnWIn();
        CloseCheat();
    }

    // Tạm dừng đồng hồ đếm thời gian của UI cheat.
    public void PauseTimer()
    {
        clock.PauseTimer();
    }

    // Đặt lại đồng hồ đếm thời gian của UI cheat.
    public void ResetTimer()
    {
        clock.ResetTimer();
    }

    // Cho đồng hồ UI cheat tiếp tục đếm.
    public void StartTimer()
    {
        clock.StartTimer();
    }

    // Buộc màn hiện tại thua qua LevelManager rồi đóng bảng cheat.
    public void Lose()
    {
        LevelManager.Ins.LoseLevel();
        CloseCheat();
    }

    // Ẩn bảng cheat và làm trong suốt nhóm điều khiển cheat.
    public void CloseProcessBar()
    {
        canvasCheat.SetActive(false);
        canvasGroup.alpha = 0f;
        //MaxManager.Ins.HideBanner()
    }

    private bool check = false;

    // Đảo trạng thái ẩn UI nổi và đóng lớp điều khiển cheat.
    public void HideUIVer2()
    {
        check = !check; 

        canvasCheat.SetActive(false);
        canvasGroup.alpha = 0f;
        UIManager.Ins.GetUI<CanvasFloating>().HideUIFloat(check);
    }

    // ================== CHEAT CORE ======================
    // Nút “Apply (Level + Money) & Reload Main Menu”
    // Áp dụng các ô nhập level, tiền và booster rồi đồng bộ ô nhập và đóng cheat.
    public void ApplyAndReload()
    {
        ApplyLevelFromInput();
        ApplyMoneyFromInput();
        ApplyBoosterClickFromInput();
        ApplyBoosterShuffleFromInput();
        ApplyBoosterMagnetFromInput();
        // Gọi OnHome để LevelManager tự LoadData() + Update UI
        // ToLevel();
        //LevelManager.Ins.OnHome();
        RefreshInputsFromData();
        CloseCheat();
    }

    // Nút “Chỉ Apply Level & Reload” (nếu bạn muốn tách nút)
    // Áp dụng level từ ô nhập rồi yêu cầu nạp lại cảnh gameplay.
    public void ApplyLevelOnly()
    {
        ApplyLevelFromInput();
        ToLevel();
        //LevelManager.Ins.OnHome();
        //CloseCheat();
    }

    // Trì hoãn hai nhịp trước khi bắt đầu coroutine nạp lại cảnh GamePlay.
    public void ToLevel()
    {
        //LevelManager.Ins.OnHome();
        DelayRunner.Delay(0.5f, () =>
        {
            DelayRunner.Delay(0.7f, () =>
            {
                StartCoroutine(ieBackHome());

                // Nạp bất đồng bộ cảnh GamePlay và chờ thao tác hoàn tất.
                IEnumerator ieBackHome()
                {
                    var sync = SceneManager.LoadSceneAsync("GamePlay");
                    yield return new WaitUntil(() => sync.isDone);
                }
            });
        });
    }

    // Nút “Chỉ Apply Money” (không reload màn, chỉ cập nhật UI tiền)
    // Áp dụng lượng thay đổi tiền từ ô nhập cheat.
    public void ApplyMoneyOnly()
    {
        ApplyMoneyFromInput();
        // Không bắt buộc OnHome; UI tiền đã tự cập nhật qua Gold.UpdateCurrency
    }

    // ------- Helpers -------
    // Đọc level dạng số từ 1, đổi về chỉ số từ 0 và lưu dữ liệu người chơi.
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


    // Đọc số từ ô nhập và cộng lượng đó vào ví qua hệ thống dữ liệu.
    private void ApplyMoneyFromInput()
    {
        if (GameDataFragment.cur == null) return;
        //Debug.LogError("-=====");
        int newMoney = 0;
        if (inputMoney != null && int.TryParse(inputMoney.text, out newMoney))
        {
            GameDataFragment.cur.ChangeMoney(newMoney,"Cheat");
        }
    }

    // Đọc lượng thay đổi và cộng vào số booster đũa.
    private void ApplyBoosterClickFromInput()
    {
        if (GameDataFragment.cur == null) return;
        //Debug.LogError("-=====");
        int newMoney = 0;
        if (inputBsClick != null && int.TryParse(inputBsClick.text, out newMoney))
        {
            GameDataFragment.cur.ChangeBoosterCount(BoosterType.Click,newMoney, "Cheat");
        }
    }

    // Đọc lượng thay đổi và cộng vào số booster hoán đổi.
    private void ApplyBoosterShuffleFromInput()
    {
        if (GameDataFragment.cur == null) return;
        //Debug.LogError("-=====");
        int newMoney = 0;
        if (inputShuffle != null && int.TryParse(inputShuffle.text, out newMoney))
        {
            GameDataFragment.cur.ChangeBoosterCount(BoosterType.Shuffle, newMoney, "Cheat");
        }
    }

    // Đọc lượng thay đổi và cộng vào số booster nam châm.
    private void ApplyBoosterMagnetFromInput()
    {
        if (GameDataFragment.cur == null) return;
        //Debug.LogError("-=====");
        int newMoney = 0;
        if (inputMagnet != null && int.TryParse(inputMagnet.text, out newMoney))
        {
            GameDataFragment.cur.ChangeBoosterCount(BoosterType.Magnet, newMoney, "Cheat");
        }
    }

}