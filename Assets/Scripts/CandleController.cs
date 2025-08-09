using UnityEngine;

public class CandleController : MonoBehaviour
{
    [Header("Referências")]
    public Transform candleBody;     // corpo (Cylinder)
    public Transform candleWick;     // pavio (Cylinder fino)
    public Transform flameGroup;     // Empty com TODAS as esferas + 1 Point Light

    private Renderer bodyRenderer;
    private Renderer wickRenderer;

    [Header("Tempo")]
    public float totalSeconds = 0f;   // duração total em segundos
    private float remaining;
    private bool isLit = false;

    [Header("Offsets (WORLD)")]
    [Tooltip("Topo do pavio acima do topo da vela.")]
    public float wickAboveTop = 0.03f;
    [Tooltip("Quanto a chama fica acima da ponta do pavio.")]
    public float flameAboveWick = 0.02f;
    [Tooltip("Quanto a chama 'entra' no pavio.")]
    public float flameOverlapIntoWick = 0.01f;

    // estados iniciais
    private float initialBodyLocalY = 1f;
    private Vector3 initialFlameGroupLocalScale = Vector3.one;

    // --- trava de formato dos filhos do FlameGroup
    private Transform[] _flameChildren;
    private Vector3[]   _childLocalPos;
    private Quaternion[] _childLocalRot;
    private Vector3[]   _childLocalScale;

    void Awake()
    {
        if (!candleBody || !candleWick || !flameGroup)
        {
            Debug.LogError("[CandleController] Arraste Body/Wick/FlameGroup no Inspector.");
            enabled = false;
            return;
        }

        bodyRenderer = candleBody.GetComponent<Renderer>() ?? candleBody.GetComponentInChildren<Renderer>();
        wickRenderer = candleWick.GetComponent<Renderer>() ?? candleWick.GetComponentInChildren<Renderer>();

        initialBodyLocalY = candleBody.localScale.y;
        initialFlameGroupLocalScale = flameGroup.localScale;

        // Cache do formato dos filhos (o shape que você montou no Editor)
        int n = flameGroup.childCount;
        _flameChildren   = new Transform[n];
        _childLocalPos   = new Vector3[n];
        _childLocalRot   = new Quaternion[n];
        _childLocalScale = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            var t = flameGroup.GetChild(i);
            _flameChildren[i]   = t;
            _childLocalPos[i]   = t.localPosition;
            _childLocalRot[i]   = t.localRotation;
            _childLocalScale[i] = t.localScale;
        }

        // Começa já encaixado no topo
        SnapFlameToTop();
        // Deixa invisível até acender
        SetFlameChildrenActive(false);
    }

    /// <summary>Inicia a vela por 'seconds' segundos.</summary>
    public void StartCandle(float seconds)
    {
        totalSeconds = Mathf.Max(1f, seconds);
        remaining = totalSeconds;
        isLit = true;

        // Reseta altura do corpo
        var s = candleBody.localScale; s.y = initialBodyLocalY; candleBody.localScale = s;

        // Restaura escala do grupo e posiciona no topo no frame 0
        flameGroup.localScale = initialFlameGroupLocalScale;
        SnapFlameToTop();

        // Garante que os filhos voltem ao shape salvo
        ReapplyFlameChildrenShape();

        SetFlameChildrenActive(true);
    }

    void Update()
    {
        if (!isLit) return;

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);

        // Derretimento linear do corpo no eixo Y local
        float t = remaining / totalSeconds;               // 1 → 0
        float newY = Mathf.Lerp(0.1f, initialBodyLocalY, t);
        var s = candleBody.localScale; s.y = newY; candleBody.localScale = s;

        // Acompanha o topo da vela
        SnapFlameToTop();

        // Reaplica o shape para impedir que qualquer script nos FILHOS mexa no formato
        ReapplyFlameChildrenShape();

        if (remaining <= 0f)
        {
            isLit = false;
            SetFlameChildrenActive(false);
        }
    }

    /// <summary>Encaixa o pavio e o grupo da chama no topo da vela (WORLD).</summary>
    private void SnapFlameToTop()
    {
        if (bodyRenderer == null || wickRenderer == null) return;

        // Topo da vela em WORLD
        float bodyTopY = bodyRenderer.bounds.max.y;
        Vector3 centerXZ = bodyRenderer.bounds.center; centerXZ.y = 0f;

        // Pavio: topo do pavio = topo da vela + offset
        float wickHalfWorld = wickRenderer.bounds.extents.y;
        float targetWickTopY = bodyTopY + wickAboveTop;
        float wickCenterY = targetWickTopY - wickHalfWorld;
        candleWick.position = new Vector3(centerXZ.x, wickCenterY, centerXZ.z);

        // Chama (grupo inteiro): um pouco “dentro” e um pouco “acima” da ponta do pavio
        float flameY = targetWickTopY - flameOverlapIntoWick + flameAboveWick;
        flameGroup.position = new Vector3(centerXZ.x, flameY, centerXZ.z);
    }

    /// <summary>Ativa/Desativa todas as esferas/luz do grupo.</summary>
    private void SetFlameChildrenActive(bool active)
    {
        if (!flameGroup) return;

        if (!flameGroup.gameObject.activeSelf)
            flameGroup.gameObject.SetActive(true); // garante o parent ativo

        for (int i = 0; i < flameGroup.childCount; i++)
        {
            var child = flameGroup.GetChild(i);
            if (child != null) child.gameObject.SetActive(active);
        }
    }

    /// <summary>Reaplica a pose local salva dos filhos (congela o formato).</summary>
    private void ReapplyFlameChildrenShape()
    {
        if (_flameChildren == null) return;

        for (int i = 0; i < _flameChildren.Length; i++)
        {
            var t = _flameChildren[i];
            if (t == null) continue;

            // Se algum script mexeu no Transform do filho, voltamos para a pose local original.
            t.localPosition = _childLocalPos[i];
            t.localRotation = _childLocalRot[i];
            t.localScale    = _childLocalScale[i];
        }
    }
}
