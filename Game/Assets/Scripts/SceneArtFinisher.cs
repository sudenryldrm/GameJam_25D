using UnityEngine;

[ExecuteAlways]
public sealed class SceneArtFinisher : MonoBehaviour
{
    private const string Root = "Finished_Environment_Art";

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorApplication.delayCall += Build;
#endif
    }

    private void Start() { if (Application.isPlaying) Build(); }

    private static GameObject Model(string name) => Resources.Load<GameObject>("SceneArt/" + name);

    private static Bounds BoundsOf(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static GameObject Place(GameObject model, Transform parent, string name, Vector3 center, Vector3 size)
    {
        if (!model) return null;
        GameObject go = Instantiate(model, parent);
        go.name = name;
        go.transform.SetPositionAndRotation(center, Quaternion.identity);
        Bounds b = BoundsOf(go);
        Vector3 s = b.size;
        go.transform.localScale = Vector3.Scale(go.transform.localScale,
            new Vector3(size.x / Mathf.Max(s.x, .001f), size.y / Mathf.Max(s.y, .001f), size.z / Mathf.Max(s.z, .001f)));
        go.transform.position += center - BoundsOf(go).center;
        foreach (Collider c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        return go;
    }

    private static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material material, Color tint)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = size;
        if (Application.isPlaying) Destroy(go.GetComponent<Collider>());
        else DestroyImmediate(go.GetComponent<Collider>());
        Renderer r = go.GetComponent<Renderer>();
        r.sharedMaterial = material;

        return go;
    }

    public void Build()
    {
        if (!this) return;
        if (gameObject.scene.name == "Scene3_TowerEscape")
            (GetComponent<CastleInteriorLighting>() ?? gameObject.AddComponent<CastleInteriorLighting>()).Apply();
        Transform existing = transform.Find(Root);
        if (existing && (gameObject.scene.name != "Scene3_TowerEscape" || existing.Find("Castle_Art_v4"))) return;
        if (existing) { if (Application.isPlaying) Destroy(existing.gameObject); else DestroyImmediate(existing.gameObject); }
        bool tower = gameObject.scene.name == "Scene3_TowerEscape";
        if (!tower && gameObject.scene.name != "Scene2_WitchChase") return;
        GameObject primary = Model(tower ? "Knight" : "Bridge");
        if (!primary) return;
        Transform art = new GameObject(Root).transform;
        art.SetParent(transform, false);
        if (tower) { BuildTower(art, primary); new GameObject("Castle_Art_v4").transform.SetParent(art, false); }
        else BuildForest(art, primary);
    }

    private void BuildForest(Transform art, GameObject bridge)
    {
        Transform old = transform.Find("Module_01/04_LowerPlatform");
        if (old) old.gameObject.SetActive(false);
        Place(bridge, art, "Bridge_Across_Gap", new Vector3(18, .32f, 0), new Vector3(4.3f, .55f, 5.5f));
        GameObject floor = new GameObject("Bridge_Walkable_Collider");
        floor.transform.SetParent(art);
        floor.transform.position = new Vector3(18, .28f, 0);
        BoxCollider c = floor.AddComponent<BoxCollider>();
        c.size = new Vector3(4.4f, .44f, 5.8f);

        GameObject pine = Model("PineTree");
        if (!pine) return;
        for (int i = 0; i < 25; i++)
        {
            float x = -17 + i * 3.8f + Mathf.Sin(i * 17.1f) * 1.2f;
            float z = 4.8f + (i % 3) * .75f;
            float h = 3.5f + (i * 7 % 6) * .55f;
            Place(pine, art, "Distant_Pine_" + i, new Vector3(x, -1.5f, z), new Vector3(1.7f, h, 1.7f));
        }
    }

    private void BuildTower(Transform art, GameObject knight)
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            if (root.name == "Scene3_TowerEnvironment") root.SetActive(false);
        Transform prince = transform.Find("PrinceBoss_Placeholder");
        if (prince)
        {
            Transform duplicate = prince.Find("Knight_Visual");
            if (duplicate) { duplicate.gameObject.SetActive(false); if (Application.isPlaying) Destroy(duplicate.gameObject); else DestroyImmediate(duplicate.gameObject); }
            Transform anchor = prince.Find("Prince_ArtAnchor");
            if (anchor)
            {
                Transform previous = anchor.Find("Knight_Visual");
                if (previous) { previous.gameObject.SetActive(false); if (Application.isPlaying) Destroy(previous.gameObject); else DestroyImmediate(previous.gameObject); }
                GameObject visual = Instantiate(knight, anchor, false);
                visual.name = "Knight_Visual";
                Bounds bounds = BoundsOf(visual);
                visual.transform.localScale *= 2.5f / Mathf.Max(bounds.size.y, .001f);
                bounds = BoundsOf(visual);
                visual.transform.position += new Vector3(prince.position.x - bounds.center.x, prince.position.y - .9f - bounds.min.y, prince.position.z - bounds.center.z);
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
                var animation = visual.AddComponent<KnightRunVisual>();
                animation.ApplyMaterials();
                Renderer capsule = prince.GetComponent<Renderer>();
                if (capsule) capsule.enabled = false;
            }
        }
        CastleTowerDesign.Build(transform, art);
    }
}
