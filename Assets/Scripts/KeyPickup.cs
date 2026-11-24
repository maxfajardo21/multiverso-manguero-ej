using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D o)
    {
        if (!o.CompareTag("Player")) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddKey();
        }

        LevelPortal portal = FindObjectOfType<LevelPortal>();
        if (portal != null)
        {
            portal.RegisterKey(1);
        }

        Destroy(gameObject);
    }
}