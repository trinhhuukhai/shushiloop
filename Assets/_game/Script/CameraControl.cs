using UnityEngine;

// The game stays face-on: fit the complete portrait playfield inside the safe area.
// Dải header của HUD (UI, co giãn theo CanvasScaler) được chừa riêng ở mép trên vùng an toàn,
// phần chơi (từ đầu khách tới đáy) xếp vào vùng còn lại nên header không bao giờ đè lên khách/order,
// kể cả trên máy có tai thỏ.
[ExecuteAlways, RequireComponent(typeof(Camera))]
public class CameraControl : SingletonMonoBehaviour<CameraControl>
{
    public Camera cam;
    [SerializeField] private Vector2 playfieldSize = new Vector2(9f, 16f);
    [SerializeField] private Vector2 playfieldCenter = Vector2.zero;

    [Header("Chừa chỗ cho header HUD")]
    [Tooltip("Chiều cao dải header (đơn vị canvas của HUD) tính từ mép trên vùng an toàn. 0 = không chừa.")]
    [SerializeField, Min(0f)] private float reservedTopCanvas = 150f;
    [Tooltip("Mép trên phần chơi (thế giới): đỉnh bảng order của khách. Điểm này nằm ngay dưới header.")]
    [SerializeField] private float contentTop = 8.05f;
    [Tooltip("Cấu hình CanvasScaler của HUD (UICGamePlay) để đổi đơn vị canvas ra pixel.")]
    [SerializeField] private Vector2 canvasReference = new Vector2(1080f, 1920f);
    [SerializeField, Range(0f, 1f)] private float canvasMatch = 0.5f;

    // Lấy camera gắn trên đối tượng và căn vùng chơi ngay khi được bật.
    private void OnEnable() { cam = GetComponent<Camera>(); Fit(); }
    // Căn lại camera sau mỗi frame để thích ứng với kích thước và vùng an toàn màn hình.
    private void LateUpdate() => Fit();

    // Chỉnh camera trực giao để toàn bộ vùng chơi dọc nằm trong vùng an toàn của thiết bị.
    private void Fit()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Application.isPlaying ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
        if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
        cam.orthographic = true;
        cam.backgroundColor = new Color(0.19f, 0.105f, 0.075f);

        float bottom = playfieldCenter.y - playfieldSize.y * 0.5f;
        if (reservedTopCanvas > 0f && contentTop > bottom)
        {
            // Vùng màn hình dành cho phần chơi: vùng an toàn trừ dải header phía trên.
            float headerPixels = reservedTopCanvas * CanvasScale();
            float availableTop = safe.yMax - headerPixels;
            float availableHeight = Mathf.Max(1f, availableTop - safe.yMin);
            float contentHeight = contentTop - bottom;
            cam.orthographicSize = 0.5f * Mathf.Max(contentHeight * Screen.height / availableHeight,
                playfieldSize.x * Screen.height / safe.width);
            // Neo mép trên phần chơi ngay dưới header; nếu thừa chỗ thì phần dư nằm dưới đáy.
            float y = contentTop - cam.orthographicSize * (2f * availableTop / Screen.height - 1f);
            float x = playfieldCenter.x - cam.orthographicSize * cam.aspect * (2f * safe.center.x / Screen.width - 1f);
            transform.SetPositionAndRotation(new Vector3(x, y, -10), Quaternion.identity);
            return;
        }

        float widthFraction = safe.width / Screen.width;
        float heightFraction = safe.height / Screen.height;
        cam.orthographicSize = Mathf.Max(playfieldSize.y / (2f * heightFraction),
            playfieldSize.x / (2f * cam.aspect * widthFraction));
        Vector2 safeOffset = new Vector2(safe.center.x / Screen.width - 0.5f,
            safe.center.y / Screen.height - 0.5f);
        Vector2 offset = Vector2.Scale(safeOffset,
            new Vector2(cam.orthographicSize * 2f * cam.aspect, cam.orthographicSize * 2f));
        transform.SetPositionAndRotation(new Vector3(playfieldCenter.x - offset.x,
            playfieldCenter.y - offset.y, -10), Quaternion.identity);
    }

    // Số pixel màn hình trên một đơn vị canvas (giống CanvasScaler: Scale With Screen Size, Match Width Or Height).
    private Canvas hudCanvas;
    private float nextHudLookup;

    private float CanvasScale()
    {
        // Ưu tiên tỉ lệ thật của canvas HUD đang hiển thị.
        if (Application.isPlaying)
        {
            if (hudCanvas == null && Time.unscaledTime >= nextHudLookup)
            {
                nextHudLookup = Time.unscaledTime + 1f;
                var hud = Object.FindFirstObjectByType<CanvasGamePlay>(FindObjectsInactive.Include);
                Canvas canvas = hud != null ? hud.GetComponentInParent<Canvas>(true) : null;
                hudCanvas = canvas != null ? canvas.rootCanvas : null;
            }
            if (hudCanvas != null && hudCanvas.scaleFactor > 0f) return hudCanvas.scaleFactor;
        }
        float widthLog = Mathf.Log(Screen.width / canvasReference.x, 2f);
        float heightLog = Mathf.Log(Screen.height / canvasReference.y, 2f);
        return Mathf.Pow(2f, Mathf.Lerp(widthLog, heightLog, canvasMatch));
    }
}
