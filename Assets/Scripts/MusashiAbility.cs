using UnityEngine;

public class MusashiAbility : ModeAbility
{
    public float parryWindow = 0.35f;
    public float parryCooldown = 1f;
    public float counterMultiplier = 3f;
    public Color parryColor = Color.cyan;

    public int kiPerParry = 1;
    public int kiMax = 3;
    public float deadlyMultiplier = 100f;
    public Color kiFullColor = new Color(1f, 0.9f, 0.4f);

    public AudioClip parrySfx;
    public AudioClip deadlySfx;

    private bool parrying;
    private float parryEndTime;
    private float nextParryTime;
    private int ki;
    private bool deadlyReady;

    public override float MeterNormalized { get { return (float)ki / kiMax; } }
    
    public override string StatusText 
    { 
        get 
        { 
            if (deadlyReady) return "KI LLENO: corte mortal!";
            return "Ki " + ki + "/" + kiMax; 
        } 
    }
    
    public override string ControlsHint { get { return "A/D mover | Espacio saltar | J katana | K desviar"; } }

    void Start()
    {
        health.DamageModifier = ModifyDamage;
        player.OnBasicAttack += OnBasicAttack;
    }

    void OnDestroy()
    {
        if (player != null) 
        {
            player.OnBasicAttack -= OnBasicAttack;
        }
    }

    // Contraataque al desviar
    int ModifyDamage(int dmg, GameObject source)
    {
        if (!parrying) return dmg;

        if (source != null)
        {
            Health enemyHealth = source.GetComponent<Health>();
            if (enemyHealth != null)
            {
                int counter = Mathf.RoundToInt(player.data.attackDamage * counterMultiplier);
                enemyHealth.TakeDamage(counter, gameObject);
            }
            
            EnemyAI ai = source.GetComponent<EnemyAI>();
            if (ai != null) 
            {
                ai.Knockback(new Vector2(player.Facing * 8f, 3f));
            }
        }

        AudioManager.Play(parrySfx);
        ki = Mathf.Min(kiMax, ki + kiPerParry);
        
        if (ki >= kiMax) 
        {
            SetDeadly(true);
        }
        return 0;
    }

    void Update()
    {
        if (parrying && Time.time >= parryEndTime) 
        {
            EndParry();
        }

        if (!player.CanAct) return;

        if (Input.GetKeyDown(KeyCode.K) && !parrying && Time.time >= nextParryTime)
        {
            parrying = true;
            parryEndTime = Time.time + parryWindow;
            nextParryTime = Time.time + parryCooldown;
            player.InputLocked = true;
            health.SetColor(parryColor);
        }
    }

    void EndParry()
    {
        parrying = false;
        player.InputLocked = false;
        
        if (deadlyReady)
            health.SetColor(kiFullColor);
        else
            health.SetColor(health.OriginalColor);
    }

    void OnBasicAttack()
    {
        if (!deadlyReady) return;
        
        AudioManager.Play(deadlySfx);
        ki = 0;
        SetDeadly(false);
    }

    void SetDeadly(bool value)
    {
        deadlyReady = value;
        if (value)
            player.DamageMultiplier = deadlyMultiplier;
        else
            player.DamageMultiplier = 1f;

        if (!parrying)
        {
            if (value)
                health.SetColor(kiFullColor);
            else
                health.SetColor(health.OriginalColor);
        }
    }
}