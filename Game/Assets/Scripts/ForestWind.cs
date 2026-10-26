using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// One clock for the entire forest; foliage movement is evaluated on the GPU.
public sealed class ForestWind : MonoBehaviour
{
    private static ForestWind instance;
    private static Shader shader;
    private static readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
    private static readonly int Clock = Shader.PropertyToID("_StoryWindTime");
    private readonly List<Material> owned = new List<Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; shader = null; materials.Clear(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance) return;
        var host = new GameObject("Forest Wind Clock"); DontDestroyOnLoad(host);
        instance = host.AddComponent<ForestWind>();
        SceneManager.sceneLoaded += instance.SceneLoaded;
    }

    public static Shader FoliageShader => shader ? shader : shader = Resources.Load<Shader>("SceneArt/ForestWind");

    public static void ApplyHierarchy(GameObject root)
    {
        if (!Application.isPlaying || !root) return;
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
            if (renderer.enabled && renderer.name.StartsWith("TreeLeaves")) Apply(renderer);
    }

    public static void Apply(MeshRenderer renderer)
    {
        if (!Application.isPlaying || !renderer || !renderer.enabled || !FoliageShader) return;
        Material[] slots = renderer.sharedMaterials;
        for (int i = 0; i < slots.Length; i++)
        {
            Material source = slots[i];
            if (!source || source.shader == FoliageShader) continue;
            if (!materials.TryGetValue(source, out Material wind))
            {
                wind = new Material(FoliageShader) { name = source.name + " (gentle wind)", hideFlags = HideFlags.DontSave, enableInstancing = true };
                if (source.HasProperty("_BaseColor")) wind.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                if (source.HasProperty("_BaseMap") && source.GetTexture("_BaseMap")) wind.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                materials.Add(source, wind); if (instance) instance.owned.Add(wind);
            }
            slots[i] = wind;
        }
        renderer.sharedMaterials = slots;
        Bounds bounds = renderer.bounds;
        var properties = new MaterialPropertyBlock(); renderer.GetPropertyBlock(properties);
        properties.SetFloat("_WindBaseY", bounds.min.y);
        properties.SetFloat("_WindHeight", Mathf.Max(.1f, bounds.size.y));
        properties.SetFloat("_WindStrength", Mathf.Clamp(bounds.size.y * .018f, .06f, .16f));
        renderer.SetPropertyBlock(properties);
        if (renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh)
        {
            Vector3 a = renderer.transform.InverseTransformVector(Vector3.right * .25f);
            Vector3 b = renderer.transform.InverseTransformVector(Vector3.forward * .25f);
            Vector3 padding = new Vector3(Mathf.Abs(a.x) + Mathf.Abs(b.x), Mathf.Abs(a.y) + Mathf.Abs(b.y), Mathf.Abs(a.z) + Mathf.Abs(b.z));
            Bounds local = filter.sharedMesh.bounds; local.Expand(padding * 2); renderer.localBounds = local;
        }
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    { foreach (GameObject root in scene.GetRootGameObjects()) ApplyHierarchy(root); }

    private void Update() { Shader.SetGlobalFloat(Clock, Time.unscaledTime); }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        foreach (Material material in owned) if (material) Destroy(material);
        if (instance == this) { instance = null; materials.Clear(); Shader.SetGlobalFloat(Clock, 0); }
    }
}
