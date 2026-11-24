using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
    [Header("Partes visuales")]
    public Transform body;
    public Transform weaponPivot;
    public SpriteRenderer aura;
    public Color auraColor = Color.white;
    public float weaponRestAngle = 30f;

    private Rigidbody2D rb;
    private Health health;
    private ModeAbility ability;
    private PlayerController player;
    private EnemyAI enemy;
    private Vector3 bodyBase;
    private Vector3 auraBase = Vector3.one;
    private float weaponAngle;
    private float swingT = -1f;
    private float lungeT = -1f;
    private bool dead;

    private const float SwingTime = 0.18f;
    private const float LungeTime = 0.2f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        ability = GetComponent<ModeAbility>();
        player = GetComponent<PlayerController>();
        enemy = GetComponent<EnemyAI>();

        if (body != null) bodyBase = body.localPosition;
        if (aura != null) auraBase = aura.transform.localScale;
        
        weaponAngle = weaponRestAngle;
    }

    void Start()
    {
        if (player != null) player.OnBasicAttack += Swing;
        if (enemy != null) enemy.OnAttackHit += Swing;
        if (health != null) health.OnDeath += OnDeath;
        
        if (weaponPivot != null) 
        {
            weaponPivot.localRotation = Quaternion.Euler(0, 0, weaponRestAngle);
        }
    }

    void OnDestroy()
    {
        if (player != null) player.OnBasicAttack -= Swing;
        if (enemy != null) enemy.OnAttackHit -= Swing;
        if (health != null) health.OnDeath -= OnDeath;
    }

    public void Swing()
    {
        swingT = 0f;
        lungeT = 0f;
    }

    void OnDeath(Health h)
    {
        dead = true;
        if (aura != null) aura.enabled = false;
    }

    void Update()
    {
        if (body == null) return;
        float dt = Time.deltaTime;

        if (dead)
        {
            body.localRotation = Quaternion.Lerp(body.localRotation, Quaternion.Euler(0, 0, -85f), 8f * dt);
            body.localPosition = Vector3.Lerp(body.localPosition, bodyBase + new Vector3(0f, -0.75f, 0f), 6f * dt);
            return;
        }

        float vx = 0f;
        if (rb != null) vx = Mathf.Abs(rb.velocity.x);
        
        bool moving = vx > 0.2f;
        float t = Time.time;

        float bob = 0f;
        if (moving) 
            bob = Mathf.Abs(Mathf.Sin(t * 12f)) * 0.07f;
        else 
            bob = Mathf.Sin(t * 2.5f) * 0.02f;

        float lean = 0f;
        if (moving) lean = -6f;

        float lunge = 0f;
        if (lungeT >= 0f)
        {
            lungeT += dt;
            float k = Mathf.Clamp01(lungeT / LungeTime);
            lunge = Mathf.Sin(k * Mathf.PI) * 0.25f;
            lean -= Mathf.Sin(k * Mathf.PI) * 8f;
            if (k >= 1f) lungeT = -1f;
        }

        body.localPosition = bodyBase + new Vector3(lunge, bob, 0f);
        body.localRotation = Quaternion.Lerp(body.localRotation, Quaternion.Euler(0, 0, lean), 10f * dt);

        if (weaponPivot != null)
        {
            if (swingT >= 0f)
            {
                swingT += dt;
                float k = Mathf.Clamp01(swingT / SwingTime);
                weaponAngle = Mathf.Lerp(weaponRestAngle + 85f, weaponRestAngle - 115f, k);
                if (k >= 1f) swingT = -1f;
            }
            else
            {
                weaponAngle = Mathf.LerpAngle(weaponAngle, weaponRestAngle, 12f * dt);
            }
            weaponPivot.localRotation = Quaternion.Euler(0, 0, weaponAngle);
        }

        if (aura != null && ability != null)
        {
            float m = Mathf.Clamp01(ability.MeterNormalized);
            float pulse = 1f + Mathf.Sin(t * 8f) * 0.06f * m;
            aura.color = new Color(auraColor.r, auraColor.g, auraColor.b, m * 0.75f);
            aura.transform.localScale = auraBase * pulse;
        }
    }
}