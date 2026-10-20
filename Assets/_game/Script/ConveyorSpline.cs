using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorSpline : MonoBehaviour
{
    public Vector2 center = new Vector2(0f, 0.5f);
    public Vector2 halfSize = new Vector2(3.7f, 1.7f);
    public float cornerRadius = 1f;

    private Vector2[] positions;
    private float[] distances;

    public float Length { get; private set; }
    public bool Ready => positions != null && Length > 0f;

    public void Initialize()
    {
        Rebuild();
    }

    public void Configure(Vector2 centre, Vector2 half, float radius)
    {
        center = centre;
        halfSize = half;
        cornerRadius = radius;

        Rebuild();
    }

    [Button]
    public void Rebuild()
    {
        float width = Mathf.Max(0.1f, halfSize.x);
        float height = Mathf.Max(0.1f, halfSize.y);
        float radius = Mathf.Clamp(
            cornerRadius, 0.01f, Mathf.Min(width, height)
        );

        float x = width - radius;
        float y = height - radius;

        List<Vector2> points = new List<Vector2>();

        // Bắt đầu giữa cạnh dưới, chạy sang trái.
        AddLine(points, new Vector2(0, -height), new Vector2(-x, -height));
        AddArc(points, new Vector2(-x, -y), radius, -90, -180);

        AddLine(points, new Vector2(-width, -y), new Vector2(-width, y));
        AddArc(points, new Vector2(-x, y), radius, 180, 90);

        AddLine(points, new Vector2(-x, height), new Vector2(x, height));
        AddArc(points, new Vector2(x, y), radius, 90, 0);

        AddLine(points, new Vector2(width, y), new Vector2(width, -y));
        AddArc(points, new Vector2(x, -y), radius, 0, -90);

        AddLine(points, new Vector2(x, -height), new Vector2(0, -height));
        points.Add(points[0]);

        positions = new Vector2[points.Count];
        distances = new float[points.Count];
        Length = 0f;

        for (int i = 0; i < points.Count; i++)
        {
            positions[i] = points[i] + center;

            if (i > 0)
                Length += Vector2.Distance(positions[i - 1], positions[i]);

            distances[i] = Length;
        }
    }

    private static void AddLine( List<Vector2> points, Vector2 start, Vector2 end)
    {
        // Đoạn thẳng chỉ cần điểm đầu và điểm cuối.
        points.Add(start);
        points.Add(end);
    }

    private static void AddArc(List<Vector2> points, Vector2 center, float radius, float startAngle, float endAngle)
    {
        const int samples = 40;
        for (int i = 0; i <= samples; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, (float)i / samples) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            points.Add(center + direction * radius);
        }
    }

    // progress: 0 → 1 là một vòng, vượt 1 sẽ tự lặp lại.
    public Vector2 AtDistance(double progress)
    {
        CheckReady();

        double wrapped = progress - Math.Floor(progress);
        float distance = (float)wrapped * Length;
        int index = Array.BinarySearch(distances, distance);
        if (index >= 0)  return positions[index];
        int next = Mathf.Clamp(~index, 1, positions.Length - 1);
        int previous = next - 1;
        float t = Mathf.InverseLerp(distances[previous], distances[next], distance);
        return Vector2.Lerp(positions[previous], positions[next], t);
    }

    // Tìm vị trí trên đường gần điểm world nhất.
    public double ClosestDistance(Vector2 point)
    {
        CheckReady();

        float bestSqrDistance = float.PositiveInfinity;
        float bestDistance = 0f;

        for (int i = 0; i < positions.Length - 1; i++)
        {
            Vector2 start = positions[i];
            Vector2 segment = positions[i + 1] - start;

            float t = segment.sqrMagnitude > 0f
                ? Mathf.Clamp01(
                    Vector2.Dot(point - start, segment) /
                    segment.sqrMagnitude
                )
                : 0f;

            float sqrDistance = (point - (start + segment * t)).sqrMagnitude;

            if (sqrDistance >= bestSqrDistance) continue;
            bestSqrDistance = sqrDistance;
            bestDistance = Mathf.Lerp(distances[i], distances[i + 1], t);
        }

        return bestDistance / Length;
    }

    private void CheckReady()
    {
        if (!Ready)
            throw new InvalidOperationException("Cần gọi Initialize() trước.");
    }
}