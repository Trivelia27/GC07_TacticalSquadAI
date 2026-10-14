using System.Collections.Generic;
using UnityEngine;

// Memilih Cover Point terbaik untuk seorang Enemy.
//
// Syarat sebuah cover:
//   1. tidak sedang direservasi Enemy lain,
//   2. terlindung dari Player: Raycast (tinggi jongkok) cover -> Player mengenai penghalang,
//   3. tidak terlalu dekat / terlalu jauh dari Player.
// Skor: cover yang tetap memiliki garis tembak (Raycast setinggi mata tidak terhalang tembok tinggi)
// diutamakan, lalu yang paling dekat dengan Enemy.
public class CoverManager : MonoBehaviour
{
    [SerializeField] private CoverPoint[] coverPoints;

    [Header("Masks")]
    [Tooltip("Penghalang tembakan saat jongkok: tembok tinggi + crate rendah.")]
    [SerializeField] private LayerMask coverBlockMask;

    [Tooltip("Penghalang pandangan: hanya tembok tinggi (crate rendah bisa ditembak melewatinya).")]
    [SerializeField] private LayerMask visionMask;

    [Header("Heights")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float shootHeight = 1.5f;

    [Header("Selection")]
    [SerializeField] private float minDistanceFromTarget = 3f;
    [SerializeField] private float maxDistanceFromTarget = 14f;

    public IReadOnlyList<CoverPoint> CoverPoints => coverPoints;

    public CoverPoint FindBestCover(EnemyTacticalAI enemy, Transform target)
    {
        if (target == null || coverPoints == null)
            return null;

        CoverPoint best = null;
        float bestScore = float.MinValue;

        foreach (CoverPoint cover in coverPoints)
        {
            if (cover == null)
                continue;

            if (cover.IsOccupied && cover.OccupiedBy != enemy)
                continue;

            float targetDistance = FlatDistance(cover.Position, target.position);
            if (targetDistance < minDistanceFromTarget || targetDistance > maxDistanceFromTarget)
                continue;

            if (!IsProtected(cover.Position, target))
                continue;

            float enemyDistance = FlatDistance(enemy.transform.position, cover.Position);

            float distanceScore = 1f / (1f + enemyDistance * 0.15f);
            float firingScore = HasFiringLine(cover.Position, target) ? 1f : 0f;

            float score = 0.5f * firingScore + 0.5f * distanceScore;

            if (score > bestScore)
            {
                bestScore = score;
                best = cover;
            }
        }

        return best;
    }

    // Terlindung = sinar dari titik cover (setinggi badan jongkok) ke Player terhalang.
    public bool IsProtected(Vector3 coverPosition, Transform target)
    {
        if (target == null)
            return false;

        // target.position = pusat kapsul Player (setinggi dada).
        Vector3 origin = coverPosition + Vector3.up * crouchHeight;
        Vector3 direction = target.position - origin;

        return Physics.Raycast(origin, direction.normalized, direction.magnitude, coverBlockMask);
    }

    // Masih bisa menembak dari cover = sinar setinggi mata tidak terhalang tembok tinggi.
    public bool HasFiringLine(Vector3 coverPosition, Transform target)
    {
        if (target == null)
            return false;

        Vector3 origin = coverPosition + Vector3.up * shootHeight;
        Vector3 direction = target.position - origin;

        return !Physics.Raycast(origin, direction.normalized, direction.magnitude, visionMask);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
