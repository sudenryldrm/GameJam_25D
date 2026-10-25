using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene2And3AutoSetup : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if (!assetPath.Contains("Drunk Run")) return;
        var importer = assetImporter as ModelImporter;
        if (importer == null) return;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = true;
    }

    [MenuItem("Tools/GameJam/1. Kur: 2. Sahne (Arka Plan + kopru.fbx)")]
    public static void SetupScene2()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform old = GameObject.Find("Scene2_WorldAndBridge")?.transform;
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        GameObject root = new GameObject("Scene2_WorldAndBridge");
        Undo.RegisterCreatedObjectUndo(root, "Setup Scene 2");

        var bridgeModel = FindModel("kopru");
        var pine = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/PineTree.fbx");
        var canopy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/ReferenceCanopyTree.obj");
        var rock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Stone.fbx");
        var grass = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Grass.fbx");
        var groundHill = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ground2.fbx");
        var logs = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/fallenTreeLogs.fbx");

        var pineMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Forest_Pine.mat");
        var pineFarMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Reference_PineFar.mat");
        var woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Forest_Wood.mat");
        var rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Forest_Rock.mat");
        var grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Forest_Grass.mat");
        var turfMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Forest_Turf.mat");

        Random.InitState(42);

        // 1. Katmanlı arka plan dünyası (boşluksuz tepeler + yoğun ağaçlandırma)
        for (int layer = 0; layer < 3; layer++)
        {
            float z = 4.2f + layer * 4.0f;
            float yBase = -1.1f + layer * 0.35f;
            for (float x = -30f; x <= 150f; x += 7.5f)
            {
                if (groundHill != null)
                {
                    GameObject hill = (GameObject)PrefabUtility.InstantiatePrefab(groundHill, scene);
                    hill.transform.SetParent(root.transform, false);
                    hill.transform.position = new Vector3(x + Random.Range(-1.2f, 1.2f), yBase - 1.6f, z + 1.0f);
                    hill.transform.localScale = new Vector3(4.8f, 2.3f + layer * 0.8f, 3.2f);
                    DisableColliders(hill);
                    ApplyMat(hill, turfMat);
                }
            }
            for (float x = -30f; x <= 150f; x += Random.Range(1.5f, 2.6f))
            {
                GameObject src = (layer == 1 && canopy != null && Random.value > 0.4f) ? canopy : pine;
                if (src == null) continue;
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(src, scene);
                tree.transform.SetParent(root.transform, false);
                float s = Random.Range(1.15f, 1.9f) * (1f + layer * 0.22f);
                tree.transform.position = new Vector3(x, yBase + Random.Range(-0.15f, 0.25f), z + Random.Range(-0.7f, 0.7f));
                tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                tree.transform.localScale = Vector3.one * s;
                DisableColliders(tree);
                ApplyTreeMats(tree, layer == 2 ? pineFarMat : pineMat, woodMat);
            }
        }

        // Ön-orta plan kayalar, kütükler ve çimenler
        for (float x = -25f; x <= 145f; x += Random.Range(2.0f, 3.6f))
        {
            if (rock != null && Random.value > 0.35f)
            {
                GameObject r = (GameObject)PrefabUtility.InstantiatePrefab(rock, scene);
                r.transform.SetParent(root.transform, false);
                r.transform.position = new Vector3(x, -0.35f, Random.Range(2.2f, 5.2f));
                r.transform.localScale = Vector3.one * Random.Range(0.85f, 1.6f);
                DisableColliders(r);
                ApplyMat(r, rockMat);
            }
            if (logs != null && Random.value > 0.65f)
            {
                GameObject lg = (GameObject)PrefabUtility.InstantiatePrefab(logs, scene);
                lg.transform.SetParent(root.transform, false);
                lg.transform.position = new Vector3(x + 0.8f, -0.25f, Random.Range(2.4f, 4.5f));
                lg.transform.localScale = Vector3.one * Random.Range(1.1f, 1.5f);
                DisableColliders(lg);
                ApplyMat(lg, woodMat);
            }
            if (grass != null)
            {
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(grass, scene);
                g.transform.SetParent(root.transform, false);
                g.transform.position = new Vector3(x, -0.15f, Random.Range(1.5f, 3.6f));
                g.transform.localScale = Vector3.one * Random.Range(0.95f, 1.45f);
                DisableColliders(g);
                ApplyMat(g, grassMat);
            }
        }

        // 2. Bir platformu uçuruma çevirip kopru.fbx yerleştir
        Collider targetPlatform = null;
        foreach (var col in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
        {
            if (col.isTrigger || col.transform.IsChildOf(root.transform)) continue;
            float x = col.bounds.center.x;
            float w = col.bounds.size.x;
            if (x > 16f && x < 78f && w >= 3f && w <= 22f) { targetPlatform = col; break; }
        }

        Vector3 gapCenter = new Vector3(36f, 0f, 0f);
        float gapWidth = 8.5f;
        if (targetPlatform != null)
        {
            gapCenter = new Vector3(targetPlatform.bounds.center.x, targetPlatform.bounds.max.y, 0f);
            gapWidth = Mathf.Clamp(targetPlatform.bounds.size.x, 5.5f, 14f);
            foreach (var r in targetPlatform.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        }

        GameObject bridgeHolder = new GameObject("Ravine_Bridge");
        bridgeHolder.transform.SetParent(root.transform, false);
        bridgeHolder.transform.position = gapCenter;

        BoxCollider walkCol = bridgeHolder.AddComponent<BoxCollider>();
        walkCol.size = new Vector3(gapWidth, 0.45f, 2.5f);
        walkCol.center = new Vector3(0f, -0.22f, 0f);

        if (bridgeModel != null)
        {
            GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(bridgeModel, scene);
            b.transform.SetParent(bridgeHolder.transform, false);
            DisableColliders(b);
            ApplyMat(b, woodMat);
            FitToSize(b, gapCenter, new Vector3(gapWidth * 1.06f, 1.9f, 2.6f));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("2. Sahne (Arka plan dünyası + kopru.fbx) kuruldu ve kaydedildi!");
    }

    [MenuItem("Tools/GameJam/2. Kur: 3. Sahne (Şövalye + Merdiven + Sarmaşık + Kale Duvarı)")]
    public static void SetupScene3()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform old = GameObject.Find("Scene3_TowerEnvironment")?.transform;
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        GameObject root = new GameObject("Scene3_TowerEnvironment");
        Undo.RegisterCreatedObjectUndo(root, "Setup Scene 3");

        var knightModel = FindModel("Drunk Run");
        var stairBlockModel = FindModel("merdivenBasamagi");
        var ivyModel = FindModel("sarmasik");

        Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Tower_Wall.mat");
        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Tower_Stone.mat");
        Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Forest_Grass.mat");
        Material princeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Blockout/Tower_Prince.mat");

        // 1. Şövalye (Drunk Run şovalye model.fbx + animasyonu) entegrasyonu
        var prince = Object.FindFirstObjectByType<PrinceChaserAI>();
        if (prince != null && knightModel != null)
        {
            Transform oldKnight = prince.transform.Find("Knight_Visual");
            if (oldKnight != null) Undo.DestroyObjectImmediate(oldKnight.gameObject);
            foreach (var r in prince.GetComponentsInChildren<Renderer>(true)) r.enabled = false;

            GameObject knight = (GameObject)PrefabUtility.InstantiatePrefab(knightModel, scene);
            Undo.RegisterCreatedObjectUndo(knight, "Install Knight");
            knight.name = "Knight_Visual";
            knight.transform.SetParent(prince.transform, false);
            knight.transform.localPosition = Vector3.zero;
            knight.transform.localRotation = Quaternion.identity;
            DisableColliders(knight);

            foreach (var r in knight.GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterial == null && princeMat != null) r.sharedMaterial = princeMat;

            FitHeightAndFeet(knight, prince.transform.position - Vector3.up * 0.9f, 2.05f);

            AnimationClip drunkRunClip = FindClip("Drunk Run");
            if (drunkRunClip != null)
            {
                var animComp = knight.GetComponent<KnightRunVisual>() ?? knight.AddComponent<KnightRunVisual>();
                animComp.SetClip(drunkRunClip);
                EditorUtility.SetDirty(animComp);
            }
        }

        // 2 & 3. merdivenBasamagi.fbx ve sarmasik.fbx entegrasyonu
        Random.InitState(77);
        var ramps = Object.FindObjectsByType<StairRamp>(FindObjectsSortMode.None);
        foreach (var ramp in ramps)
        {
            BoxCollider box = ramp.GetComponent<BoxCollider>();
            if (box == null) continue;
            foreach (var r in ramp.GetComponentsInChildren<Renderer>(true))
                if (r.transform != ramp.transform) r.enabled = false;

            float len = box.size.x * ramp.transform.lossyScale.x;
            int steps = Mathf.Max(4, Mathf.RoundToInt(len / 0.85f));
            for (int i = 0; i < steps; i++)
            {
                float t = (i + 0.5f) / steps - 0.5f;
                Vector3 worldPos = ramp.transform.TransformPoint(new Vector3(t * box.size.x, 0f, 0f));

                if (stairBlockModel != null)
                {
                    GameObject stepObj = (GameObject)PrefabUtility.InstantiatePrefab(stairBlockModel, scene);
                    stepObj.transform.SetParent(root.transform, true);
                    DisableColliders(stepObj);
                    ApplyMat(stepObj, stoneMat);
                    FitToSize(stepObj, worldPos, new Vector3(0.92f, 0.46f, 1.85f));
                }

                if (ivyModel != null && Random.value < 0.42f)
                {
                    GameObject ivy = (GameObject)PrefabUtility.InstantiatePrefab(ivyModel, scene);
                    ivy.transform.SetParent(root.transform, true);
                    DisableColliders(ivy);
                    ApplyMat(ivy, grassMat);
                    float ivyH = Random.Range(1.3f, 2.8f);
                    FitToSize(ivy, worldPos + new Vector3(0f, -ivyH * 0.55f, Random.Range(-0.6f, 0.6f)),
                        new Vector3(0.8f, ivyH, 0.55f));
                    ivy.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
            }
        }

        // 4. Kale içi taş duvar arka planı (Tower tarzı taş tuğla + sütunlar + duvar sarmaşıkları)
        BuildTowerCastleWall(scene, root.transform, wallMat, stoneMat, grassMat, ivyModel);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("3. Sahne (Şövalye + Merdiven + Sarmaşık + Kale Duvarı) kuruldu ve kaydedildi!");
    }

    private static void BuildTowerCastleWall(Scene scene, Transform root, Material wallMat, Material stoneMat, Material vineMat, GameObject ivyModel)
    {
        GameObject wallRoot = new GameObject("CastleInteriorWall");
        wallRoot.transform.SetParent(root, false);

        GameObject backPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backPlane.transform.SetParent(wallRoot.transform, false);
        backPlane.transform.position = new Vector3(0f, 20f, 3.2f);
        backPlane.transform.localScale = new Vector3(32f, 50f, 0.6f);
        Object.DestroyImmediate(backPlane.GetComponent<Collider>());
        if (wallMat != null) backPlane.GetComponent<Renderer>().sharedMaterial = wallMat;

        for (int row = 0; row < 35; row++)
        {
            float y = -2f + row * 1.35f;
            float offset = (row % 2) * 1.4f;
            for (float x = -13.5f; x <= 13.5f; x += 2.8f)
            {
                GameObject brick = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brick.transform.SetParent(wallRoot.transform, false);
                brick.transform.position = new Vector3(x + offset, y, 2.82f + (row % 3) * 0.04f);
                brick.transform.localScale = new Vector3(2.55f, 1.18f, 0.35f);
                Object.DestroyImmediate(brick.GetComponent<Collider>());
                if (stoneMat != null) brick.GetComponent<Renderer>().sharedMaterial = stoneMat;
            }
        }

        for (float px = -10.5f; px <= 10.5f; px += 7f)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.transform.SetParent(wallRoot.transform, false);
            pillar.transform.position = new Vector3(px, 20f, 2.55f);
            pillar.transform.localScale = new Vector3(1.1f, 24f, 1.1f);
            Object.DestroyImmediate(pillar.GetComponent<Collider>());
            if (stoneMat != null) pillar.GetComponent<Renderer>().sharedMaterial = stoneMat;
        }
    }

    private static GameObject FindModel(string keyword)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Model"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLowerInvariant().Contains(keyword.ToLowerInvariant()))
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    private static AnimationClip FindClip(string keyword)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Model"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.ToLowerInvariant().Contains(keyword.ToLowerInvariant())) continue;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
        }
        return null;
    }

    private static void DisableColliders(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
    }

    private static void FitToSize(GameObject go, Vector3 center, Vector3 targetSize)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) { go.transform.position = center; return; }
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        if (b.size.magnitude < 0.001f) return;
        go.transform.localScale = new Vector3(
            b.size.x > 0.01f ? targetSize.x / b.size.x : 1f,
            b.size.y > 0.01f ? targetSize.y / b.size.y : 1f,
            b.size.z > 0.01f ? targetSize.z / b.size.z : 1f);
        b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        go.transform.position += center - b.center;
    }

    private static void FitHeightAndFeet(GameObject go, Vector3 feetPos, float targetHeight)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        if (b.size.y < 0.001f) return;
        go.transform.localScale *= targetHeight / b.size.y;
        b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        go.transform.position += new Vector3(feetPos.x - b.center.x, feetPos.y - b.min.y, feetPos.z - b.center.z);
    }

    private static void ApplyMat(GameObject go, Material m)
    {
        if (m == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            if (r.sharedMaterial == null || r.sharedMaterial.name.Contains("Default"))
                r.sharedMaterial = m;
    }

    private static void ApplyTreeMats(GameObject go, Material leaves, Material trunk)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterial = r.name.ToLowerInvariant().Contains("leaf") || r.name.ToLowerInvariant().Contains("leaves") ? leaves : trunk;
    }
}