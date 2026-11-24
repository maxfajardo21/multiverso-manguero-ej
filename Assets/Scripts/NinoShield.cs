using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Health))]
public class NinoShield : MonoBehaviour
{
    public static NinoShield Instance { get; private set; }

    private struct Breadcrumb
    {
        public float time;
        public float playerX;
        public float playerY;
        public float moveInput;
        public int facing;
    }

    [Header("Vida, Victoria y Derrota")]
    public int maxHealth = 100;
    public float fallDeathY = -25f;
    public string musashiObjectiveText = "Objetivo: Proteje al niño y derrota el jefe final";
    public bool updateOnlyTopObjective = true;
    public string victoryMessage = "Victoria, terminaste el juego";
    public bool freezeGameOnVictory = true;
    public string defeatMessage = "¡El niño ha caído! Derrota";
    public string restartHint = "Presiona [R] para reiniciar";
    public KeyCode restartKey = KeyCode.R;
    public bool pauseOnDefeat = false;

    [Header("Escudo con Tecla E (Reemplaza Espada)")]
    public KeyCode shieldKey = KeyCode.E;
    public float shieldDuration = 2f;
    public float shieldCooldown = 4f;

    [Header("Seguimiento en Cadena y Salto Simultáneo")]
    public bool simultaneousJumpWithPlayer = true;
    public float chainDelay = 0.12f;
    public float followStopDistance = 1.5f;
    public float catchUpDistance = 3.0f;
    public float catchUpSpeedMultiplier = 1.35f;
    public float fallbackMoveSpeed = 6.5f;
    public float fallbackJumpForce = 12f;
    public float groundCheckRadius = 0.28f;
    public LayerMask groundLayer;

    [Header("Visual y Barras HUD")]
    public GameObject shieldVisual;
    public Vector2 hudStartPosition = new Vector2(12f, 118f);
    public float hudBarWidth = 260f;
    public float hudHealthBarHeight = 16f;
    public float hudShieldBarHeight = 14f;
    public float hudBarSpacing = 6f;
    public Slider healthSlider;
    public Slider shieldCooldownSlider;
    public Color normalColor = Color.white;
    public Color shieldActiveColor = new Color(0.45f, 0.88f, 1f, 1f);

    public GameObject escudoVisual
    {
        get => shieldVisual;
        set => shieldVisual = value;
    }

    public int CurrentHealth { get; private set; }
    public int MaxHealth => Mathf.Max(1, maxHealth);
    public bool IsShieldActive { get; private set; }
    public bool IsShieldOnCooldown => cooldownTimer > 0f;
    public float ShieldRemainingNormalized => IsShieldActive ? Mathf.Clamp01(activeTimer / Mathf.Max(0.01f, shieldDuration)) : 0f;
    public float CooldownRemainingNormalized => shieldCooldown > 0f ? Mathf.Clamp01(cooldownTimer / shieldCooldown) : 0f;
    public Health HealthComp { get; private set; }

    public event Action<NinoShield> OnShieldStateChanged;
    public event Action<NinoShield> OnNinoDied;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private PlayerController playerController;
    private Vector3 baseScale;

    private readonly Queue<Breadcrumb> chainHistory = new Queue<Breadcrumb>();
    private float desiredVx;
    private bool jumpNowRequested;
    private bool isGrounded;
    private float jumpCooldownTimer;
    private float lastPlayerY;
    private bool loggedMissingPlayer;

    private float activeTimer;
    private float cooldownTimer;
    private bool isDead;
    private bool defeatTriggered;
    private bool victoryTriggered;
    private string runtimeEndBannerText;

    private float EffectiveMoveSpeed
    {
        get
        {
            if (playerController != null && playerController.data != null)
            {
                return playerController.data.moveSpeed * playerController.SpeedMultiplier;
            }
            return fallbackMoveSpeed;
        }
    }

