using UnityEngine;
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlicker : MonoBehaviour
{
    SpriteRenderer sr;
    float seed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        seed = Random.value * 10f;
    }

    void Update()
    {
        float v = 0.8f + 0.2f * Mathf.PerlinNoise(Time.time * 8f, seed);
        sr.color = new Color(1f, v, v * 0.8f, 1f);
    }
}
