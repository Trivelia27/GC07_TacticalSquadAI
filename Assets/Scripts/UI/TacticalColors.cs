using UnityEngine;

// Warna konsisten untuk Role dan Tactical State di seluruh UI / visual debug.
public static class TacticalColors
{
    public static Color Role(EnemyRole role)
    {
        switch (role)
        {
            case EnemyRole.Attacker: return new Color(0.95f, 0.35f, 0.30f);
            case EnemyRole.FlankerLeft: return new Color(0.30f, 0.80f, 1.00f);
            case EnemyRole.FlankerRight: return new Color(0.40f, 0.60f, 1.00f);
            default: return new Color(0.72f, 0.45f, 1.00f);
        }
    }

    public static string RoleLabel(EnemyRole role)
    {
        switch (role)
        {
            case EnemyRole.Attacker: return "ATTACKER";
            case EnemyRole.FlankerLeft: return "FLANKER LEFT";
            case EnemyRole.FlankerRight: return "FLANKER RIGHT";
            default: return "COVER SHOOTER";
        }
    }

    public static Color State(TacticalState state)
    {
        switch (state)
        {
            case TacticalState.Attack: return new Color(0.95f, 0.28f, 0.30f);
            case TacticalState.MoveToCover: return new Color(0.72f, 0.45f, 1.00f);
            case TacticalState.Flank: return new Color(0.30f, 0.80f, 1.00f);
            case TacticalState.Search: return new Color(1.00f, 0.85f, 0.25f);
            default: return new Color(0.55f, 0.62f, 0.74f); // Guard
        }
    }

    public static string StateLabel(TacticalState state)
    {
        switch (state)
        {
            case TacticalState.MoveToCover: return "MOVE TO COVER";
            default: return state.ToString().ToUpperInvariant();
        }
    }
}
