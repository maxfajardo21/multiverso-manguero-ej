using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 14f;
    public float lifetime = 2f;
    public LayerMask groundLayer;

    private int damage;
    private Vector2 direction = Vector2.right;

    public void Launch(int facing, int dmg)
    {
        damage = dmg;
        direction = new Vector2(facing, 0f);
        
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(facing * Mathf.Abs(s.x), s.y, s.z);
        
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            Health h = other.GetComponent<Health>();
            if (h != null && !h.IsDead)
            {
                h.TakeDamage(damage, gameObject);
                enemy.Knockback(new Vector2(direction.x * 4f, 2f));
                Destroy(gameObject);
            }
            return;
        }

        if ((groundLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            Destroy(gameObject);
        }
    }
}