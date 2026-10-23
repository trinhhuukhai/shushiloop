using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWatch : MonoBehaviour
{
    public TMP_Text timeText;       // hiển thị phút:giây
    public TMP_Text totalSecondsText; // hiển thị tổng giây

    private float elapsedTime = 0f;
    private bool isRunning = false;

    void FixedUpdate()
    {
        if (isRunning)
        {
            elapsedTime += Time.fixedDeltaTime;

            // Tính phút, giây
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);

            // Update UI
            if (timeText != null)
                timeText.text = $"{minutes:00}:{seconds:00}";

            if (totalSecondsText != null)
                totalSecondsText.text = Mathf.FloorToInt(elapsedTime).ToString();
        }
    }

    // Bắt đầu hoặc tiếp tục chạy
    public void StartTimer()
    {
        isRunning = true;
    }

    // Tạm dừng
    public void PauseTimer()
    {
        isRunning = false;
    }

    // Reset về 0
    public void ResetTimer()
    {
        isRunning = false;
        elapsedTime = 0f;

        if (timeText != null)
            timeText.text = "00:00";

        if (totalSecondsText != null)
            totalSecondsText.text = "0";
    }
}
