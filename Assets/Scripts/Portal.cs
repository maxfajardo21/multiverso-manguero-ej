using UnityEngine;

public class Portal : MonoBehaviour
{
    public bool abierto;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Color.gray;
    }

    public void Abrir()
    {
        abierto = true;
        if (sr != null) sr.color = Color.white;
    }

    void Update()
    {
        if (abierto) transform.Rotate(0f, 0f, 90f * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D o)
    {
        if (abierto && o.CompareTag("Player"))
        {
            GameManager.Instance.WinLevel();
        }
    }
}