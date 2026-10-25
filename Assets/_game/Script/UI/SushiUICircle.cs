using UnityEngine;
using UnityEngine.UI;

// Vector UI medallion: crisp at every resolution without enlarging a tiny bitmap.
public sealed class SushiUICircle : Image
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float feather = 1f / Mathf.Max(0.01f, canvas != null ? canvas.scaleFactor : 1f);
        Color edge = color; edge.a = 0;
        mesh.AddVert(center, color, Vector2.zero);
        const int segments = 96;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            mesh.AddVert(center + direction * Mathf.Max(0, radius - feather), color, Vector2.zero);
            mesh.AddVert(center + direction * radius, edge, Vector2.zero);
            if (i == 0) continue;
            int inner = 1 + i * 2, previous = inner - 2;
            mesh.AddTriangle(0, previous, inner);
            mesh.AddTriangle(previous, previous + 1, inner + 1);
            mesh.AddTriangle(previous, inner + 1, inner);
        }
    }
}