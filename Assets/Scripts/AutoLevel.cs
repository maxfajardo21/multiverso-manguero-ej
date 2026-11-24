using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public enum NivelModo { Deku, Guts, Musashi }

[ExecuteAlways]
public class AutoLevel : MonoBehaviour
{
    [Header("Configuracion General")]
    public NivelModo modo = NivelModo.Deku;
    public bool crearSuelo = false;
    public bool crearEnemigosAuto = false;
    public EnemyAI jefeExistente;
    public int capaSuelo = 8;
    public int capaEnemigo = 10;

    [Header("Edicion en Escena (Visible sin Play)")]
    [Tooltip("Marca para generar o regenerar el nivel en modo edicion.")]
    public bool generarAhoraEnEscena = false;
    [Tooltip("Si esta activo, al darle Play no borra ni recrea NivelGenerado si ya existe.")]
    public bool respetarNivelYaGenerado = true;
    public bool limpiarAntesDeConstruir = true;
    public List<Transform> plataformasManuales = new List<Transform>();

    [Header("Sprites Pixel Art 16x16")]
    public Sprite spriteLlave;
    public Sprite spriteCuracion;
    public Sprite spritePortal;
    public Sprite spritePlataforma;
    public Sprite spriteSuelo;
    public Sprite spriteNino;
    public Sprite spriteEscudo;

    [Header("Escalas Visuales")]
    public float escalaLlave = 3.2f;
    public float escalaCuracion = 3.2f;
    public float escalaPortal = 4.5f;
    public float escalaPlataforma = 2.4f;
    public float escalaNino = 3.6f;
    public float escalaEscudo = 1.75f;

    [Header("Ajuste Pixel Art")]
    public bool autoCargarDesdeAssetsArt = true;
    public bool usarColorBlancoConSprite = true;
    public FilterMode filtroTextura = FilterMode.Point;

    private Transform player;
    private Transform contenedorNivel;
    private GameManager gm;
    private Portal portal;
    private Text objetivoCreado;
    private bool bloquearPosicionObjetivo = false;
    private float startX, feetY;
    private static Sprite cuadrado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawnOnSceneLoad()
    {
        if (FindObjectOfType<AutoLevel>() != null) return;

        string sceneName = SceneManager.GetActiveScene().name.ToLower();
        NivelModo? detectedMode = null;

        if (sceneName.Contains("deku")) detectedMode = NivelModo.Deku;
        else if (sceneName.Contains("guts")) detectedMode = NivelModo.Guts;
        else if (sceneName.Contains("musashi") || sceneName.Contains("mus")) detectedMode = NivelModo.Musashi;

        if (detectedMode.HasValue)
        {
            GameObject go = new GameObject("AutoLevel");
            AutoLevel al = go.AddComponent<AutoLevel>();
            al.modo = detectedMode.Value;
        }
    }

    void OnValidate()
    {
        if (autoCargarDesdeAssetsArt)
        {
            CargarSpritesDesdeArt();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying && generarAhoraEnEscena)
        {
            generarAhoraEnEscena = false;
            EditorApplication.delayCall += () =>
            {
                if (this != null && !Application.isPlaying)
                {
                    ConstruirNivelEnEscena(true);
                }
            };
        }
#endif
    }

#if UNITY_EDITOR
    [ContextMenu("Construir Nivel en Escena")]
    public void MenuConstruirNivelEnEscena()
    {
        ConstruirNivelEnEscena(true);
    }

    [ContextMenu("Limpiar Nivel Generado")]
    public void LimpiarNivelGenerado()
    {
        Transform cont = transform.Find("NivelGenerado");
        if (cont != null)
        {
            Undo.DestroyObjectImmediate(cont.gameObject);
        }
        plataformasManuales.Clear();
        MarcarEscenaSucia();
    }

    private void MarcarEscenaSucia()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(gameObject);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }
