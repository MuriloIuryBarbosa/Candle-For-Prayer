using System;
using System.Collections.Generic;

[Serializable]
public class PrayerConfig
{
    public string prayerText;
    public int durationDays;             // limite na free = 3
    public List<DailyReminder> reminders = new();
    public DateTime startUtc;
}

[Serializable]
public class DailyReminder
{
    public int hour;
    public int minute;
}

[Serializable]
public class SaveData
{
    public PrayerConfig current;         // sua vela ativa
    public bool noAds;                   // versão paga
    public int adsShownOnce;             // 0/1
}
