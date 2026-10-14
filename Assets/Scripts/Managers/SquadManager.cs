using System.Collections.Generic;
using UnityEngine;

// Shared Memory / Shared Blackboard squad: target bersama, posisi terakhir Player, dan status Alert.
// Satu Enemy yang melihat Player melapor ke sini, lalu seluruh squad ikut mengetahuinya.
public class SquadManager : MonoBehaviour
{
    [Header("Shared Tactical Data")]
    [SerializeField] private Transform sharedTarget;

    [SerializeField] private float sharedMemoryDuration = 8f;

    // Selama laporan terakhir masih lebih baru dari ini, dianggap ada Enemy yang SEDANG melihat Player.
    [SerializeField] private float liveContactWindow = 0.4f;

    private readonly List<EnemyTacticalAI> members = new List<EnemyTacticalAI>();

    private float lastReportTime = float.NegativeInfinity;

    public Transform SharedTarget => sharedTarget;
    public Vector3 SharedLastKnownPosition { get; private set; }
    public bool IsAlert { get; private set; }

    public float LastReportTime => lastReportTime;
    public float SecondsSinceReport => Time.time - lastReportTime;

    // True bila minimal satu anggota squad melihat Player saat ini (bukan sekadar ingatan).
    public bool HasLiveContact => IsAlert && SecondsSinceReport <= liveContactWindow;

    public IReadOnlyList<EnemyTacticalAI> Members => members;

    public void Register(EnemyTacticalAI member)
    {
        if (!members.Contains(member))
            members.Add(member);
    }

    public int IndexOf(EnemyTacticalAI member)
    {
        return Mathf.Max(0, members.IndexOf(member));
    }

    public void ReportPlayerSeen(Transform target, Vector3 position)
    {
        sharedTarget = target;
        SharedLastKnownPosition = position;
        lastReportTime = Time.time;

        if (!IsAlert)
        {
            IsAlert = true;
            Debug.Log("[Squad] ALERT - Player terlihat.");
        }
    }

    private void Update()
    {
        if (!IsAlert)
            return;

        // Memory timeout: tanpa laporan baru, squad kembali Calm.
        if (SecondsSinceReport > sharedMemoryDuration)
        {
            IsAlert = false;
            sharedTarget = null;
            Debug.Log("[Squad] CALM - memory squad habis.");
        }
    }
}
