using System;
using UnityEngine;

public static class Notifications
{
    /// <summary>
    /// Chame 1x no início do app (ex.: Awake do MenuController) para pedir permissão no iOS
    /// e preparar o canal no Android.
    /// </summary>
    public static void Initialize()
    {
#if UNITY_IOS && !UNITY_EDITOR
        RequestIOSAuthorization();
#elif UNITY_ANDROID && !UNITY_EDITOR
        EnsureAndroidChannel();
#endif
    }

    /// <summary>
    /// Agenda uma notificação diária (local) para o horário HH:mm.
    /// </summary>
    public static void ScheduleDailyNotification(string message, string hhmm)
    {
        if (!TryParseHHmm(hhmm, out int h, out int m))
        {
            Debug.LogWarning("[Notifications] Horário inválido, usando 08:00.");
            h = 8; m = 0;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        EnsureAndroidChannel();
        var now = DateTime.Now;
        var fireTime = new DateTime(now.Year, now.Month, now.Day, h, m, 0);
        if (fireTime <= now) fireTime = fireTime.AddDays(1);

        var notif = new Unity.Notifications.Android.AndroidNotification
        {
            Title   = "Candle For Prayer",
            Text    = message,
            FireTime = fireTime,
            ShouldAutoCancel = true,
            RepeatInterval = TimeSpan.FromDays(1)
        };

        Unity.Notifications.Android.AndroidNotificationCenter.SendNotification(
            notif, ANDROID_CHANNEL_ID);
#elif UNITY_IOS && !UNITY_EDITOR
        RequestIOSAuthorization();

        var now = DateTime.Now;
        var fireTime = new DateTime(now.Year, now.Month, now.Day, h, m, 0);
        if (fireTime <= now) fireTime = fireTime.AddDays(1);

        var trigger = new Unity.Notifications.iOS.iOSNotificationCalendarTrigger
        {
            Year = fireTime.Year,
            Month = fireTime.Month,
            Day = fireTime.Day,
            Hour = fireTime.Hour,
            Minute = fireTime.Minute,
            Second = 0,
            Repeats = true
        };

        var notif = new Unity.Notifications.iOS.iOSNotification
        {
            Identifier = "daily_prayer",
            Title = "Candle For Prayer",
            Body = message,
            ShowInForeground = true,
            ForegroundPresentationOption =
                Unity.Notifications.iOS.PresentationOption.Alert |
                Unity.Notifications.iOS.PresentationOption.Sound,
            Trigger = trigger
        };

        Unity.Notifications.iOS.iOSNotificationCenter.ScheduleNotification(notif);
#else
        // Editor / plataformas não-suportadas: só loga (evita erro de compilação).
        Debug.Log($"[Notifications] (simulado) {message} às {h:00}:{m:00} todos os dias.");
#endif
    }

    /// <summary>Cancela todas as notificações agendadas.</summary>
    public static void CancelAll()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Unity.Notifications.Android.AndroidNotificationCenter.CancelAllScheduledNotifications();
        Unity.Notifications.Android.AndroidNotificationCenter.CancelAllDisplayedNotifications();
#elif UNITY_IOS && !UNITY_EDITOR
        Unity.Notifications.iOS.iOSNotificationCenter.RemoveAllScheduledNotifications();
        Unity.Notifications.iOS.iOSNotificationCenter.RemoveAllDeliveredNotifications();
#else
        Debug.Log("[Notifications] (simulado) CancelAll");
#endif
    }

    // ----------------- helpers internos -----------------
    static bool TryParseHHmm(string hhmm, out int h, out int m)
    {
        h = m = 0;
        if (string.IsNullOrWhiteSpace(hhmm)) return false;
        var parts = hhmm.Split(':');
        if (parts.Length != 2) return false;
        return int.TryParse(parts[0], out h) && int.TryParse(parts[1], out m);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    const string ANDROID_CHANNEL_ID = "candle_channel";

    static void EnsureAndroidChannel()
    {
        var chan = new Unity.Notifications.Android.AndroidNotificationChannel(
            ANDROID_CHANNEL_ID,
            "Candle For Prayer",
            "Lembretes diários de oração",
            Unity.Notifications.Android.Importance.Default
        );
        Unity.Notifications.Android.AndroidNotificationCenter.RegisterNotificationChannel(chan);
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    static void RequestIOSAuthorization()
    {
        var settings = Unity.Notifications.iOS.iOSAuthorizationRequestData.CreateDefault();
        var req = new Unity.Notifications.iOS.iOSAuthorizationRequest(settings, true);
        // (opcional) aguardar req.IsFinished em uma coroutine se quiser feedback
    }
#endif
}
