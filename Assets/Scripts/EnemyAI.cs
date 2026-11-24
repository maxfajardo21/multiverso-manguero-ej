using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyAI : MonoBehaviour
{
    [Header("Datos Generales")]
    public string enemyName = "Enemigo";
    public int maxHealth = 30;
    public int scoreValue = 100;
    public bool isFinalBoss = false;

    [Header("Movimiento y Rango")]
    public float moveSpeed = 2.5f;
    public float detectionRange = 8f;
    public float attackRange = 1.1f;
    public float maxVerticalRange = 1.2f;

    [Header("Combate")]
    public int damage = 10;
    public float attackCooldown = 1.5f;
    public float attackWindup = 0.4f;
    public bool isDisciple = false;

    [Header("Objetivo en Modo_Musashi")]
    public string musashiObjectiveMessage = "Proteje al niño y derrota el jefe final";

    [Header("Audio")]
    public AudioClip attackSfx;
    public AudioClip deathSfx;

    public event System.Action OnAttackHit;

    private Rigidbody2D rb;
    private Health health;
    private SpriteRenderer sr;
    private Transform target;
    private Health targetHealth;
    private Transform playerT;
    private Health playerH;
    private Vector3 baseScale;
    private float nextAttackTime;
    private float stunUntil;
    private bool attacking;

    private static bool musashiObjectiveShownThisLoad = false;
    private static string activeFallbackObjectiveText = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticState()
    {
        musashiObjectiveShownThisLoad = false;
        activeFallbackObjectiveText = "";
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        sr = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;

        if (health != null)
        {
            health.SetMax(maxHealth);
            health.OnDeath += Die;
        }
    }

    void Start()
    {
        if (sr == null)
        {
            sr = GetComponentInChildren<SpriteRenderer>();
        }

        if (!isDisciple && sr != null && sr.sprite != null)
        {
            if (sr.sprite.name.ToLower().Contains("discipulo"))
            {
                isDisciple = true;
            }
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerT = player.transform;
            playerH = player.GetComponent<Health>();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SendMessage("RegisterEnemy", SendMessageOptions.DontRequireReceiver);
        }

        ShowMusashiObjectiveIfNeeded();
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= Die;
        }
    }

    void OnGUI()
    {
        if (!string.IsNullOrEmpty(activeFallbackObjectiveText) && SceneManager.GetActiveScene().name == "Modo_Musashi")
        {
            float w = 520f;
            float h = 42f;
            Rect rect = new Rect((Screen.width - w) * 0.5f, 24f, w, h);
            GUI.Box(rect, activeFallbackObjectiveText);
        }
    }

    void ShowMusashiObjectiveIfNeeded()
    {
        if (musashiObjectiveShownThisLoad) return;
        if (SceneManager.GetActiveScene().name != "Modo_Musashi") return;

        musashiObjectiveShownThisLoad = true;
        activeFallbackObjectiveText = musashiObjectiveMessage;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SendMessage("SetObjectiveMessage", musashiObjectiveMessage, SendMessageOptions.DontRequireReceiver);
            GameManager.Instance.SendMessage("ShowMessage", musashiObjectiveMessage, SendMessageOptions.DontRequireReceiver);
        }

        UIManager ui = FindObjectOfType<UIManager>();
        if (ui != null)
        {
            ui.SendMessage("SetObjectiveMessage", musashiObjectiveMessage, SendMessageOptions.DontRequireReceiver);
            ui.SendMessage("ShowMessage", musashiObjectiveMessage, SendMessageOptions.DontRequireReceiver);
            if (ui.messageText != null)
            {
                ui.messageText.text = musashiObjectiveMessage;
                activeFallbackObjectiveText = "";
            }
        }
    }

    void ChooseTarget()
    {
        target = null;
        targetHealth = null;

        if (isDisciple)
        {
            if (NinoShield.Instance != null)
            {
                Health nh = NinoShield.Instance.GetComponent<Health>();
                if (nh != null && !nh.IsDead)
                {
                    float vDist = Mathf.Abs(NinoShield.Instance.transform.position.y - transform.position.y);
                    if (vDist <= maxVerticalRange * 2f)
                    {
                        target = NinoShield.Instance.transform;
                        targetHealth = nh;
                    }
                }
            }
            return;
        }

        if (playerT != null && playerH != null && !playerH.IsDead)
        {
            float vDist = Mathf.Abs(playerT.position.y - transform.position.y);
            if (vDist <= maxVerticalRange * 2f)
            {
                target = playerT;
                targetHealth = playerH;
            }
        }
    }

    void FixedUpdate()
    {
        if (health != null && health.IsDead) return;
        if (Time.time < stunUntil || attacking) return;

        ChooseTarget();
        if (target == null || targetHealth == null)
        {
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        float dx = target.position.x - transform.position.x;
        float dy = target.position.y - transform.position.y;
        float dist = Mathf.Abs(dx);
        float vDist = Mathf.Abs(dy);

        float dir = dx < 0f ? -1f : 1f;
        transform.localScale = new Vector3(dir * Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);

        if (dist > detectionRange || vDist > maxVerticalRange * 2f)
        {
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        }
        else if (dist > attackRange || vDist > maxVerticalRange)
        {
            if (rb != null) rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
        }
        else
        {
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
            }
        }
    }

    IEnumerator AttackRoutine()
    {
        attacking = true;
        float startTime = Time.time;
        if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);

        if (sr != null) sr.color = Color.yellow;
        yield return new WaitForSeconds(attackWindup);

        if (health != null && health.IsDead) yield break;
        if (sr != null && health != null) sr.color = health.BaseColor;

        bool interrupted = stunUntil > startTime;
        if (!interrupted && target != null && targetHealth != null)
        {
            if (OnAttackHit != null) OnAttackHit();

            float dist = Mathf.Abs(target.position.x - transform.position.x);
            float vDist = Mathf.Abs(target.position.y - transform.position.y);
            if (dist <= attackRange * 1.3f && vDist <= maxVerticalRange)
            {
                NinoShield ninoShield = targetHealth.GetComponent<NinoShield>();
                if (ninoShield != null && ninoShield.TryAbsorbDamage(damage, gameObject))
                {
                    AudioManager.Play(attackSfx);
                }
                else
                {
                    targetHealth.TakeDamage(damage, gameObject);
                    AudioManager.Play(attackSfx);
                }
            }
        }

        nextAttackTime = Time.time + attackCooldown;
        attacking = false;
    }

    public void Knockback(Vector2 force)
    {
        if (health != null && health.IsDead) return;
        stunUntil = Time.time + 0.25f;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    void Die(Health h)
    {
        attacking = false;
        AudioManager.Play(deathSfx);
        if (sr != null) sr.color = Color.gray;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SendMessage("EnemyDefeated", scoreValue, SendMessageOptions.DontRequireReceiver);
            if (isFinalBoss)
            {
                GameManager.Instance.SendMessage("BossDefeated", gameObject, SendMessageOptions.DontRequireReceiver);
            }
        }
        Destroy(gameObject, 0.4f);
    }
}