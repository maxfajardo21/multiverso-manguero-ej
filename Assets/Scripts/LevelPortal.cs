using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelPortal : MonoBehaviour
{
    public enum PortalCondition
    {
        RequireAllKeys,
        RequireBossDefeated,
        FinalVictory
    }

    [Header("Configuración del Portal")]
    public PortalCondition condition = PortalCondition.RequireAllKeys;
    public string nextSceneName = "Modo_Guts";

    [Header("Requisitos")]
    public int requiredKeys = 1;
    public Health bossHealth; 

    [Header("Panel Final (Solo Modo_Musashi)")]
    public GameObject victoryPanel;
    public Text victoryText;
    public Button retryButton;
    public string firstSceneName = "Modo_Deku";

    private int collectedKeys = 0;
    private bool unlocked = false;

    void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RestartStory);
        }

        if (condition == PortalCondition.RequireBossDefeated && bossHealth != null)
        {
            bossHealth.OnDeath += OnBossDefeated;
        }
    }

    void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDeath -= OnBossDefeated;
        }
    }

    public void RegisterKey(int amount = 1)
    {
        if (condition != PortalCondition.RequireAllKeys) return;

        collectedKeys += amount;
        if (collectedKeys >= requiredKeys && !unlocked)
        {
            unlocked = true;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMessage("¡Ya puedes acceder al portal!");
            }
        }
        else if (!unlocked && UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage($"Llaves: {collectedKeys}/{requiredKeys}");
        }
    }

    void OnBossDefeated(Health h)
    {
        unlocked = true;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage("¡Jefe derrotado! Ya puedes entrar al portal.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (condition == PortalCondition.RequireAllKeys)
        {
            if (collectedKeys < requiredKeys)
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowMessage($"Necesitas todas las llaves ({collectedKeys}/{requiredKeys}) para entrar.");
                return;
            }
            SceneManager.LoadScene(nextSceneName);
        }
        else if (condition == PortalCondition.RequireBossDefeated)
        {
            bool bossDead = (bossHealth == null) || bossHealth.IsDead;
            if (!bossDead && !unlocked)
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowMessage("Debes derrotar al jefe final antes de entrar al portal.");
                return;
            }
            SceneManager.LoadScene(nextSceneName);
        }
        else if (condition == PortalCondition.FinalVictory)
        {
            Time.timeScale = 0f;
            if (victoryText != null)
                victoryText.text = "¡Felicidades, completaste la historia!";
            if (victoryPanel != null)
                victoryPanel.SetActive(true);
        }
    }

    public void RestartStory()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(firstSceneName);
    }
}