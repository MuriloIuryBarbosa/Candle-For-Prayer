using UnityEngine;

public class CloudFloat : MonoBehaviour
{
    public float speed = 0.2f;          // vel. horizontal
    public float ampY = 0.1f;           // amplitude vertical
    public float freqY = 0.2f;          // frequência do sobe/desce
    public float loopX = 8f;            // largura para "wrap"

    Vector3 startPos;
    float seed;

    void Start()
    {
        startPos = transform.position;
        seed = Random.Range(0f, 1000f);
    }

    void Update()
    {
        // move X
        float x = transform.position.x + speed * Time.deltaTime;
        if (x > startPos.x + loopX) x = startPos.x - loopX; // reaparece do lado esquerdo

        // leve “flutuar” no Y
        float y = startPos.y + Mathf.Sin((Time.time + seed) * freqY) * ampY;

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
