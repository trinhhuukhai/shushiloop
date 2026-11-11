using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Chuyển board các level về đúng 4 cột (board ingame có 4 vị trí cột cố định).
// - Cột thứ 5, 6... được dồn xuống cuối các cột ngắn nhất (khay cứng đi nguyên khối).
// - Ô nối không còn ở 2 cột kề nhau thì gỡ nối; lớp băng vượt số lượt phá được thì giảm.
// - Màn ít hơn 4 cột giữ nguyên dữ liệu, board tự thêm cột trống khi chơi.
// - Level nào không hợp lệ sau khi chuyển thì giữ nguyên và ghi vào báo cáo Temp/BoardConvertReport.txt.
public static class SushiBoardColumnConverter
{
    private const int TargetColumns = 4;

    [MenuItem("SushiKhai/Levels/Convert Boards To 4 Columns")]
    public static void ConvertAll()
    {
        var report = new StringBuilder();
        int converted = 0, skipped = 0, failed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:SushiLevelData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var level = AssetDatabase.LoadAssetAtPath<SushiLevelData>(path);
            if (level == null) continue;
            string result = Convert(level, out bool changed);
            if (result != null) { failed++; report.AppendLine("FAIL " + path + ": " + result); }
            else if (changed) { converted++; report.AppendLine("OK   " + path); }
            else skipped++;
        }
        AssetDatabase.SaveAssets();
        report.Insert(0, $"Converted {converted}, unchanged {skipped}, failed {failed}\n");
        Directory.CreateDirectory("Temp");
        File.WriteAllText("Temp/BoardConvertReport.txt", report.ToString());
        Debug.Log("[Board 4 cột] " + report);
    }

    // Trả về null nếu thành công/không cần đổi, ngược lại là lý do thất bại (level giữ nguyên).
    private static string Convert(SushiLevelData level, out bool changed)
    {
        changed = false;
        if (level.ColumnCount <= TargetColumns) return null;

        List<List<FoodData>> board = level.boardColumns.Select(c => c.foods.Select(f => f.Copy()).ToList()).ToList();
        var moved = new HashSet<FoodData>();
        while (board.Count > TargetColumns)
        {
            List<FoodData> extra = board[board.Count - 1];
            board.RemoveAt(board.Count - 1);
            foreach (List<FoodData> block in Blocks(extra))
            {
                int target = 0;
                for (int c = 1; c < board.Count; c++)
                    if (board[c].Count <= board[target].Count) target = c;
                board[target].AddRange(block);
                foreach (FoodData food in block) moved.Add(food);
            }
        }
        FixLinks(board, _ => false);
        FixIce(board);

        string failure = Validate(level, board);
        if (failure != null)
        {
            // Cặp nối chặn đường nhau: gỡ nối các ô đã bị dời, rồi đến mọi cặp nối.
            FixLinks(board, food => moved.Contains(food));
            failure = Validate(level, board);
        }
        if (failure != null)
        {
            FixLinks(board, _ => true);
            failure = Validate(level, board);
        }
        if (failure != null) return failure;

        Undo.RecordObject(level, "Convert board to 4 columns");
        level.boardColumns = board.Select(c => new FoodQueueData { foods = c }).ToList();
        EditorUtility.SetDirty(level);
        changed = true;
        return null;
    }

    // Tách cột thành các khối: khay cứng (các ô liên tiếp cùng trayId) đi nguyên khối, ô khác đi lẻ.
    private static IEnumerable<List<FoodData>> Blocks(List<FoodData> column)
    {
        for (int i = 0; i < column.Count;)
        {
            var block = new List<FoodData> { column[i] };
            int tray = column[i].trayId;
            i++;
            while (tray > 0 && i < column.Count && column[i].trayId == tray) block.Add(column[i++]);
            yield return block;
        }
    }

    private static int LinkOf(FoodData food) => food.linkId > 0 ? food.linkId : food.lockId;

    private static void Unlink(FoodData food)
    {
        food.linkId = 0;
        food.lockId = 0;
        if (food.foodState == FoodState.Lock) food.foodState = FoodState.Normal;
    }

    // Gỡ các cặp nối không còn ở 2 cột kề nhau, hoặc có ô thoả điều kiện force.
    private static void FixLinks(List<List<FoodData>> board, System.Func<FoodData, bool> force)
    {
        var groups = new Dictionary<int, List<(int column, FoodData food)>>();
        for (int c = 0; c < board.Count; c++)
            foreach (FoodData food in board[c])
            {
                int link = LinkOf(food);
                if (link <= 0) continue;
                if (!groups.TryGetValue(link, out var group)) groups[link] = group = new List<(int, FoodData)>();
                group.Add((c, food));
            }
        foreach (var group in groups.Values)
        {
            bool valid = group.Count == 2 && Mathf.Abs(group[0].column - group[1].column) == 1 &&
                !group.Any(item => force(item.food));
            if (!valid) foreach (var item in group) Unlink(item.food);
        }
    }

    // Giảm lớp băng về số lượt phá được từ các cột kề (công thức giống ValidateBoard).
    private static void FixIce(List<List<FoodData>> board)
    {
        for (int c = 0; c < board.Count; c++)
            for (int r = 0; r < board[c].Count; r++)
            {
                FoodData food = board[c][r];
                if (food.iceLayers <= 0) continue;
                int moves = (c > 0 ? Mathf.Max(0, board[c - 1].Count - r) : 0) +
                    (c + 1 < board.Count ? Mathf.Max(0, board[c + 1].Count - r) : 0);
                food.iceLayers = Mathf.Min(food.iceLayers, moves);
                if (food.iceLayers == 0 && food.foodState == FoodState.Ice) food.foodState = FoodState.Normal;
            }
    }

    private static string Validate(SushiLevelData level, List<List<FoodData>> board)
    {
        var probe = Object.Instantiate(level);
        probe.boardColumns = board.Select(c => new FoodQueueData { foods = c.Select(f => f.Copy()).ToList() }).ToList();
        string error = probe.ValidateData();
        Object.DestroyImmediate(probe);
        return error;
    }
}
