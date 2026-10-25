using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class DwarfModelVisual : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float visualHeight = 1.8f;
    [SerializeField, Min(0f)] private float groundInset = 0.025f;
    private bool rebuildRequested;
    private GameObject model;
    private Transform anchor;
    private Renderer placeholder;
    private Renderer[] renderers;
    private Transform hips;
    private Vector3 hipsRestPosition;
    private bool reportedMissingAssets;
    private readonly List<Transform> feet = new List<Transform>();
    private PlayableGraph graph;
    private AnimationClipPlayable run;
    private bool moving;
    private float retryAt;
    private bool builtForPlay;
    private Material runtimeMaterial;
    private float clipTime;
    private float clipLength;
    private Vector3 fittedLocalPosition;
    private float soleOffset;

    public bool Ready => model != null;

    public void SetMoving(bool value) { moving = value; }

    private void Update()
    {
        if (rebuildRequested) { rebuildRequested = false; RebuildVisual(); }
        if (model != null && builtForPlay != Application.IsPlaying(gameObject)) RebuildVisual();
        if (model == null && Time.realtimeSinceStartup >= retryAt)
        {
            retryAt = Time.realtimeSinceStartup + 1f;
            BuildVisual();
        }
        if (run.IsValid() && graph.IsValid())
        {
            // Start from the fitted anchor every frame; never accumulate a bounds correction.
            model.transform.localPosition = fittedLocalPosition;
            if (builtForPlay && moving && clipLength > 0f)
                clipTime = (clipTime + Time.deltaTime) % clipLength;
            run.SetTime(clipTime);
            graph.Evaluate(0f);
        }
    }

    private void LateUpdate()
    {
        if (!Ready || anchor == null) return;
        // Generic run clips can retain horizontal hip translation even with root
        // motion disabled. Only the chase Rigidbody should move the character.
        model.transform.localPosition = fittedLocalPosition;
        if (hips != null)
        {
            Vector3 pose = hips.localPosition;
            hips.localPosition = new Vector3(hipsRestPosition.x, pose.y, hipsRestPosition.z);
        }
        if (feet.Count > 0)
        {
            float delta = FloorY - (LowestFootBone() - soleOffset);
            if (!float.IsNaN(delta) && !float.IsInfinity(delta))
                model.transform.position += Vector3.up * Mathf.Clamp(delta, -visualHeight, visualHeight);
        }
    }

    private float FloorY => anchor.position.y - groundInset;

    public void RebuildVisual()
    {
        ReleaseVisual();
        anchor = transform.Find("Art_Anchor");
        if (anchor != null)
            for (int i = anchor.childCount - 1; i >= 0; i--)
                if (anchor.GetChild(i).name == "Gnome_Run_Visual")
                    RemoveObject(anchor.GetChild(i).gameObject);
        retryAt = 0f;
    }

    private void BuildVisual()
    {
        GameObject source = Resources.Load<GameObject>("Dwarfs/GnomeRun");
        Material material = Resources.Load<Material>("Dwarfs/Gnome");
        Texture2D diffuse = Resources.Load<Texture2D>("Dwarfs/GnomeDiffuse");
        Texture2D normal = Resources.Load<Texture2D>("Dwarfs/GnomeNormal");
        anchor = transform.Find("Art_Anchor");
        if (source == null || material == null || diffuse == null || anchor == null)
        {
            if (!reportedMissingAssets)
            {
                Debug.LogWarning("Dwarf assets are not imported yet. Stop Play Mode and use Tools > Dwarfs > Repair Materials and Run Animation.", this);
                reportedMissingAssets = true;
            }
            return;
        }
        reportedMissingAssets = false;
        AnimationClip clip = Resources.Load<AnimationClip>("Dwarfs/GnomeRunAnimation");
        if (clip == null)
            foreach (AnimationClip candidate in Resources.LoadAll<AnimationClip>("Dwarfs/GnomeRun"))
                if (!candidate.name.StartsWith("__preview__")) { clip = candidate; break; }

        builtForPlay = Application.IsPlaying(gameObject);
        runtimeMaterial = new Material(material) { hideFlags = HideFlags.DontSave };
        if (runtimeMaterial.HasProperty("_BaseMap")) runtimeMaterial.SetTexture("_BaseMap", diffuse);
        if (runtimeMaterial.HasProperty("_MainTex")) runtimeMaterial.SetTexture("_MainTex", diffuse);
        if (normal != null && runtimeMaterial.HasProperty("_BumpMap"))
        { runtimeMaterial.SetTexture("_BumpMap", normal); runtimeMaterial.EnableKeyword("_NORMALMAP"); }

        model = Instantiate(source, anchor, false);
        model.name = "Gnome_Run_Visual";
        foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.hideFlags = HideFlags.DontSave;
            string name = child.name;
            if (name.EndsWith("LeftFoot") || name.EndsWith("RightFoot") ||
                name.EndsWith("LeftToeBase") || name.EndsWith("RightToeBase") ||
                name.EndsWith("LeftToe_End") || name.EndsWith("RightToe_End")) feet.Add(child);
            if (name.EndsWith(":Hips") || name == "Hips") hips = child;
        }
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = source.transform.localRotation;
        model.transform.localScale = source.transform.localScale;
        renderers = model.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] slots = renderer.sharedMaterials;
            if (slots.Length == 0) slots = new Material[1];
            for (int i = 0; i < slots.Length; i++) slots[i] = runtimeMaterial;
            renderer.sharedMaterials = slots;
        }
        foreach (SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            skin.enabled = true;
            skin.forceRenderingOff = false;
            skin.updateWhenOffscreen = true;
        }
        // Use the source mesh bounds before playing any animation. These are
        // stable and do not depend on culling or BakeMesh scale conventions.
        if (!TrySourceBounds(out Bounds bounds) || bounds.size.y < 0.000001f)
        { ReleaseVisual(); return; }
        model.transform.localScale *= visualHeight / bounds.size.y;
        if (!TrySourceBounds(out bounds)) { ReleaseVisual(); return; }
        model.transform.position += new Vector3(anchor.position.x - bounds.center.x,
            FloorY - bounds.min.y, anchor.position.z - bounds.center.z);
        fittedLocalPosition = model.transform.localPosition;
        if (feet.Count > 0)
            soleOffset = Mathf.Clamp(LowestFootBone() - FloorY,
                0f, visualHeight * 0.15f);
        if (hips != null) hipsRestPosition = hips.localPosition;
        Transform old = transform.Find("Capsule_Placeholder");
        placeholder = old != null ? old.GetComponent<Renderer>() : null;
        if (placeholder != null) placeholder.enabled = false;
        // A missing run clip must not make a successfully imported model disappear.
        if (clip == null || clip.length <= 0f)
        { Debug.LogWarning("Dwarf model is visible but its run clip needs reimporting.", this); return; }
        Animator animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create("Dwarf Run");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        run = AnimationClipPlayable.Create(graph, clip);
        run.SetApplyFootIK(false);
        run.SetSpeed(0d);
        AnimationPlayableOutput.Create(graph, "Run", animator).SetSourcePlayable(run);
        clipLength = clip.length;
        clipTime = 0f;
        graph.Play();
        graph.Evaluate(0f);
    }

    private float LowestFootBone()
    {
        float y = float.PositiveInfinity;
        foreach (Transform foot in feet) if (foot != null) y = Mathf.Min(y, foot.position.y);
        return y;
    }

    private bool TrySourceBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skin) mesh = skin.sharedMesh;
            else if (renderer.TryGetComponent(out MeshFilter filter)) mesh = filter.sharedMesh;
            if (mesh == null) continue;
            Bounds local = mesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 offset = new Vector3((corner & 1) == 0 ? -local.extents.x : local.extents.x,
                    (corner & 2) == 0 ? -local.extents.y : local.extents.y,
                    (corner & 4) == 0 ? -local.extents.z : local.extents.z);
                Vector3 point = renderer.transform.TransformPoint(local.center + offset);
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
            }
        }
        return found;
    }

    private void OnEnable() { RebuildVisual(); }
    private void OnValidate() { rebuildRequested = true; }
    private void OnDisable() { ReleaseVisual(); }

    private void RemoveObject(Object value)
    {
        if (value == null) return;
        if (value is GameObject go) go.SetActive(false);
        if (Application.IsPlaying(gameObject)) Destroy(value); else DestroyImmediate(value);
    }

    private void ReleaseVisual()
    {
        if (graph.IsValid()) graph.Destroy();
        if (placeholder != null) placeholder.enabled = true;
        RemoveObject(model);
        RemoveObject(runtimeMaterial);
        model = null;
        runtimeMaterial = null;
        hips = null;
        renderers = null;
        feet.Clear();
    }
}
