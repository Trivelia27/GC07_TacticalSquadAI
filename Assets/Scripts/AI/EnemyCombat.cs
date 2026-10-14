using UnityEngine;

// Serangan sederhana tanpa projectile (hitscan): Enemy menembak bila Player terlihat dan dalam jangkauan.
// Console: "Enemy_03_CoverShooter fires at Player". Tracer kuning singkat + damage kecil ke Player.
public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private EnemyPerception perception;

    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float fireInterval = 1f;

    [Tooltip("Jeda reaksi sejak Player pertama kali terlihat sampai Enemy mulai menembak.")]
    [SerializeField] private float reactionDelay = 0.4f;

    [SerializeField] private int damagePerShot = 1;

    [Tooltip("Peluang tembakan mengenai Player (Enemy tidak boleh terlalu sempurna).")]
    [Range(0f, 1f)]
    [SerializeField] private float hitChance = 0.5f;

    private float nextFireTime;
    private float seenSince = -1f;

    private LineRenderer tracer;
    private float tracerHideTime;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (perception == null)
            perception = GetComponent<EnemyPerception>();

        GameObject tracerObject = new GameObject(name + "_Tracer");
        tracer = tracerObject.AddComponent<LineRenderer>();
        tracer.positionCount = 2;
        tracer.startWidth = 0.07f;
        tracer.endWidth = 0.03f;
        tracer.material = new Material(Shader.Find("Sprites/Default"));
        tracer.startColor = new Color(1f, 0.9f, 0.35f, 1f);
        tracer.endColor = new Color(1f, 0.6f, 0.2f, 0.9f);
        tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tracer.enabled = false;
    }

    private void Start()
    {
        if (perception != null && perception.Player != null)
            playerHealth = perception.Player.GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (tracer.enabled && Time.time >= tracerHideTime)
            tracer.enabled = false;

        if (perception == null || perception.Player == null)
            return;

        if (!perception.CanSeePlayer)
        {
            seenSince = -1f;
            return;
        }

        if (seenSince < 0f)
            seenSince = Time.time;

        if (Time.time - seenSince < reactionDelay)
            return;

        if (perception.DistanceToPlayer > attackRange)
            return;

        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + fireInterval;
        Fire();
    }

    private void Fire()
    {
        Debug.Log(name + " fires at Player");

        bool hit = Random.value <= hitChance;

        Vector3 from = perception.EyePosition;
        Vector3 to = perception.Player.position;

        // Tembakan meleset: tracer menyimpang ke samping.
        if (!hit)
            to += new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(-0.4f, 0.6f), Random.Range(-1.2f, 1.2f));

        tracer.SetPosition(0, from);
        tracer.SetPosition(1, to);
        tracer.enabled = true;
        tracerHideTime = Time.time + 0.08f;

        if (hit && playerHealth != null)
            playerHealth.TakeDamage(damagePerShot);
    }

    private void OnDestroy()
    {
        if (tracer != null)
            Destroy(tracer.gameObject);
    }
}
