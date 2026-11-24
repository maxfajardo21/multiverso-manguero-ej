using UnityEngine;

[ExecuteAlways]
public class EnemyHealthBarAnchor : MonoBehaviour
{
    [Header("Referencia al Enemigo")]
    public Transform objetivo;

    [Header("Posicionamiento sobre el Sprite")]
    public Vector3 offset = new Vector3(0f, 1.35f, 0f);
    public bool usarAlturaDelSprite = true;
    public float margenExtraY = 0.25f;

    [Header("Estabilidad Física y de Escala")]
    public bool bloquearRotacionEnemigoZ = true;
    public bool forzarEscalaPositiva = true;

    private SpriteRenderer objetivoSprite;
    private Rigidbody2D objetivoRb;

    void OnEnable()
    {
        RefrescarReferencias();
    }

    void OnValidate()
    {
        RefrescarReferencias();
    }

    private void RefrescarReferencias()
    {
        if (objetivo == null && transform.parent != null)
        {
            objetivo = transform.parent;
        }

        if (objetivo != null)
        {
            objetivoSprite = objetivo.GetComponentInChildren<SpriteRenderer>();
            objetivoRb = objetivo.GetComponent<Rigidbody2D>();

            if (bloquearRotacionEnemigoZ && objetivoRb != null)
            {
                objetivoRb.freezeRotation = true;
            }
        }
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        if (objetivoSprite == null)
        {
            objetivoSprite = objetivo.GetComponentInChildren<SpriteRenderer>();
        }

        if (bloquearRotacionEnemigoZ && objetivoRb != null && !objetivoRb.freezeRotation)
        {
            objetivoRb.freezeRotation = true;
        }

        float alturaY = offset.y;

        if (usarAlturaDelSprite && objetivoSprite != null && objetivoSprite.sprite != null)
        {
            Bounds b = objetivoSprite.bounds;
            alturaY = (b.max.y - objetivo.position.y) + margenExtraY;
        }

        transform.position = new Vector3(
            objetivo.position.x + offset.x,
            objetivo.position.y + alturaY,
            objetivo.position.z + offset.z
        );

        transform.rotation = Quaternion.identity;

        if (forzarEscalaPositiva)
        {
            Vector3 esc = transform.localScale;
            esc.x = Mathf.Abs(esc.x);
            esc.y = Mathf.Abs(esc.y);
            esc.z = Mathf.Abs(esc.z);
            transform.localScale = esc;
        }
    }
}