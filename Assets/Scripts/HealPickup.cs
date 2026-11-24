using UnityEngine;

public class HealPickup : MonoBehaviour
{
    public int cantidad = 30;

    void OnTriggerEnter2D(Collider2D o)
    {
        if (!o.CompareTag("Player")) return;
        Health h = o.GetComponent<Health>();
        if (h != null) h.Heal(cantidad);
        Destroy(gameObject);
    }
}