#endif

    public void ConstruirNivelEnEscena(bool forzarReconstruccion)
    {
        Transform existente = transform.Find("NivelGenerado");

        if (existente != null && !forzarReconstruccion && respetarNivelYaGenerado)
        {
            contenedorNivel = existente;
            ReconectarReferenciasExistentes();
            return;
        }

        if (!InicializarContexto(forzarReconstruccion)) return;
        ConstruirSegunModo();

#if UNITY_EDITOR
        MarcarEscenaSucia();
#endif
    }

    private void ReconectarReferenciasExistentes()
    {
        if (contenedorNivel == null) return;
        Portal pExistente = contenedorNivel.GetComponentInChildren<Portal>(true);
        if (pExistente != null) portal = pExistente;

        plataformasManuales.Clear();
        foreach (Transform child in contenedorNivel)
        {
            if (child != null && child.name.StartsWith("Plataforma"))
            {
                plataformasManuales.Add(child);
            }
        }
    }

    private void CargarSpritesDesdeArt()
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art" });
        List<Sprite> encontrados = new List<Sprite>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null)
            {
                encontrados.Add(sp);
            }
        }

        if (spriteLlave == null) spriteLlave = BuscarPorPalabras(encontrados, "llave_16x16", "llave", "key");
        if (spriteCuracion == null) spriteCuracion = BuscarPorPalabras(encontrados, "curacion_16x16", "cura", "heal", "corazon", "vida");
        if (spritePortal == null) spritePortal = BuscarPorPalabras(encontrados, "portal_16x16", "portal", "puerta");
        if (spritePlataforma == null) spritePlataforma = BuscarPorPalabras(encontrados, "plataforma_16x16", "plata", "platform", "bloque");
        if (spriteSuelo == null) spriteSuelo = BuscarPorPalabras(encontrados, "suelo_16x16", "suelo", "ground", "tierra");
        if (spriteNino == null) spriteNino = BuscarPorPalabras(encontrados, "nino_16x16", "nino", "niño", "child");
        if (spriteEscudo == null) spriteEscudo = BuscarPorPalabras(encontrados, "escudo_16x16", "escudo", "shield");
#endif
    }

    private Sprite BuscarPorPalabras(List<Sprite> sprites, params string[] keywords)
    {
        foreach (string kw in keywords)
        {
            foreach (Sprite sp in sprites)
            {
                if (sp != null && sp.name.ToLower().Contains(kw))
                {
                    return sp;
                }
            }
        }
        return null;
    }

    static Sprite Cuadrado()
    {
        if (cuadrado == null)
        {
            Texture2D t = new Texture2D(4, 4);
            Color[] px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            t.SetPixels(px);
            t.filterMode = FilterMode.Point;
            t.Apply();
            cuadrado = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
        return cuadrado;
    }

    private Sprite ResolverSprite(Sprite customSprite, out bool esSpritePersonalizado)
    {
        if (customSprite != null)
        {
            if (customSprite.texture != null)
            {
                customSprite.texture.filterMode = filtroTextura;
            }
            esSpritePersonalizado = true;
            return customSprite;
        }

        esSpritePersonalizado = false;
        return Cuadrado();
    }

    private Color ResolverColor(bool esSpritePersonalizado, Color fallbackColor)
    {
        if (esSpritePersonalizado && usarColorBlancoConSprite)
        {
            return Color.white;
        }
        return fallbackColor;
    }

    private void AjustarColliderAlSprite(BoxCollider2D bc, SpriteRenderer sr)
    {
        if (bc == null || sr == null || sr.sprite == null) return;
        Bounds b = sr.sprite.bounds;
        bc.size = b.size;
        bc.offset = b.center;
    }

    private void PrepararContenedor(bool forzarReconstruccion)
    {
        Transform existente = transform.Find("NivelGenerado");
        if (existente != null)
        {
            if (forzarReconstruccion || limpiarAntesDeConstruir)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) Undo.DestroyObjectImmediate(existente.gameObject);
                else Destroy(existente.gameObject);
#else
                Destroy(existente.gameObject);
#endif
                contenedorNivel = null;
            }
            else
            {
                contenedorNivel = existente;
                return;
            }
        }

        GameObject cont = new GameObject("NivelGenerado");
