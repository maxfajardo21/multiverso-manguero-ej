using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(Health))]
public abstract class ModeAbility : MonoBehaviour
{
    protected PlayerController player;
    protected Health health;

    public abstract float MeterNormalized { get; }
    public abstract string StatusText { get; }
    public abstract string ControlsHint { get; }

    protected virtual void Awake()
    {
        player = GetComponent<PlayerController>();
        health = GetComponent<Health>();
    }
}