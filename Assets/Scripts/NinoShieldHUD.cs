using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class NinoShieldHUD : MonoBehaviour
{
    [Header("Referencias del Niño")]
    public Health ninoHealth;
    public MonoBehaviour ninoShieldComponent;

    [Header("Sliders UI")]
    public Slider ninoHealthBar;
    public Slider shieldChargeBar;

    [Header("Opcional: Campo/Propiedad de carga")]
    public string shieldChargeMemberName = "";

    private static readonly string[] CandidateChargeNames =
    {
        "ChargeNormalized",
        "ShieldNormalized",
        "MeterNormalized",
        "chargeNormalized",
        "shieldNormalized",
        "currentCharge",
        "charge",
        "shieldCharge",
        "chargeAmount",
        "timer",
        "remainingTime"
    };

    private static readonly string[] CandidateMaxNames =
    {
        "maxCharge",
        "MaxCharge",
        "shieldMaxCharge",
        "maxShield",
        "duration",
        "maxDuration",
        "cooldown",
        "maxCooldown"
    };

    void Start()
    {
        ResolveReferences();

        if (ninoHealth != null)
        {
            ninoHealth.OnHealthChanged += UpdateNinoHealth;
            UpdateNinoHealth(ninoHealth.Current, ninoHealth.maxHealth);
        }
    }

    void OnDestroy()
    {
        if (ninoHealth != null)
        {
            ninoHealth.OnHealthChanged -= UpdateNinoHealth;
        }
    }

    void Update()
    {
        ResolveReferences();

        if (ninoShieldComponent != null && shieldChargeBar != null)
        {
            shieldChargeBar.value = Mathf.Clamp01(ReadChargeNormalized(ninoShieldComponent));
        }
    }

    void ResolveReferences()
    {
        if (ninoHealth == null)
        {
            GameObject ninoObj = GameObject.Find("Nino") ?? GameObject.Find("Niño");
            if (ninoObj != null)
            {
                ninoHealth = ninoObj.GetComponent<Health>();
            }
        }
    }

    void UpdateNinoHealth(int current, int max)
    {
        if (ninoHealthBar == null) return;
        ninoHealthBar.minValue = 0f;
        ninoHealthBar.maxValue = Mathf.Max(1, max);
        ninoHealthBar.value = Mathf.Clamp(current, 0, max);
    }

    float ReadChargeNormalized(Component comp)
    {
        System.Type type = comp.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (!string.IsNullOrWhiteSpace(shieldChargeMemberName) &&
            TryGetFloatValue(comp, type, shieldChargeMemberName, flags, out float customVal))
        {
            return customVal > 1f ? Mathf.Clamp01(customVal / 100f) : Mathf.Clamp01(customVal);
        }

        foreach (string name in CandidateChargeNames)
        {
            if (!TryGetFloatValue(comp, type, name, flags, out float val)) continue;

            if (val >= 0f && val <= 1f) return val;

            foreach (string maxName in CandidateMaxNames)
            {
                if (TryGetFloatValue(comp, type, maxName, flags, out float maxVal) && maxVal > 0f)
                {
                    return Mathf.Clamp01(val / maxVal);
                }
            }

            return Mathf.Clamp01(val / 100f);
        }

        return 0f;
    }

    bool TryGetFloatValue(object target, System.Type type, string memberName, BindingFlags flags, out float value)
    {
        value = 0f;

        FieldInfo field = type.GetField(memberName, flags);
        if (field != null)
        {
            object raw = field.GetValue(target);
            if (raw is float f) { value = f; return true; }
            if (raw is double d) { value = (float)d; return true; }
            if (raw is int i) { value = i; return true; }
        }

        PropertyInfo prop = type.GetProperty(memberName, flags);
        if (prop != null && prop.CanRead)
        {
            object raw = prop.GetValue(target);
            if (raw is float f) { value = f; return true; }
            if (raw is double d) { value = (float)d; return true; }
            if (raw is int i) { value = i; return true; }
        }

        return false;
    }
}