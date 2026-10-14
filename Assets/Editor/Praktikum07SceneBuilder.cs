using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Membangun scene Praktikum07 secara otomatis:
//   Navigation (NavMeshSurface + bake), Environment (Ground, Wall_01-03 tinggi, Crate_01-04 rendah),
//   Player, Squad (Attacker, Flanker, Cover Shooter), TacticalPoints (6 Cover Point),
//   Managers (SquadManager, CoverManager), HUD, serta seluruh reference Inspector.
// Menu: Praktikum 07 > Build Scene
public static class Praktikum07SceneBuilder
{
    private const string SceneName = "Praktikum07";
    private const string ScenePath = "Assets/Scenes/Praktikum07.unity";

    [MenuItem("Praktikum 07/Build Scene")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets", "Scenes");

        int obstacleLayer = EnsureLayer("Obstacle");
        int playerLayer = EnsureLayer("Player");
        int enemyLayer = EnsureLayer("Enemy");
        int groundLayer = EnsureLayer("Ground");
        int coverLayer = EnsureLayer("Cover");

        Material groundMat = GetMaterial("GroundMaterial", new Color(0.17f, 0.20f, 0.26f));
        Material wallMat = GetMaterial("WallMaterial", new Color(0.42f, 0.46f, 0.56f));
        Material crateMat = GetMaterial("CrateMaterial", new Color(0.60f, 0.47f, 0.28f));
        Material playerMat = GetMaterial("PlayerMaterial", new Color(0.25f, 0.55f, 1f));
        Material noseMat = GetMaterial("MarkerMaterial", Color.white);
        Material coverMarkerMat = GetMaterial("CoverMarkerMaterial", new Color(0.20f, 0.90f, 0.70f));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---------- Kamera (top-down miring) ----------
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 27f, -19f);
            cam.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
        }

        // ---------- Navigation ----------
        GameObject navigation = new GameObject("Navigation");
        NavMeshSurface surface = navigation.AddComponent<NavMeshSurface>();

        // ---------- Environment ----------
        GameObject environment = new GameObject("Environment");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.layer = groundLayer;
        ground.transform.SetParent(environment.transform);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // Tembok tinggi (layer Obstacle): menghalangi pandangan DAN tembakan.
        CreateBlock("Wall_01", environment.transform, new Vector3(-7f, 1.5f, 2f), new Vector3(6f, 3f, 1f), obstacleLayer, wallMat);
        CreateBlock("Wall_02", environment.transform, new Vector3(7f, 1.5f, 2f), new Vector3(6f, 3f, 1f), obstacleLayer, wallMat);
        CreateBlock("Wall_03", environment.transform, new Vector3(0f, 1.5f, 6.5f), new Vector3(4f, 3f, 1f), obstacleLayer, wallMat);

        // Crate rendah (layer Cover): melindungi dari tembakan saat jongkok, tetapi tidak menghalangi pandangan.
        CreateBlock("Crate_01", environment.transform, new Vector3(-4f, 0.55f, -3f), new Vector3(2f, 1.1f, 1f), coverLayer, crateMat);
        CreateBlock("Crate_02", environment.transform, new Vector3(4f, 0.55f, -3f), new Vector3(2f, 1.1f, 1f), coverLayer, crateMat);
        CreateBlock("Crate_03", environment.transform, new Vector3(-9f, 0.55f, -6f), new Vector3(2f, 1.1f, 1f), coverLayer, crateMat);
        CreateBlock("Crate_04", environment.transform, new Vector3(9f, 0.55f, -6f), new Vector3(2f, 1.1f, 1f), coverLayer, crateMat);

        // ---------- Player ----------
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.layer = playerLayer;
        player.transform.position = new Vector3(0f, 1f, -11f);
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>()); // digantikan CharacterController
        player.AddComponent<CharacterController>();
        PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
        player.AddComponent<SimplePlayerController>();
        IgnoreInNavMeshBuild(player);
        CreateMarker(player.transform, noseMat);

        // ---------- Managers ----------
        GameObject managers = new GameObject("Managers");

        GameObject squadObject = new GameObject("SquadManager");
        squadObject.transform.SetParent(managers.transform);
        SquadManager squadManager = squadObject.AddComponent<SquadManager>();

        // ---------- Tactical Points (6 Cover Point, di sisi utara crate / tembok) ----------
        GameObject tacticalPoints = new GameObject("TacticalPoints");
        CoverPoint[] covers =
        {
            CreateCover("CoverPoint_01", tacticalPoints.transform, new Vector3(-4f, 0f, -1.8f), coverMarkerMat),
            CreateCover("CoverPoint_02", tacticalPoints.transform, new Vector3(4f, 0f, -1.8f), coverMarkerMat),
            CreateCover("CoverPoint_03", tacticalPoints.transform, new Vector3(-9f, 0f, -4.8f), coverMarkerMat),
            CreateCover("CoverPoint_04", tacticalPoints.transform, new Vector3(9f, 0f, -4.8f), coverMarkerMat),
            CreateCover("CoverPoint_05", tacticalPoints.transform, new Vector3(-7f, 0f, 3.4f), coverMarkerMat),
            CreateCover("CoverPoint_06", tacticalPoints.transform, new Vector3(7f, 0f, 3.4f), coverMarkerMat)
        };

        GameObject coverObject = new GameObject("CoverManager");
        coverObject.transform.SetParent(managers.transform);
        CoverManager coverManager = coverObject.AddComponent<CoverManager>();
        SetRefArray(coverManager, "coverPoints", covers);
        SetLayerMask(coverManager, "coverBlockMask", (1 << obstacleLayer) | (1 << coverLayer));
        SetLayerMask(coverManager, "visionMask", 1 << obstacleLayer);

        // ---------- Squad (3 Enemy) ----------
        GameObject squad = new GameObject("Squad");

        CreateEnemy("Enemy_01_Attacker", squad.transform, new Vector3(0f, 1f, 10f), EnemyRole.Attacker,
            GetMaterial("AttackerMaterial", TacticalColors.Role(EnemyRole.Attacker)), noseMat,
            player.transform, 1 << obstacleLayer, enemyLayer, squadManager, coverManager);

        CreateEnemy("Enemy_02_Flanker", squad.transform, new Vector3(-8f, 1f, 11f), EnemyRole.FlankerLeft,
            GetMaterial("FlankerMaterial", TacticalColors.Role(EnemyRole.FlankerLeft)), noseMat,
            player.transform, 1 << obstacleLayer, enemyLayer, squadManager, coverManager);

        CreateEnemy("Enemy_03_CoverShooter", squad.transform, new Vector3(8f, 1f, 11f), EnemyRole.CoverShooter,
            GetMaterial("CoverShooterMaterial", TacticalColors.Role(EnemyRole.CoverShooter)), noseMat,
            player.transform, 1 << obstacleLayer, enemyLayer, squadManager, coverManager);

        // ---------- HUD ----------
        GameObject hud = new GameObject("SquadHUD");
        SquadHUD squadHud = hud.AddComponent<SquadHUD>();
        SetRef(squadHud, "squad", squadManager);
        SetRef(squadHud, "covers", coverManager);
        SetRef(squadHud, "playerHealth", playerHealth);

        GameObject playerHudObject = new GameObject("PlayerHUD");
        SetRef(playerHudObject.AddComponent<PlayerHUD>(), "playerHealth", playerHealth);

        // ---------- Bake NavMesh ----------
        surface.BuildNavMesh();

        if (surface.navMeshData == null)
        {
            Debug.LogError("[Praktikum07] NavMesh gagal di-bake. Bake manual lewat NavMeshSurface > Bake.");
        }
        else
        {
            EnsureFolder("Assets/Scenes", SceneName);
            string navPath = "Assets/Scenes/" + SceneName + "/NavMesh-Navigation.asset";
            AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navPath);
            EditorUtility.SetDirty(surface);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EnsureSceneInBuildSettings();

        Selection.activeGameObject = squadObject;

        Debug.Log("[Praktikum07] Scene dibuat: " + ScenePath + ". Tekan Play untuk menguji.");
    }

    [MenuItem("Praktikum 07/Rebake NavMesh")]
    public static void Rebake()
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();

        if (surface == null)
        {
            Debug.LogError("[Praktikum07] NavMeshSurface tidak ditemukan di scene ini.");
            return;
        }

        Scene scene = surface.gameObject.scene;
        string oldPath = surface.navMeshData != null ? AssetDatabase.GetAssetPath(surface.navMeshData) : "";

        surface.BuildNavMesh();

        if (surface.navMeshData == null)
        {
            Debug.LogError("[Praktikum07] Bake gagal.");
            return;
        }

        string newPath = string.IsNullOrEmpty(oldPath)
            ? "Assets/Scenes/" + scene.name + "/NavMesh-Navigation.asset"
            : oldPath;

        if (string.IsNullOrEmpty(oldPath))
            EnsureFolder("Assets/Scenes", scene.name);
        else
            AssetDatabase.DeleteAsset(oldPath);

        AssetDatabase.CreateAsset(surface.navMeshData, newPath);
        EditorUtility.SetDirty(surface);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[Praktikum07] NavMesh di-bake ulang dan scene disimpan.");
    }

    // Tombol Restart (R) memuat ulang scene lewat nama, sehingga scene harus ada di Build Settings.
    private static void EnsureSceneInBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        foreach (EditorBuildSettingsScene existing in scenes)
        {
            if (existing.path == ScenePath)
                return;
        }

        EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[scenes.Length + 1];
        System.Array.Copy(scenes, updated, scenes.Length);
        updated[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updated;
    }

    // ------------------------------------------------------------------
    // Objek scene
    // ------------------------------------------------------------------

    private static void CreateEnemy(
        string name, Transform parent, Vector3 position, EnemyRole role, Material mat, Material noseMat,
        Transform player, int obstacleMask, int enemyLayer, SquadManager squadManager, CoverManager coverManager)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = name;
        enemy.layer = enemyLayer;
        enemy.transform.SetParent(parent);
        enemy.transform.position = position;
        enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // menghadap selatan (ke arah Player)
        enemy.GetComponent<Renderer>().sharedMaterial = mat;

        NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f;
        agent.angularSpeed = 360f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 1.5f;
        agent.baseOffset = 1f; // pivot kapsul di tengah, NavMesh di lantai

        EnemyPerception perception = enemy.AddComponent<EnemyPerception>();
        SetRef(perception, "player", player);
        SetLayerMask(perception, "obstacleMask", obstacleMask);

        EnemyMemory memory = enemy.AddComponent<EnemyMemory>();

        EnemyTacticalAI tactical = enemy.AddComponent<EnemyTacticalAI>();
        SetEnum(tactical, "role", (int)role);
        SetRef(tactical, "perception", perception);
        SetRef(tactical, "memory", memory);
        SetRef(tactical, "squadManager", squadManager);
        SetRef(tactical, "coverManager", coverManager);
        SetRef(tactical, "agent", agent);

        EnemyCombat combat = enemy.AddComponent<EnemyCombat>();
        SetRef(combat, "perception", perception);

        EnemyDebugVisual debug = enemy.AddComponent<EnemyDebugVisual>();
        SetRef(debug, "tacticalAI", tactical);
        SetRef(debug, "perception", perception);

        enemy.AddComponent<TacticalNameplate>();

        IgnoreInNavMeshBuild(enemy);
        CreateMarker(enemy.transform, noseMat);
    }

    private static CoverPoint CreateCover(string name, Transform parent, Vector3 position, Material markerMat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Marker";
        Object.DestroyImmediate(disc.GetComponent<CapsuleCollider>());
        disc.transform.SetParent(go.transform, false);
        disc.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        disc.transform.localScale = new Vector3(1.1f, 0.02f, 1.1f);
        disc.GetComponent<Renderer>().sharedMaterial = markerMat;

        CoverPoint cover = go.AddComponent<CoverPoint>();
        SetRef(cover, "marker", disc.GetComponent<Renderer>());

        IgnoreInNavMeshBuild(go);
        return cover;
    }

    private static void CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale, int layer, Material mat)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent);
        block.transform.position = position;
        block.transform.localScale = scale;
        block.layer = layer;
        block.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // Kubus kecil di depan kapsul agar arah hadap (forward) terlihat saat demo FOV.
    private static void CreateMarker(Transform parent, Material mat)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "FacingMarker";
        Object.DestroyImmediate(marker.GetComponent<BoxCollider>());
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = new Vector3(0f, 0.5f, 0.5f);
        marker.transform.localScale = new Vector3(0.3f, 0.3f, 0.5f);
        marker.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // Player/Enemy/Cover Point jangan ikut di-bake menjadi bagian NavMesh.
    private static void IgnoreInNavMeshBuild(GameObject go)
    {
        NavMeshModifier modifier = go.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;
    }

    // ------------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------------

    private static Material GetMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
            return existing;

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogError("[Praktikum07] Tidak ada slot Layer kosong untuk '" + layerName + "'.");
        return 0;
    }

    private static SerializedProperty FindProp(SerializedObject so, Object target, string field)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
            Debug.LogError("[Praktikum07] Field tidak ditemukan: " + target.GetType().Name + "." + field);
        return prop;
    }

    private static void SetRef(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = FindProp(so, target, field);
        if (prop == null) return;
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRefArray(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = FindProp(so, target, field);
        if (prop == null) return;
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string field, int value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = FindProp(so, target, field);
        if (prop == null) return;
        prop.enumValueIndex = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayerMask(Object target, string field, int mask)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = FindProp(so, target, field);
        if (prop == null) return;
        prop.intValue = mask;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
