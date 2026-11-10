using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Chụp màn chơi thật trong Editor đang mở: tạo file Temp/capture_ingame.trigger
// -> vào Play Mode, bấm Play ở Home, chờ level chạy, chụp Game view ra Temp/IngameCapture/*.png rồi thoát Play Mode.
// Không chạy nếu scene đang mở có thay đổi chưa lưu.
[InitializeOnLoad]
internal static class SushiIngameAutoCapture
{
    private const string Trigger = "Temp/capture_ingame.trigger";
    private const string Folder = "Temp/IngameCapture";
    private const string PhaseKey = "SushiIngameAutoCapture.Phase";
    private const string TimeKey = "SushiIngameAutoCapture.Time";
    private const string NameKey = "SushiIngameAutoCapture.Name";
    private static double nextPoll;

    static SushiIngameAutoCapture()
    {
        EditorApplication.update += Update;
        EditorApplication.delayCall += Start;
    }

    private static void Start()
    {
        if (!File.Exists(Trigger) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        string name = File.ReadAllText(Trigger).Trim();
        File.Delete(Trigger);
        Directory.CreateDirectory(Folder);
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            File.WriteAllText(Folder + "/result.txt", "SKIPPED: active scene has unsaved changes.");
            return;
        }
        SessionState.SetString(NameKey, string.IsNullOrEmpty(name) ? "ingame" : name);
        SessionState.SetInt(PhaseKey, 1);
        SessionState.SetFloat(TimeKey, (float)EditorApplication.timeSinceStartup);
        EditorApplication.EnterPlaymode();
    }

    private static float Elapsed => (float)EditorApplication.timeSinceStartup - SessionState.GetFloat(TimeKey, 0f);

    private static void Next(int phase)
    {
        SessionState.SetInt(PhaseKey, phase);
        SessionState.SetFloat(TimeKey, (float)EditorApplication.timeSinceStartup);
    }

    private static void Update()
    {
        int phase = SessionState.GetInt(PhaseKey, 0);
        if (phase == 0)
        {
            // Kiểm tra trigger mỗi giây (không cần chờ biên dịch lại).
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (File.Exists(Trigger)) Start();
            return;
        }
        try
        {
            if (phase == 9)
            {
                if (EditorApplication.isPlaying) return;
                SessionState.SetInt(PhaseKey, 0);
                return;
            }
            if (!EditorApplication.isPlaying) { if (Elapsed > 240f) Finish("FAILED: did not enter Play Mode."); return; }
            if (Elapsed > 240f) { Finish("FAILED: timed out at phase " + phase); return; }
            var level = Object.FindFirstObjectByType<LevelManager>();
            if (phase == 1)
            {
                // Chờ tới Home rồi bấm Play.
                if (UIManager.Ins == null || !UIManager.Ins.IsOpened<CanvasMainMenu>()) return;
                if (Elapsed < 3f) return;
                UIManager.Ins.GetUI<CanvasMainMenu>().Play();
                Next(2);
                return;
            }
            if (phase == 2)
            {
                if (level == null || !level.IsPlaying || Elapsed < 4f) return;
                string name = SessionState.GetString(NameKey, "ingame");
                ScreenCapture.CaptureScreenshot(Folder + "/" + name + ".png");
                Next(3);
                return;
            }
            string shot = SessionState.GetString(NameKey, "ingame");
            if (phase == 3)
            {
                // Nhặt đầu vài cột lên băng chuyền để thấy đĩa trắng hiện trên ô có món.
                if (Elapsed < 1f) return;
                var belt = Object.FindFirstObjectByType<ConveyorManager>();
                if (belt != null && level != null && level.Board != null)
                    for (int i = 0; i < level.Board.FoodQueues.Count && i < 3; i++)
                        belt.TryTakeFromQueue(level.Board.FoodQueues[i]);
                Next(4);
                return;
            }
            if (phase == 4)
            {
                if (Elapsed < 2.5f) return;
                ScreenCapture.CaptureScreenshot(Folder + "/" + shot + "_belt.png");
                Next(5);
                return;
            }
            if (phase == 5)
            {
                // Mở chi tiết order của khách đang ngồi đầu tiên.
                if (Elapsed < 1f) return;
                var customers = Object.FindFirstObjectByType<CustomerManager>();
                if (customers != null)
                    foreach (Customer customer in customers.ActiveCustomers)
                    {
                        if (customer == null || customer.Dishes.Count == 0) continue;
                        var view = customer.GetComponent<CustomerRecipeView>();
                        if (view != null && view.ShowRecipeDetails(view.CardAt(0))) break;
                    }
                Next(6);
                return;
            }
            if (phase == 6)
            {
                if (Elapsed < 1f) return;
                ScreenCapture.CaptureScreenshot(Folder + "/" + shot + "_detail.png");
                Next(7);
                return;
            }
            if (phase == 7)
            {
                if (Elapsed < 1.5f) return;
                Finish("OK");
            }
        }
        catch (Exception error) { Finish("FAILED: " + error); }
    }

    private static void Finish(string result)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/result.txt", result);
        SessionState.SetInt(PhaseKey, 9);
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }
}
