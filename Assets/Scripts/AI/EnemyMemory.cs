using UnityEngine;

// Local Memory: apa yang DIKETAHUI Enemy ini sendiri (posisi terakhir Player + batas waktu ingatan).
public class EnemyMemory : MonoBehaviour
{
    [SerializeField] private float memoryDuration = 5f;

    // Belum pernah melihat Player = tidak punya ingatan.
    // (Jika diisi 0, Time.time - 0 <= 5 membuat Enemy "ingat" Player palsu pada 5 detik pertama.)
    private float lastSeenTime = float.NegativeInfinity;

    public Vector3 LastKnownPlayerPosition { get; private set; }

    public float LastSeenTime => lastSeenTime;

    public float MemoryAge => Time.time - lastSeenTime;

    public bool HasValidMemory => MemoryAge <= memoryDuration;

    public void Remember(Vector3 position)
    {
        LastKnownPlayerPosition = position;
        lastSeenTime = Time.time;
    }

    public void ClearMemory()
    {
        lastSeenTime = float.NegativeInfinity;
    }
}