    private float EffectiveJumpForce
    {
        get
        {
            if (playerController != null && playerController.data != null)
            {
                return playerController.data.jumpForce;
            }
            return fallbackJumpForce;
        }
    }

    private LayerMask EffectiveGroundLayer
    {
        get
        {
            if (groundLayer.value != 0) return groundLayer;
            if (playerController != null && playerController.groundLayer.value != 0) return playerController.groundLayer;
            return 0;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;
        CurrentHealth = MaxHealth;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        HealthComp = GetComponent<Health>();
        if (HealthComp != null)
        {
            HealthComp.SetMax(MaxHealth);
            HealthComp.OnDeath += HandleDeath;
        }

        ApplyShieldVisuals();
        RefreshUI();
    }

    void Start()
    {
        FindPlayer();
        if (playerTransform != null)
        {
            lastPlayerY = playerTransform.position.y;
        }
        ReplaceObjectiveTextInUI();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (HealthComp != null)
        {
            HealthComp.OnDeath -= HandleDeath;
        }
    }

    void Update()
    {
        if (victoryTriggered)
        {
            SetShieldActive(false);
            ReplaceObjectiveTextInUI();
            if (Input.GetKeyDown(restartKey))
            {
                RestartLevel();
            }
            return;
        }

        if (isDead || defeatTriggered)
        {
            SetShieldActive(false);
            ReplaceObjectiveTextInUI();

            if (Input.GetKeyDown(restartKey))
            {
                RestartLevel();
            }
            return;
        }

        if (HealthComp != null && HealthComp.IsDead)
        {
            CurrentHealth = 0;
            HandleDeath(HealthComp);
            return;
        }

        if (transform.position.y <= fallDeathY)
        {
            CurrentHealth = 0;
            if (HealthComp != null && !HealthComp.IsDead)
            {
                HealthComp.TakeDamage(MaxHealth * 10, gameObject);
            }
            HandleDeath(HealthComp);
            return;
        }

        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer = Mathf.Max(0f, jumpCooldownTimer - Time.deltaTime);
        }

        FindPlayer();
        CheckPlayerPortalProximity();
        HandleShieldInputAndTimers();
        RecordAndReplayChain();
        RefreshUI();
        ReplaceObjectiveTextInUI();
    }

    void LateUpdate()
    {
        ReplaceObjectiveTextInUI();
    }

    void FixedUpdate()
    {
        if (isDead || victoryTriggered)
        {
            StopHorizontal();
            return;
        }

        CheckGrounded();

        if (rb != null)
        {
            rb.WakeUp();
            rb.velocity = new Vector2(desiredVx, rb.velocity.y);

            if (jumpNowRequested && isGrounded && jumpCooldownTimer <= 0f)
            {
                rb.velocity = new Vector2(rb.velocity.x, EffectiveJumpForce);
                isGrounded = false;
                jumpNowRequested = false;
                jumpCooldownTimer = 0.12f;
            }
        }
        else if (Mathf.Abs(desiredVx) > 0.01f)
        {
            transform.position += new Vector3(desiredVx * Time.fixedDeltaTime, 0f, 0f);
        }
    }

    void RecordAndReplayChain()
    {
        if (playerTransform == null)
        {
            desiredVx = 0f;
            return;
        }

        int pFacing = playerController != null
            ? playerController.Facing
            : (playerTransform.localScale.x < 0f ? -1 : 1);

        float pInput = (playerController != null && playerController.CanAct)
            ? Input.GetAxisRaw("Horizontal")
            : 0f;

        bool playerCanJump = playerController == null || playerController.CanAct;
        bool jumpPressedThisFrame = playerCanJump && (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space));

        float deltaPlayerY = playerTransform.position.y - lastPlayerY;
        lastPlayerY = playerTransform.position.y;

        bool playerStartedRising = (playerRb != null && playerRb.velocity.y > 0.3f && deltaPlayerY > 0.01f)
            || deltaPlayerY > 0.05f;

        float dxToPlayer = playerTransform.position.x - transform.position.x;
        float absDxToPlayer = Mathf.Abs(dxToPlayer);

