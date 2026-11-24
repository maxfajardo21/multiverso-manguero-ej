using UnityEngine;

public enum GameMode { Deku, Guts, Musashi }
[CreateAssetMenu(fileName = "NewCharacter", menuName = "TresFilos/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identidad")]
    public string displayName = "Personaje";
    public GameMode mode;

    [Header("Estadisticas")]
    public int maxHealth = 100;
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Ataque basico")]
    public int attackDamage = 10;
    public float attackCooldown = 0.4f;
    public float attackRange = 1.2f;
}
