using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// One shared imported model/material for every dwarf; generated visuals are not
// saved into the scene. They are also shown in the editor after asset import.
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class DwarfModelVisual : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float visualHeight = 1.8f;
    private bool rebuildRequested;
    private GameObject model;
    private Renderer placeholder;
    private PlayableGraph graph;
    private AnimationClipPlayable run;
    private bool moving;
    private float retryAt;
    private bool builtForPlay;
    private Material runtimeMaterial;
    private float clipTime;
    private float clipLength;

    private void Update()
    {
        if (rebuildRequested)
        {
            rebuildRequested = false;
            RebuildVisual();
        }
        // Also handle entering play mode with domain/scene reload disabled.
        if (model != null && builtForPlay != Application.IsPlaying(gameObject)) ReleaseVisual();
        if (model == null && Time.realtimeSinceStartup >= retryAt)
        {
            retryAt = Time.realtimeSinceStartup + 1f;
            BuildVisual();
        }
        if (run.IsValid() && graph.IsValid())
        {
            if (moving && clipLength > 0f) clipTime = (clipTime + Time.deltaTime) % clipLength;
            run.SetTime(clipTime);
            graph.Evaluate(0f);
        }
    }

    public void SetMoving(bool value) { moving = value; }

    public void RebuildVisual()
    {
        ReleaseVisual();
        // DontSave children can outlive nonserialized C# fields after a script reload.
        Transform anchor = transform.Find("Art_Anchor");
        if (anchor != null)
            for (int i = anchor.childCount - 1; i >= 0; i--)
                if (anchor.GetChild(i).name == "Gnome_Run_Visual")
                {
                    GameObject stale = anchor.GetChild(i).gameObject;
                    stale.SetActive(false);
                    if (Application.IsPlaying(gameObject)) Destroy(stale);
                    else DestroyImmediate(stale);
                }
        retryAt = 0f;
    }

    private void BuildVisual()
    {
        GameObject source = Resources.Load<GameObject>("Dwarfs/GnomeRun");
        Material material = Resources.Load<Material>("Dwarfs/Gnome");
        Transform anchor = transform.Find("Art_Anchor");
        Texture2D diffuse = Resources.Load<Texture2D>("Dwarfs/GnomeDiffuse");
        Texture2D normal = Resources.Load<Texture2D>("Dwarfs/GnomeNormal");
        if (source == null || material == null || diffuse == null || anchor == null) return;
        // Resolve by asset path as well as GUID, so regenerated texture metas cannot
        // leave a permanently white cached visual after first import.
        runtimeMaterial = new Material(material) { hideFlags = HideFlags.DontSave };
        if (runtimeMaterial.HasProperty("_BaseMap")) runtimeMaterial.SetTexture("_BaseMap", diffuse);
        if (runtimeMaterial.HasProperty("_MainTex")) runtimeMaterial.SetTexture("_MainTex", diffuse);
        if (normal != null && runtimeMaterial.HasProperty("_BumpMap"))
        {
            runtimeMaterial.SetTexture("_BumpMap", normal);
            runtimeMaterial.EnableKeyword("_NORMALMAP");
        }
        builtForPlay = Application.IsPlaying(gameObject);

        model = Instantiate(source, anchor, false);
        model.name = "Gnome_Run_Visual";
        foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
            child.gameObject.hideFlags = HideFlags.DontSave;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (Renderer renderer in renderers)
        {
            Material[] slots = renderer.sharedMaterials;
            if (slots.Length == 0) slots = new Material[1];
            for (int i = 0; i < slots.Length; i++) slots[i] = runtimeMaterial;
            renderer.sharedMaterials = slots;
            if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!hasBounds || bounds.size.y < 0.001f) { ReleaseVisual(); return; }

        // Enlarge the art while preserving its grounded feet and chase physics.
        model.transform.localScale *= visualHeight / bounds.size.y;
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        CapsuleCollider bodyCollider = GetComponent<CapsuleCollider>();
        // The chase root is centered above the floor. Align the model's actual
        // lowest mesh point with the capsule's bottom, not the root's pivot.
        float groundY = bodyCollider != null
            ? transform.position.y + bodyCollider.center.y - bodyCollider.height * 0.5f
            : transform.position.y;
        model.transform.position += new Vector3(anchor.position.x - bounds.center.x,
            groundY - bounds.min.y, anchor.position.z - bounds.center.z);

        Transform old = transform.Find("Capsule_Placeholder");
        placeholder = old != null ? old.GetComponent<Renderer>() : null;
        if (placeholder != null) placeholder.enabled = false;

        if (!Application.IsPlaying(gameObject)) return;
        AnimationClip clip = Resources.Load<AnimationClip>("Dwarfs/GnomeRunAnimation");
        if (clip == null) foreach (AnimationClip candidate in Resources.LoadAll<AnimationClip>("Dwarfs/GnomeRun"))
            if (!candidate.name.StartsWith("__preview__")) { clip = candidate; break; }
        if (clip == null)
        {
            Debug.LogError("Gnome run animation was not imported.", this);
            ReleaseVisual();
            return;
        }
        Animator animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create("Dwarf Run");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        clipLength = clip.length;
        clipTime = 0f;
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Run", animator);
        run = AnimationClipPlayable.Create(graph, clip);
        run.SetApplyFootIK(false);
        run.SetSpeed(0d);
        output.SetSourcePlayable(run);
        graph.Play();
        graph.Evaluate(0f);
    }

    private void OnEnable() { RebuildVisual(); }
    private void OnValidate() { rebuildRequested = true; }
    private void OnDisable() { ReleaseVisual(); }

    private void ReleaseVisual()
    {
        if (graph.IsValid()) graph.Destroy();
        if (placeholder != null) placeholder.enabled = true;
        if (model != null)
        {
            if (Application.IsPlaying(gameObject)) Destroy(model);
            else DestroyImmediate(model);
        }
        model = null;
        if (runtimeMaterial != null)
        {
            if (Application.IsPlaying(gameObject)) Destroy(runtimeMaterial);
            else DestroyImmediate(runtimeMaterial);
        }
        runtimeMaterial = null;
    }
}

