using UnityEngine;

// Satu titik cover. Status Occupied + OccupiedBy dipakai untuk Cover Reservation:
// sebuah cover hanya boleh dipakai satu Enemy pada satu waktu.
public class CoverPoint : MonoBehaviour
{
    [SerializeField] private Renderer marker;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock block;
    private bool shownOccupied;
    private bool colorInitialized;

    public bool IsOccupied { get; private set; }
    public EnemyTacticalAI OccupiedBy { get; private set; }
    public Vector3 Position => transform.position;

    public bool TryReserve(EnemyTacticalAI enemy)
    {
        if (IsOccupied && OccupiedBy != enemy)
            return false;

        IsOccupied = true;
        OccupiedBy = enemy;
        return true;
    }

    public void Release(EnemyTacticalAI enemy)
    {
        if (OccupiedBy != enemy)
            return;

        IsOccupied = false;
        OccupiedBy = null;
    }

    private void Update()
    {
        // Marker di lantai: hijau = bebas, merah = sudah direservasi.
        if (marker == null)
            return;

        if (colorInitialized && shownOccupied == IsOccupied)
            return;

        colorInitialized = true;
        shownOccupied = IsOccupied;

        if (block == null)
            block = new MaterialPropertyBlock();

        Color color = IsOccupied ? new Color(0.95f, 0.30f, 0.30f) : new Color(0.20f, 0.90f, 0.70f);
        marker.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        marker.SetPropertyBlock(block);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
    }
}
