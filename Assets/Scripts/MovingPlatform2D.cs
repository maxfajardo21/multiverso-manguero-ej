using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class MovingPlatform2D : MonoBehaviour
{
    [Header("Movimiento vertical")]
    public float moveDistance = 2.5f;
    public float speed = 2f;
    public bool startMovingUp = true;

    private Vector3 startPos;
    private Vector3 previousPos;
    private Rigidbody2D playerRb;
    private Collider2D platformCol;

    void Awake()
    {
        startPos = transform.position;
        previousPos = startPos;
        platformCol = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        float dir = startMovingUp ? 1f : -1f;
        float offset = Mathf.PingPong(Time.fixedTime * speed, moveDistance) * dir;
        Vector3 targetPos = startPos + Vector3.up * offset;

        Vector3 delta = targetPos - previousPos;
        transform.position = targetPos;
        previousPos = targetPos;

        if (playerRb != null && delta.sqrMagnitude > 0f)
        {
            playerRb.position += new Vector2(delta.x, delta.y);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y < -0.35f && collision.collider.bounds.min.y >= platformCol.bounds.max.y - 0.15f)
                {
                    playerRb = collision.rigidbody;
                    return;
                }
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player") && collision.rigidbody == playerRb)
        {
            playerRb = null;
        }
    }
}