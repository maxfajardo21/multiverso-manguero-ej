using UnityEngine;
[RequireComponent(typeof(Collider2D))]
public class DamageZone : MonoBehaviour
{
    public int damage = 10;
    public float tickInterval = 0.5f;
    public bool instantKill;
    public bool affectEnemies;      

    float nextTick;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Health h = other.GetComponent<Health>();
        if (h == null || h.IsDead) return;

        bool isPlayer = other.CompareTag("Player");
        if (!isPlayer && !affectEnemies) return;

        if (!instantKill && Time.time < nextTick) return;

        h.TakeDamage(instantKill ? 9999 : damage, gameObject);
        if (!instantKill) nextTick = Time.time + tickInterval;
    }
}
