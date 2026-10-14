using UnityEngine;

// Visual debug yang tampil di Game View (bukan hanya Scene View):
//  - kerucut FOV di lantai (putih = tidak melihat, merah = melihat Player)
//  - garis ke tujuan NavMeshAgent + lingkaran penanda tujuan (warna sesuai Role)
//  - garis ke Cover Point yang direservasi
// Tombol V mengaktifkan/menonaktifkan seluruh visual ini.
public class EnemyDebugVisual : MonoBehaviour
{
    [SerializeField] private EnemyTacticalAI tacticalAI;
    [SerializeField] private EnemyPerception perception;

    public static bool Enabled = true;

    private const int ArcSegments = 20;
    private const int CircleSegments = 24;

    private LineRenderer fov;
    private LineRenderer path;
    private LineRenderer marker;
    private LineRenderer cover;
    private Material lineMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Enabled = true;
    }

    private void Awake()
    {
        if (tacticalAI == null) tacticalAI = GetComponent<EnemyTacticalAI>();
        if (perception == null) perception = GetComponent<EnemyPerception>();

        lineMaterial = new Material(Shader.Find("Sprites/Default"));

        fov = CreateLine("FOV", ArcSegments + 3, 0.06f);
        path = CreateLine("Path", 2, 0.05f);
        marker = CreateLine("DestinationMarker", CircleSegments + 1, 0.05f);
        cover = CreateLine("CoverLink", 2, 0.04f);
    }

    private LineRenderer CreateLine(string lineName, int points, float width)
    {
        GameObject go = new GameObject(name + "_" + lineName);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = points;
        lr.startWidth = lr.endWidth = width;
        lr.material = lineMaterial;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.numCornerVertices = 2;
        return lr;
    }

    private void LateUpdate()
    {
        bool show = Enabled && perception != null && tacticalAI != null;

        fov.enabled = show;
        path.enabled = show;
        marker.enabled = show;
        cover.enabled = show;

        if (!show)
            return;

        DrawFov();
        DrawPathAndMarker();
        DrawCoverLink();
    }

    private void DrawFov()
    {
        Color c = perception.CanSeePlayer
            ? new Color(1f, 0.25f, 0.25f, 0.95f)
            : new Color(1f, 1f, 1f, 0.35f);

        fov.startColor = fov.endColor = c;

        Vector3 origin = transform.position;
        origin.y = 0.07f;

        float radius = perception.ViewRadius;
        float half = perception.ViewAngle * 0.5f;
        float baseAngle = transform.eulerAngles.y;

        int index = 0;
        fov.SetPosition(index++, origin);

        for (int i = 0; i <= ArcSegments; i++)
        {
            float angle = (baseAngle - half + perception.ViewAngle * i / ArcSegments) * Mathf.Deg2Rad;
            fov.SetPosition(index++, origin + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius);
        }

        fov.SetPosition(index, origin);
    }

    private void DrawPathAndMarker()
    {
        Color roleColor = TacticalColors.Role(tacticalAI.Role);
        bool moving = tacticalAI.CurrentState != TacticalState.Guard &&
                      (tacticalAI.Destination - transform.position).sqrMagnitude > 0.5f;

        path.enabled = moving;
        marker.enabled = moving;

        if (!moving)
            return;

        Vector3 from = transform.position;
        from.y = 0.09f;
        Vector3 to = tacticalAI.Destination;
        to.y = 0.09f;

        path.startColor = path.endColor = new Color(roleColor.r, roleColor.g, roleColor.b, 0.8f);
        path.SetPosition(0, from);
        path.SetPosition(1, to);

        marker.startColor = marker.endColor = roleColor;

        for (int i = 0; i <= CircleSegments; i++)
        {
            float a = i * Mathf.PI * 2f / CircleSegments;
            marker.SetPosition(i, to + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.6f);
        }
    }

    private void DrawCoverLink()
    {
        CoverPoint selected = tacticalAI.SelectedCover;
        cover.enabled = selected != null;

        if (selected == null)
            return;

        cover.startColor = cover.endColor = new Color(0.8f, 0.5f, 1f, 0.9f);
        cover.SetPosition(0, transform.position + Vector3.up * 0.2f);
        cover.SetPosition(1, selected.Position + Vector3.up * 0.2f);
    }

    private void OnDestroy()
    {
        foreach (LineRenderer lr in new[] { fov, path, marker, cover })
        {
            if (lr != null)
                Destroy(lr.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if (tacticalAI == null || perception == null || perception.Player == null)
            return;

        Gizmos.color = perception.CanSeePlayer ? Color.green : Color.gray;
        Gizmos.DrawLine(transform.position + Vector3.up, perception.Player.position + Vector3.up);
    }
}
