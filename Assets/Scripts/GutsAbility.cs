using UnityEngine;

public class GutsAbility : ModeAbility
{
    public float maxRage = 100f;
    public float minRageToActivate = 25f;
    public float rageGainPerDamage = 2f;
    public float berserkDuration = 8f;
    public float damageMultiplier = 2f;
    public float speedMultiplier = 1.3f;
    public float healthDrainPerSecond = 2f;
    public Color berserkColor = new Color(0.7f, 0.15f, 0.15f);

    public Projectile boltPrefab;
    public int boltDamage = 20;
    public float crossbowCooldown = 0.8f;
    public Transform firePoint;
    public AudioClip crossbowSfx;
    public AudioClip berserkSfx;

    private float rage;
    private bool berserk;
    private float nextShotTime;
    private float drainAccumulator;

    public override float MeterNormalized { get { return rage / maxRage; } }
    
    public override string StatusText 
    { 
        get 
        { 
            if (berserk) return "BERSERKER! " + Mathf.RoundToInt(rage) + "%";
            return "Rabia " + Mathf.RoundToInt(rage) + "%"; 
        } 
    }
    
    public override string ControlsHint { get { return "A/D mover | Espacio saltar | J espada | K Berserker | L ballesta"; } }

    void Start()
    {
        health.OnDamaged += OnDamaged;
        health.DamageModifier = ModifyDamage;
    }

    void OnDestroy()
    {
        if (health != null) 
        {
            health.OnDamaged -= OnDamaged;
        }
    }

    void OnDamaged(int amount)
    {
        rage = Mathf.Min(maxRage, rage + amount * rageGainPerDamage);
    }

    int ModifyDamage(int dmg, GameObject source)
    {
        if (berserk) return Mathf.Max(1, dmg / 2);
        return dmg;
    }

    void Update()
    {
        if (!player.CanAct) return;

        // Modo Berserker con la K
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (berserk) 
                SetBerserk(false);
            else if (rage >= minRageToActivate) 
                SetBerserk(true);
        }

        if (berserk)
        {
            rage -= (maxRage / berserkDuration) * Time.deltaTime;
            drainAccumulator += healthDrainPerSecond * Time.deltaTime;
            
            if (drainAccumulator >= 1f)
            {
                int whole = (int)drainAccumulator;
                health.SelfDamage(whole);
                drainAccumulator -= whole;
            }
            
            if (rage <= 0f)
            {
                rage = 0f;
                SetBerserk(false);
            }
        }

        // Disparo de ballesta con la L
        if (Input.GetKeyDown(KeyCode.L) && Time.time >= nextShotTime && boltPrefab != null)
        {
            nextShotTime = Time.time + crossbowCooldown;
            Vector3 pos = transform.position;
            if (firePoint != null) pos = firePoint.position;

            Projectile p = Instantiate(boltPrefab, pos, Quaternion.identity);
            p.Launch(player.Facing, boltDamage);
            AudioManager.Play(crossbowSfx);
        }
    }

    void SetBerserk(bool value)
    {
        berserk = value;
        if (value)
        {
            player.DamageMultiplier = damageMultiplier;
            player.SpeedMultiplier = speedMultiplier;
            health.SetColor(berserkColor);
            AudioManager.Play(berserkSfx);
        }
        else
        {
            player.DamageMultiplier = 1f;
            player.SpeedMultiplier = 1f;
            health.SetColor(health.OriginalColor);
        }
    }
}