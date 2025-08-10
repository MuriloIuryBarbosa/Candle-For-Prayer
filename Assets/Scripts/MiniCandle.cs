using UnityEngine;

/// Mini visual da vela no menu.
/// Lê o que está salvo (endUnix + dias) e apenas atualiza a escala do corpo
/// e posiciona wick + FlameGroup em conjunto (NÃO mexe nos filhos da chama).
public class MiniCandle : MonoBehaviour
{
    [Header("Referências 3D")]
    public Transform candleBody;
    public Transform candleWick;
    public Transform flameGroup;

    [Header("Offsets (WORLD) – iguais ao CandleController")]
    public float wickAboveTop = 0.03f;
    public float flameAboveWick = 0.02f;
    public float flameOverlapIntoWick = 0.01f;

    [Header("Configurações")]
    [Tooltip("Altura mínima visual da vela quando 'quase acabando'.")]
    public float minBodyLocalY = 0.10f;

    // chaves PlayerPrefs (mantenha iguais ao CandleApp)
    const string K_ACTIVE   = "candle_active";
    const string K_ENDUNIX  = "candle_end_unix";
    const string K_DAYS     = "prayer_days";

    // use o mesmo valor que você usa no CandleApp (60 para testes, 86400 em produção)
    public const float SECONDS_PER_DAY = 60f;

    Renderer _bodyR;
    Renderer _wickR;
    float _initialBodyLocalY = 1f;

    void Awake()
    {
        if (!candleBody || !candleWick || !flameGroup)
        {
            Debug.LogError("[MiniCandle] Arraste Body/Wick/FlameGroup no Inspector.");
            enabled = false;
            return;
        }

        _bodyR = candleBody.GetComponent<Renderer>() ?? candleBody.GetComponentInChildren<Renderer>();
        _wickR = candleWick.GetComponent<Renderer>() ?? candleWick.GetComponentInChildren<Renderer>();

        _initialBodyLocalY = candleBody.localScale.y;

        // Garantir que o FlameGroup é movido como bloco único (não mexemos nos filhos)
        // Nenhuma alteração extra é necessária aqui.
    }

    void Update()
    {
        // Se não tem vela ativa: esconde só a chama; deixa o corpo em tamanho cheio.
        if (PlayerPrefs.GetInt(K_ACTIVE, 0) != 1)
        {
            SetBodyScale(_initialBodyLocalY);
            if (flameGroup.gameObject.activeSelf) flameGroup.gameObject.SetActive(false);
            SnapToTop(); // ainda posiciona wick (fica ok visualmente)
            return;
        }

        // Há uma vela ativa: calcula quanto resta
        long end = (long)PlayerPrefs.GetFloat(K_ENDUNIX, 0f);
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long remaining = System.Math.Max(0L, end - now);

        float days = PlayerPrefs.GetFloat(K_DAYS, 1f);
        float totalSeconds = Mathf.Max(1f, days * SECONDS_PER_DAY);
        float t = Mathf.Clamp01(remaining / totalSeconds); // fração 1..0

        // Escala do corpo (derretendo)
        float newY = Mathf.Lerp(minBodyLocalY, _initialBodyLocalY, t);
        SetBodyScale(newY);

        // Garante que a chama aparece e acompanha o topo
        if (!flameGroup.gameObject.activeSelf) flameGroup.gameObject.SetActive(true);
        SnapToTop();
    }

    void SetBodyScale(float y)
    {
        var s = candleBody.localScale;
        s.y = y;
        candleBody.localScale = s;
    }

    /// Posiciona wick e o FlameGroup JUNTOS no topo (WORLD), preservando o formato da chama.
    void SnapToTop()
    {
        if (_bodyR == null || _wickR == null) return;

        float bodyTopY = _bodyR.bounds.max.y;
        Vector3 centerXZ = _bodyR.bounds.center; centerXZ.y = 0f;

        float wickHalfWorld = _wickR.bounds.extents.y;
        float targetWickTopY = bodyTopY + wickAboveTop;
        float wickCenterY = targetWickTopY - wickHalfWorld;
        candleWick.position = new Vector3(centerXZ.x, wickCenterY, centerXZ.z);

        float flameY = targetWickTopY - flameOverlapIntoWick + flameAboveWick;
        flameGroup.position = new Vector3(centerXZ.x, flameY, centerXZ.z);
        // Perceba: não tocamos em flameGroup.localScale nem nos filhos -> formato permanece.
    }
}
