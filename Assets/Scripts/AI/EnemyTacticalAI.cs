using UnityEngine;
using UnityEngine.AI;

public enum TacticalState
{
    Guard,
    Attack,
    MoveToCover,
    Flank,
    Search
}

// Decision + Tactical Positioning. Pipeline per Enemy:
//   Sense (EnemyPerception) -> Remember (EnemyMemory) -> Share (SquadManager)
//   -> Evaluate (tiap decisionInterval) -> pilih aksi sesuai Role -> pilih posisi taktis
//   -> Navigate (NavMeshAgent)
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyTacticalAI : MonoBehaviour
{
    [Header("Role")]
    [SerializeField] private EnemyRole role;

    [Header("References")]
    [SerializeField] private EnemyPerception perception;
    [SerializeField] private EnemyMemory memory;
    [SerializeField] private SquadManager squadManager;
    [SerializeField] private CoverManager coverManager;
    [SerializeField] private NavMeshAgent agent;

    [Header("Information")]
    [Tooltip("ON: melapor ke dan memakai data SquadManager. OFF: hanya memakai perception & memory sendiri (Eksperimen 3).")]
    [SerializeField] private bool shareWithSquad = true;

    [Header("Combat")]
    [SerializeField] private float idealCombatDistance = 6f;

    [Header("Flanking")]
    [SerializeField] private float flankDistance = 5f;
    [SerializeField] private float flankBackOffset = 1.5f;

    [Header("Cover")]
    [SerializeField] private float coverArriveDistance = 1f;

    [Header("Search")]
    [SerializeField] private float searchSpreadRadius = 2.5f;
    [SerializeField] private float searchLookSpeed = 90f;

    [Header("Decision")]
    [SerializeField] private float decisionInterval = 0.35f;

    [Header("Debug")]
    [SerializeField] private TacticalState currentState = TacticalState.Guard;

    public EnemyRole Role => role;
    public TacticalState CurrentState => currentState;
    public CoverPoint SelectedCover => selectedCover;
    public EnemyPerception Perception => perception;
    public EnemyMemory Memory => memory;
    public Vector3 FlankPosition => flankPosition;
    public bool HasFlankPosition => currentState == TacticalState.Flank;

    public bool ShareWithSquad
    {
        get => shareWithSquad;
        set => shareWithSquad = value;
    }

    public Vector3 Destination => agent != null && agent.hasPath ? agent.destination : transform.position;

    private float nextDecisionTime;
    private CoverPoint selectedCover;

    private Vector3 homePosition;
    private Quaternion homeRotation;

    // Hasil evaluasi terakhir, dipakai tiap frame untuk menghadap target.
    private Transform cachedTarget;
    private bool hasContact;

    private Vector3 searchPoint;
    private bool searchArrived;
    private Vector3 flankPosition;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (perception == null) perception = GetComponent<EnemyPerception>();
        if (memory == null) memory = GetComponent<EnemyMemory>();

        if (squadManager != null)
            squadManager.Register(this);
    }

    private void Start()
    {
        homePosition = transform.position;
        homeRotation = transform.rotation;

        // Antar Enemy diberi offset agar keputusan tidak dihitung pada frame yang sama.
        nextDecisionTime = Time.time + Random.Range(0f, decisionInterval);
    }

    private void Update()
    {
        UpdatePerceptionAndMemory();

        // Keputusan taktis hanya dihitung ~3x per detik; pergerakan tetap dijalankan Unity tiap frame.
        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + decisionInterval;
            EvaluateTacticalDecision();
        }

        UpdateFacing();
    }

    // ==================================================
    // SENSE -> REMEMBER -> SHARE
    // ==================================================

    private void UpdatePerceptionAndMemory()
    {
        if (perception == null || !perception.CanSeePlayer)
            return;

        Transform player = perception.Player;
        if (player == null)
            return;

        Vector3 seenPosition = perception.SeenPlayerPosition;

        if (memory != null)
            memory.Remember(seenPosition);

        if (shareWithSquad && squadManager != null)
            squadManager.ReportPlayerSeen(player, seenPosition);
    }

    // Sumber pengetahuan: data squad (shared) atau data milik sendiri (local).
    private void GetKnowledge(out bool alert, out bool contact, out Transform target, out Vector3 lastKnown)
    {
        if (shareWithSquad && squadManager != null)
        {
            alert = squadManager.IsAlert;
            contact = squadManager.HasLiveContact;
            target = squadManager.SharedTarget;
            lastKnown = squadManager.SharedLastKnownPosition;
            return;
        }

        alert = memory != null && memory.HasValidMemory;
        contact = perception != null && perception.CanSeePlayer;
        target = perception != null ? perception.Player : null;
        lastKnown = memory != null ? memory.LastKnownPlayerPosition : transform.position;
    }

    // ==================================================
    // EVALUATE -> CHOOSE TACTICAL ACTION
    // ==================================================

    private void EvaluateTacticalDecision()
    {
        GetKnowledge(out bool alert, out bool contact, out Transform target, out Vector3 lastKnown);

        hasContact = alert && contact && target != null;
        cachedTarget = hasContact ? target : null;

        if (!alert)
        {
            ReturnToPost();
            return;
        }

        if (!hasContact)
        {
            SearchLastKnownPosition(lastKnown);
            return;
        }

        switch (role)
        {
            case EnemyRole.Attacker:
                HandleAttacker(target);
                break;

            case EnemyRole.FlankerLeft:
                HandleFlanker(target, -1f);
                break;

            case EnemyRole.FlankerRight:
                HandleFlanker(target, 1f);
                break;

            case EnemyRole.CoverShooter:
                HandleCoverShooter(target);
                break;
        }
    }

    // Attacker: menekan dari depan; mendekat sampai jarak tempur ideal.
    private void HandleAttacker(Transform target)
    {
        currentState = TacticalState.Attack;

        float distance = FlatDistance(transform.position, target.position);
        bool hasLineOfSight = perception != null && perception.CanSeePlayer;

        // Berhenti di jarak ideal HANYA jika Player benar-benar terlihat; jika terhalang tembok,
        // terus bergerak mencari sudut pandang.
        if (!hasLineOfSight)
            MoveTo(target.position, 1.5f);
        else if (distance > idealCombatDistance)
            MoveTo(target.position, idealCombatDistance * 0.8f);
        else
            Stop();
    }

    // Flanker: bergerak ke sisi Player (kanan/kiri Player), sedikit di belakangnya.
    private void HandleFlanker(Transform target, float side)
    {
        Vector3 desired =
            target.position
            + target.right * flankDistance * side
            - target.forward * flankBackOffset;

        // Titik hasil rumus belum tentu berada di NavMesh (bisa di dalam tembok / luar map).
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            flankPosition = hit.position;

            if (FlatDistance(transform.position, flankPosition) <= 1.2f)
            {
                currentState = TacticalState.Attack; // sudah di posisi flank
                Stop();
            }
            else
            {
                currentState = TacticalState.Flank;
                MoveTo(flankPosition, 0.3f);
            }
        }
        else
        {
            // Fallback: titik flank tidak valid -> tekan langsung seperti Attacker.
            HandleAttacker(target);
        }
    }

    // Cover Shooter: pilih Cover Point, reservasi, bergerak ke sana, lalu menembak dari cover.
    private void HandleCoverShooter(Transform target)
    {
        if (coverManager == null)
        {
            HandleAttacker(target);
            return;
        }

        // Cover lama tidak lagi melindungi (Player berpindah) -> lepas reservasi.
        if (selectedCover != null && !coverManager.IsProtected(selectedCover.Position, target))
            ReleaseCover();

        if (selectedCover == null)
        {
            CoverPoint candidate = coverManager.FindBestCover(this, target);

            if (candidate != null && candidate.TryReserve(this))
                selectedCover = candidate;
        }

        // Tidak ada cover tersedia -> fallback agar AI tidak berhenti total.
        if (selectedCover == null)
        {
            HandleAttacker(target);
            return;
        }

        if (FlatDistance(transform.position, selectedCover.Position) <= coverArriveDistance)
        {
            currentState = TacticalState.Attack; // menembak dari cover
            Stop();
        }
        else
        {
            currentState = TacticalState.MoveToCover;
            MoveTo(selectedCover.Position, 0.3f);
        }
    }

    // Player hilang dari pandangan: seluruh squad menuju posisi terakhir (tersebar sedikit agar tidak menumpuk).
    private void SearchLastKnownPosition(Vector3 lastKnown)
    {
        currentState = TacticalState.Search;
        ReleaseCover();

        int index = squadManager != null ? squadManager.IndexOf(this) : (int)role;
        float angle = index * 120f * Mathf.Deg2Rad;
        Vector3 desired = lastKnown + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * searchSpreadRadius;

        if (!NavMesh.SamplePosition(desired, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            hit.position = lastKnown;

        if ((searchPoint - hit.position).sqrMagnitude > 0.25f)
            searchArrived = false;

        searchPoint = hit.position;

        if (FlatDistance(transform.position, searchPoint) <= 1.2f)
        {
            searchArrived = true;
            Stop();
        }
        else
        {
            MoveTo(searchPoint, 0.3f);
        }
    }

    // Tidak ada ancaman: kembali ke pos jaga.
    private void ReturnToPost()
    {
        currentState = TacticalState.Guard;
        ReleaseCover();
        searchArrived = false;

        if (FlatDistance(transform.position, homePosition) > 0.8f)
            MoveTo(homePosition, 0.3f);
        else
            Stop();
    }

    // ==================================================
    // NAVIGATE
    // ==================================================

    private void MoveTo(Vector3 position, float stoppingDistance)
    {
        agent.isStopped = false;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(position);
    }

    private void Stop()
    {
        if (agent.hasPath)
            agent.ResetPath();
    }

    private void UpdateFacing()
    {
        // Saat bergerak, rotasi diatur NavMeshAgent.
        if (agent.velocity.sqrMagnitude > 0.1f)
            return;

        if (currentState == TacticalState.Guard)
        {
            if (FlatDistance(transform.position, homePosition) <= 1f)
                RotateTowards(homeRotation, 4f);
            return;
        }

        if (currentState == TacticalState.Search)
        {
            if (searchArrived)
                transform.Rotate(0f, searchLookSpeed * Time.deltaTime, 0f); // melihat sekeliling
            return;
        }

        if (cachedTarget != null)
        {
            Vector3 direction = cachedTarget.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
                RotateTowards(Quaternion.LookRotation(direction), 6f);
        }
    }

    private void RotateTowards(Quaternion target, float speed)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, target, speed * Time.deltaTime);
    }

    private void ReleaseCover()
    {
        if (selectedCover == null)
            return;

        selectedCover.Release(this);
        selectedCover = null;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDisable()
    {
        ReleaseCover();
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        if (selectedCover != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, selectedCover.Position);
        }

        if (currentState == TacticalState.Flank)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(flankPosition, 0.5f);
        }
    }
}
