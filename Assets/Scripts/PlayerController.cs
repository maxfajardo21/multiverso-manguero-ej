using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public CharacterData data;

    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    public LayerMask enemyLayer;
    public GameObject slashEffect;
    public AudioClip attackSfx;
    public AudioClip jumpSfx;

    public float DamageMultiplier = 1f;
    public float SpeedMultiplier = 1f;
    public bool InputLocked;

    public int Facing { get; private set; } = 1;
    public bool IsGrounded { get; private set; }
    public Health Health { get; private set; }

    public event Action OnBasicAttack;

    public bool CanAct
    {
        get
        {
            bool over = GameManager.Instance != null && GameManager.Instance.GameOver;
            return !InputLocked && !over;
        }
    }

    private Rigidbody2D rb;
    private Vector3 baseScale;
    private float moveInput;
    private float nextAttackTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Health = GetComponent<Health>();
        baseScale = transform.localScale;
        
        if (data != null) 
        {
            Health.SetMax(data.maxHealth);
        }
        
        Health.OnDeath += HandleDeath;
    }

    void OnDestroy()
    {
        if (Health != null) 
        {
            Health.OnDeath -= HandleDeath;
        }
    }

    void Update()
    {
        if (CanAct)
            moveInput = Input.GetAxisRaw("Horizontal");
        else
            moveInput = 0f;

        // Girar el sprite segun la direccion
        if (moveInput > 0.01f) Facing = 1;
        else if (moveInput < -0.01f) Facing = -1;

        if (Mathf.Sign(transform.localScale.x) != Facing)
        {
            transform.localScale = new Vector3(Facing * Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }

        // Revisar si toca el suelo
        if (groundCheck != null)
        {
            IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        }

        if (!CanAct) return;

        // Salto
        if (Input.GetButtonDown("Jump") && IsGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, data.jumpForce);
            AudioManager.Play(jumpSfx);
        }

        // Ataque con la J
        if (Input.GetKeyDown(KeyCode.J) && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + data.attackCooldown;
            int dmg = Mathf.RoundToInt(data.attackDamage * DamageMultiplier);
            Hit(dmg, data.attackRange);
            AudioManager.Play(attackSfx);
            
            if (OnBasicAttack != null) OnBasicAttack.Invoke();
        }
    }

    void FixedUpdate()
    {
        float speed = data.moveSpeed * SpeedMultiplier;
        rb.velocity = new Vector2(moveInput * speed, rb.velocity.y);
    }

    public int Hit(int damage, float range, float knockback = 6f)
    {
        Vector2 hitPos = (Vector2)transform.position + new Vector2(Facing * range * 0.5f, 0f);
        float hitRadius = range * 0.6f;

        if (slashEffect != null)
        {
            GameObject fx = Instantiate(slashEffect, hitPos, Quaternion.identity);
            fx.transform.localScale = new Vector3(Facing * range, range, 1f);
            Destroy(fx, 0.15f);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitPos, hitRadius, enemyLayer);
        int count = 0;

        foreach (Collider2D c in hits)
        {
            Health h = c.GetComponent<Health>();
            if (h != null && !h.IsDead)
            {
                h.TakeDamage(damage, gameObject);
                EnemyAI ai = c.GetComponent<EnemyAI>();
                if (ai != null) 
                {
                    ai.Knockback(new Vector2(Facing * knockback, 3f));
                }
                count++;
            }
        }
        return count;
    }

    void HandleDeath(Health h)
    {
        InputLocked = true;
        rb.velocity = Vector2.zero;
        Health.SetColor(Color.gray);
        
        if (GameManager.Instance != null) 
        {
            GameManager.Instance.PlayerDied();
        }
    }
}