#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(cont, "Crear NivelGenerado");
#endif
        cont.transform.SetParent(transform, false);
        contenedorNivel = cont.transform;
        plataformasManuales.Clear();
    }

    private bool InicializarContexto(bool forzarReconstruccion)
    {
        if (autoCargarDesdeAssetsArt)
        {
            CargarSpritesDesdeArt();
        }

        GameObject p = GameObject.FindWithTag("Player");
        if (p == null)
        {
            return false;
        }
        player = p.transform;
        gm = GameManager.Instance;

        startX = player.position.x;
        feetY = player.position.y - 0.5f;

        RaycastHit2D hit = Physics2D.Raycast(player.position, Vector2.down, 30f, 1 << capaSuelo);
        if (hit.collider != null)
        {
            feetY = hit.point.y;
        }

        PrepararContenedor(forzarReconstruccion);
        return true;
    }

    void Start()
    {
        Transform existente = transform.Find("NivelGenerado");
        if (existente != null && respetarNivelYaGenerado)
        {
            contenedorNivel = existente;
            ReconectarReferenciasExistentes();
            if (Application.isPlaying)
            {
                gm = GameManager.Instance;
                ConfigurarGM(modo == NivelModo.Musashi, ObtenerTextoObjetivoPorModo());
            }
            return;
        }

        if (!InicializarContexto(false)) return;
        ConstruirSegunModo();
    }

    void LateUpdate()
    {
        if (Application.isPlaying && bloquearPosicionObjetivo && objetivoCreado != null)
        {
            RectTransform r = objetivoCreado.rectTransform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(24f, -155f);
            r.sizeDelta = new Vector2(720f, 60f);
        }
    }

    private string ObtenerTextoObjetivoPorModo()
    {
        if (modo == NivelModo.Guts) return "Objetivo: derrota a Femto";
        if (modo == NivelModo.Musashi) return "Objetivo: derrota a todos los rivales y protege al nino (E: escudo)";
        return "";
    }

    private void ConstruirSegunModo()
    {
        if (modo == NivelModo.Deku) ConstruirDeku();
        else if (modo == NivelModo.Guts) ConstruirGuts();
        else ConstruirMusashi();
    }

    // ---------------- NIVELES ----------------

    void ConstruirDeku()
    {
        Parkour(startX + 4f, 4);

        float finTorres = 0f;
        for (int i = 0; i < 3; i++)
        {
            float bx = startX + 22f + i * 22f;
            int n = 3 + i;
            for (int k = 0; k < n; k++)
            {
                float esc = (k % 2 == 0) ? escalaPlataforma * 1.15f : escalaPlataforma;
                float x = bx + k * 3.0f;
                float top = feetY + 1.45f * (k + 1);
                Plataforma(x, top, esc, new Color(0.35f, 0.55f, 0.85f));

                if (k == n - 1)
                {
                    CrearLlave(new Vector3(x, top + 1.2f, 0f));
                }
            }
            finTorres = bx + n * 3.0f;
        }

        float px = finTorres + 7f;
        CrearPortal(new Vector3(px, feetY + escalaPortal * 0.45f, 0f));
        CrearSuelo(startX - 8f, px + 12f);
        ConfigurarGM(false, "");
    }

    void ConstruirGuts()
    {
        Parkour(startX + 4f, 4);

        float ax = startX + 34f;
        float px = ax + 22f;
        CrearPortal(new Vector3(px, feetY + escalaPortal * 0.45f, 0f));

        if (jefeExistente != null)
        {
            BossPhases bp = jefeExistente.GetComponent<BossPhases>();
            if (bp != null)
            {
                bp.portal = portal;
            }
        }

        CrearSuelo(startX - 8f, px + 12f);
        ConfigurarGM(false, "Objetivo: derrota a Femto");
    }

    void ConstruirMusashi()
    {
        Parkour(startX + 4f, 4);
        CrearNino();
        CrearSuelo(startX - 8f, startX + 65f);
        ConfigurarGM(true, "Objetivo: derrota a todos los rivales y protege al nino (E: escudo)");
    }

    // ---------------- PIEZAS ----------------

    void Parkour(float x, int n)
    {
        for (int k = 0; k < n; k++)
        {
            float top = feetY + 1.35f * (k + 1);
            Plataforma(x + k * 3.0f, top, escalaPlataforma, new Color(0.35f, 0.85f, 0.5f));
        }
        float lx = x + (n - 1) * 3.0f;
        float lt = feetY + 1.35f * n;
        CrearCuracion(new Vector3(lx, lt + 1.2f, 0f));
    }

    GameObject Plataforma(float cx, float top, float esc, Color c)
    {
        GameObject go = new GameObject("Plataforma");
        if (contenedorNivel != null) go.transform.SetParent(contenedorNivel, true);
        go.layer = capaSuelo;
        go.transform.position = new Vector3(cx, top, 0f);
        go.transform.localScale = new Vector3(esc, esc, 1f);

        bool custom;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ResolverSprite(spritePlataforma, out custom);
        sr.color = ResolverColor(custom, c);
        sr.sortingOrder = 10;

        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        AjustarColliderAlSprite(bc, sr);

        float topColliderMundo = go.transform.position.y + (bc.offset.y + bc.size.y * 0.5f) * go.transform.localScale.y;
        go.transform.position += new Vector3(0f, top - topColliderMundo, 0f);

        plataformasManuales.Add(go.transform);
        return go;
    }

    void CrearSuelo(float x1, float x2)
    {
        if (!crearSuelo) return;
        GameObject go = new GameObject("SueloAuto");
        if (contenedorNivel != null) go.transform.SetParent(contenedorNivel, true);
        go.layer = capaSuelo;
        go.transform.position = new Vector3((x1 + x2) / 2f, feetY - 0.5f, 0f);
        go.transform.localScale = new Vector3(x2 - x1, 1f, 1f);

        bool custom;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ResolverSprite(spriteSuelo, out custom);
        sr.color = ResolverColor(custom, new Color(0.18f, 0.18f, 0.24f));
        sr.sortingOrder = 8;

        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        AjustarColliderAlSprite(bc, sr);
    }

    void CrearLlave(Vector3 pos)
    {
        GameObject go = new GameObject("Llave");
        if (contenedorNivel != null) go.transform.SetParent(contenedorNivel, true);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(escalaLlave, escalaLlave, 1f);

        bool custom;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ResolverSprite(spriteLlave, out custom);
        sr.color = ResolverColor(custom, new Color(1f, 0.85f, 0.05f));
        sr.sortingOrder = 16;

        CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.45f;

        go.AddComponent<KeyPickup>();
        go.AddComponent<AutoLevelBob>();
    }

    void CrearCuracion(Vector3 pos)
    {
        GameObject go = new GameObject("Curacion");
        if (contenedorNivel != null) go.transform.SetParent(contenedorNivel, true);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(escalaCuracion, escalaCuracion, 1f);

        bool custom;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ResolverSprite(spriteCuracion, out custom);
        sr.color = ResolverColor(custom, new Color(0.2f, 1f, 0.45f));
        sr.sortingOrder = 16;

        CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.45f;

        go.AddComponent<HealPickup>();
        go.AddComponent<AutoLevelBob>();
    }

    void CrearPortal(Vector3 pos)
    {
        GameObject go = new GameObject("Portal");
        if (contenedorNivel != null) go.transform.SetParent(contenedorNivel, true);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(escalaPortal, escalaPortal, 1f);

        bool custom;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ResolverSprite(spritePortal, out custom);
        sr.color = ResolverColor(custom, Color.white);
        sr.sortingOrder = 14;

        CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.45f;

        portal = go.AddComponent<Portal>();
    }

    void CrearNino()
    {
        GameObject n = new GameObject("Nino");
        if (contenedorNivel != null) n.transform.SetParent(contenedorNivel, true);
        n.SetActive(false);
        n.layer = player.gameObject.layer;
        n.transform.position = new Vector3(startX + 1.5f, feetY + 1.5f, 0f);
        n.transform.localScale = new Vector3(escalaNino, escalaNino, 1f);

        bool customNino;
        SpriteRenderer sr = n.AddComponent<SpriteRenderer>();
        sr.sprite = ResolverSprite(spriteNino, out customNino);
        sr.color = ResolverColor(customNino, new Color(0.3f, 0.85f, 1f));
        sr.sortingOrder = 16;

        Rigidbody2D rb = n.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        BoxCollider2D bc = n.AddComponent<BoxCollider2D>();
        AjustarColliderAlSprite(bc, sr);

        Health h = n.AddComponent<Health>();
        h.maxHealth = 60;

        GameObject esc = new GameObject("Escudo");
        esc.transform.SetParent(n.transform, false);
        esc.transform.localScale = new Vector3(escalaEscudo, escalaEscudo, 1f);

        bool customEscudo;
        SpriteRenderer esr = esc.AddComponent<SpriteRenderer>();
        esr.sprite = ResolverSprite(spriteEscudo, out customEscudo);
        esr.color = customEscudo && usarColorBlancoConSprite
            ? new Color(1f, 1f, 1f, 0.9f)
            : new Color(0.25f, 0.75f, 1f, 0.45f);
        esr.sortingOrder = 17;

        NinoShield ns = n.AddComponent<NinoShield>();
        ns.escudoVisual = esc;

        n.SetActive(true);
    }

    // ---------------- UI / GAMEMANAGER ----------------

    void ConfigurarGM(bool winPorEnemigos, string texto)
    {
        if (!Application.isPlaying || gm == null) return;
        gm.winByEnemies = winPorEnemigos;
        gm.customObjective = texto;
        gm.keysNeeded = 3;
        gm.portal = portal;
        string finalTexto = string.IsNullOrEmpty(texto)
            ? "Objetivo: junta las llaves (0/3) y llega al portal"
            : texto;
        gm.objectiveText = CrearObjetivo(finalTexto);
        bloquearPosicionObjetivo = true;
    }

    Text CrearObjetivo(string texto)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return null;

        Transform existente = canvas.transform.Find("ObjectiveText");
        GameObject go = existente != null ? existente.gameObject : new GameObject("ObjectiveText");
        go.transform.SetParent(canvas.transform, false);

        Text t = go.GetComponent<Text>();
        if (t == null) t = go.AddComponent<Text>();

        Font f = null;
        if (UIManager.Instance != null && UIManager.Instance.scoreText != null) f = UIManager.Instance.scoreText.font;
        if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 20);

        t.font = f;
        t.fontSize = 22;
        t.color = Color.white;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.text = texto;

        RectTransform r = t.rectTransform;
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(24f, -155f);
        r.sizeDelta = new Vector2(720f, 60f);

        objetivoCreado = t;
        return t;
    }
}

public class AutoLevelBob : MonoBehaviour
{
    private Vector3 startPos;
    private float phase;

    void Start()
    {
        startPos = transform.position;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time * 3f + phase) * 0.12f, 0f);
    }
}