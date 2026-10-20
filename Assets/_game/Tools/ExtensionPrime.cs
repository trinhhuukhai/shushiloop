using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DG.Tweening;
//using I2.Loc;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class ExtensionPrime
{
    //otp variables
    //private static Vector3 tempVector3 = new Vector3();


    //ultilities method

    /// <summary>
    /// return enum name with space between capital words
    /// </summary>
    public static string ToFormattedText<T>(this T value) where T : Enum
    {
        return new string(value.ToString()
            .SelectMany(c =>
                char.IsUpper(c)
                    ? new[] { ' ', c }
                    : new[] { c })
            .ToArray()).Trim();
    }

    //Controll method
    public static void FadeColorAlpha(this Image image, float start, float end, float time = .24f,
        Ease ease = Ease.Linear)
    {
        var c = image.color;
        c.a = start;
        image.color = c;

        DOVirtual.Float(start, end, time, x =>
        {
            c.a = x;
            image.color = c;
        }).SetEase(ease);
    }

    public static void FadeColorAlpha(this Image image, float start, float end, float time, Ease ease, Timer.Task task)
    {
        var c = image.color;
        c.a = start;
        image.color = c;

        DOVirtual.Float(start, end, time, x =>
        {
            c.a = x;
            image.color = c;
        }).SetEase(ease).OnComplete(() => task?.Invoke());
    }

    public static void SetAlpha(this SpriteRenderer sr, float alpha)
    {
        var c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    public static float GetAlpha(this Image image)
    {
        var c = image.color;
        return c.a;
    }

    private const string FORMAT_TIME = "00";
    private const string TIME_DIVIDE = ":";
    private const string D_UPPER = "D ";
    private const string H_UPPER = "H ";
    private const string M_UPPER = "M ";
    private const string S_UPPER = "S ";
    private const string D_LOWER = "d ";
    private const string H_LOWER = "h ";
    private const string M_LOWER = "m ";
    private const string S_LOWER = "s ";

    //public static string ToTimeFormat_H_M_S(this TimeSpan timeSpan)
    //{
    //    return timeSpan.Hours.ToString(FORMAT_TIME) + TIME_DIVIDE + timeSpan.Minutes.ToString(FORMAT_TIME) + TIME_DIVIDE +
    //           timeSpan.Seconds.ToString(FORMAT_TIME);
    //}

    //public static string ToTimeFormat_H_M_S(this TimeSpan timeSpan)
    //{
    //    return timeSpan.Hours.ToString(FORMAT_TIME) + TIME_DIVIDE + timeSpan.Minutes.ToString(FORMAT_TIME) + TIME_DIVIDE +
    //           timeSpan.Seconds.ToString(FORMAT_TIME);
    //}

    public static string ToTimeFormat_M_S_Text(this TimeSpan timeSpan)
    {
        return timeSpan.Minutes.ToString(FORMAT_TIME) + M_UPPER + timeSpan.Seconds.ToString(FORMAT_TIME) + S_UPPER;
    }

    public static string ToTimeFormat_M_S(this TimeSpan timeSpan)
    {
        return timeSpan.Minutes.ToString(FORMAT_TIME) + TIME_DIVIDE + timeSpan.Seconds.ToString(FORMAT_TIME);
    }

    public static string ToTimeFormat_H_M_S_Dynamic_Text(this TimeSpan timeSpan)
    {
        var h = timeSpan.Hours;
        var m = timeSpan.Minutes;
        if (h > 0) return h.ToString(FORMAT_TIME) + H_UPPER + m.ToString(FORMAT_TIME) + M_UPPER;

        var s = timeSpan.Seconds;
        if (m > 0) return m.ToString(FORMAT_TIME) + M_UPPER + s.ToString(FORMAT_TIME) + S_UPPER;

        return s.ToString(FORMAT_TIME) + S_UPPER;
    }

    //public static string ToTimeFormat_H_M_S_Dynamic_Lower_Text(this TimeSpan timeSpan)
    //{
    //    var h = timeSpan.Hours;
    //    var m = timeSpan.Minutes;
    //    if (h > 0) return h.ToString(FORMAT_TIME) + H_LOWER + m.ToString(FORMAT_TIME) + M_LOWER;

    //    var s = timeSpan.Seconds;
    //    if (m > 0) return m.ToString(FORMAT_TIME) + M_LOWER + s.ToString(FORMAT_TIME) + S_LOWER;

    //    return s.ToString(FORMAT_TIME) + S_LOWER;
    //}

    public static string ToTimeFormat_M_S_Dynamic_Text(this TimeSpan timeSpan)
    {
        var m = timeSpan.Minutes;
        var s = timeSpan.Seconds;
        if (m > 0) return m.ToString(FORMAT_TIME) + TIME_DIVIDE + s.ToString(FORMAT_TIME);

        return s.ToString(FORMAT_TIME);
    }

    public static string ToTimeFormat_D_H_M_S_Dynamic_Text(this TimeSpan timeSpan) // default at DH then HM then MS
    {
        var d = timeSpan.Days;
        var h = timeSpan.Hours;
        if (d > 0) return d.ToString() + D_UPPER + h.ToString() + H_UPPER;

        var m = timeSpan.Minutes;
        if (h > 0) return h.ToString() + H_UPPER + m.ToString(FORMAT_TIME) + M_UPPER;

        var s = timeSpan.Seconds;
        if (m > 0) return m.ToString(FORMAT_TIME) + M_UPPER + s.ToString(FORMAT_TIME) + S_UPPER;

        return s.ToString(FORMAT_TIME) + S_UPPER;
    }

    public static string ToTimeFormat_D_H_M_S_Dynamic_Lower_Text(this TimeSpan timeSpan) // default at DH then HM then MS
    {
        var d = timeSpan.Days;
        var h = timeSpan.Hours;
        if (d > 0) return d.ToString() + D_LOWER + h.ToString() + H_LOWER;

        var m = timeSpan.Minutes;
        if (h > 0) return h.ToString() + H_LOWER + m.ToString(FORMAT_TIME) + M_LOWER;

        var s = timeSpan.Seconds;
        if (m > 0) return m.ToString(FORMAT_TIME) + M_LOWER + s.ToString(FORMAT_TIME) + S_LOWER;

        return s.ToString(FORMAT_TIME) + S_LOWER;
    }

    public static readonly Dictionary<string, (string day, string hour, string minute, string second)> LocalUnits =
        new Dictionary<string, (string, string, string, string)>
        {
            { "en", ("d ", "h ", "m ", "s ") },
            { "zh", ("天 ", "小时 ", "分钟 ", "秒 ") },
            { "ko", ("일 ", "시간 ", "분 ", "초 ") },
            { "pt", ("d ", "h ", "min ", "s ") },
            { "es", ("d ", "h ", "min ", "s ") },
            { "ar", ("يوم", "ساعة", "دقيقة", "ثانية") },
            { "de", ("T ", "Std ", "Min ", "Sek ") },
            { "it", ("g ", "h ", "min ", "s ") },
            { "ja", ("日 ", "時間 ", "分 ", "秒 ") },
            { "ru", ("д ", "ч ", "мин ", "с ") },
            { "th", ("วัน ", "ชม. ", "นาที ", "วิ ") },
            { "fr", ("j ", "h ", "min ", "s ") },
            { "id", ("h ", "j ", "mnt ", "dtk ") },
            { "vi", (" ngày ", " giờ ", " phút ", " giây ") },
        };

    public static string ToTimeFormatLocalizedCountdown(this TimeSpan ts, string lang = "en")
    {
        // Nếu lang null/rỗng thì mặc định EN
        if (string.IsNullOrEmpty(lang))
            lang = "en";

        lang = lang.ToLower();

        // Không tìm thấy language -> EN
        if (!LocalUnits.TryGetValue(lang, out var units))
            units = LocalUnits["en"];

        var parts = new List<string>();

        if (ts.Days > 0)
            parts.Add($"{ts.Days}{units.day}");

        if (ts.Hours > 0)
            parts.Add($"{ts.Hours}{units.hour}");

        if (ts.Minutes > 0 && parts.Count < 2)
            parts.Add($"{ts.Minutes}{units.minute}");

        if (parts.Count < 2)
            parts.Add($"{ts.Seconds}{units.second}");

        return string.Join("", parts);
    }


    //public static string ToTimeFormatLocalizedCountdown(this TimeSpan ts)
    //{
    //    string lang = LocalizationManager.CurrentLanguageCode;

    //    if (!LocalUnits.TryGetValue(lang, out var units)) units = LocalUnits["en"];

    //    var parts = new List<string>();

    //    if (ts.Days > 0)
    //        parts.Add($"{ts.Days.ToString()}{units.day}");
    //    if (ts.Hours > 0)
    //        parts.Add($"{ts.Hours.ToString()}{units.hour}");
    //    if (ts.Minutes > 0 && parts.Count < 2)
    //        parts.Add($"{ts.Minutes.ToString()}{units.minute}");
    //    if (parts.Count < 2)
    //        parts.Add($"{ts.Seconds.ToString()}{units.second}");

    //    return string.Join("", parts);
    //}
    //public static void SetLanguage(this string langCode)
    //{
    //    LocalizationManager.CurrentLanguageCode = langCode;
    //}

    public static void ResetAllAnimatorTriggers(this Animator animator)
    {
        foreach (var trigger in animator.parameters)
        {
            if (trigger.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(trigger.nameHash);
            }
        }
    }

    /// <summary>
    /// Slerp with local pivot
    /// </summary>
    public static Vector3 SlerpDirSupreme(this Vector3 start, Vector3 end, float angularSpeed)
    {
        return Vector3.Slerp(start, end, Time.deltaTime * angularSpeed / Vector3.Angle(start, end));
    }

    public static Tween DropDownTo(this RectTransform rectTransform, float targetHeight, float duration = .24f)
    {
        // Debug.Log("DROP EXten");
        var tmp = new Vector2();
        var xRect = rectTransform.sizeDelta.x;
        return DOVirtual.Float(rectTransform.sizeDelta.y, targetHeight, duration, x =>
        {
            //   Debug.Log("Drop " + x);
            tmp.Set(xRect, x);
            rectTransform.sizeDelta = tmp;
        }).SetEase(Ease.OutSine);
    }

    public static Tween PopingCHange(this Transform trans, Timer.Task task)
    {
        var scale = trans.localScale;
        return trans.DOScale(scale * .94f, .24f).SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                task?.Invoke();
                trans.DOScale(scale, .18f).SetEase(Ease.OutSine);
            });
    }

    public static T GetRandom<T>(this T[] array)
    {
        return array[Random.Range(0, array.Length)];
    }

    public static T GetRandom<T>(this IReadOnlyList<T> list)
    {
        return list[Random.Range(0, list.Count)];
    }

    public static string ToStringSupreme<T>(this T[] array) where T : Object
    {
        var sb = new StringBuilder();
        sb.Append("{");
        foreach (var item in array)
        {
            sb.Append(item.ToString());
            sb.Append(",");
        }

        sb.Append("END_LIST}");
        return sb.ToString();
    }

    public static string ToStringSupreme<T>(this IReadOnlyList<T> list) where T : Object
    {
        var sb = new StringBuilder();
        sb.Append("{");
        foreach (var item in list)
        {
            sb.Append(item.ToString());
            sb.Append(",");
        }

        sb.Append("END_LIST}");

        return sb.ToString();
    }

    public static string ToStringSupremeForEnum<T>(this T[] array) where T : Enum
    {
        var sb = new StringBuilder();
        sb.Append("{");
        foreach (var item in array)
        {
            sb.Append(item.ToString());
            sb.Append(",");
        }

        sb.Append("END_LIST}");

        return sb.ToString();
    }

    public static string ToStringSupremeForEnum<T>(this IReadOnlyList<T> list) where T : Enum
    {
        var sb = new StringBuilder();
        sb.Append("{");
        foreach (var item in list)
        {
            sb.Append(item.ToString());
            sb.Append(",");
        }

        sb.Append("END_LIST}");

        return sb.ToString();
    }

    public static void SetParentWithMultiParetn(this Transform transform, Transform target, bool isWorldPositionStay = true)
    {
        List<Transform> list = new List<Transform>();
        list.Add(target);
        for (int i = 0; i < 10; i++)
        {
            Transform trans = list[^1].parent;
            if (trans == null) break;
            list.Add(trans);
        }

        for (int i = 1; i <= list.Count; i++)
        {
            transform.SetParent(list[^i], true);
            // Vector3 myScale = transform.localScale;
            // Vector3 yourScale = list[^i].localScale;
            // Vector3 myScale2 = new Vector3(myScale.x / yourScale.x, myScale.y / yourScale.y, myScale.z / yourScale.z);
            // transform.localScale = myScale2;
            // Debug.Log("ITERATE " + i + "   :  " + transform.position + "   " + transform.eulerAngles + "   " + transform.localScale
            //           + "   " + yourScale + "  " + myScale);
        }
    }

    // Only use for expose matrix in  inspector

    #region SAFE AREA

    public static float GetSafeAreaLength(this Canvas canvas)
    {
        // return (Screen.height - Screen.safeArea.size.y) / canvas.scaleFactor;
        // Debug.Log("SAFE " + Screen.safeArea + "   " + Screen.height + "   " + Screen.safeArea.height + "   " + Screen.safeArea.y + "    " + (Screen.height - Screen.safeArea.y - Screen.safeArea.height));
        return (Screen.height - Screen.safeArea.y - Screen.safeArea.height) / canvas.scaleFactor;
    }

    public static float GetSafeAreaBottomLength(this Canvas canvas)
    {
        return (Screen.safeArea.y) / canvas.scaleFactor;
    }

    public static float SafeAreaAdaption(this RectTransform rect, Canvas canvas) //invidual item
    {
        var offset = GetSafeAreaLength(canvas);
        rect.anchoredPosition -= offset * Vector2.up;
        return offset;
    }

    public static float SafeAreaParentAdoption(this RectTransform rect, Canvas canvas) // panel
    {
        var offset = GetSafeAreaLength(canvas);
        var tmp = rect.offsetMax;
        tmp.y -= offset;
        rect.offsetMax = tmp;
        return offset;
    }

    public static float GetNewScreenRatioToRootScreenOffset(float offset = 1.46f) //??????? // only increase if thinner than original
    {
        return offset * GetScreenRatioMul();
    }

    public static float GetScreenRatioMul()
    {
        float tarRatio = (float)Screen.height / Screen.width;
        const float ROOT_RATIO = 1920f / 1080;
        if (tarRatio > ROOT_RATIO)
        {
            return tarRatio / ROOT_RATIO;
        }

        return 1;
    }

    #endregion

    #region LIST ULTILS

    public static T Pop<T>(this List<T> list)
    {
        if (list.Count == 0) return default;
        var index = list.Count - 1;
        var tmp = list[index];
        list.RemoveAt(index);
        return tmp;
    }

    public static T Peek<T>(this List<T> list)
    {
        return list.Count == 0 ? default : list[^1];
    }

    public static void Push<T>(this List<T> list, T item)
    {
        list.Add(item);
    }

    public static void Enqueue<T>(this List<T> list, T item)
    {
        list.Add(item);
    }

    public static T Dequeue<T>(this List<T> list)
    {
        if (list.Count == 0) return default;
        var tmp = list[0];
        list.RemoveAt(0);
        return tmp;
    }

    public static void SubtractList<T>(this List<T> list, List<T> subTractList)
    {
        for (int i = 0; i < subTractList.Count; i++)
        {
            list.Remove(subTractList[i]);
        }
    }

    #endregion

    public static void SetPositionAndRotation(this Transform trans, Transform target)
    {
        trans.SetPositionAndRotation(target.position, target.rotation);
    }

    #region Canvas

    // public static Vector2 ToFullScreenCanvasPos(this Vector3 worldPos, RectTransform canvasRect)
    // {
    //     Vector2 viewportPosition = Camera.main.WorldToViewportPoint(worldPos);
    //     var sizeDelta = canvasRect.sizeDelta;
    //     return new Vector2(
    //         ((viewportPosition.x * sizeDelta.x) - (sizeDelta.x * 0.5f)),
    //         ((viewportPosition.y * sizeDelta.y) - (sizeDelta.y * 0.5f)));
    // }

    public static Vector2 ToCanvasPosFromMousePos(this Vector3 mousePos, RectTransform canvasRect, bool isOverlayCanvas)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, isOverlayCanvas ? null : Camera.main, out var localPoint);
        return localPoint;
    }

    //public static Vector2 ToCanvasPosFromWorldPos(this Vector3 worldPos, RectTransform canvasRect, bool isOverlayCanvas)
    //{
    //    Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPos);
    //    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, isOverlayCanvas ? null : Camera.main, out var localPoint);
    //    return localPoint;
    //}

    public static Vector2 ToFullScreenCanvasPosFromOverLayCanvasPos(this Vector3 pos, RectTransform canvasRect)
    {
        var viewPortPos = Camera.main.ScreenToViewportPoint(pos);
        var sizeDelta = canvasRect.sizeDelta;
        var canvasPos = new Vector2(
            viewPortPos.x * sizeDelta.x - sizeDelta.x / 2,
            viewPortPos.y * sizeDelta.y - sizeDelta.y / 2
        );

        return canvasPos;
    }

    public static Vector3 ToWorldFromFullCanvas(this Vector2 canvasPos, RectTransform canvasRect, float offsetFromCam)
    {
        var sizeDelta = canvasRect.sizeDelta;
        var viewPortPoint = new Vector2(
            (canvasPos.x + sizeDelta.x / 2) / sizeDelta.x,
            (canvasPos.y + sizeDelta.y / 2) / sizeDelta.y
        );

        return Camera.main.ViewportToWorldPoint(new Vector3(viewPortPoint.x, viewPortPoint.y, offsetFromCam));
    }

    public static Vector3 ToWorldPosFromWorldCanvas(this Vector3 pos, float offset) // overlay canvas
    {
        var tmp = Camera.main.ScreenToWorldPoint(pos + Vector3.forward * offset);
        return tmp;
    }

    public static Vector3 ToWorldPosFromWorldWithOffset(this Vector3 pos, float offset)
    {
        var tmp = Camera.main.WorldToScreenPoint(pos);
        tmp.z = offset;
        return Camera.main.ScreenToWorldPoint(tmp);
    }

    public static void SetPanelHorizontalOffsetFromAnchor(this RectTransform rect, float offset) // right is positive
    {
        rect.offsetMax = new Vector2(-offset, 0);
        rect.offsetMin = new Vector2(-offset, 0);
        // Debug.Log("RECT " + rect.offsetMax + "  " + rect.offsetMin);
    }

    public static void SetPanelVerticalOffsetFromAnchor(this RectTransform rect, float offset) // up is positive
    {
        rect.offsetMax = new Vector2(0, -offset);
        rect.offsetMin = new Vector2(0, -offset);
    }

    #endregion

    #region Vector

    public static float DistanceFromPointToFiniteLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        var closestPoint = GetClosestPointOnFiniteLine(point, lineStart, lineEnd);
        return Vector3.Distance(closestPoint, point);
    }

    // For finite lines:
    public static Vector3 GetClosestPointOnFiniteLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.magnitude;
        lineDirection.Normalize();
        float projectLength = Mathf.Clamp(Vector3.Dot(point - lineStart, lineDirection), 0f, lineLength);
        return lineStart + lineDirection * projectLength;
    }

    // For infinite lines:
    public static Vector3 GetClosestPointOnInfiniteLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        return lineStart + Vector3.Project(point - lineStart, lineEnd - lineStart);
    }

    #endregion

    public static void SimulatePhysics()
    {
        var mode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        Physics.Simulate(Time.fixedDeltaTime);
        Physics.simulationMode = mode;
    }
}


#region NotExtension

[Serializable]
public class ArrayPrime<Fuck>
{
    public Fuck[] elements;
    public Fuck this[int index] => elements[index];
}

// Only use for expose matrix in  inspector
[Serializable]
public class MatrixPrime<Fuck>
{
    public ArrayPrime<Fuck>[] elements;
    public Fuck this[int x, int y] => elements[x][y];
    public Fuck[] this[int i] => elements[i].elements;

    public int GetLength(int dim)
    {
        return dim == 0 ? elements.Length : elements[0].elements.Length;
    }
}

#endregion