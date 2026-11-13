using System;
using System.Collections.Generic;

// Thuật toán chia một ảnh (RGBA, w*h*4 byte) thành các miếng. Không phụ thuộc Unity để chạy thử được ngoài editor.
// Kết quả: nhãn miếng cho từng điểm ảnh (-1 = trong suốt), miếng rơi trước mang nhãn nhỏ hơn.
public static class SushiPieceSplitCore
{
    private sealed class Regions
    {
        public int w, h, opaqueCount;
        public bool[] opaque;
        public int[] component;
        public List<int> areas = new List<int>();
    }

    // Các lát xếp chồng có viền tối bao quanh (lát cá sashimi, cuộn maki): mỗi vùng sáng đủ lớn là một lát.
    // Lát bị che nhiều nhất (vùng nhỏ nhất) đứng trước. count = 0 khi ảnh chỉ là một khối.
    public static int[] SplitSlices(byte[] rgba, int w, int h, out int count)
    {
        count = 0;
        int[] labels = NewLabels(w * h);
        Regions regions = FindRegions(rgba, w, h, 2);
        if (regions == null) return labels;
        var kept = new List<int>();
        for (int id = 0; id < regions.areas.Count; id++)
            if (regions.areas[id] >= regions.opaqueCount * 0.05f) kept.Add(id);
        kept.Sort((a, b) => regions.areas[a].CompareTo(regions.areas[b]));
        if (kept.Count < 2 || kept.Count > 6) return labels;
        var remap = new int[regions.areas.Count];
        for (int id = 0; id < remap.Length; id++) remap[id] = -1;
        for (int k = 0; k < kept.Count; k++) remap[kept[k]] = k;
        for (int i = 0; i < labels.Length; i++)
            if (regions.component[i] >= 0) labels[i] = remap[regions.component[i]];
        Grow(labels, rgba, w, h);
        count = kept.Count;
        return labels;
    }

    // Một đống miếng nhỏ (topping gunkan: hạt lựu, trứng cá): chia thành tối đa `chunks` cụm gần bằng nhau quanh tâm.
    // Đường chia đi theo viền tối giữa các miếng nhỏ khi có thể. Cụm nằm phía sau (cao hơn trong ảnh) đứng trước.
    public static int[] SplitChunks(byte[] rgba, int w, int h, int chunks, out int count)
    {
        count = 0;
        int n = w * h;
        int[] labels = NewLabels(n);
        Regions regions = FindRegions(rgba, w, h, 1);
        if (regions == null || chunks < 2) return labels;

        // K-means trên toạ độ điểm ảnh đục; hạt giống ban đầu rải đều quanh tâm.
        double cx = 0, cy = 0, spread = 0;
        for (int i = 0; i < n; i++)
            if (regions.opaque[i]) { cx += i % w; cy += i / w; }
        cx /= regions.opaqueCount; cy /= regions.opaqueCount;
        for (int i = 0; i < n; i++)
            if (regions.opaque[i]) spread += Math.Abs(i % w - cx) + Math.Abs(i / w - cy);
        spread /= regions.opaqueCount;
        var seedX = new double[chunks];
        var seedY = new double[chunks];
        for (int k = 0; k < chunks; k++)
        {
            double angle = Math.PI / 2 + k * 2 * Math.PI / chunks;
            seedX[k] = cx + Math.Cos(angle) * spread;
            seedY[k] = cy + Math.Sin(angle) * spread;
        }
        for (int pass = 0; pass < 8; pass++)
        {
            var sumX = new double[chunks];
            var sumY = new double[chunks];
            var total = new int[chunks];
            for (int i = 0; i < n; i++)
            {
                if (!regions.opaque[i]) continue;
                int k = Nearest(seedX, seedY, i % w, i / w);
                sumX[k] += i % w; sumY[k] += i / w; total[k]++;
            }
            for (int k = 0; k < chunks; k++)
                if (total[k] > 0) { seedX[k] = sumX[k] / total[k]; seedY[k] = sumY[k] / total[k]; }
        }

        // Thứ tự rơi: cụm phía sau (y lớn, vì hàng 0 là đáy ảnh) trước.
        var order = new int[chunks];
        for (int k = 0; k < chunks; k++) order[k] = k;
        Array.Sort(order, (a, b) => seedY[b].CompareTo(seedY[a]));
        var rank = new int[chunks];
        for (int k = 0; k < chunks; k++) rank[order[k]] = k;

        // Vùng sáng nhỏ (một mặt miếng topping) đi trọn về một cụm theo tâm của nó; vùng lớn thì chia theo từng điểm ảnh.
        int regionCount = regions.areas.Count;
        var regionX = new double[regionCount];
        var regionY = new double[regionCount];
        for (int i = 0; i < n; i++)
        {
            int id = regions.component[i];
            if (id < 0) continue;
            regionX[id] += i % w; regionY[id] += i / w;
        }
        var regionLabel = new int[regionCount];
        for (int id = 0; id < regionCount; id++)
            regionLabel[id] = regions.areas[id] <= regions.opaqueCount * 0.2f
                ? rank[Nearest(seedX, seedY, regionX[id] / regions.areas[id], regionY[id] / regions.areas[id])] : -1;
        var used = new bool[chunks];
        for (int i = 0; i < n; i++)
        {
            int id = regions.component[i];
            if (id < 0) continue;
            labels[i] = regionLabel[id] >= 0 ? regionLabel[id] : rank[Nearest(seedX, seedY, i % w, i / w)];
            used[labels[i]] = true;
        }
        Grow(labels, rgba, w, h);

        // Bỏ nhãn trống để nhãn liên tục 0..count-1.
        var compact = new int[chunks];
        for (int k = 0; k < chunks; k++) compact[k] = used[k] ? count++ : -1;
        if (count < 2) { count = 0; return NewLabels(n); }
        for (int i = 0; i < n; i++)
            if (labels[i] >= 0) labels[i] = compact[labels[i]];
        return labels;
    }

