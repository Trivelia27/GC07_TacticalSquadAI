using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// HUD debugging Tactical AI:
//  - Kartu SQUAD STATUS: Calm/Alert, Shared Target, Last Known Position, Live Contact, Info Sharing
//  - Daftar anggota: Role, Tactical State, apakah melihat Player, Cover yang direservasi
//  - Kartu COVER POINTS: bebas / direservasi siapa
// Hotkey: V = visual debug, T = Shared Information ON/OFF (Eksperimen 3), K = isi health Player, R = restart.
public class SquadHUD : MonoBehaviour
{
    [SerializeField] private SquadManager squad;
    [SerializeField] private CoverManager covers;
    [SerializeField] private PlayerHealth playerHealth;

    private class MemberRow
    {
        public EnemyTacticalAI ai;
        public Chip stateChip;
        public Text subText;
    }

    private class CoverCell
    {
        public CoverPoint cover;
        public Image dot;
        public Text text;
    }

    private Chip alertChip;
    private Text targetValue;
    private Text lastKnownValue;
    private Text contactValue;
    private Text sharingValue;

    private readonly List<MemberRow> rows = new List<MemberRow>();
    private readonly List<CoverCell> cells = new List<CoverCell>();

    private static readonly Color Ok = new Color(0.30f, 0.85f, 0.45f);
    private static readonly Color Bad = new Color(0.95f, 0.28f, 0.30f);

    private void Start()
    {
        Build();
    }

    private void Update()
    {
        HandleHotkeys();
        Refresh();
    }

    // ------------------------------------------------------------------

