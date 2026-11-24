using UnityEngine;

public class DekuAbility : ModeAbility
{
    public float chargeTime = 1.5f;
    public int minDamage = 15;
    public int maxDamage = 80;
    public float smashRange = 2.5f;
    public float smashKnockback = 12f;
    public float cooldown = 1f;

    public float safeThreshold = 0.45f;
    public int maxSelfDamage = 25;

    public AudioClip chargeSfx;
    public AudioClip smashSfx;

    private float charge;
    private bool charging;
    private float nextSmashTime;

    public override float MeterNormalized { get { return charge; } }
    public override string StatusText { get { return "Full Cowl " + Mathf.RoundToInt(charge * 100f) + "%"; } }
    public override string ControlsHint { get { return "A/D mover | Espacio saltar | J golpe | K cargar Smash"; } }

    void Update()
    {
        if (!player.CanAct)
        {
            StopCharging();
            return;
        }

        if (Input.GetKeyDown(KeyCode.K) && Time.time >= nextSmashTime)
        {
            charging = true;
            AudioManager.Play(chargeSfx);
        }

        if (charging)
        {
            charge = Mathf.Clamp01(charge + Time.deltaTime / chargeTime);
            player.SpeedMultiplier = 0.4f;
            health.SetColor(Color.Lerp(health.OriginalColor, Color.green, charge));

            if (Input.GetKeyUp(KeyCode.K)) 
            {
                ReleaseSmash();
            }
        }
    }

    void ReleaseSmash()
    {
        int damage = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, charge));
        player.Hit(damage, smashRange, smashKnockback);
        AudioManager.Play(smashSfx);

        // si carga demasiado se hace daño a si mismo
        if (charge > safeThreshold)
        {
            float extra = (charge - safeThreshold) / (1f - safeThreshold);
            health.SelfDamage(Mathf.RoundToInt(extra * maxSelfDamage));
        }

        nextSmashTime = Time.time + cooldown;
        StopCharging();
    }

    void StopCharging()
    {
        if (!charging && charge == 0f) return;
        charging = false;
        charge = 0f;
        player.SpeedMultiplier = 1f;
        health.SetColor(health.OriginalColor);
    }
}