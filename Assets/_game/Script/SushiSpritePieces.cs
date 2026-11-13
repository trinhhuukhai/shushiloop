using System;
using System.Collections.Generic;
using UnityEngine;

// Tách một ảnh lớp thành nhiều miếng lúc chạy để từng miếng rơi riêng xuống đĩa.
// Mỗi miếng là một sprite cùng khung, cùng pivot với ảnh gốc: đặt chồng đúng chỗ thì trông y hệt ảnh gốc.
//  - GetSlices: các lát xếp chồng có viền tối (3 lát cá sashimi, 3 cuộn maki vẽ chung một ảnh).
//  - GetChunks: một đống miếng nhỏ (topping gunkan) chia thành vài cụm.
// Ảnh không tách được trả về null.
public static class SushiSpritePieces
{
    private static readonly Dictionary<Sprite, Sprite[]> slices = new Dictionary<Sprite, Sprite[]>();
    private static readonly Dictionary<Sprite, Sprite[]> chunks = new Dictionary<Sprite, Sprite[]>();

    // Các lát của ảnh, lát nằm sau cùng đứng trước. Null khi ảnh chỉ là một khối.
    public static Sprite[] GetSlices(Sprite source) => Get(slices, source, 0);

    // Chia ảnh thành tối đa `count` cụm quanh tâm, cụm phía sau đứng trước. Null khi không chia được.
    public static Sprite[] GetChunks(Sprite source, int count) => Get(chunks, source, Mathf.Max(2, count));

    private static Sprite[] Get(Dictionary<Sprite, Sprite[]> cache, Sprite source, int chunkCount)
    {
        if (source == null) return null;
        if (cache.TryGetValue(source, out Sprite[] pieces)) return pieces;
        try { pieces = Build(source, chunkCount); }
        catch (Exception error)
        {
            Debug.LogWarning("Không tách được miếng của ảnh " + source.name + ": " + error.Message, source);
            pieces = null;
        }
        cache[source] = pieces;
        return pieces;
    }

    private static Sprite[] Build(Sprite source, int chunkCount)
    {
        Texture2D texture = source.texture;
        Rect area = source.textureRect;
        int w = Mathf.RoundToInt(area.width), h = Mathf.RoundToInt(area.height);
        if (texture == null || w < 8 || h < 8) return null;

        // Texture gốc không bật Read/Write và có thể đang nén: vẽ qua RenderTexture rồi đọc lại vùng của sprite.
        RenderTexture target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        var readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels;
        try
        {
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            readable.ReadPixels(new Rect(Mathf.RoundToInt(area.x), Mathf.RoundToInt(area.y), w, h), 0, 0);
            readable.Apply();
            pixels = readable.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            if (Application.isPlaying) UnityEngine.Object.Destroy(readable);
            else UnityEngine.Object.DestroyImmediate(readable);
        }

        var rgba = new byte[pixels.Length * 4];
        for (int i = 0; i < pixels.Length; i++)
        {
            rgba[i * 4] = pixels[i].r; rgba[i * 4 + 1] = pixels[i].g;
            rgba[i * 4 + 2] = pixels[i].b; rgba[i * 4 + 3] = pixels[i].a;
        }
        int[] labels = chunkCount > 0
            ? SushiPieceSplitCore.SplitChunks(rgba, w, h, chunkCount, out int count)
            : SushiPieceSplitCore.SplitSlices(rgba, w, h, out count);
        if (count < 2) return null;

        var result = new Sprite[count];
        Vector2 pivot = new Vector2(source.pivot.x / Mathf.Max(1f, source.rect.width), source.pivot.y / Mathf.Max(1f, source.rect.height));
        for (int piece = 0; piece < count; piece++)
        {
            var colors = new Color32[pixels.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                // Giữ nguyên màu, chỉ cắt alpha: mép cắt không bị viền tối khi lọc ảnh. Mỗi miếng lấn sang miếng kế bên
                // 1 px để khi nằm đúng chỗ không lộ khe hở giữa các miếng.
                Color32 color = pixels[i];
                if (!Owns(labels, w, h, i, piece)) color.a = 0;
                colors[i] = color;
            }
            var pieceTexture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = source.name + " piece " + (piece + 1),
                wrapMode = TextureWrapMode.Clamp,
                filterMode = texture.filterMode,
                hideFlags = HideFlags.DontSave
            };
            pieceTexture.SetPixels32(colors);
            pieceTexture.Apply(false, true);
            result[piece] = Sprite.Create(pieceTexture, new Rect(0, 0, w, h), pivot, source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            result[piece].name = pieceTexture.name;
            result[piece].hideFlags = HideFlags.DontSave;
        }
        return result;
    }

    // Điểm ảnh thuộc miếng này, hoặc là điểm của miếng khác nằm sát mép miếng này.
    private static bool Owns(int[] labels, int w, int h, int i, int piece)
    {
        int label = labels[i];
        if (label == piece) return true;
        if (label < 0) return false;
        int x = i % w, y = i / w;
        return (x > 0 && labels[i - 1] == piece) || (x < w - 1 && labels[i + 1] == piece) ||
            (y > 0 && labels[i - w] == piece) || (y < h - 1 && labels[i + w] == piece);
    }
}
