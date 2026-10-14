using UnityEngine;

// Perception: apa yang bisa DILIHAT Enemy sekarang (jarak + Field of View + Line of Sight).
// Tidak menyimpan ingatan (itu tugas EnemyMemory) dan tidak mengambil keputusan.
public class EnemyPerception : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Vision")]
    [SerializeField] private float viewRadius = 12f;

    [Range(0f, 360f)]
    [SerializeField] private float viewAngle = 90f;

    [SerializeField] private LayerMask obstacleMask;

    // Pivot kapsul berada di tengah badan, sehingga mata berada +0.5 m di atas pivot.
    [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 0.5f, 0f);

    public bool CanSeePlayer { get; private set; }
    public float DistanceToPlayer { get; private set; }

    // Posisi Player pada saat terakhir benar-benar terlihat. Dipakai Memory/Squad agar yang
    // dilaporkan adalah apa yang DILIHAT, bukan posisi Player yang sebenarnya saat ini.
    public Vector3 SeenPlayerPosition { get; private set; }

    public Transform Player => player;
    public float ViewRadius => viewRadius;
    public float ViewAngle => viewAngle;
    public Vector3 EyePosition => transform.position + eyeOffset;

    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        CheckVision();
    }

    private void CheckVision()
    {
        CanSeePlayer = false;

        if (player == null)
            return;

        Vector3 directionToPlayer = player.position - transform.position;
        DistanceToPlayer = directionToPlayer.magnitude;

        // Player yang sudah kalah tidak lagi dianggap target.
        if (playerHealth != null && playerHealth.IsDead)
            return;

        // 1. Jarak
        if (DistanceToPlayer > viewRadius)
            return;

        // 2. Field of View (diukur pada bidang horizontal)
        Vector3 flat = directionToPlayer;
        flat.y = 0f;

        if (flat.sqrMagnitude > 0.0001f &&
            Vector3.Angle(transform.forward, flat.normalized) > viewAngle * 0.5f)
            return;

        // 3. Line of Sight: ada obstacle di antara mata Enemy dan Player?
        Vector3 eye = EyePosition;
        Vector3 rayDirection = player.position - eye;

        bool blocked = Physics.Raycast(
            eye,
            rayDirection.normalized,
            rayDirection.magnitude,
            obstacleMask);

        CanSeePlayer = !blocked;

        if (CanSeePlayer)
            SeenPlayerPosition = player.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 left = DirectionFromAngle(-viewAngle * 0.5f);
        Vector3 right = DirectionFromAngle(viewAngle * 0.5f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + left * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + right * viewRadius);
    }

    private Vector3 DirectionFromAngle(float angle)
    {
        float radians = (transform.eulerAngles.y + angle) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
    }
}
