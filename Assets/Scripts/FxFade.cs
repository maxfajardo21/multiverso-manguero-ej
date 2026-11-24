using UnityEngine;
public class FxFade : MonoBehaviour
{
    public float life = 0.15f;

    SpriteRenderer sr;
    Vector3 startScale;
    float t;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;   
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / life);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f - k;
            sr.color = c;
        }
        transform.localScale = startScale * (1f + 0.25f * k);
    }
}
