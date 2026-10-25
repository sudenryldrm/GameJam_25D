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
    private SkinnedMeshRenderer[] skins;
    private Mesh[] snapshots;
    private readonly List<Vector3> vertices = new List<Vector3>();
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
    private float nextPoseSample;

    public bool Ready => model != null && graph.IsValid();

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
        // Recalibrate occasionally from the actual skinned soles. In between,
        // inexpensive foot/toe bone tracking keeps the supporting foot planted.
        if (builtForPlay && Time.time >= nextPoseSample)
        {
            if (TryPoseBounds(out Bounds pose))
            {
                if (feet.Count > 0) soleOffset = LowestFootBone() - pose.min.y;
                else model.transform.position += Vector3.up * (FloorY - pose.min.y);
            }
            nextPoseSample = Time.time + 0.18f;
        }
        if (feet.Count > 0)
            model.transform.position += Vector3.up * (FloorY - (LowestFootBone() - soleOffset));
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
        if (source == null || material == null || diffuse == null || anchor == null) return;
        AnimationClip clip = Resources.Load<AnimationClip>("Dwarfs/GnomeRunAnimation");
        if (clip == null)
            foreach (AnimationClip candidate in Resources.LoadAll<AnimationClip>("Dwarfs/GnomeRun"))
                if (!candidate.name.StartsWith("__preview__")) { clip = candidate; break; }
        if (clip == null || clip.length <= 0f)
        { Debug.LogError("Dwarf run clip is missing or empty.", this); return; }

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
        }
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        renderers = model.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] slots = renderer.sharedMaterials;
            if (slots.Length == 0) slots = new Material[1];
            for (int i = 0; i < slots.Length; i++) slots[i] = runtimeMaterial;
            renderer.sharedMaterials = slots;
        }
        skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        snapshots = new Mesh[skins.Length];
        for (int i = 0; i < skins.Length; i++)
        {
            snapshots[i] = new Mesh { name = "Dwarf Pose Contact", hideFlags = HideFlags.DontSave };
            skins[i].updateWhenOffscreen = true;
        }
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
        // Measure after evaluating the animation, never from imported/T-pose bounds.
        if (!TryPoseBounds(out Bounds bounds) || bounds.size.y < 0.001f)
        { ReleaseVisual(); return; }
        model.transform.localScale *= visualHeight / bounds.size.y;
        if (!TryPoseBounds(out bounds)) { ReleaseVisual(); return; }
        model.transform.position += new Vector3(anchor.position.x - bounds.center.x,
            FloorY - bounds.min.y, anchor.position.z - bounds.center.z);
        fittedLocalPosition = model.transform.localPosition;
        if (feet.Count > 0) soleOffset = LowestFootBone() - FloorY;
        nextPoseSample = Time.time + 0.12f + (Mathf.Abs(GetInstanceID() % 7) * 0.02f);
        Transform old = transform.Find("Capsule_Placeholder");
        placeholder = old != null ? old.GetComponent<Renderer>() : null;
        if (placeholder != null) placeholder.enabled = false;
    }

    private float LowestFootBone()
    {
        float y = float.PositiveInfinity;
        foreach (Transform foot in feet) if (foot != null) y = Mathf.Min(y, foot.position.y);
        return y;
    }

    private bool TryPoseBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        for (int i = 0; skins != null && i < skins.Length; i++)
        {
            if (skins[i] == null || skins[i].sharedMesh == null) continue;
            skins[i].BakeMesh(snapshots[i], false);
            vertices.Clear();
            snapshots[i].GetVertices(vertices);
            foreach (Vector3 vertex in vertices)
            {
                Vector3 point = skins[i].transform.TransformPoint(vertex);
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
            }
        }
        if (!found && renderers != null)
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
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
        if (snapshots != null) foreach (Mesh mesh in snapshots) RemoveObject(mesh);
        RemoveObject(runtimeMaterial);
        model = null;
        runtimeMaterial = null;
        snapshots = null;
        skins = null;
        renderers = null;
        feet.Clear();
    }
}
