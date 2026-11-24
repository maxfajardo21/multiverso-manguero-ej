using UnityEngine;

public class BossPhases : MonoBehaviour
{
    public Transform[] spawnPoints;    
    public GameObject enemyPrefab;
    public Portal portal;
    public float distanciaInicio = 12f;

    private Health h;
    private EnemyAI ai;
    private Transform player;
    private int fase = 1;
    private bool iniciado;

    void Start()
    {
        h = GetComponent<Health>();
        ai = GetComponent<EnemyAI>();
        h.OnHealthChanged += CambioVida;
        h.OnDeath += Muerte;
    }

    void OnDestroy()
    {
        if (h != null)
        {
            h.OnHealthChanged -= CambioVida;
            h.OnDeath -= Muerte;
        }
    }

    void Update()
    {
        if (iniciado) return;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        if (Vector2.Distance(player.position, transform.position) < distanciaInicio)
        {
            iniciado = true;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                GameObject go = Instantiate(enemyPrefab, spawnPoints[i].position, Quaternion.identity);
                go.SetActive(true);
            }
        }
    }

    void CambioVida(int cur, int max)
    {
        float p = cur / (float)max;

        if (fase == 1 && p <= 0.66f)
        {
            fase = 2;
            ai.moveSpeed *= 1.4f;
            ai.attackCooldown *= 0.7f;
            h.SetColor(new Color(1f, 0.6f, 0.4f));
        }
        else if (fase == 2 && p <= 0.33f)
        {
            fase = 3;
            ai.moveSpeed *= 1.3f;
            ai.attackCooldown *= 0.6f;
            ai.damage += 5;
            h.SetColor(Color.red);
        }
    }

    void Muerte(Health _)
    {
        if (portal != null) portal.Abrir();
    }
}