        if (simultaneousJumpWithPlayer && (jumpPressedThisFrame || playerStartedRising) && absDxToPlayer < 16f)
        {
            jumpNowRequested = true;
            if (isGrounded && jumpCooldownTimer <= 0f && rb != null)
            {
                rb.velocity = new Vector2(rb.velocity.x, EffectiveJumpForce);
                isGrounded = false;
                jumpNowRequested = false;
                jumpCooldownTimer = 0.12f;
            }
        }
        else if ((playerTransform.position.y - transform.position.y) > 0.7f && absDxToPlayer < 14f)
        {
            jumpNowRequested = true;
        }

        chainHistory.Enqueue(new Breadcrumb
        {
            time = Time.time,
            playerX = playerTransform.position.x,
            playerY = playerTransform.position.y,
            moveInput = pInput,
            facing = pFacing
        });

        Breadcrumb currentStep = default;
        bool hasStep = false;
        while (chainHistory.Count > 0 && Time.time - chainHistory.Peek().time >= chainDelay)
        {
            currentStep = chainHistory.Dequeue();
            hasStep = true;
        }

        float baseSpeed = EffectiveMoveSpeed;

        if (absDxToPlayer > followStopDistance)
        {
            float dir = dxToPlayer > 0f ? 1f : -1f;
            float mult = absDxToPlayer > catchUpDistance ? catchUpSpeedMultiplier : 1f;
            desiredVx = dir * baseSpeed * mult;
            transform.localScale = new Vector3(dir * Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }
        else if (hasStep && Mathf.Abs(currentStep.moveInput) > 0.05f && absDxToPlayer > 0.85f)
        {
            float dir = Mathf.Sign(currentStep.moveInput);
            desiredVx = dir * baseSpeed * 0.75f;
            transform.localScale = new Vector3(dir * Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }
        else
        {
            desiredVx = 0f;
            int lookDir = hasStep ? currentStep.facing : pFacing;
            transform.localScale = new Vector3(lookDir * Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }
    }

    void CheckGrounded()
    {
        if (rb == null)
        {
            isGrounded = true;
            return;
        }

        LayerMask mask = EffectiveGroundLayer;
        Vector2 footPos = col != null
            ? new Vector2(col.bounds.center.x, col.bounds.min.y)
            : (Vector2)transform.position;

        Collider2D hit = mask.value != 0
            ? Physics2D.OverlapCircle(footPos, groundCheckRadius, mask)
            : Physics2D.OverlapCircle(footPos, groundCheckRadius);

        if (hit != null && hit.gameObject != gameObject && !hit.isTrigger)
        {
            isGrounded = Mathf.Abs(rb.velocity.y) <= 3.5f;
            return;
        }

        isGrounded = Mathf.Abs(rb.velocity.y) < 0.2f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || victoryTriggered) return;

        if (IsPortalObject(other.gameObject))
        {
            TriggerMusashiVictory();
            return;
        }

        string n = other.gameObject.name.ToLower();
        string t = other.gameObject.tag.ToLower();
        if (n.Contains("vacio") || n.Contains("vacío") || n.Contains("void") || n.Contains("abismo") || n.Contains("kill") || n.Contains("muerte") || t.Contains("kill") || t.Contains("hazard"))
        {
            CurrentHealth = 0;
            if (HealthComp != null && !HealthComp.IsDead)
            {
                HealthComp.TakeDamage(MaxHealth * 10, other.gameObject);
            }
            HandleDeath(HealthComp);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || victoryTriggered) return;
        if (collision != null && collision.gameObject != null && IsPortalObject(collision.gameObject))
        {
            TriggerMusashiVictory();
        }
    }

    void CheckPlayerPortalProximity()
    {
        if (!IsMusashiScene() || playerTransform == null || victoryTriggered || isDead) return;

        Collider2D[] nearColliders = Physics2D.OverlapCircleAll(playerTransform.position, 0.75f);
        for (int i = 0; i < nearColliders.Length; i++)
        {
            if (nearColliders[i] != null && IsPortalObject(nearColliders[i].gameObject))
            {
                TriggerMusashiVictory();
                return;
            }
        }
    }

    bool IsPortalObject(GameObject obj)
    {
        if (obj == null) return false;
        string n = obj.name.ToLower();
        string t = obj.tag.ToLower();
        if (n.Contains("portal") || t.Contains("portal")) return true;

        MonoBehaviour[] behaviours = obj.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && behaviours[i].GetType().Name.ToLower().Contains("portal"))
            {
                return true;
            }
        }
        return false;
    }

    public void TriggerMusashiVictory()
    {
        if (victoryTriggered || isDead) return;
        victoryTriggered = true;
        SetShieldActive(false);
        StopHorizontal();

        if (playerController != null)
        {
            playerController.InputLocked = true;
        }

        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
        }

        runtimeEndBannerText = victoryMessage;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SendMessage("Victory", SendMessageOptions.DontRequireReceiver);
            GameManager.Instance.SendMessage("OnVictory", SendMessageOptions.DontRequireReceiver);
            GameManager.Instance.SendMessage("ShowVictory", victoryMessage, SendMessageOptions.DontRequireReceiver);
        }

        if (freezeGameOnVictory)
        {
            Time.timeScale = 0f;
        }
    }

    void OnGUI()
    {
        if (!IsMusashiScene()) return;

        float x = hudStartPosition.x;
        float y = hudStartPosition.y;

        float hpRatio = MaxHealth > 0 ? Mathf.Clamp01((float)CurrentHealth / MaxHealth) : 0f;
        GUI.color = new Color(0.22f, 0.22f, 0.22f, 0.92f);
        GUI.DrawTexture(new Rect(x, y, hudBarWidth, hudHealthBarHeight), Texture2D.whiteTexture);

        GUI.color = new Color(0.86f, 0.16f, 0.16f, 1f);
        GUI.DrawTexture(new Rect(x, y, hudBarWidth * hpRatio, hudHealthBarHeight), Texture2D.whiteTexture);

        GUIStyle miniLabel = new GUIStyle(GUI.skin.label);
        miniLabel.fontSize = 11;
        miniLabel.fontStyle = FontStyle.Bold;
        miniLabel.alignment = TextAnchor.MiddleLeft;
        miniLabel.normal.textColor = Color.white;

        GUI.color = Color.white;
        GUI.Label(new Rect(x + 6f, y - 1f, hudBarWidth - 12f, hudHealthBarHeight + 2f), $"Niño  {CurrentHealth}/{MaxHealth}", miniLabel);

        float shieldY = y + hudHealthBarHeight + hudBarSpacing;
        float shieldRatio;
        string shieldStateText;
        Color shieldFillColor;

        if (IsShieldActive)
        {
            shieldRatio = ShieldRemainingNormalized;
            shieldStateText = $"Escudo [E]  {activeTimer:0.0}s";
            shieldFillColor = new Color(0.22f, 0.68f, 0.95f, 1f);
        }
        else if (cooldownTimer > 0f)
        {
            shieldRatio = 1f - CooldownRemainingNormalized;
            shieldStateText = $"Escudo [E] Recargando  {cooldownTimer:0.0}s";
            shieldFillColor = new Color(0.85f, 0.58f, 0.15f, 1f);
        }
        else
        {
            shieldRatio = 1f;
            shieldStateText = "Escudo [E]  Listo";
            shieldFillColor = new Color(0.25f, 0.72f, 0.85f, 1f);
        }

        GUI.color = new Color(0.22f, 0.22f, 0.22f, 0.92f);
        GUI.DrawTexture(new Rect(x, shieldY, hudBarWidth, hudShieldBarHeight), Texture2D.whiteTexture);

        GUI.color = shieldFillColor;
        GUI.DrawTexture(new Rect(x, shieldY, hudBarWidth * shieldRatio, hudShieldBarHeight), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.Label(new Rect(x + 6f, shieldY - 1f, hudBarWidth - 12f, hudShieldBarHeight + 2f), shieldStateText, miniLabel);

        if (!string.IsNullOrEmpty(runtimeEndBannerText))
        {
            GUIStyle bannerStyle = new GUIStyle(GUI.skin.box);
            bannerStyle.fontSize = 16;
            bannerStyle.fontStyle = FontStyle.Bold;
            bannerStyle.alignment = TextAnchor.MiddleCenter;
            bannerStyle.normal.textColor = Color.white;

            float dw = 480f;
            float dh = 92f;
            Rect dRect = new Rect((Screen.width - dw) * 0.5f, (Screen.height - dh) * 0.5f, dw, dh);
            GUI.color = Color.white;
            GUI.Box(dRect, runtimeEndBannerText, bannerStyle);
        }
    }

    bool IsMusashiScene()
    {
        return SceneManager.GetActiveScene().name.ToLower().Contains("musashi");
    }

    void ReplaceObjectiveTextInUI()
    {
        if (!IsMusashiScene() || string.IsNullOrEmpty(musashiObjectiveText)) return;

        UIManager ui = FindObjectOfType<UIManager>();
        Text centralMessageText = ui != null ? ui.messageText : null;

        if (centralMessageText != null && centralMessageText.gameObject.activeSelf)
        {
            centralMessageText.text = "";
            centralMessageText.gameObject.SetActive(false);
        }

        if (ui != null)
        {
            Type t = ui.GetType();
            foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.Name.ToLower().Contains("objetivo") || f.Name.ToLower().Contains("objective"))
                {
                    object val = f.GetValue(ui);
                    ApplyTextAndBlackColor(val, musashiObjectiveText);
                }
            }
        }

        Text[] allTexts = FindObjectsOfType<Text>();
        for (int i = 0; i < allTexts.Length; i++)
        {
            Text t = allTexts[i];
            if (t == null || t == centralMessageText) continue;

            string current = t.text;
            bool isObjective = !string.IsNullOrEmpty(current) &&
                (current.ToLower().Contains("objetivo:") || current.ToLower().Contains("proteje al niño") || current.ToLower().Contains("junta las llaves"));

            if (isObjective || (!updateOnlyTopObjective && t.gameObject.name.ToLower().Contains("objetivo")))
            {
                t.text = musashiObjectiveText;
                t.color = Color.black;
            }
        }

        MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>();
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            MonoBehaviour mb = allBehaviours[i];
            if (mb == null) continue;
            if (mb.GetType().Name == "TextMeshProUGUI" || mb.GetType().Name == "TMP_Text")
            {
                if (centralMessageText != null && mb.gameObject == centralMessageText.gameObject) continue;
                string tmpText = GetTextPropertyIfExists(mb);
                if (!string.IsNullOrEmpty(tmpText) &&
                    (tmpText.ToLower().Contains("objetivo:") || tmpText.ToLower().Contains("proteje al niño") || tmpText.ToLower().Contains("junta las llaves")))
                {
                    SetTextAndBlackColor(mb, musashiObjectiveText);
                }
            }
        }
    }

    void ApplyTextAndBlackColor(object target, string value)
    {
        if (target == null) return;
        if (target is Text uiText)
        {
            uiText.text = value;
            uiText.color = Color.black;
            return;
        }
        SetTextAndBlackColor(target, value);
    }

    string GetTextPropertyIfExists(object target)
    {
        if (target == null) return null;
        PropertyInfo prop = target.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        return prop != null ? prop.GetValue(target, null) as string : null;
    }

    void SetTextAndBlackColor(object target, string value)
    {
        if (target == null) return;
        Type t = target.GetType();
        PropertyInfo textProp = t.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        if (textProp != null && textProp.CanWrite)
        {
            textProp.SetValue(target, value, null);
        }

        PropertyInfo colorProp = t.GetProperty("color", BindingFlags.Instance | BindingFlags.Public);
        if (colorProp != null && colorProp.CanWrite && colorProp.PropertyType == typeof(Color))
        {
            colorProp.SetValue(target, Color.black, null);
        }
    }

    void FindPlayer()
    {
        if (playerTransform != null) return;

        playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
        }
        else
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
                playerController = p.GetComponent<PlayerController>();
            }
        }

        if (playerTransform != null)
        {
            playerRb = playerTransform.GetComponent<Rigidbody2D>();
            lastPlayerY = playerTransform.position.y;
            loggedMissingPlayer = false;
        }
        else if (!loggedMissingPlayer)
        {
            loggedMissingPlayer = true;
            Debug.LogWarning("[NinoShield] No se encontró PlayerController ni objeto con tag Player en la escena.");
        }
    }

    void HandleShieldInputAndTimers()
    {
        if (Input.GetKeyDown(shieldKey) && !IsShieldActive && cooldownTimer <= 0f)
        {
            ActivateShield();
        }

        if (IsShieldActive)
        {
            activeTimer -= Time.deltaTime;
            if (activeTimer <= 0f)
            {
                activeTimer = 0f;
                IsShieldActive = false;
                cooldownTimer = shieldCooldown;
                ApplyShieldVisuals();
                OnShieldStateChanged?.Invoke(this);
            }
        }
        else if (cooldownTimer > 0f)
        {
            cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
        }
    }

    public void SetShieldActive(bool active)
    {
        if (active)
        {
            ActivateShield();
            return;
        }

        if (!IsShieldActive) return;
        activeTimer = 0f;
        IsShieldActive = false;
        ApplyShieldVisuals();
        OnShieldStateChanged?.Invoke(this);
    }

    public void ActivateShield()
    {
        if (isDead || victoryTriggered || cooldownTimer > 0f) return;
        IsShieldActive = true;
        activeTimer = shieldDuration;
        cooldownTimer = 0f;
        ApplyShieldVisuals();
        OnShieldStateChanged?.Invoke(this);
    }

    public bool TryAbsorbDamage(int amount, GameObject attacker = null)
    {
        if (isDead || victoryTriggered) return false;

        if (IsShieldActive && activeTimer > 0f)
        {
            return true;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, amount));
        RefreshUI();
        return false;
    }

    void StopHorizontal()
    {
        desiredVx = 0f;
        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    void ApplyShieldVisuals()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(IsShieldActive);
        }

        if (sr != null)
        {
            sr.color = IsShieldActive ? shieldActiveColor : normalColor;
        }
    }

    void RefreshUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = MaxHealth;
            healthSlider.value = CurrentHealth;
        }

        if (shieldCooldownSlider != null)
        {
            shieldCooldownSlider.maxValue = 1f;
            shieldCooldownSlider.value = IsShieldActive ? ShieldRemainingNormalized : (1f - CooldownRemainingNormalized);
        }
    }

    void HandleDeath(Health h)
    {
        if (defeatTriggered || victoryTriggered) return;
        defeatTriggered = true;
        isDead = true;
        CurrentHealth = 0;
        IsShieldActive = false;
        ApplyShieldVisuals();
        StopHorizontal();
        RefreshUI();
        ReplaceObjectiveTextInUI();

        runtimeEndBannerText = string.IsNullOrEmpty(restartHint)
            ? defeatMessage
            : $"{defeatMessage}\n{restartHint}";

        OnNinoDied?.Invoke(this);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SendMessage("OnNinoDefeated", SendMessageOptions.DontRequireReceiver);
            GameManager.Instance.SendMessage("GameOver", SendMessageOptions.DontRequireReceiver);
            GameManager.Instance.SendMessage("ShowDefeat", runtimeEndBannerText, SendMessageOptions.DontRequireReceiver);
        }

        if (pauseOnDefeat)
        {
            Time.timeScale = 0f;
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }
}