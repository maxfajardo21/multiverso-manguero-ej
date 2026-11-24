using System.Collections;
using UnityEngine;

public class EnemyDropAmbush : MonoBehaviour
{
    [Header("Activación al llegar el jugador")]
    public float triggerRangeX = 7.5f;
    public float minHeightDifference = 1.2f;

    [Header("Forma de bajar")]
    [Tooltip("Si es true, atraviesa la plataforma de arriba. Si es false, avanza al borde para caer.")]
    public bool dropThroughPlatform = true;
    public float walkOffSpeed = 4.5f;
    public float hopForce = 2.5f;
    public float ignorePlatformDuration = 0.45f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D enemyCol;
    private bool triggered;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyCol = GetComponent<Collider2D>();
    }

    void Start()
    {
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null) player = pc.transform;
    }

    void Update()
    {
        if (triggered || player == null) return;

        float dx = Mathf.Abs(player.position.x - transform.position.x);
        float dy = transform.position.y - player.position.y;

        if (dx <= triggerRangeX && dy >= minHeightDifference)
        {
            triggered = true;
            if (dropThroughPlatform)
            {
                StartCoroutine(DropThroughRoutine());
            }
            else
            {
                StartCoroutine(WalkOffEdgeRoutine());
            }
        }
    }

    IEnumerator DropThroughRoutine()
    {
        Collider2D platformCol = GetPlatformBelow();
        if (platformCol != null && enemyCol != null)
        {
            Physics2D.IgnoreCollision(enemyCol, platformCol, true);
        }

        if (rb != null)
        {
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            rb.velocity = new Vector2(dir * 1.5f, -1f);
        }

        yield return new WaitForSeconds(ignorePlatformDuration);

        if (platformCol != null && enemyCol != null)
        {
            Physics2D.IgnoreCollision(enemyCol, platformCol, false);
        }
    }

    IEnumerator WalkOffEdgeRoutine()
    {
        float timer = 0f;
        while (timer < 1.5f)
        {
            timer += Time.deltaTime;
            if (rb != null && player != null)
            {
                float dir = Mathf.Sign(player.position.x - transform.position.x);
                rb.velocity = new Vector2(dir * walkOffSpeed, rb.velocity.y);
            }
            yield return null;
        }
    }

    Collider2D GetPlatformBelow()
    {
        Vector2 origin = enemyCol != null
            ? new Vector2(enemyCol.bounds.center.x, enemyCol.bounds.min.y + 0.05f)
            : (Vector2)transform.position;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 1.0f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider != null && hits[i].collider != enemyCol && !hits[i].collider.isTrigger)
            {
                return hits[i].collider;
            }
        }
        return null;
    }
}