using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Score { get; private set; }
    public int EnemiesAlive { get; private set; }
    public bool GameOver { get; private set; }
    public int KeysCollected { get; private set; }

    [Header("Objetivo")]
    public bool winByEnemies = true;
    public int keysNeeded = 3;
    public string customObjective;
    public string gutsObjective = "Objetivo: derrota a los enemigos y llega al portal";
    public Text objectiveText;
    public Portal portal;

    [Header("Audio")]
    public AudioClip winSfx;
    public AudioClip loseSfx;

    private bool anyEnemyRegistered;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        int p = LayerMask.NameToLayer("Player");
        int e = LayerMask.NameToLayer("Enemy");
        if (p >= 0 && e >= 0)
        {
            Physics2D.IgnoreLayerCollision(p, e, true);
            Physics2D.IgnoreLayerCollision(e, e, true);
        }
    }

    void Start()
    {
        RefreshObjective();
    }

    public void AddKey()
    {
        KeysCollected++;

        if (KeysCollected >= keysNeeded)
        {
            if (portal != null)
            {
                portal.Abrir();
            }

            LevelPortal levelPortal = FindObjectOfType<LevelPortal>();
            if (levelPortal != null)
            {
                levelPortal.RegisterKey(0);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMessage("¡Ya puedes acceder al portal!");
            }
        }

        RefreshObjective();
    }

    void RefreshObjective()
    {
        if (objectiveText == null) return;

        if (!string.IsNullOrEmpty(customObjective))
        {
            objectiveText.text = customObjective;
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Modo_Guts")
        {
            objectiveText.text = string.IsNullOrEmpty(gutsObjective)
                ? "Objetivo: Pasa las trampas y derrota el jefe final"
                : gutsObjective;
            return;
        }

        if (KeysCollected >= keysNeeded)
            objectiveText.text = "Objetivo: ¡ya puedes acceder al portal!";
        else
            objectiveText.text = "Objetivo: junta las llaves (" + KeysCollected + "/" + keysNeeded + ") y llega al portal";
    }

    public void WinLevel()
    {
        if (!GameOver) Win();
    }

    void Win()
    {
        GameOver = true;
        AudioManager.Play(winSfx);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage("VICTORIA!\nR: reiniciar ");
        }
    }

    public void PlayerDied()
    {
        if (GameOver) return;
        GameOver = true;
        AudioManager.Play(loseSfx);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage("DERROTA\nR: reiniciar ");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) LoadMode(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) LoadMode(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) LoadMode(2);
    }

    void LoadMode(int index)
    {
        if (index < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(index);
        }
    }
}