// Assets/Scripts/AdsManager.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;

/// Manager de anúncios com Unity Ads (Interstitial).
/// - Inicializa Unity Ads (gameId por plataforma, testMode opcional)
/// - Faz preload do interstitial
/// - Exibe no startup e ao retornar do background
/// - Recarrega automaticamente após cada exibição
public class AdsManager : MonoBehaviour,
    IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    public static AdsManager Instance { get; private set; }

    [Header("Unity Ads")]
    [Tooltip("Game ID Android, pegue no Dashboard da Unity (Operate → Monetization).")]
    [SerializeField] private string androidGameId = "5920288";

    [Tooltip("Game ID iOS, pegue no Dashboard da Unity.")]
    [SerializeField] private string iOSGameId = "5920289";

    [Tooltip("Placement ID Interstitial no Android (ex.: Interstitial_Android).")]
    [SerializeField] private string androidInterstitialPlacementId = "Interstitial_Android";

    [Tooltip("Placement ID Interstitial no iOS (ex.: Interstitial_iOS).")]
    [SerializeField] private string iOSInterstitialPlacementId = "Interstitial_iOS";

    [Tooltip("Habilite em desenvolvimento; desabilite para loja.")]
    [SerializeField] private bool testMode = true;

    [Header("Preferências")]
    [Tooltip("Se true, não mostra anúncios (ex.: versão sem-ads). Lido também de PlayerPrefs(no_ads).")]
    [SerializeField] public bool removeAds = false;

    [Tooltip("Atraso (s) antes de mostrar anúncio ao retornar do background.")]
    [SerializeField] private float resumeAdDelay = 0.8f;

    // estado interno
    private string _gameId;
    private string _interstitialPlacementId;
    private bool _isInitialized;
    private bool _interstitialLoaded;
    private bool _showing;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // carrega flag de remoção de anúncios (0/1)
        removeAds = removeAds || PlayerPrefs.GetInt("no_ads", 0) == 1;

#if UNITY_ANDROID
        _gameId = androidGameId;
        _interstitialPlacementId = androidInterstitialPlacementId;
#elif UNITY_IOS
        _gameId = iOSGameId;
        _interstitialPlacementId = iOSInterstitialPlacementId;
#else
        _gameId = string.Empty;
        _interstitialPlacementId = string.Empty;
#endif

        if (!removeAds && !string.IsNullOrEmpty(_gameId))
        {
            Advertisement.Initialize(_gameId, testMode, this);
        }
        else
        {
            Debug.Log("[Ads] Ads desabilitados (removeAds) ou plataforma sem Game ID.");
        }
    }

    // -------- API de alto nível --------

    /// Mostra interstitial no começo e espera fechar.
    public IEnumerator ShowStartupAndWait()
    {
        if (removeAds) yield break;
        // aguarda init + preload
        yield return WaitUntilReady();

        bool done = false;
        ShowInterstitial(() => done = true);
        while (!done) yield return null;
    }

    /// Mostra interstitial automaticamente quando o app retorna do background.
    public void ShowOnResume()
    {
        if (removeAds) return;
        StartCoroutine(ShowAfterDelay(resumeAdDelay));
    }

    // -------- Unity lifecycle hooks --------

    void OnApplicationPause(bool pause)
    {
        if (!pause)
            ShowOnResume();
    }

    // -------- Internos --------

    IEnumerator ShowAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        // garante que está carregado
        yield return WaitUntilReady();
        ShowInterstitial();
    }

    IEnumerator WaitUntilReady()
    {
        // espera inicializar
        float t = 0f;
        while (!_isInitialized && t < 10f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        // tenta carregar se ainda não carregado
        if (!_interstitialLoaded)
        {
            Advertisement.Load(_interstitialPlacementId, this);
            t = 0f;
            while (!_interstitialLoaded && t < 10f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    /// Exibe interstitial (se carregado). Recarrega após fechar.
    public void ShowInterstitial(Action onClosed = null)
    {
        if (removeAds) { onClosed?.Invoke(); return; }

        if (!_isInitialized)
        {
            Debug.Log("[Ads] Ainda inicializando; tentando carregar e mostrar depois.");
            StartCoroutine(ShowAfterDelay(1f));
            onClosed?.Invoke();
            return;
        }

        if (_interstitialLoaded && !_showing)
        {
            _showing = true;
            Advertisement.Show(_interstitialPlacementId, this);
            // callback onClosed será chamado dentro de OnUnityAdsShowComplete
            if (onClosed != null) _pendingClosed += onClosed;
        }
        else
        {
            // se não está carregado, tenta carregar e avisa via callback
            Debug.Log("[Ads] Interstitial não carregado; requisitando Load.");
            Advertisement.Load(_interstitialPlacementId, this);
            onClosed?.Invoke();
        }
    }

    // Mantém callbacks de fechamento encadeados (startup e afins)
    private event Action _pendingClosed;

    // --------- LISTENERS Unity Ads ---------

    // Initialization
    public void OnInitializationComplete()
    {
        _isInitialized = true;
        Debug.Log("[Ads] Initialization complete.");
        Advertisement.Load(_interstitialPlacementId, this);
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        _isInitialized = false;
        Debug.LogWarning($"[Ads] Initialization failed: {error} - {message}");
    }

    // Load
    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (placementId == _interstitialPlacementId)
        {
            _interstitialLoaded = true;
            Debug.Log("[Ads] Interstitial loaded.");
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        if (placementId == _interstitialPlacementId)
        {
            _interstitialLoaded = false;
            Debug.LogWarning($"[Ads] Failed to load interstitial: {error} - {message}");
            // tenta de novo depois de um tempinho
            StartCoroutine(RetryLoad(2f));
        }
    }

    IEnumerator RetryLoad(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (!_interstitialLoaded && _isInitialized)
            Advertisement.Load(_interstitialPlacementId, this);
    }

    // Show
    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log("[Ads] Show start.");
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        // opcional: métricas de clique
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        _showing = false;
        Debug.Log($"[Ads] Show complete: {showCompletionState}");

        // recarrega para a próxima
        _interstitialLoaded = false;
        Advertisement.Load(_interstitialPlacementId, this);

        // dispara callbacks pendentes
        _pendingClosed?.Invoke();
        _pendingClosed = null;
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        _showing = false;
        _interstitialLoaded = false;
        Debug.LogWarning($"[Ads] Show failed: {error} - {message}");
        // tenta recarregar
        Advertisement.Load(_interstitialPlacementId, this);

        // ainda assim, solta callbacks para não travar fluxos que aguardam
        _pendingClosed?.Invoke();
        _pendingClosed = null;
    }
}
