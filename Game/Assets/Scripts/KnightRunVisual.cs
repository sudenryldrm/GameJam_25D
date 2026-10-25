using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class KnightRunVisual : MonoBehaviour
{
    [SerializeField] private AnimationClip runClip;
    private PlayableGraph graph;
    private AnimationClipPlayable run;
    private Transform hips;
    private Transform leftFoot, rightFoot;
    private Vector3 restHips, restPosition, previousPosition;
    private Quaternion restRotation;
    private float time, duration;

    private void OnEnable()
    {
        ApplyMaterials();
        if (!Application.isPlaying) return;
        PlayRun();
    }

    public void SetClip(AnimationClip clip)
    {
        runClip = clip;
        if (Application.isPlaying && isActiveAndEnabled) PlayRun();
    }

    public void ApplyMaterials()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            string part = renderer.name.ToLowerInvariant();
            string name = part.Contains("pants") ? "Knight_Cloth" :
                part.Contains("sword") || part.Contains("polysurface10") ? "Knight_Sword" : "Knight_Armor";
            Material material = Resources.Load<Material>("SceneArt/" + name);
            if (!material) continue;
            Material[] slots = renderer.sharedMaterials;
            if (slots.Length == 0) slots = new Material[1];
            for (int i = 0; i < slots.Length; i++) slots[i] = material;
            renderer.sharedMaterials = slots;
        }
    }

    private void PlayRun()
    {
        if (graph.IsValid()) graph.Destroy();
        time = 0f;
        AnimationClip clip = runClip;
        if (!clip)
            foreach (var candidate in Resources.LoadAll<AnimationClip>("SceneArt/Knight"))
                if (!candidate.name.StartsWith("__preview__") && candidate.length > 0) { clip = candidate; break; }
        if (!clip) { Debug.LogError("Knight FBX contains no imported run animation.", this); return; }
        Animator animator = GetComponentInChildren<Animator>();
        if (!animator) animator = gameObject.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (Transform bone in GetComponentsInChildren<Transform>())
            if (bone.name == "Hips" || bone.name.EndsWith(":Hips")) { hips = bone; restHips = bone.localPosition; break; }
        foreach (Transform bone in GetComponentsInChildren<Transform>()) { if (bone.name.EndsWith("LeftFoot")) leftFoot = bone; if (bone.name.EndsWith("RightFoot")) rightFoot = bone; }
        restPosition = transform.localPosition;
        restRotation = transform.localRotation;
        previousPosition = transform.parent.position;
        duration = clip.length;
        graph = PlayableGraph.Create("Knight Run");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        run = AnimationClipPlayable.Create(graph, clip);
        AnimationPlayableOutput.Create(graph, "Run", animator).SetSourcePlayable(run);
        graph.Play();
    }

    private void LateUpdate()
    {
        if (!graph.IsValid()) return;
        Vector3 current = transform.parent.position;
        bool moving = (current - previousPosition).sqrMagnitude > .000001f;
        previousPosition = current;
        if (moving && Time.deltaTime > 0) time = (time + Time.deltaTime) % duration;
        run.SetTime(time);
        graph.Evaluate(0);
        transform.localPosition = restPosition;
        transform.localRotation = restRotation;
        if (hips) hips.localPosition = new Vector3(restHips.x, hips.localPosition.y, restHips.z);
        if (LevelIntroCutscene.IsPlaying && leftFoot && rightFoot) { var capsule = GetComponentInParent<CapsuleCollider>(); if (capsule) transform.position += Vector3.up * (capsule.bounds.min.y + .06f - Mathf.Min(leftFoot.position.y, rightFoot.position.y)); }
    }

    private void OnDisable() { if (graph.IsValid()) graph.Destroy(); }
}
