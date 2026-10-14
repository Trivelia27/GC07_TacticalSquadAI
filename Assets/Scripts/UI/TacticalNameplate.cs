using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Nameplate di atas kepala Enemy: Role, Tactical State saat ini, dan penanda "SEES" saat Player terlihat.
// Digambar di canvas overlay sehingga ukurannya konsisten di layar. Nameplate yang saling menimpa
// (Enemy berdekatan) otomatis disusun ke atas agar tetap terbaca.
public class TacticalNameplate : MonoBehaviour
{
    [SerializeField] private float heightAboveTarget = 2.5f;

    private const float PlateWidth = 204f;
    private const float PlateHeight = 52f;
    private const float Gap = 6f;

    private static readonly List<TacticalNameplate> all = new List<TacticalNameplate>();
    private static int updatedThisFrame;
    private static int lastFrame = -1;

    private EnemyTacticalAI ai;
    private RectTransform plate;
    private RectTransform canvasRect;
    private Chip stateChip;
    private Image seesPill;
    private Camera cam;
    private Vector2 desired;
    private bool visible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        all.Clear();
        updatedThisFrame = 0;
        lastFrame = -1;
    }

    private void Start()
    {
        ai = GetComponent<EnemyTacticalAI>();
        if (ai == null)
            return;

        canvasRect = UIKit.SharedRoot;

        plate = UIKit.Rect(name + "_Nameplate", canvasRect);
        plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
        plate.pivot = new Vector2(0.5f, 0f);
        plate.sizeDelta = new Vector2(PlateWidth, PlateHeight);

        Image pointer = UIKit.NewImage(plate, "Pointer", UIKit.PanelColor);
        RectTransform pr = pointer.rectTransform;
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0f);
        pr.pivot = new Vector2(0.5f, 0.5f);
        pr.anchoredPosition = Vector2.zero;
        pr.sizeDelta = new Vector2(14f, 14f);
        pr.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image bg = UIKit.NewImage(plate, "Background", UIKit.PanelColor, 12);
        UIKit.Stretch(bg.rectTransform);

        Color roleColor = TacticalColors.Role(ai.Role);

        Image accent = UIKit.NewImage(plate, "Accent", roleColor, 2);
        UIKit.TopLeft(accent.rectTransform, 10f, 9f, 4f, 16f);

        Text roleText = UIKit.NewText(plate, "Role", TacticalColors.RoleLabel(ai.Role), 14, UIKit.TextColor,
            TextAnchor.MiddleLeft, FontStyle.Bold, true);
        UIKit.TopLeft(roleText.rectTransform, 22f, 5f, 130f, 24f);

        seesPill = UIKit.NewImage(plate, "SeesPill", UIKit.RedColor, 8);
        UIKit.TopRight(seesPill.rectTransform, 8f, 8f, 40f, 18f);
        Text seesText = UIKit.NewText(seesPill.transform, "Text", "SEES", 11, UIKit.DarkTextColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        UIKit.Stretch(seesText.rectTransform);

        stateChip = new Chip(plate, "StateChip", 12, 9);
        UIKit.TopLeft(stateChip.Root, 10f, 29f, PlateWidth - 20f, 18f);

        all.Add(this);
    }

    private void LateUpdate()
    {
        if (plate == null)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * heightAboveTarget);
        visible = screen.z > 0f;

        if (plate.gameObject.activeSelf != visible)
            plate.gameObject.SetActive(visible);

        if (visible)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            desired = local + new Vector2(0f, 10f);
            plate.anchoredPosition = desired;

            stateChip.Set(TacticalColors.StateLabel(ai.CurrentState), TacticalColors.State(ai.CurrentState));

            bool sees = ai.Perception != null && ai.Perception.CanSeePlayer;
            if (seesPill.gameObject.activeSelf != sees)
                seesPill.gameObject.SetActive(sees);
        }

        // Setelah SEMUA nameplate memperbarui posisinya pada frame ini, atur ulang yang bertumpuk.
        if (lastFrame != Time.frameCount)
        {
            lastFrame = Time.frameCount;
            updatedThisFrame = 0;
        }

        updatedThisFrame++;

        if (updatedThisFrame >= all.Count)
            ResolveOverlaps();
    }

    private static void ResolveOverlaps()
    {
        List<TacticalNameplate> ordered = new List<TacticalNameplate>();

        foreach (TacticalNameplate p in all)
        {
            if (p != null && p.plate != null && p.visible)
                ordered.Add(p);
        }

        // Yang paling bawah di layar ditempatkan lebih dulu; yang bertabrakan digeser ke atas.
        ordered.Sort((a, b) => a.desired.y.CompareTo(b.desired.y));

        List<Rect> placed = new List<Rect>();

        foreach (TacticalNameplate p in ordered)
        {
            Vector2 pos = p.desired;

            for (int guard = 0; guard < 10; guard++)
            {
                Rect candidate = new Rect(pos.x - PlateWidth * 0.5f, pos.y, PlateWidth, PlateHeight);
                Rect? hit = null;

                foreach (Rect r in placed)
                {
                    if (r.Overlaps(candidate))
                    {
                        hit = r;
                        break;
                    }
                }

                if (hit == null)
                    break;

                pos.y = hit.Value.yMax + Gap;
            }

            p.plate.anchoredPosition = pos;
            placed.Add(new Rect(pos.x - PlateWidth * 0.5f, pos.y, PlateWidth, PlateHeight));
        }
    }

    private void OnDestroy()
    {
        all.Remove(this);

        if (plate != null)
            Destroy(plate.gameObject);
    }
}
