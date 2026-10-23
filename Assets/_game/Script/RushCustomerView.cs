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
    private Customer customer;
    private float nextTick;

    private void Awake()
    {
        customer = GetComponent<Customer>();
        indicator.SetActive(false);
    }

    private void Update()
    {
        bool visible = customer.IsRush && customer.IsSeated && !customer.IsCompleted;
        indicator.SetActive(visible);
        if (!visible) return;
        float fraction = Mathf.Clamp01(customer.RushRemaining / Mathf.Max(0.01f, customer.RushDuration));
        Color color = fraction < 0.2f ? urgentColor : fraction < 0.5f ? warningColor : normalColor;
        timer.color = countdown.color = color;
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
