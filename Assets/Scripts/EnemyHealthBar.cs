using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyHealthBar : MonoBehaviour
{
    public float width = 0.9f;
    public float yOffset = 0.3f;

    Health health;
    Collider2D col;
    Transform bar;
    Transform fill;

    static Sprite Make(Vector2 pivot)
    {
        Texture2D t = Texture2D.whiteTexture;
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, t.width);
    }

    void Awake()
    {
        health = GetComponent<Health>();
        col = GetComponent<Collider2D>();

        bar = new GameObject("BarraVida (" + name + ")").transform;

        GameObject bg = new GameObject("Fondo");
        bg.transform.SetParent(bar, false);
        bg.transform.localScale = new Vector3(width + 0.06f, 0.14f, 1f);
        SpriteRenderer bsr = bg.AddComponent<SpriteRenderer>();
        bsr.sprite = Make(new Vector2(0.5f, 0.5f));
        bsr.color = new Color(0f, 0f, 0f, 0.7f);
        bsr.sortingOrder = 20;

        GameObject fg = new GameObject("Relleno");
        fg.transform.SetParent(bar, false);
        fg.transform.localPosition = new Vector3(-width / 2f, 0f, 0f);
        fg.transform.localScale = new Vector3(width, 0.09f, 1f);
        SpriteRenderer fsr = fg.AddComponent<SpriteRenderer>();
        fsr.sprite = Make(new Vector2(0f, 0.5f));
        fsr.color = new Color(0.9f, 0.2f, 0.2f, 1f);
        fsr.sortingOrder = 21;
        fill = fg.transform;

        health.OnHealthChanged += Refresh;
        health.OnDeath += OnDeath;
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= Refresh;
            health.OnDeath -= OnDeath;
        }
        if (bar != null) Destroy(bar.gameObject);
    }

    void OnDeath(Health h)
    {
        if (bar != null) bar.gameObject.SetActive(false);
    }

    void Refresh(int current, int max)
    {
        if (fill == null || max <= 0) return;
        float ratio = Mathf.Clamp01((float)current / max);
        fill.localScale = new Vector3(width * ratio, 0.09f, 1f);
    }

    void LateUpdate()
    {
        if (bar == null) return;
        float top = (col != null && col.enabled) ? col.bounds.max.y : transform.position.y + 1f;
        bar.position = new Vector3(transform.position.x, top + yOffset, 0f);
    }
}
