using UnityEngine;

// The game stays face-on: fit the complete portrait playfield inside the safe area.
[ExecuteAlways, RequireComponent(typeof(Camera))]
public class CameraControl : SingletonMonoBehaviour<CameraControl>
{
    public Camera cam;
    [SerializeField] private Vector2 playfieldSize = new Vector2(9f, 16f);
    [SerializeField] private Vector2 playfieldCenter = Vector2.zero;

    private void OnEnable() { cam = GetComponent<Camera>(); Fit(); }
    private void LateUpdate() => Fit();

    private void Fit()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Application.isPlaying ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
        if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
        float widthFraction = safe.width / Screen.width;
        float heightFraction = safe.height / Screen.height;
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(playfieldSize.y / (2f * heightFraction),
            playfieldSize.x / (2f * cam.aspect * widthFraction));
        Vector2 safeOffset = new Vector2(safe.center.x / Screen.width - 0.5f,
            safe.center.y / Screen.height - 0.5f);
        Vector2 offset = Vector2.Scale(safeOffset,
            new Vector2(cam.orthographicSize * 2f * cam.aspect, cam.orthographicSize * 2f));
        transform.SetPositionAndRotation(new Vector3(playfieldCenter.x - offset.x,
            playfieldCenter.y - offset.y, -10), Quaternion.identity);
        cam.backgroundColor = new Color(0.19f, 0.105f, 0.075f);
    }
}
