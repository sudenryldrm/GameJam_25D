using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// One shared imported model/material for every dwarf; generated visuals are not
// saved into the scene. They are also shown in the editor after asset import.
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class DwarfModelVisual : MonoBehaviour
{
    private GameObject model;
    private Renderer placeholder;
    private PlayableGraph graph;
    private AnimationClipPlayable run;
    private bool moving;
    private float retryAt;
    private bool builtForPlay;

    private void Update()
    {
        // Also handle entering play mode with domain/scene reload disabled.
        if (model != null && builtForPlay != Application.IsPlaying(gameObject)) ReleaseVisual();
        if (model == null && Time.realtimeSinceStartup >= retryAt)
        {
            retryAt = Time.realtimeSinceStartup + 1f;
            BuildVisual();
        }
        if (run.IsValid()) run.SetSpeed(moving ? 1d : 0d);
    }

    public void SetMoving(bool value) { moving = value; }

    private void BuildVisual()
    {
        GameObject source = Resources.Load<GameObject>("Dwarfs/GnomeRun");
        Material material = Resources.Load<Material>("Dwarfs/Gnome");
        Transform anchor = transform.Find("Art_Anchor");
        if (source == null || material == null || anchor == null) return;
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
            for (int i = 0; i < slots.Length; i++) slots[i] = material;
            renderer.sharedMaterials = slots;
            if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!hasBounds || bounds.size.y < 0.001f) { ReleaseVisual(); return; }

        // Fit the art to the existing 1.3-unit collider and put its feet on the anchor.
        model.transform.localScale *= 1.3f / bounds.size.y;
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        model.transform.position += anchor.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        Transform old = transform.Find("Capsule_Placeholder");
        placeholder = old != null ? old.GetComponent<Renderer>() : null;
        if (placeholder != null) placeholder.enabled = false;

        if (!Application.IsPlaying(gameObject)) return;
        AnimationClip clip = null;
        foreach (AnimationClip candidate in Resources.LoadAll<AnimationClip>("Dwarfs/GnomeRun"))
            if (!candidate.name.StartsWith("__preview__")) { clip = candidate; break; }
        if (clip == null)
        {
            Debug.LogError("Gnome run animation was not imported.", this);
            return;
        }
        Animator animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create("Dwarf Run");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Run", animator);
        run = AnimationClipPlayable.Create(graph, clip);
        run.SetApplyFootIK(false);
        run.SetSpeed(moving ? 1d : 0d);
        output.SetSourcePlayable(run);
        graph.Play();
    }

    private void OnEnable() { retryAt = 0f; }
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
    }
}
