using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CandleApp : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelSetup;
    public GameObject panelCandle;

    [Header("Setup UI")]
    public TMP_InputField inputPrayer;
    public Slider sliderDays;
    public TMP_Text textDaysValue;
    public TMP_InputField inputReminderTime;

    [Header("Candle UI")]
    public TMP_Text textPrayerDisplay;

    [Header("3D")]
    public CandleController candleController;

    public const int MAX_DAYS = 7;            // app vai até 7 dias
    public const float SECONDS_PER_DAY = 60f; // 60s p/ testes; para produção 86400f

    void Start()
    {
        inputPrayer.text = PlayerPrefs.GetString("prayer_text", "");
        float savedDays = PlayerPrefs.GetFloat("prayer_days", 1f);
        sliderDays.value = Mathf.Clamp(savedDays, 1, MAX_DAYS);
        textDaysValue.text = $"{sliderDays.value:0} dia(s)";
        inputReminderTime.text = PlayerPrefs.GetString("reminder_time", "08:00");

        // Não mostra nada aqui — o MenuController decide (menu/vela/setup)
        ShowNone();
    }

    // ------- Aberturas chamadas pelo menu -------
    public void OpenSetup()                  => ShowSetup(true);
    public void OpenCandlePanelOnly()        => ShowSetup(false);
    public void ShowNone()                   { panelSetup.SetActive(false); panelCandle.SetActive(false); }

    /// <summary>Se existir vela ativa salva, recomeça do tempo restante e abre o painel da vela.</summary>
    public bool TryResumeCandleFromSave()
    {
        long end = (long)PlayerPrefs.GetFloat("candle_end_unix", 0f);
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool active = PlayerPrefs.GetInt("candle_active", 0) == 1 && end > now;

        if (!active) return false;

        float remaining = (float)(end - now);
        string prayer = PlayerPrefs.GetString("prayer_text", "");
        textPrayerDisplay.text = $"Oração: {prayer}";
        candleController.StartCandle(remaining);
        ShowSetup(false);
        return true;
    }

    // ------- callbacks da UI de setup -------
    public void OnDaysSliderChanged()
    {
        if (sliderDays.value > MAX_DAYS) sliderDays.value = MAX_DAYS;
        textDaysValue.text = $"{sliderDays.value:0} dia(s)";
    }

    public void OnClickLightCandle()
    {
        string prayer = inputPrayer.text.Trim();
        if (string.IsNullOrEmpty(prayer)) { Debug.LogWarning("Preencha a oração."); return; }
        if (!ValidateTime(inputReminderTime.text)) { Debug.LogWarning("Use HH:MM, ex.: 08:00"); return; }

        PlayerPrefs.SetString("prayer_text", prayer);
        PlayerPrefs.SetFloat("prayer_days", Mathf.Clamp(sliderDays.value, 1, MAX_DAYS));
        PlayerPrefs.SetString("reminder_time", inputReminderTime.text);
        PlayerPrefs.Save();

        // (opcional) agendar notificação diária
        Notifications.ScheduleDailyNotification(prayer, inputReminderTime.text);

        float totalSeconds = sliderDays.value * SECONDS_PER_DAY;

        candleController.StartCandle(totalSeconds);
        textPrayerDisplay.text = $"Oração: {prayer}";
        ShowSetup(false);

        long nowUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetFloat("candle_end_unix", nowUnix + (long)totalSeconds);
        PlayerPrefs.SetInt("candle_active", 1);
        PlayerPrefs.Save();
    }

    public void OnClickBack() => ShowSetup(true);

    // ------- util internos -------
    private void ShowSetup(bool show)
    {
        panelSetup.SetActive(show);
        panelCandle.SetActive(!show);
    }

    private bool ValidateTime(string hhmm)
    {
        if (string.IsNullOrWhiteSpace(hhmm)) return false;
        var p = hhmm.Split(':'); if (p.Length != 2) return false;
        return int.TryParse(p[0], out int h) && int.TryParse(p[1], out int m) && h >= 0 && h <= 23 && m >= 0 && m <= 59;
    }
}
