using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Static decoration is merged by floor and material, with no decorative colliders or shadow maps.
public static class CastleTowerDesign
{
    private sealed class Batch
    {
        public Material material;
        public readonly List<CombineInstance> parts = new List<CombineInstance>();
    }

    public static void Build(Transform sceneRoot, Transform art)
    {
        var batches = new Dictionary<string, Batch>();
        Mesh cube = sceneRoot.Find("Flight_01/Step_01").GetComponent<MeshFilter>().sharedMesh;
        Material stone = Load("Castle_Ashlar"), pale = Load("Castle_Limestone");
        Material dark = Load("Castle_Recess"), gold = Load("Castle_Brass");
        Material red = Load("Castle_Crimson"), blue = Load("Castle_RoyalBlue");
        Material glass = Load("Castle_MoonGlass"), flame = Load("Castle_Amber");
        Material marble = Load("Castle_Marble");

        void Box(Material material, Vector3 p, Vector3 s, float angle = 0)
        {
            int floor = Mathf.Clamp(Mathf.FloorToInt(p.y / 6.5f), 0, 6);
            string key = floor + "_" + material.name;
            if (!batches.TryGetValue(key, out Batch batch))
            { batch = new Batch { material = material }; batches.Add(key, batch); }
            batch.parts.Add(new CombineInstance { mesh = cube,
                transform = art.worldToLocalMatrix * Matrix4x4.TRS(p, Quaternion.Euler(0, 0, angle), s) });
        }

        // A continuous dark mortar bed prevents holes at the staggered masonry edges.
        Box(dark, new Vector3(0, 21, 4.65f), new Vector3(25, 44, .3f));
        for (int row = 0; row < 44; row++)
            for (int col = 0; col < 14; col++)
            {
                float x = -12.1f + col * 1.85f + (row % 2) * .925f;
                if (x > 12.3f) continue;
                Box((row * 7 + col * 3) % 9 == 0 ? pale : stone,
                    new Vector3(x, row + .2f, 4.35f), new Vector3(1.80f, .955f, .32f));
            }

        for (int floor = 0; floor < 7; floor++)
        {
            float y = floor * 6.5f;
            Box(pale, new Vector3(0, y - .28f, 3.98f), new Vector3(23.8f, .24f, .55f));
            Box(dark, new Vector3(0, y - .47f, 4.02f), new Vector3(23.8f, .12f, .48f));
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 10.8f;
                Box(pale, new Vector3(x, y + 2.8f, 3.85f), new Vector3(.65f, 6, .85f));
                Box(pale, new Vector3(x, y, 3.65f), new Vector3(1.05f, .4f, 1.05f));
                Box(gold, new Vector3(x, y + .27f, 3.54f), new Vector3(.77f, .08f, .12f));

                // Split-tail royal standards, brass rods, borders, and a diamond crest.
                float bx = side * 7.7f, by = y + 3.9f;
                Material cloth = (floor + (side > 0 ? 1 : 0)) % 2 == 0 ? red : blue;
                Box(cloth, new Vector3(bx, by, 3.5f), new Vector3(1.25f, 2.35f, .09f));
                Box(cloth, new Vector3(bx - .37f, by - 1.18f, 3.5f), new Vector3(.50f, .55f, .09f));
                Box(cloth, new Vector3(bx + .37f, by - 1.18f, 3.5f), new Vector3(.50f, .55f, .09f));
                Box(gold, new Vector3(bx, by + 1.27f, 3.35f), new Vector3(1.65f, .12f, .16f));
                Box(gold, new Vector3(bx - .56f, by, 3.4f), new Vector3(.045f, 2.3f, .04f));
                Box(gold, new Vector3(bx + .56f, by, 3.4f), new Vector3(.045f, 2.3f, .04f));
                Box(gold, new Vector3(bx, by + .15f, 3.35f), new Vector3(.4f, .4f, .06f), 45);
                Box(gold, new Vector3(bx, by + .15f, 3.3f), new Vector3(.075f, .95f, .06f));

                // Wall sconces use emissive geometry; no extra realtime lights or particles.
                float tx = side * 4.4f, ty = y + 3.45f;
                Box(gold, new Vector3(tx, ty, 3.5f), new Vector3(.24f, .8f, .45f));
                Box(dark, new Vector3(tx, ty + .15f, 3.18f), new Vector3(.48f, .35f, .45f));
                Box(flame, new Vector3(tx, ty + .57f, 3.15f), new Vector3(.22f, .65f, .22f), 8);
                Box(flame, new Vector3(tx, ty + .76f, 3.15f), new Vector3(.14f, .4f, .16f), -12);
            }

            // Recessed arched window, pale voussoirs, blue glass and brass mullions.
            float spring = y + 3.65f;
            for (int strip = 0; strip < 14; strip++)
            {
                float x = -1.05f + (strip + .5f) * .15f;
                float h = 2.05f + Mathf.Sqrt(Mathf.Max(0, 1.1f * 1.1f - x * x));
                Box(glass, new Vector3(x, spring - 2.05f + h * .5f, 3.88f), new Vector3(.155f, h, .12f));
            }
            Box(pale, new Vector3(-1.25f, spring - 1, 3.67f), new Vector3(.35f, 2.15f, .42f));
            Box(pale, new Vector3(1.25f, spring - 1, 3.67f), new Vector3(.35f, 2.15f, .42f));
            for (int segment = 0; segment < 12; segment++)
            {
                float a = (segment + .5f) * Mathf.PI / 12;
                Box(pale, new Vector3(Mathf.Cos(a) * 1.25f, spring + Mathf.Sin(a) * 1.25f, 3.67f),
                    new Vector3(.36f, .36f, .44f), a * Mathf.Rad2Deg);
            }
            Box(pale, new Vector3(0, spring - 2.1f, 3.6f), new Vector3(2.95f, .22f, .65f));
            Box(gold, new Vector3(0, spring - .65f, 3.6f), new Vector3(.08f, 2.9f, .07f));
            Box(gold, new Vector3(0, spring - .7f, 3.6f), new Vector3(2.1f, .07f, .07f));
        }

