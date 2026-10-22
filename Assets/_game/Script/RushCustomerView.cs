using TMPro;
using UnityEngine;

[RequireComponent(typeof(Customer))]
public class RushCustomerView : MonoBehaviour
{
    [SerializeField] private AudioClip tickingClip;
    private Customer customer;
    private TextMeshPro timer;
    private LineRenderer border;
    private SpriteRenderer timerBackground;
    private AudioSource audioSource;
    private float nextTick;
    private readonly Vector3 timerPosition = new Vector3(1.05f, 0.65f, -0.4f);

    private void Awake()
    {
        customer = GetComponent<Customer>();
        timer = SushiWorldVisual.Label(transform, "Rush timer", timerPosition, 3.2f, 25);
        timer.rectTransform.sizeDelta = new Vector2(1, 0.4f);
        timerBackground = SushiWorldVisual.Panel(transform, "Timer badge", 23);
        timerBackground.transform.localPosition = timerPosition;
        timerBackground.transform.localScale = new Vector3(1.03f, 0.4f, 1);
        timerBackground.color = new Color(0.2f, 0.14f, 0.1f);
        border = new GameObject("Order countdown border").AddComponent<LineRenderer>();
        border.transform.SetParent(transform, false);
        border.material = SushiWorldVisual.LineMaterial;
        border.useWorldSpace = false;
        border.startWidth = border.endWidth = 0.05f;
        border.sortingOrder = 22;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0;
        timer.gameObject.SetActive(false);
        timerBackground.gameObject.SetActive(false);
        border.gameObject.SetActive(false);
    }

    private void Update()
    {
        bool visible = customer.IsRush && customer.IsSeated && !customer.IsCompleted;
        timer.gameObject.SetActive(visible);
        border.gameObject.SetActive(visible);
        timerBackground.gameObject.SetActive(visible);
        if (!visible) return;
        float fraction = Mathf.Clamp01(customer.RushRemaining / Mathf.Max(0.01f, customer.RushDuration));
        Color color = fraction < 0.2f ? Color.red : fraction < 0.5f ? Color.yellow : Color.green;
        timer.color = color;
        int seconds = Mathf.CeilToInt(customer.RushRemaining);
        timer.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        bool warning = fraction < 0.2f && !customer.RushExpired &&
            (LevelManager.Ins == null || !LevelManager.Ins.IsPopupOpen);
        timer.transform.localPosition = timerPosition + (warning ? Vector3.right * Mathf.Sin(Time.unscaledTime * 35) * 0.025f : Vector3.zero);
        border.startColor = border.endColor = color;
        // Đi dọc viền thẻ order, chỉ giữ phần tương ứng thời gian còn lại.
        int dishCount = customer.Dishes.Count;
        Rect bounds = dishCount > 0 ? CustomerRecipeView.OrderBounds(dishCount) : new Rect(-0.85f, -0.7f, 1.7f, 1.35f);
        float halfWidth = bounds.width / 2;
        float top = bounds.yMax;
        float bottom = bounds.yMin;
        Vector3[] corners = { new Vector3(-halfWidth, top, -0.1f), new Vector3(halfWidth, top, -0.1f),
            new Vector3(halfWidth, bottom, -0.1f), new Vector3(-halfWidth, bottom, -0.1f), new Vector3(-halfWidth, top, -0.1f) };
        float remaining = fraction * (4 * halfWidth + 2 * (top - bottom));
        var points = new System.Collections.Generic.List<Vector3> { corners[0] };
        for (int i = 0; i < 4 && remaining > 0; i++)
        {
            float length = Vector3.Distance(corners[i], corners[i + 1]);
            points.Add(Vector3.Lerp(corners[i], corners[i + 1], Mathf.Clamp01(remaining / length)));
            remaining -= length;
        }
        border.positionCount = points.Count;
        border.SetPositions(points.ToArray());
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
