using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DG.Tweening;

public enum SettingType { Music, Sound, Vibrate }

[RequireComponent(typeof(Image))]
public class ToggleButton : MonoBehaviour
{
    [Header("Configure in Inspector")]
    [Tooltip("Chọn biến trong DataManager mà toggle này sẽ điều khiển")]
    [SerializeField] private SettingType settingType;

    [Header("UI References")]
    [SerializeField] private GameObject onGroup;    // chứa ImageOn + TextOn
    [SerializeField] private Image imageOn;    // Filled Image

    [SerializeField] private RectTransform handle;  // tay gạt
    [SerializeField] private Vector2 handleOffPos;
    [SerializeField] private Vector2 handleOnPos;
    [SerializeField] private float tweenDur = 0.3f;

    [SerializeField] private GameObject imageOnGreen;    // Filled Image
    [SerializeField] private GameObject ImageOff;
    [Header("Events (optional)")]
    public UnityEvent OnTurnOn;
    public UnityEvent OnTurnOff;

    private bool IsOn
    {
        get => ReadData();
        set
        {
            // 1) Update UI
            SetState(value);
            // 2) Viết ngược lại DataManager
            WriteData(value);
        }
    }

    private void Start()
    {
        // Khi khởi đầu, đồng bộ UI với data
        //bool startValue = ReadData();
        //SetState(startValue);
        ImmediatelySetUI(ReadData());
    }

    public void OnPointerClick()
    {
        // Đảo trạng thái
        IsOn = !IsOn;
        //SoundManager.PlayEfx(SoundID.sfx_ui_button_click);
        //SoundManager.PlayEfx(SoundID.sfx_click);

        // Chỉ rung khi người chơi vừa BẬT vibration
        if (settingType == SettingType.Vibrate && IsOn)
        {
            //MMVibrationManager.Haptic(HapticTypes.LightImpact);
        }

    }

    /// <summary>
    /// Ngay lập tức gán UI (không animation) từ data
    /// </summary>
    private void ImmediatelySetUI(bool on)
    {
        // handle
        handle.anchoredPosition = on ? handleOnPos : handleOffPos;
        // onGroup + fill
        onGroup.SetActive(on);
        //imageOn.fillAmount = on ? 1f : 0f;
    }

    /// <summary>
    /// Chạy animation bật/tắt
    /// </summary>
    private void SetState(bool on)
    {
        imageOn.DOKill();
        handle.DOKill();

        if (on)
        {
            onGroup.SetActive(true);
            imageOn.DOFillAmount(1f, tweenDur).SetUpdate(true);
            handle.DOAnchorPos(handleOnPos, tweenDur).SetUpdate(true);

            imageOnGreen.SetActive(true);
            ImageOff.SetActive(false);
            OnTurnOn?.Invoke();
        }
        else
        {
            imageOn.DOFillAmount(0f, tweenDur)
                .SetUpdate(true)
                .OnComplete(() => onGroup.SetActive(false));

            handle.DOAnchorPos(handleOffPos, tweenDur).SetUpdate(true);

            imageOnGreen.SetActive(false);
            ImageOff.SetActive(true);
            OnTurnOff?.Invoke();
        }
    }

    /// <summary>
    /// Đọc giá trị hiện tại từ DataManager
    /// </summary>
    private bool ReadData()
    {
        switch (settingType)
        {
            case SettingType.Music: return DataManager.ins.setupData.isMusic;
            case SettingType.Sound: return DataManager.ins.setupData.isSound;
            case SettingType.Vibrate: return DataManager.ins.setupData.isVibrate;
            default: return false;
        }
    }

    /// <summary>
    /// Ghi giá trị mới vào DataManager (và lưu nếu cần)
    /// </summary>
    private void WriteData(bool on)
    {
        switch (settingType)
        {
            case SettingType.Music:
                DataManager.ins.setupData.isMusic = on;
                //SoundManager.SetupMusicBg(on);
                break;
            case SettingType.Sound:
                DataManager.ins.setupData.isSound = on;
                //SoundManager.SetupSound(on);
                break;
            case SettingType.Vibrate:
                DataManager.ins.setupData.isVibrate = on;
                break;
        }
        DataManager.ins.SaveData(); // giả định bạn có hàm này
    }
}
