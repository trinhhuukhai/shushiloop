using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Customer))]
public class RushCustomerView : MonoBehaviour
{
    [SerializeField] private AudioClip tickingClip;
    [SerializeField] private GameObject indicator;
    [SerializeField] private TMP_Text timer;
    [SerializeField] private Image countdown;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color warningColor;
    [SerializeField] private Color urgentColor;
    [SerializeField] private Sprite normalFill;
    [SerializeField] private Sprite warningFill;
    [SerializeField] private Sprite urgentFill;
    private Customer customer;
    private float nextTick;

    private void Awake()
    {
        customer = GetComponent<Customer>();
        indicator.SetActive(false);
    }

    private void Update()
    {
        bool visible = customer.IsSeated && customer.IsRush;
        indicator.SetActive(visible);
        if (!visible) return;
        timer.transform.parent.gameObject.SetActive(customer.IsRush && !customer.IsCompleted);
        float fraction = Mathf.Clamp01(customer.RushRemaining / Mathf.Max(0.01f, customer.RushDuration));
        Color color = fraction < 0.2f ? urgentColor : fraction < 0.5f ? warningColor : normalColor;
        timer.color = countdown.color = color;
        Sprite fillSprite = fraction < 0.2f ? urgentFill : fraction < 0.5f ? warningFill : normalFill;
        if (fillSprite != null) { countdown.sprite = fillSprite; countdown.color = Color.white; }
        countdown.fillAmount = fraction;
        int seconds = Mathf.CeilToInt(customer.RushRemaining);
        timer.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        bool warning = fraction < 0.2f && !customer.RushExpired &&
            (LevelManager.Ins == null || !LevelManager.Ins.IsPopupOpen);
        if (!warning || Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + 0.75f;
        if (tickingClip == null) tickingClip = CreateTick();
        audioSource.PlayOneShot(tickingClip, 0.15f);
    }

    private static AudioClip CreateTick()
    {
        const int sampleRate = 22050;
        var samples = new float[1102];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = Mathf.Sin(2 * Mathf.PI * 1000 * i / sampleRate) * (1f - (float)i / samples.Length);
        AudioClip clip = AudioClip.Create("Rush tick", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