    private void HandleHotkeys()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.vKey.wasPressedThisFrame)
            EnemyDebugVisual.Enabled = !EnemyDebugVisual.Enabled;

        if (kb.tKey.wasPressedThisFrame)
        {
            bool next = !AllSharing();

            foreach (EnemyTacticalAI member in squad.Members)
                member.ShareWithSquad = next;

            Debug.Log("[Squad] Shared information " + (next ? "ON" : "OFF"));
        }

        if (kb.kKey.wasPressedThisFrame && playerHealth != null)
            playerHealth.ResetHealth();

        if (kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private bool AllSharing()
    {
        foreach (EnemyTacticalAI member in squad.Members)
        {
            if (!member.ShareWithSquad)
                return false;
        }

        return true;
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------

    private void Build()
    {
        Transform root = UIKit.SharedRoot;

        UIKit.TitlePill(root, "SQUAD TACTICAL AI");

        const float cardW = 440f;
        const float pad = 20f;
        float innerW = cardW - pad * 2f;

        int memberCount = squad.Members.Count;
        float cardH = 188f + memberCount * 62f + 12f;

        RectTransform card = UIKit.Card(root, "SquadCard", 24f, 24f, cardW, cardH);

        Image accent = UIKit.NewImage(card, "Accent", UIKit.GreenColor, 2);
        UIKit.TopLeft(accent.rectTransform, pad, 19f, 4f, 16f);

        Text title = UIKit.NewText(card, "Title", "SQUAD STATUS", 15, UIKit.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold, true);
        UIKit.TopLeft(title.rectTransform, pad + 12f, 14f, 200f, 26f);

        alertChip = new Chip(card, "AlertChip", 14, 12);
        UIKit.TopLeft(alertChip.Root, cardW - pad - 110f, 14f, 110f, 26f);

        targetValue = KeyValue(card, pad, 52f, innerW, "Shared Target");
        lastKnownValue = KeyValue(card, pad, 76f, innerW, "Last Known Position");
        contactValue = KeyValue(card, pad, 100f, innerW, "Live Contact");
        sharingValue = KeyValue(card, pad, 124f, innerW, "Shared Information");

        Text caption = UIKit.NewText(card, "MembersCaption", "SQUAD MEMBERS", 11, UIKit.DimTextColor, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.TopLeft(caption.rectTransform, pad, 156f, innerW, 16f);

        for (int i = 0; i < memberCount; i++)
        {
            EnemyTacticalAI ai = squad.Members[i];
            float y = 178f + i * 62f;

            Image bg = UIKit.NewImage(card, "Member_" + ai.name, UIKit.RowColor, 10);
            UIKit.TopLeft(bg.rectTransform, pad, y, innerW, 54f);

            Image strip = UIKit.NewImage(bg.transform, "Strip", TacticalColors.Role(ai.Role), 2);
            UIKit.TopLeft(strip.rectTransform, 9f, 10f, 4f, 34f);

            Text role = UIKit.NewText(bg.transform, "Role", TacticalColors.RoleLabel(ai.Role), 15, UIKit.TextColor,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.TopLeft(role.rectTransform, 24f, 6f, 190f, 24f);

            Text sub = UIKit.NewText(bg.transform, "Sub", "", 12, UIKit.DimTextColor, TextAnchor.MiddleLeft);
            UIKit.TopLeft(sub.rectTransform, 24f, 29f, 230f, 20f);

            Chip chip = new Chip(bg.transform, "StateChip", 12, 11);
            UIKit.TopRight(chip.Root, 10f, 14f, 136f, 26f);

            rows.Add(new MemberRow { ai = ai, stateChip = chip, subText = sub });
        }

        BuildCoverCard(root, 24f + cardH + 14f, cardW, pad);

        UIKit.ControlsBar(root,
            new[] { "V", "T", "K", "R", "WASD" },
            new[] { "Debug visual", "Shared info", "Heal Player", "Restart", "Gerak" });
    }

    private void BuildCoverCard(Transform root, float y, float cardW, float pad)
    {
        IReadOnlyList<CoverPoint> points = covers != null ? covers.CoverPoints : null;
        if (points == null || points.Count == 0)
            return;

        int rowsNeeded = (points.Count + 1) / 2;
        float height = 46f + rowsNeeded * 30f + 10f;

        RectTransform card = UIKit.Card(root, "CoverCard", 24f, y, cardW, height);

        Image accent = UIKit.NewImage(card, "Accent", new Color(0.2f, 0.9f, 0.7f), 2);
        UIKit.TopLeft(accent.rectTransform, pad, 19f, 4f, 16f);

        Text title = UIKit.NewText(card, "Title", "COVER POINTS", 15, UIKit.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold, true);
        UIKit.TopLeft(title.rectTransform, pad + 12f, 14f, 200f, 26f);

        float cellW = (cardW - pad * 2f) * 0.5f;

        for (int i = 0; i < points.Count; i++)
        {
            float x = pad + (i % 2) * cellW;
            float cy = 48f + (i / 2) * 30f;

            Image dot = UIKit.NewImage(card, "Dot", Ok, 6);
            UIKit.TopLeft(dot.rectTransform, x, cy + 8f, 12f, 12f);

            Text text = UIKit.NewText(card, "Text", "", 13, UIKit.TextColor, TextAnchor.MiddleLeft);
            UIKit.TopLeft(text.rectTransform, x + 20f, cy, cellW - 24f, 28f);

            cells.Add(new CoverCell { cover = points[i], dot = dot, text = text });
        }
    }

    private static Text KeyValue(Transform parent, float x, float y, float width, string key)
    {
        Text k = UIKit.NewText(parent, "Key_" + key, key, 13, UIKit.DimTextColor, TextAnchor.MiddleLeft);
        UIKit.TopLeft(k.rectTransform, x, y, 200f, 22f);

        Text v = UIKit.NewText(parent, "Value_" + key, "-", 13, UIKit.TextColor, TextAnchor.MiddleRight, FontStyle.Bold);
        UIKit.TopLeft(v.rectTransform, x + width - 240f, y, 240f, 22f);
        return v;
    }

    // ------------------------------------------------------------------
    // Refresh
    // ------------------------------------------------------------------

    private void Refresh()
    {
        if (alertChip == null)
            return;

        alertChip.Set(squad.IsAlert ? "ALERT" : "CALM", squad.IsAlert ? Bad : Ok);

        targetValue.text = squad.SharedTarget != null ? squad.SharedTarget.name : "-";

        if (float.IsInfinity(squad.SecondsSinceReport))
        {
            lastKnownValue.text = "-";
        }
        else
        {
            Vector3 p = squad.SharedLastKnownPosition;
            lastKnownValue.text = "(" + p.x.ToString("F1") + ", " + p.z.ToString("F1") + ")   " +
                                  squad.SecondsSinceReport.ToString("F1") + " dtk lalu";
        }

        bool contact = squad.HasLiveContact;
        contactValue.text = contact ? "YA" : "tidak";
        contactValue.color = contact ? Bad : UIKit.TextColor;

        bool sharing = AllSharing();
        sharingValue.text = sharing ? "ON" : "OFF";
        sharingValue.color = sharing ? Ok : Bad;

        foreach (MemberRow row in rows)
        {
            EnemyTacticalAI ai = row.ai;
            TacticalState state = ai.CurrentState;

            row.stateChip.Set(TacticalColors.StateLabel(state), TacticalColors.State(state));

            bool sees = ai.Perception != null && ai.Perception.CanSeePlayer;
            string coverText = ai.SelectedCover != null ? ai.SelectedCover.name : "-";
            row.subText.text = (sees ? "melihat Player" : "tidak melihat") + "   |   cover: " + coverText;
        }

        foreach (CoverCell cell in cells)
        {
            if (cell.cover == null)
                continue;

            bool occupied = cell.cover.IsOccupied;
            cell.dot.color = occupied ? Bad : Ok;

            string owner = occupied && cell.cover.OccupiedBy != null
                ? TacticalColors.RoleLabel(cell.cover.OccupiedBy.Role)
                : "bebas";

            cell.text.text = cell.cover.name.Replace("CoverPoint_", "Cover ") + "  -  " + owner;
        }
    }
}
