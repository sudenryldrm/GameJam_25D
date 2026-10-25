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
        Transform existing = transform.Find(Root);
        if (existing && (gameObject.scene.name != "Scene3_TowerEscape" || existing.Find("Castle_Art_v2"))) return;
        if (existing) { if (Application.isPlaying) Destroy(existing.gameObject); else DestroyImmediate(existing.gameObject); }
        bool tower = gameObject.scene.name == "Scene3_TowerEscape";
        if (!tower && gameObject.scene.name != "Scene2_WitchChase") return;
        GameObject primary = Model(tower ? "Knight" : "Bridge");
        if (!primary) return;
        Transform art = new GameObject(Root).transform;
        art.SetParent(transform, false);
        if (tower) { new GameObject("Castle_Art_v2").transform.SetParent(art, false); BuildTower(art, primary); }
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
        Transform prince = transform.Find("PrinceBoss_Placeholder");
        if (prince)
        {
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
                visual.AddComponent<KnightRunVisual>();
                Renderer capsule = prince.GetComponent<Renderer>();
                if (capsule) capsule.enabled = false;
            }
        }
        Material marble = Resources.Load<Material>("SceneArt/Castle_Marble");
        GameObject stair = Model("StairStep");
        GameObject ivy = Model("Ivy");
        int index = 0;
        for (int flight = 1; flight <= 6; flight++)
        {
            Transform f = transform.Find("Flight_0" + flight);
            if (!f) continue;
            foreach (Transform step in f)
            {
                if (!step.name.StartsWith("Step_")) continue;
                Renderer r = step.GetComponent<Renderer>();
                if (!r) continue;
                Bounds b = r.bounds;
                if (stair)
                {
                    Transform oldStep = step.Find("Stone_Step_Visual");
                    if (oldStep) { oldStep.gameObject.SetActive(false); if (Application.isPlaying) Destroy(oldStep.gameObject); else DestroyImmediate(oldStep.gameObject); }
                    GameObject stoneStep = Place(stair, step, "Stone_Step_Visual", b.center, b.size);
                    if (marble) foreach (Renderer surface in stoneStep.GetComponentsInChildren<Renderer>())
                    { var slots = surface.sharedMaterials; for (int s = 0; s < slots.Length; s++) slots[s] = marble; surface.sharedMaterials = slots; }
                    r.enabled = false;
                }
                if (ivy && index++ % 7 == 2)
                    Place(ivy, art, "Hanging_Ivy_" + index,
                        b.center + new Vector3(0, -.85f, -2.8f), new Vector3(.55f, 1.4f, .35f));
            }
        }

        Renderer backdrop = transform.Find("Backdrop")?.GetComponent<Renderer>();
        Material stone = Resources.Load<Material>("SceneArt/Castle_Wall");
        if (!stone && backdrop) stone = backdrop.sharedMaterial;
        if (backdrop && stone) backdrop.sharedMaterial = stone;
        foreach (Transform child in transform)
            if (stone && child.name.Contains("Wall"))
            { Renderer surface = child.GetComponent<Renderer>(); if (surface) surface.sharedMaterial = stone; }
        for (int column = -1; column <= 1; column += 2)
        {
            Box(art, "Castle_Pilaster", new Vector3(column * 10.7f, 21, 3.75f), new Vector3(.7f, 44, .8f), stone, Color.gray);
            for (int tier = 0; tier < 7; tier++)
                Box(art, "Castle_Cornice", new Vector3(column * 10.7f, tier * 6.5f + .3f, 3.5f), new Vector3(1.2f, .3f, 1.1f), marble, Color.gray);
        }
        foreach (Transform child in transform)
            if (marble && (child.name.Contains("Landing") || child.name.Contains("Floor")))
            { Renderer surface = child.GetComponent<Renderer>(); if (surface) surface.sharedMaterial = marble; }
        for (int y = 0; y < 22; y++)
            for (int x = 0; x < 10; x++)
            {
                float shift = y % 2 == 0 ? 0 : 1.2f;
                float px = -11.7f + x * 2.4f + shift;
                if (px > 11.7f) continue;
                float shade = .27f + ((x * 11 + y * 7) % 5) * .018f;
                Box(art, "Wall_Stone", new Vector3(px, y * 2 + 1, 4.15f),
                    new Vector3(2.33f, 1.92f, .38f), stone, new Color(shade + .2f, shade + .2f, shade + .2f));
            }
    }
}
