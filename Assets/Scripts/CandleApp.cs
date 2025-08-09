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

    private const int FREE_MAX_DAYS = 3;
    // private const float SECONDS_PER_DAY = 86400f; // <- CORRETO
    private const float SECONDS_PER_DAY = 60f; // <- Dias reduzidos para 1 minuto para testes

    void Start()
    {
        inputPrayer.text = PlayerPrefs.GetString("prayer_text", "");
        float savedDays = PlayerPrefs.GetFloat("prayer_days", 1f);
        sliderDays.value = Mathf.Clamp(savedDays, 1, FREE_MAX_DAYS);
        textDaysValue.text = $"{sliderDays.value:0} dia(s)";
        inputReminderTime.text = PlayerPrefs.GetString("reminder_time", "08:00");
        ShowSetup(true);
    }

    public void OnDaysSliderChanged()
    {
        if (sliderDays.value > FREE_MAX_DAYS) sliderDays.value = FREE_MAX_DAYS;
        textDaysValue.text = $"{sliderDays.value:0} dia(s)";
    }

    public void OnClickLightCandle()
    {
        string prayer = inputPrayer.text.Trim();
        if (string.IsNullOrEmpty(prayer)) { Debug.LogWarning("Preencha a oração."); return; }
        if (!ValidateTime(inputReminderTime.text)) { Debug.LogWarning("Use HH:MM, ex.: 08:00"); return; }

        PlayerPrefs.SetString("prayer_text", prayer);
        PlayerPrefs.SetFloat("prayer_days", sliderDays.value);
        PlayerPrefs.SetString("reminder_time", inputReminderTime.text);
        PlayerPrefs.Save();

        // DIAS → SEGUNDOS (24h)
        float totalSeconds = sliderDays.value * SECONDS_PER_DAY;

        candleController.StartCandle(totalSeconds);
        textPrayerDisplay.text = $"Oração: {prayer}";
        ShowSetup(false);
    }

    public void OnClickBack() => ShowSetup(true);

    private void ShowSetup(bool show)
    {
        panelSetup.SetActive(show);
        panelCandle.SetActive(!show);
    }

    private bool ValidateTime(string hhmm)
    {
        if (string.IsNullOrWhiteSpace(hhmm)) return false;
        var p = hhmm.Split(':'); if (p.Length != 2) return false;
        return int.TryParse(p[0], out int h) && int.TryParse(p[1], out int m) && h>=0 && h<=23 && m>=0 && m<=59;
    }
}
