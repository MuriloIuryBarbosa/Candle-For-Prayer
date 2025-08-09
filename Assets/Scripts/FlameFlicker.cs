using UnityEngine;

public class FlameFlicker : MonoBehaviour
{
    [Header("Referências (opcional)")]
    public Light flameLight;

    [Header("Tremular")]
    public float noiseSpeed = 3f;
    public float posJitterY = 0.015f; // só posição Y (sem rotação)

    [Header("Luz (se houver)")]
    public float lightBase = 2.2f;
    public float lightJitter = 1.0f;

    [Header("Emissão (Material)")]
    public bool matchLightColor = true;
    public float emissionBase = 1.8f;
    public float emissionJitter = 0.8f;

    Renderer[] _renderers;
    Material[] _mats;
    float _seed;
    Vector3 _baseWorldPos;   // posição-base vinda do CandleController
    Quaternion _baseWorldRot; // guardamos só pra comparar (não vamos aplicar)

    void Awake()
    {
        _seed = Random.Range(0f, 1000f);
        _baseWorldPos = transform.position;
        _baseWorldRot = transform.rotation;

        if (flameLight == null) flameLight = GetComponentInChildren<Light>();

        _renderers = GetComponentsInChildren<Renderer>();
        if (_renderers != null && _renderers.Length > 0)
        {
            _mats = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _mats[i] = _renderers[i].material;
                if (_mats[i] != null) _mats[i].EnableKeyword("_EMISSION");
            }
        }
    }

    void LateUpdate()
    {
        // Se o CandleController reposicionou o grupo, adotamos como nova base
        if ((transform.position - _baseWorldPos).sqrMagnitude > 0.0001f)
            _baseWorldPos = transform.position;

        if (Quaternion.Angle(transform.rotation, _baseWorldRot) > 0.1f)
            _baseWorldRot = transform.rotation; // só atualiza a referência; NÃO aplicamos rotação

        float t = Time.time * noiseSpeed;
        float n1 = Mathf.PerlinNoise(_seed + t, _seed * 0.37f); // 0..1

        // ===== SÓ POSIÇÃO Y (sem tocar na rotação) =====
        Vector3 worldPos = _baseWorldPos;
        worldPos.y += (n1 - 0.5f) * 2f * posJitterY;
        transform.position = worldPos;

        // Luz opcional
        if (flameLight != null)
            flameLight.intensity = lightBase + n1 * lightJitter;

        // Emissão nas esferas-filhas
        if (_mats != null)
        {
            Color baseCol = (matchLightColor && flameLight != null) ? flameLight.color : Color.yellow;
            float intensity = emissionBase + n1 * emissionJitter;
            Color dir = baseCol.maxColorComponent > 0 ? baseCol / baseCol.maxColorComponent : Color.white;
            Color emissive = dir * intensity;

            for (int i = 0; i < _mats.Length; i++)
                if (_mats[i] != null) _mats[i].SetColor("_EmissionColor", emissive);
        }
    }
}