    private static int[] NewLabels(int n)
    {
        var labels = new int[n];
        for (int i = 0; i < n; i++) labels[i] = -1;
        return labels;
    }

    private static int Nearest(double[] seedX, double[] seedY, double x, double y)
    {
        int best = 0;
        double bestDistance = double.MaxValue;
        for (int k = 0; k < seedX.Length; k++)
        {
            double dx = seedX[k] - x, dy = seedY[k] - y, distance = dx * dx + dy * dy;
            if (distance < bestDistance) { bestDistance = distance; best = k; }
        }
        return best;
    }

    // Tách viền tối khỏi phần sáng bằng ngưỡng Otsu theo độ sáng, co phần sáng `erode` lần (để chỗ viền hở
    // không nối hai miếng kề nhau) rồi đánh số các vùng sáng liền nhau. Null khi ảnh gần như trống.
    private static Regions FindRegions(byte[] rgba, int w, int h, int erode)
    {
        int n = w * h;
        var regions = new Regions { w = w, h = h, opaque = new bool[n], component = new int[n] };
        var value = new byte[n];
        var histogram = new int[256];
        for (int i = 0; i < n; i++)
        {
            regions.component[i] = -1;
            regions.opaque[i] = rgba[i * 4 + 3] > 60;
            if (!regions.opaque[i]) continue;
            byte brightness = Math.Max(rgba[i * 4], Math.Max(rgba[i * 4 + 1], rgba[i * 4 + 2]));
            value[i] = brightness;
            histogram[brightness]++;
            regions.opaqueCount++;
        }
        if (regions.opaqueCount < 64) return null;

        double sum = 0;
        for (int i = 0; i < 256; i++) sum += i * (double)histogram[i];
        double sumBelow = 0, best = -1;
        int below = 0, threshold = 128;
        for (int i = 0; i < 256; i++)
        {
            below += histogram[i];
            if (below == 0) continue;
            int above = regions.opaqueCount - below;
            if (above == 0) break;
            sumBelow += i * (double)histogram[i];
            double meanBelow = sumBelow / below, meanAbove = (sum - sumBelow) / above;
            double between = (double)below * above * (meanBelow - meanAbove) * (meanBelow - meanAbove);
            if (between > best) { best = between; threshold = i; }
        }

        var interior = new bool[n];
        for (int i = 0; i < n; i++) interior[i] = regions.opaque[i] && value[i] > threshold;
        for (int pass = 0; pass < erode; pass++)
        {
            var next = new bool[n];
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    int i = y * w + x;
                    next[i] = interior[i] && interior[i - 1] && interior[i + 1] && interior[i - w] && interior[i + w];
                }
            interior = next;
        }

        var stack = new Stack<int>();
        for (int start = 0; start < n; start++)
        {
            if (!interior[start] || regions.component[start] >= 0) continue;
            int id = regions.areas.Count, size = 0;
            regions.component[start] = id;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                size++;
                int x = i % w;
                if (x > 0 && interior[i - 1] && regions.component[i - 1] < 0) { regions.component[i - 1] = id; stack.Push(i - 1); }
                if (x < w - 1 && interior[i + 1] && regions.component[i + 1] < 0) { regions.component[i + 1] = id; stack.Push(i + 1); }
                if (i >= w && interior[i - w] && regions.component[i - w] < 0) { regions.component[i - w] = id; stack.Push(i - w); }
                if (i < n - w && interior[i + w] && regions.component[i + w] < 0) { regions.component[i + w] = id; stack.Push(i + w); }
            }
            regions.areas.Add(size);
        }
        return regions;
    }

    // Lan các nhãn đã có ra mọi điểm ảnh nhìn thấy còn lại (viền, bóng mờ) theo khoảng cách gần nhất.
    private static void Grow(int[] labels, byte[] rgba, int w, int h)
    {
        int n = w * h;
        var queue = new Queue<int>();
        for (int i = 0; i < n; i++)
            if (labels[i] >= 0) queue.Enqueue(i);
        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            int x = i % w, label = labels[i];
            if (x > 0 && rgba[(i - 1) * 4 + 3] > 0 && labels[i - 1] < 0) { labels[i - 1] = label; queue.Enqueue(i - 1); }
            if (x < w - 1 && rgba[(i + 1) * 4 + 3] > 0 && labels[i + 1] < 0) { labels[i + 1] = label; queue.Enqueue(i + 1); }
            if (i >= w && rgba[(i - w) * 4 + 3] > 0 && labels[i - w] < 0) { labels[i - w] = label; queue.Enqueue(i - w); }
            if (i < n - w && rgba[(i + w) * 4 + 3] > 0 && labels[i + w] < 0) { labels[i + w] = label; queue.Enqueue(i + w); }
        }
    }
}
