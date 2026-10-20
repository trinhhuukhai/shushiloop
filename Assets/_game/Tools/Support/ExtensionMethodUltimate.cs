using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class ExtensionMethodUltimate
{
    //otp variables
    private static Vector3 tempVector3 = new Vector3();


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

    public static void SetAlpha(this Image image, float value)
    {
        var c = image.color;
        c.a = value;
        image.color = c;
    }

    public static float GetAlpha(this Image image)
    {
        var c = image.color;
        return c.a;
    }

    #region Time Format

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

    public static string ToTimeFormat_H_M_S(this TimeSpan timeSpan)
    {
        return timeSpan.Hours.ToString(FORMAT_TIME) + TIME_DIVIDE + timeSpan.Minutes.ToString(FORMAT_TIME) + TIME_DIVIDE +
               timeSpan.Seconds.ToString(FORMAT_TIME);
    }

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

    public static string ToTimeFormat_H_M_S_Dynamic_Lower_Text(this TimeSpan timeSpan)
    {
        var h = timeSpan.Hours;
        var m = timeSpan.Minutes;
        if (h > 0) return h.ToString(FORMAT_TIME) + H_LOWER + m.ToString(FORMAT_TIME) + M_LOWER;

        var s = timeSpan.Seconds;
        if (m > 0) return m.ToString(FORMAT_TIME) + M_LOWER + s.ToString(FORMAT_TIME) + S_LOWER;

        return s.ToString(FORMAT_TIME) + S_LOWER;
    }

    public static DateTime GetLastBeginOfWeekDate()
    {
        var cur = UnbiasedTime.TrueDateTime.Date; // .Date already at midnight
        switch (cur.DayOfWeek)
        {
            case DayOfWeek.Monday:
                break;
            case DayOfWeek.Tuesday:
                cur = cur.Subtract(TimeSpan.FromDays(1));
                break;
            case DayOfWeek.Wednesday:
                cur = cur.Subtract(TimeSpan.FromDays(2));
                break;
            case DayOfWeek.Thursday:
                cur = cur.Subtract(TimeSpan.FromDays(3));
                break;
            case DayOfWeek.Friday:
                cur = cur.Subtract(TimeSpan.FromDays(4));
                break;
            case DayOfWeek.Saturday:
                cur = cur.Subtract(TimeSpan.FromDays(5));
                break;
            case DayOfWeek.Sunday:
                cur = cur.Subtract(TimeSpan.FromDays(6));
                break;
        }

        return cur;
    }

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

    public static double GetCurrentTimeSpanToRootDate()
    {
        return (UnbiasedTime.TrueDateTime.Subtract(Variables.rootDate).TotalSeconds);
    }

    public static readonly Dictionary<string, (string day, string hour, string minute, string second)> LocalUnits =
        new Dictionary<string, (string, string, string, string)>
        {
            { "en", ("d ", "h ", "m ", "s ") },
            { "zh", ("天 ", "小时 ", "分钟 ", "秒 ") },
            { "ko", ("일 ", "시간 ", "분 ", "초 ") },
            { "pt", ("\u00A0d ", "\u00A0h ", "\u00A0min ", "\u00A0s ") },
            { "es", ("\u00A0d ", "\u00A0h ", "\u00A0min ", "\u00A0s ") },
            { "ar", ("\u00A0يوم ", "\u00A0ساعة ", "\u00A0دقيقة ", "\u00A0ثانية ") },
            { "de", ("\u00A0T ", "\u00A0Std ", "\u00A0Min ", "\u00A0Sek ") },
            { "it", ("\u00A0g ", "\u00A0h ", "\u00A0min ", "\u00A0s ") },
            { "ja", ("日 ", "時間 ", "分 ", "秒 ") },
            { "ru", ("\u00A0д ", "\u00A0ч ", "\u00A0мин ", "\u00A0с ") },
            { "th", ("วัน ", "ชม. ", "นาที ", "วิ ") },
            { "fr", ("\u00A0j ", "\u00A0h ", "\u00A0min ", "\u00A0s ") },
            { "id", ("\u00A0h ", "\u00A0j ", "\u00A0mnt ", "\u00A0dtk ") },
            { "vi", ("\u00A0ngày ", "\u00A0giờ ", "\u00A0phút ", "\u00A0giây ") },
        };



    public static readonly Dictionary<string, string> LocalDateYM =
        new Dictionary<string, string>
        {
            { "en", "{0}-{1}" }, // 2025-03
            { "zh", "{0}年{1}月" }, // 2025年3月
            { "ko", "{0}년 {1}월" }, // 2025년 3월
            { "pt", "{1}/{0}" }, // 03/2025
            { "es", "{1}/{0}" }, // 03/2025
            { "ar", "{0}/{1}" }, // 2025/03 (Arabic keeps Y/M order)
            { "de", "{1}.{0}" }, // 03.2025
            { "it", "{1}/{0}" }, // 03/2025
            { "ja", "{0}年{1}月" }, // 2025年3月
            { "ru", "{1}.{0}" }, // 03.2025
            { "th", "{1}/{0}" }, // 03/2025 (Thai UI often uses western format)
            { "fr", "{1}/{0}" }, // 03/2025
            { "id", "{1}/{0}" }, // 03/2025
            { "vi", "{1}/{0}" }, // 03/2025
        };


    public static readonly Dictionary<string, string[]> LocalizedMonths =
        new Dictionary<string, string[]>
        {
            // ENGLISH
            { "en", new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" } },

            // CHINESE
            { "zh", new[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月" } },

            // KOREAN
            { "ko", new[] { "1월", "2월", "3월", "4월", "5월", "6월", "7월", "8월", "9월", "10월", "11월", "12월" } },

            // PORTUGUESE
            { "pt", new[] { "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro" } },

            // SPANISH
            { "es", new[] { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" } },

            // ARABIC
            { "ar", new[] { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" } },

            // GERMAN
            { "de", new[] { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember" } },

            // ITALIAN
            { "it", new[] { "Gennaio", "Febbraio", "Marzo", "Aprile", "Maggio", "Giugno", "Luglio", "Agosto", "Settembre", "Ottobre", "Novembre", "Dicembre" } },

            // JAPANESE
            { "ja", new[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月" } },

            // RUSSIAN
            { "ru", new[] { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" } },

            // THAI
            { "th", new[] { "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" } },

            // FRENCH
            { "fr", new[] { "Janvier", "Février", "Mars", "Avril", "Mai", "Juin", "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" } },

            // INDONESIAN
            { "id", new[] { "Januari", "Februari", "Maret", "April", "Mei", "Juni", "Juli", "Agustus", "September", "Oktober", "November", "Desember" } },

            // VIETNAMESE
            { "vi", new[] { "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6", "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12" } },
        };

    public static readonly Dictionary<string, string> MonthYearFormat =
        new Dictionary<string, string>
        {
            // en → January 2025
            { "en", "{0} {1}" },

            // zh → 2025年1月
            { "zh", "{1}年{0}" },

            // ko → 2025년 1월
            { "ko", "{1}년 {0}" },

            // pt → Janeiro 2025
            { "pt", "{0} {1}" },

            // es → Enero 2025
            { "es", "{0} {1}" },

            // ar → يناير 2025
            { "ar", "{0} {1}" },

            // de → Januar 2025
            { "de", "{0} {1}" },

            // it → Gennaio 2025
            { "it", "{0} {1}" },

            // ja → 2025年1月
            { "ja", "{1}年{0}" },

            // ru → Январь 2025
            { "ru", "{0} {1}" },

            // th → มกราคม 2025
            { "th", "{0} {1}" },

            // fr → Janvier 2025
            { "fr", "{0} {1}" },

            // id → Januari 2025
            { "id", "{0} {1}" },

            // vi → Tháng 1 2025
            { "vi", "{0} {1}" },
        };

    public static string FormatMonthYear(int year, int month, string lang)
    {
        if (!LocalizedMonths.TryGetValue(lang, out var months))
            months = LocalizedMonths["en"];

        if (!MonthYearFormat.TryGetValue(lang, out var pattern))
            pattern = MonthYearFormat["en"];

        string monthName = months[month - 1];

        return string.Format(pattern, monthName, year);
    }


    #endregion

    //public static double GetCurrentTimeSpanToRootDate()
    //{
    //    return (UnbiasedTime.TrueDateTime.Subtract(Variables.rootDate).TotalSeconds);
    //}

    //public static TimeSpan GetTimeLeftFromCurrentDate(double targetTime, double lastTime)
    //{
    //    return TimeSpan.FromSeconds(targetTime - ((UnbiasedTime.TrueDateTime.Subtract(Variables.rootDate).TotalSeconds) - lastTime));
    //}

    //public static DateTime GetLastBeginOfWeekDate()
    //{
    //    var cur = UnbiasedTime.TrueDateTime.Date; // .Date already at midnight
    //    switch (cur.DayOfWeek)
    //    {
    //        case DayOfWeek.Monday:
    //            break;
    //        case DayOfWeek.Tuesday:
    //            cur = cur.Subtract(TimeSpan.FromDays(1));
    //            break;
    //        case DayOfWeek.Wednesday:
    //            cur = cur.Subtract(TimeSpan.FromDays(2));
    //            break;
    //        case DayOfWeek.Thursday:
    //            cur = cur.Subtract(TimeSpan.FromDays(3));
    //            break;
    //        case DayOfWeek.Friday:
    //            cur = cur.Subtract(TimeSpan.FromDays(4));
    //            break;
    //        case DayOfWeek.Saturday:
    //            cur = cur.Subtract(TimeSpan.FromDays(5));
    //            break;
    //        case DayOfWeek.Sunday:
    //            cur = cur.Subtract(TimeSpan.FromDays(6));
    //            break;
    //    }

    //    return cur;
    //}

    public static TimeSpan GetAbsoluteTimeSpan(this TimeSpan timeSpan)
    {
        return timeSpan.TotalMilliseconds < 0 ? timeSpan.Negate() : timeSpan;
    }

    //public static DayOfWeek GetCurrentDayOfWeek()
    //{
    //    return UnbiasedTime.TrueDateTime.DayOfWeek;
    //}

    //public static DayOfWeek GetFirstDayOfWeekInMonth()
    //{
    //    var curDate = UnbiasedTime.TrueDateTime;
    //    var month = curDate.Month;
    //    var year = curDate.Year;

    //    return new DateTime(year, month, 1).DayOfWeek;
    //}

    //public static int GetNumOfDaysInMonth()
    //{
    //    var curDate = UnbiasedTime.TrueDateTime;
    //    var month = curDate.Month;
    //    var year = curDate.Year;

    //    return DateTime.DaysInMonth(year, month);
    //}

    public static string ToFuckingStringUpperCase(this DayOfWeek dow)
    {
        return dow switch
        {
            DayOfWeek.Monday => "MONDAY",
            DayOfWeek.Tuesday => "TUESDAY",
            DayOfWeek.Wednesday => "WEDNESDAY",
            DayOfWeek.Thursday => "THURSDAY",
            DayOfWeek.Friday => "FRIDAY",
            DayOfWeek.Saturday => "SATURDAY",
            DayOfWeek.Sunday => "SUNDAY",
            _ => "EVERY DAY"
        };
    }

    public static string MonthToFuckingStringUpperCase(this int month)
    {
        return month switch
        {
            1 => "JANUARY",
            2 => "FEBRUARY",
            3 => "MARCH",
            4 => "APRIL",
            5 => "MAY",
            6 => "JUNE",
            7 => "JULY",
            8 => "AUGUST",
            9 => "SEPTEMBER",
            10 => "OCTOBER",
            11 => "NOVEMBER",
            12 => "DECEMBER",
            _ => "EVERY MONTH",
        };
    }

    public static string MonthToFuckingStringUpperCaseShort(this int month)
    {
        return month switch
        {
            1 => "JAN",
            2 => "FEB",
            3 => "MAR",
            4 => "APR",
            5 => "MAY",
            6 => "JUN",
            7 => "JUL",
            8 => "AUG",
            9 => "SEPT",
            10 => "OCT",
            11 => "NOV",
            12 => "DEC",
            _ => "HIHI",
        };
    }

    /// //////////////////////////////////////////////////////////// may use string builder //////////////////////////////////////////////////////////////////////////
    public static void LookAtSupreme(this Transform transform, Vector3 target)
    {
        var lookDir = target - transform.position;
        transform.eulerAngles = Vector3Pot.GetVector3(0, Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg);
    }

    public static void LookAtSupremeDir(this Transform transform, Vector3 lookDir)
    {
        transform.eulerAngles = Vector3Pot.GetVector3(0, Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg);
    }

    public static Tween LookAtSupreme(this Transform transform, Vector3 target, float duration)
    {
        var lookDir = target - transform.position;
        lookDir = Vector3Pot.GetVector3(0, Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg);
        return transform.DORotate(lookDir, duration, RotateMode.FastBeyond360).SetEase(Ease.OutSine);
    }

    public static Tween LookAtSupreme(this Transform transform, Vector3 target, float duration, Timer.Task task)
    {
        var lookDir = target - transform.position;
        lookDir = Vector3Pot.GetVector3(0, Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg);
        return transform.DORotate(lookDir, duration).SetEase(Ease.OutSine).OnComplete(() => task());
    }

    public static void LookAtSupremeLerp(this Transform transform, Vector3 target,
        float speed = 9.2f) //caution extend 359 to 0 ?? anymore ??
    {
        var lookDir = target - transform.position;
        lookDir.Normalize();
        lookDir = Vector3.Lerp(transform.forward, lookDir, speed * Time.deltaTime);
        transform.eulerAngles = Vector3Pot.GetVector3(0, Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg);
    }

    //public static void LookAtSupSLerp(this Transform transform, Vector3 target,
    //    float speed = 360f) //caution extend 359 to 0 ?? anymore ??
    //{
    //    var lookDir = target - transform.position;
    //    lookDir.Normalize();
    //    lookDir = transform.forward.SlerpDirSupreme(lookDir, speed);
    //    transform.eulerAngles = Vector3Pot.GetVector3(0, Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg);
    //}

    //public static void LookAtSupSLerpDir(this Transform transform, Vector3 lookDir,
    //    float speed = 360f) //caution extend 359 to 0 ?? anymore ??
    //{
    //    lookDir.Normalize();
    //    lookDir = transform.forward.SlerpDirSupreme(lookDir, speed);
    //    transform.eulerAngles = Vector3Pot.GetVector3(0, Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg);
    //}

    public static void Popup(this Transform transform, float duration = .32f, float originXYZScale = 1)
    {
        transform.localScale = Vector3Pot.zero;
        DOVirtual.Float(0, originXYZScale, duration, x => transform.localScale = Vector3Pot.one * x)
            .SetEase(Ease.OutSine).OnComplete(() => transform.localScale = Vector3Pot.one);
    }

    public static void Popup(this Transform transform, AnimationCurve animationCurve, float duration = .32f,
        float originXYZScale = 1)
    {
        transform.localScale = Vector3Pot.zero;
        DOVirtual.Float(0, originXYZScale, duration, x => transform.localScale = Vector3Pot.one * x)
            .SetEase(animationCurve).OnComplete(() => transform.localScale = Vector3Pot.one);
    }

    public static void Shrink(this Transform transform, float duration = .32f, float originXYZScale = 1)
    {
        transform.localScale = Vector3Pot.one * originXYZScale;
        DOVirtual.Float(originXYZScale, 0, duration, x => transform.localScale = Vector3Pot.one * x)
            .SetEase(Ease.OutSine).OnComplete(() => transform.localScale = Vector3Pot.zero);
    }

    public static void Shrink(this Transform transform, AnimationCurve animationCurve, float duration = .32f,
        float originXYZScale = 1)
    {
        transform.localScale = Vector3Pot.one * originXYZScale;
        DOVirtual.Float(originXYZScale, 0, duration, x => transform.localScale = Vector3Pot.one * x)
            .SetEase(animationCurve).OnComplete(() => transform.localScale = Vector3Pot.zero);
    }

    public static Tween Fade(this CanvasGroup canvasGroup, bool isFadeToOne, float duration = .32f)
    {
        canvasGroup.alpha = isFadeToOne ? 0 : 1;
        return DOVirtual.Float(isFadeToOne ? 0 : 1, isFadeToOne ? 1 : 0, duration, x => canvasGroup.alpha = x)
            .SetEase(isFadeToOne ? Ease.OutSine : Ease.InSine).SetUpdate(true);
    }

    public static Tween Fade(this CanvasGroup canvasGroup, bool isFadeToOne, Timer.Task task, float duration = .32f)
    {
        canvasGroup.alpha = isFadeToOne ? 0 : 1;
        return DOVirtual.Float(isFadeToOne ? 0 : 1, isFadeToOne ? 1 : 0, duration, x => canvasGroup.alpha = x)
            .SetEase(isFadeToOne ? Ease.OutSine : Ease.InSine).OnComplete(() => task());
    }

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

    public static void FuckingDisappearTask(this MonoBehaviour mono, Timer.Task task = default, float duration = .32f)
    {
        var transform = mono.transform;
        var gameObject = mono.gameObject;
        transform.DOScale(Vector3Pot.zero, duration).SetEase(Ease.Linear).OnComplete(() =>
        {
            gameObject.SetActive(false);
            task?.Invoke();
        });
    }

    public static void FuckingDisappearTask(this MonoBehaviour mono, Transform transform, GameObject gameObject, Timer.Task task = default, float duration = .32f)
    {
        transform.DOScale(Vector3Pot.zero, duration).SetEase(Ease.Linear).OnComplete(() =>
        {
            gameObject.SetActive(false);
            task?.Invoke();
        });
    }

    public static void FuckingDisappear(this MonoBehaviour mono, float duration = .32f)
    {
        var transform = mono.transform;
        var gameObject = mono.gameObject;
        transform.DOScale(Vector3Pot.zero, duration).SetEase(Ease.Linear).OnComplete(() => { gameObject.SetActive(false); });
    }

    public static void FuckingDisappear(this MonoBehaviour mono, Transform transform, GameObject gameObject, float duration = .32f)
    {
        transform.DOScale(Vector3Pot.zero, duration).SetEase(Ease.Linear).OnComplete(() => { gameObject.SetActive(false); });
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

    public static void DrawFullQuad(this Object _, Material mat)
    {
        GL.PushMatrix();
        GL.LoadOrtho();
        // call after rendertedxture.avtive = smRT for example
        // activate the first shader pass (in this case we know it is the only pass)
        mat.SetPass(0);
        // draw a quad over whole screen
        GL.Begin(GL.QUADS);
        GL.TexCoord2(0f, 0f);
        GL.Vertex3(0f, 0f, 0f); /* note the order! */
        GL.TexCoord2(0f, 1f);
        GL.Vertex3(0f, 1f, 0f); /* also, we need TexCoord2 */
        GL.TexCoord2(1f, 1f);
        GL.Vertex3(1f, 1f, 0f);
        GL.TexCoord2(1f, 0f);
        GL.Vertex3(1f, 0f, 0f);
        GL.End();
        //use gl because graphic.blit not work with stencil

        GL.PopMatrix();
    }

    public static void DrawFullQuadIndex(this Object _, Material mat, Mesh mesh)
    {
        GL.PushMatrix();
        GL.LoadOrtho();

        // activate the first shader pass (in this case we know it is the only pass)
        mat.SetPass(0);
        // draw a quad over whole screen
        GL.Begin(GL.QUADS);
        GL.TexCoord2(0f, 0f);
        GL.Vertex3(0f, 0f, 2f); /* note the order! */
        GL.TexCoord2(0f, 1f);
        GL.Vertex3(0f, 1f, 0f); /* also, we need TexCoord2 */
        GL.TexCoord2(1f, 1f);
        GL.Vertex3(1f, 1f, 1f);
        GL.TexCoord2(1f, 0f);
        GL.Vertex3(1f, 0f, 3f);

        GL.End();
        //use gl because graphic.blit not work with stencil

        GL.PopMatrix();

        ////////////////
        // mat.SetPass(0);
        // Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
    }

    // Only use for expose matrix in  inspector

    #region SAFE AREA

    //public static float GetSafeAreaLength(this Canvas canvas)
    //{
    //    // return (Screen.height - Screen.safeArea.size.y) / canvas.scaleFactor;
    //    // Debug.Log("SAFE " + Screen.safeArea + "   " + Screen.height + "   " + Screen.safeArea.height + "   " + Screen.safeArea.y + "    " + (Screen.height - Screen.safeArea.y - Screen.safeArea.height));
    //    return (Screen.height - Screen.safeArea.y - Screen.safeArea.height) / canvas.scaleFactor;
    //}

    public static float GetSafeAreaBottomLength(this Canvas canvas)
    {
        return (Screen.safeArea.y) / canvas.scaleFactor;
    }

    //public static float SafeAreaAdaption(this RectTransform rect, Canvas canvas) //invidual item
    //{
    //    var offset = GetSafeAreaLength(canvas);
    //    rect.anchoredPosition -= offset * Vector2.up;
    //    return offset;
    //}

    //public static float SafeAreaParentAdoption(this RectTransform rect, Canvas canvas) // panel
    //{
    //    var offset = GetSafeAreaLength(canvas);
    //    var tmp = rect.offsetMax;
    //    tmp.y -= offset;
    //    rect.offsetMax = tmp;
    //    return offset;
    //}

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

    public static void SetActive(this IEnumerable<GameObject> list, bool isActive)
    {
        foreach (var gameObject in list)
        {
            gameObject.SetActive(isActive);
        }
    }

    public static void SetActiveAtIndex(this IEnumerable<GameObject> list, int index)
    {
        var i = 0;

        foreach (var gameObject in list)
        {
            gameObject.SetActive(index == i);
            i++;
        }
    }

    #endregion

    public static void ClearAllChild(this Transform trans)
    {
        if (trans.childCount == 0) return;
        for (int i = trans.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(trans.GetChild(i).gameObject);
        }
    }

    public static void SetPositionAndRotation(this Transform trans, Transform target)
    {
        trans.SetPositionAndRotation(target.position, target.rotation);
    }

    #region Canvas

    // public static Vector2 ToFullScreenCanvasPos(this Vector3 worldPos, RectTransform canvasRect)
    // {
    //     Vector2 viewportPosition = CameraController.Ins.cam.WorldToViewportPoint(worldPos);
    //     var sizeDelta = canvasRect.sizeDelta;
    //     return new Vector2(
    //         ((viewportPosition.x * sizeDelta.x) - (sizeDelta.x * 0.5f)),
    //         ((viewportPosition.y * sizeDelta.y) - (sizeDelta.y * 0.5f)));
    // }

    public static Vector2 ToCanvasPosFromMousePos(this Vector3 mousePos, RectTransform canvasRect, bool isOverlayCanvas)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, isOverlayCanvas ? null : CameraControl.Ins.cam, out var localPoint);
        return localPoint;
    }

    public static Vector2 ToCanvasPosFromWorldPos(this Vector3 worldPos, RectTransform canvasRect, bool isOverlayCanvas)
    {
        Vector2 screenPoint = CameraControl.Ins.cam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, isOverlayCanvas ? null : CameraControl.Ins.cam, out var localPoint);
        return localPoint;
    }

    //public static Vector2 ToFullScreenCanvasPosFromOverLayCanvasPos(this Vector3 pos, RectTransform canvasRect)
    //{
    //    var viewPortPos = CameraControl.Ins.cam.ScreenToViewportPoint(pos);
    //    var sizeDelta = canvasRect.sizeDelta;
    //    var canvasPos = new Vector2(
    //        viewPortPos.x * sizeDelta.x - sizeDelta.x / 2,
    //        viewPortPos.y * sizeDelta.y - sizeDelta.y / 2
    //    );

    //    return canvasPos;
    //}

    public static Vector3 ToWorldFromFullCanvas(this Vector2 canvasPos, RectTransform canvasRect, float offsetFromCam)
    {
        var sizeDelta = canvasRect.sizeDelta;
        var viewPortPoint = new Vector2(
            (canvasPos.x + sizeDelta.x / 2) / sizeDelta.x,
            (canvasPos.y + sizeDelta.y / 2) / sizeDelta.y
        );

        return CameraControl.Ins.cam.ViewportToWorldPoint(new Vector3(viewPortPoint.x, viewPortPoint.y, offsetFromCam));
    }

    public static Vector3 ToWorldPosFromWorldCanvas(this Vector3 pos, float offset) // overlay canvas
    {
        var tmp = CameraControl.Ins.cam.ScreenToWorldPoint(pos + Vector3.forward * offset);
        return tmp;
    }

    public static Vector3 ToWorldPosFromWorldWithOffset(this Vector3 pos, float offset)
    {
        var tmp = CameraControl.Ins.cam.WorldToScreenPoint(pos);
        tmp.z = offset;
        return CameraControl.Ins.cam.ScreenToWorldPoint(tmp);
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
