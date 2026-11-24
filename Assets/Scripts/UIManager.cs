using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Barras")]
    public Slider healthBar;
    public Slider abilityBar;

    [Header("Textos UI")]
    public Text modeText;
    public Text controlsText;
    public Text abilityText;
    public Text scoreText;
    public Text enemiesText;
    public Text messageText;

    private Health playerHealth;
    private ModeAbility ability;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            ability = player.GetComponent<ModeAbility>();
            PlayerController pc = player.GetComponent<PlayerController>();

            playerHealth.OnHealthChanged += UpdateHealth;
            UpdateHealth(playerHealth.Current, playerHealth.maxHealth);

            if (modeText != null && pc != null && pc.data != null) 
            {
                modeText.text = pc.data.displayName;
            }
            
            if (controlsText != null && ability != null) 
            {
                controlsText.text = ability.ControlsHint;
            }
        }
        ShowMessage("");
    }

    void OnDestroy()
    {
        if (playerHealth != null) 
        {
            playerHealth.OnHealthChanged -= UpdateHealth;
        }
    }

    void Update()
    {
        if (ability != null)
        {
            if (abilityBar != null) abilityBar.value = ability.MeterNormalized;
            if (abilityText != null) abilityText.text = ability.StatusText;
        }

        if (GameManager.Instance != null)
        {
            if (scoreText != null) scoreText.text = "Puntaje: " + GameManager.Instance.Score;
            if (enemiesText != null) enemiesText.text = "Enemigos: " + GameManager.Instance.EnemiesAlive;
        }
    }

    void UpdateHealth(int current, int max)
    {
        if (healthBar == null) return;
        healthBar.maxValue = max;
        healthBar.value = current;
    }

    public void ShowMessage(string message)
    {
        if (messageText != null) messageText.text = message;
    }
}