using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Header("Paineis")]
    public GameObject panelMenu;            // Menu principal
    public GameObject panelSetup;           // Cadastro da vela
    public GameObject panelCandle;          // Vela queimando (3D)
    public GameObject panelConfirmReplace;  // Confirmação “substituir vela?”

    [Header("Referências")]
    public CandleApp candleApp;             // arraste o CandleApp da cena

    void Start()
    {
        Notifications.Initialize();
        ShowMenu();
    }

    // ---------- NAV ----------
    void ShowMenu()
    {
        panelMenu.SetActive(true);
        panelSetup.SetActive(false);
        panelCandle.SetActive(false);
        panelConfirmReplace.SetActive(false);
        candleApp.ShowNone(); // garante que o CandleApp não force nada
    }

    void ShowSetup()
    {
        panelMenu.SetActive(false);
        panelSetup.SetActive(true);
        panelCandle.SetActive(false);
        panelConfirmReplace.SetActive(false);
        candleApp.OpenSetup();
    }

    void ShowCandle()
    {
        panelMenu.SetActive(false);
        panelSetup.SetActive(false);
        panelCandle.SetActive(true);
        panelConfirmReplace.SetActive(false);
        candleApp.OpenCandlePanelOnly();
    }

    void ShowConfirmReplace()
    {
        panelMenu.SetActive(false);
        panelSetup.SetActive(false);
        panelCandle.SetActive(false);
        panelConfirmReplace.SetActive(true);
    }

    // ---------- BOTÕES DO MENU ----------
    // “Acenda sua vela”
    public void OnClickMenuLight()
    {
        bool active = PlayerPrefs.GetInt("candle_active", 0) == 1;
        long end = (long)PlayerPrefs.GetFloat("candle_end_unix", 0f);
        long now = NowUnix();

        if (active && end > now)
        {
            // Já tem vela válida → perguntar se quer substituir
            ShowConfirmReplace();
        }
        else
        {
            // Não tem / expirou → ir direto ao cadastro
            ShowSetup();
        }
    }

    // “Sobre o aplicativo”
    public void OnClickMenuAbout()
    {
        Debug.Log("Sobre: implementar painel/scene de 'Sobre'.");
    }

    // ---------- CONFIRMAR SUBSTITUIÇÃO ----------
    // Botão “Sim” → descarta dados, cancela notificações e vai ao cadastro
    public void OnClickConfirmYes()
    {
        PlayerPrefs.DeleteKey("prayer_text");
        PlayerPrefs.DeleteKey("prayer_days");
        PlayerPrefs.DeleteKey("reminder_time");
        PlayerPrefs.DeleteKey("candle_end_unix");
        PlayerPrefs.SetInt("candle_active", 0);
        PlayerPrefs.Save();
        Notifications.CancelAll();

        ShowSetup();
    }

    // Botão “Não” → continua com a vela atual (abre painel da vela)
    public void OnClickConfirmNo()
    {
        // Tenta retomar a vela salva (posiciona e atualiza o CandleController)
        if (candleApp.TryResumeCandleFromSave())
        {
            ShowCandle();
        }
        else
        {
            // Por segurança, se não houver o que retomar, volta ao menu
            ShowMenu();
        }
    }

    // ---------- OUTROS BOTÕES ----------
    // Use este no botão “Voltar” dentro do painel da vela
    public void OnClickBackToMenu()
    {
        ShowMenu();
    }

    // Botão “Continuar oração” (se existir no menu)
    public void OnClickContinuePrayer()
    {
        if (candleApp.TryResumeCandleFromSave())
            ShowCandle();
        else
            ShowSetup();
    }

    static long NowUnix() => System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