        for (int flight = 1; flight <= 6; flight++)
        {
            Transform f = sceneRoot.Find("Flight_0" + flight);
            if (!f) continue;
            Vector3 first = Vector3.zero, last = Vector3.zero;
            foreach (Transform step in f)
            {
                if (!step.name.StartsWith("Step_")) continue;
                Renderer original = step.GetComponent<Renderer>();
                Bounds b = original.bounds;
                if (first == Vector3.zero) first = b.center;
                last = b.center;
                original.enabled = false;
                Transform imported = step.Find("Stone_Step_Visual");
                if (imported) imported.gameObject.SetActive(false);
                Box(marble, b.center, b.size);
                Box(pale, b.center + new Vector3(0, -.16f, 0), new Vector3(b.size.x, .2f, b.size.z));
                Box(gold, b.center + new Vector3(0, .08f, -3.025f), new Vector3(b.size.x, .045f, .045f));
            }
            Vector3 delta = last - first;
            Box(pale, (first + last) * .5f - Vector3.up * .28f,
                new Vector3(delta.magnitude + .8f, .3f, 5.85f), Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        Transform exit = sceneRoot.Find("Tower_TopExit");
        if (exit)
        {
            Material wood = Load("Castle_Oak");
            Vector3 p = exit.position;
            for (int plank = 0; plank < 6; plank++)
                Box(wood, new Vector3(p.x - .65f + plank * .26f, p.y - .28f, 1.8f), new Vector3(.245f, 3.2f, .18f));
            for (int strap = 0; strap < 3; strap++)
                Box(gold, new Vector3(p.x, p.y - 1.35f + strap, 1.65f), new Vector3(1.6f, .12f, .12f));
            Box(gold, new Vector3(p.x + .38f, p.y - .35f, 1.52f), new Vector3(.15f, .2f, .1f), 45);
            for (int side = -1; side <= 1; side += 2)
                Box(pale, new Vector3(p.x + side * .95f, p.y - .28f, 1.65f), new Vector3(.32f, 3.6f, .5f));
            Box(pale, new Vector3(p.x, p.y + 1.45f, 1.65f), new Vector3(2.3f, .35f, .5f));
        }

        foreach (Transform child in sceneRoot)
        {
            Renderer r = child.GetComponent<Renderer>();
            if (!r) continue;
            if (child.name == "Checkpoint_Marker")
            {
                Bounds b = r.bounds;
                r.enabled = false;
                Vector3 p = new Vector3(b.center.x, b.min.y, b.center.z);
                Box(pale, p + Vector3.up * .15f, new Vector3(.7f, .3f, .7f));
                Box(gold, p + Vector3.up * .35f, new Vector3(.55f, .12f, .55f));
                Box(glass, p + Vector3.up * .8f, new Vector3(.38f, .6f, .35f), 45);
            }
            if (child.name.StartsWith("Exit_Door")) r.enabled = false;
            if (child.name.Contains("Landing") || child.name.Contains("Floor"))
            {
                r.sharedMaterial = marble;
                Bounds b = r.bounds;
                Box(gold, new Vector3(b.center.x, b.max.y - .07f, b.min.z - .03f), new Vector3(b.size.x, .07f, .08f));
                Box(pale, new Vector3(b.center.x, b.min.y - .16f, b.center.z), new Vector3(b.size.x + .2f, .25f, b.size.z));
            }
            if (child.name.Contains("Wall") || child.name == "Backdrop") r.sharedMaterial = dark;
            if (child.name.Contains("Wall") || child.name.Contains("Floor") || child.name.Contains("Landing") || child.name == "Backdrop")
                r.shadowCastingMode = ShadowCastingMode.Off;
        }

        foreach (var item in batches)
        {
            string meshName = "CastleBatchV3_" + item.Key;
            Mesh mesh = Application.isPlaying ? Resources.Load<Mesh>("SceneArt/Generated/" + meshName) : null;
            bool generated = !mesh;
            if (generated)
            {
                mesh = new Mesh { name = meshName, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(item.Value.parts.ToArray(), true, true);
                mesh.RecalculateBounds();
                if (Application.isPlaying)
                    (art.GetComponent<CastleMeshLifetime>() ?? art.gameObject.AddComponent<CastleMeshLifetime>()).Own(mesh);
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                const string folder = "Assets/Resources/SceneArt/Generated";
                if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                    UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/SceneArt", "Generated");
                string path = folder + "/" + mesh.name + ".asset";
                Mesh saved = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved) { UnityEditor.EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); mesh = saved; }
                else UnityEditor.AssetDatabase.CreateAsset(mesh, path);
            }
#endif
            var go = new GameObject(mesh.name);
            go.transform.SetParent(art, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = item.Value.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.gameObject.scene == sceneRoot.gameObject.scene && light.type == LightType.Directional)
            { light.color = new Color(1, .96f, .9f); light.intensity = 1.35f; light.shadows = LightShadows.Soft; }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.43f, .49f, .62f);
        RenderSettings.ambientEquatorColor = new Color(.4f, .36f, .32f);
        RenderSettings.ambientGroundColor = new Color(.22f, .24f, .3f);
        Debug.Log("Castle rebuilt: " + batches.Count + " merged decoration batches; 96 marble treads; no ivy or decorative shadow casters.");
    }

    private static Material Load(string name)
    {
        Material material = Resources.Load<Material>("SceneArt/" + name);
        if (!material) throw new System.InvalidOperationException("Missing castle material: " + name);
        return material;
    }
}
