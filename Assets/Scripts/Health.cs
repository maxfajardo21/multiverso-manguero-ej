using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public float invulnerabilityTime = 0.2f;
    public AudioClip hurtSfx;

    public int Current { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamaged;
    public event Action<Health> OnDeath;

    public Func<int, GameObject, int> DamageModifier;

    public Color BaseColor { get; private set; } = Color.white;
    public Color OriginalColor { get; private set; } = Color.white;

    private SpriteRenderer sr;
    private float lastHitTime = -99f;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) 
        { 
            BaseColor = sr.color; 
            OriginalColor = sr.color; 
        }
        Current = maxHealth;
    }

    void Start()
    {
        if (OnHealthChanged != null)
        {
            OnHealthChanged.Invoke(Current, maxHealth);
        }
    }

    public void SetMax(int max)
    {
        maxHealth = max;
        Current = max;
        if (OnHealthChanged != null)
        {
            OnHealthChanged.Invoke(Current, maxHealth);
        }
    }

    public void SetColor(Color c)
    {
        BaseColor = c;
        if (sr != null) sr.color = c;
    }

    public void TakeDamage(int amount, GameObject source = null)
    {
        if (IsDead) return;
        if (Time.time - lastHitTime < invulnerabilityTime) return;

        if (DamageModifier != null) 
        {
            amount = DamageModifier(amount, source);
        }
        
        if (amount <= 0) return;

        lastHitTime = Time.time;
        Current = Mathf.Max(0, Current - amount);
        
        if (OnDamaged != null) OnDamaged.Invoke(amount);
        AudioManager.Play(hurtSfx);
        
        if (OnHealthChanged != null) OnHealthChanged.Invoke(Current, maxHealth);

        if (sr != null && gameObject.activeInHierarchy) 
        {
            StartCoroutine(Flash());
        }

        if (Current == 0)
        {
            IsDead = true;
            if (OnDeath != null) OnDeath.Invoke(this);
        }
    }

    public void SelfDamage(int amount)
    {
        if (IsDead || amount <= 0) return;
        Current = Mathf.Max(1, Current - amount);
        if (OnHealthChanged != null) OnHealthChanged.Invoke(Current, maxHealth);
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        if (OnHealthChanged != null) OnHealthChanged.Invoke(Current, maxHealth);
    }

    IEnumerator Flash()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (sr != null) sr.color = BaseColor;
    }
}