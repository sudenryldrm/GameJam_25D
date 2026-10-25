using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class ForestAtmosphere : MonoBehaviour
{
    [SerializeField] private float routeStart = -35f;
    [SerializeField] private float routeEnd = 30f;
    private Transform player;
    private Light sun;
    private Material sky, previousSky;
    private GameObject mountains;
    private Renderer backdrop;
    private bool backdropWasEnabled;
    private readonly List<Object> owned = new List<Object>();
    private readonly List<Material> ridgeMaterials = new List<Material>();
    private float nextUpdate;
    private bool WitchForest => gameObject.scene.name == "Scene2_WitchChase";

    private void OnEnable()
    {
        if (gameObject.scene.name != "SampleScene" && !WitchForest) return;
        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.gameObject.scene == gameObject.scene && light.type == LightType.Directional) { sun = light; break; }
        foreach (PlayerController25D candidate in FindObjectsByType<PlayerController25D>(FindObjectsSortMode.None))
            if (candidate.gameObject.scene == gameObject.scene) { player = candidate.transform; break; }
        Shader shader = Shader.Find("Skybox/Procedural");
        if (shader)
        {
            sky = new Material(shader) { name = "Forest Atmosphere Sky", hideFlags = HideFlags.DontSave };
            sky.SetFloat("_SunDisk", 1); sky.SetFloat("_SunSize", .025f);
            sky.SetFloat("_AtmosphereThickness", .8f);
            owned.Add(sky);
        }
        if (WitchForest) BuildMountains();
        ApplyMood();
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup < nextUpdate) return;
        nextUpdate = Time.realtimeSinceStartup + .2f;
        ApplyMood();
    }

    private void ApplyMood()
    {
        if (SceneManager.GetActiveScene() != gameObject.scene) return;
        float progress = Application.IsPlaying(gameObject) && player
            ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(routeStart, routeEnd, player.position.x)) : .35f;
        float t = WitchForest ? .5f + .5f * progress : .5f * progress;
        Color Blend(Color warm, Color twilight, Color haunted) => t < .5f
            ? Color.Lerp(warm, twilight, t * 2) : Color.Lerp(twilight, haunted, (t - .5f) * 2);
        Color mist = Blend(new Color(.49f,.53f,.4f), new Color(.29f,.39f,.43f), new Color(.2f,.24f,.34f));
        if (sun)
        {
            sun.color = Blend(new Color(1,.85f,.65f), new Color(.8f,.9f,1), new Color(.68f,.76f,1));
            sun.intensity = Mathf.Lerp(1.65f, 1.15f, t);
            sun.useColorTemperature = false;
            sun.transform.rotation = Quaternion.Euler(38, -32, 0);
            sun.shadows = LightShadows.Soft; sun.shadowStrength = Mathf.Lerp(.55f,.45f,t);
            sun.shadowBias = .05f; sun.shadowNormalBias = .25f;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Blend(new Color(.43f,.47f,.43f), new Color(.34f,.4f,.49f), new Color(.29f,.32f,.45f));
        RenderSettings.ambientEquatorColor = Blend(new Color(.32f,.34f,.27f), new Color(.26f,.32f,.35f), new Color(.23f,.28f,.33f));
        RenderSettings.ambientGroundColor = new Color(.16f,.18f,.2f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = mist;
        // The playable lane is about 20 units from the camera: fog begins behind it.
        RenderSettings.fogStartDistance = Mathf.Lerp(32,29,t);
        RenderSettings.fogEndDistance = Mathf.Lerp(105,78,t);
        RenderSettings.reflectionIntensity = Mathf.Lerp(.55f,.35f,t);
        if (sky)
        {
            if (RenderSettings.skybox != sky) { previousSky = RenderSettings.skybox; RenderSettings.skybox = sky; }
            sky.SetColor("_SkyTint", Blend(new Color(.52f,.58f,.62f), new Color(.38f,.46f,.6f), new Color(.3f,.31f,.5f)));
            sky.SetColor("_GroundColor", mist); sky.SetFloat("_Exposure", Mathf.Lerp(1.1f,.85f,t));
        }
        for (int i = 0; i < ridgeMaterials.Count; i++)
        {
            Color ridge = Color.Lerp(new Color(.1f,.19f,.24f), new Color(.24f,.31f,.4f), i / 2f);
            ridgeMaterials[i].SetColor("_BaseColor", Color.Lerp(ridge, mist, .16f + i * .12f));
        }
    }

    private void BuildMountains()
    {
        // Three low-poly ridges, three draw calls, no physics and no realtime shadow casters.
        mountains = new GameObject("Distant Mountain Layers") { hideFlags = HideFlags.DontSave };
        mountains.transform.SetParent(transform, false);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) return;
        for (int layer = 0; layer < 3; layer++)
        {
            const int count = 47;
            var vertices = new Vector3[count * 2];
            var triangles = new int[(count - 1) * 6];
            for (int i = 0; i < count; i++)
            {
                float x = -42 + i * 3.5f;
                float peak = 4 + layer * 2 + Mathf.PerlinNoise(i * .47f, layer * 3.7f + .4f) * (7 + layer * 2);
                float z = 15 + layer * 11;
                vertices[i * 2] = new Vector3(x,-16,z);
                vertices[i * 2 + 1] = new Vector3(x,peak,z);
                if (i == count - 1) continue;
                int k = i * 6, v = i * 2;
                triangles[k]=v; triangles[k+1]=v+1; triangles[k+2]=v+3;
                triangles[k+3]=v; triangles[k+4]=v+3; triangles[k+5]=v+2;
            }
            var mesh = new Mesh { name = "Mountain Ridge " + layer, hideFlags = HideFlags.DontSave };
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var material = new Material(shader) { name = "Mountain Haze " + layer, hideFlags = HideFlags.DontSave };
            var go = new GameObject("Ridge " + layer) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(mountains.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            owned.Add(mesh); owned.Add(material); ridgeMaterials.Add(material);
        }
        foreach (Renderer candidate in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (candidate.gameObject.scene == gameObject.scene && candidate.name == "Backdrop")
            { backdrop = candidate; backdropWasEnabled = candidate.enabled; backdrop.enabled = false; break; }
    }

    private void OnDisable()
    {
        if (sky && RenderSettings.skybox == sky) RenderSettings.skybox = previousSky;
        if (backdrop) backdrop.enabled = backdropWasEnabled;
        if (mountains) Release(mountains);
        foreach (Object asset in owned) if (asset) Release(asset);
        owned.Clear(); ridgeMaterials.Clear(); sky = null;
    }

    private static void Release(Object asset)
    { if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset); }
}
