using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class KnightRunVisual : MonoBehaviour
{
    [SerializeField] private AnimationClip runClip;
    [SerializeField] private float groundOffset = -0.02f;
    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private float clipTime;
    private Vector3 lastParentPos;
    private Transform parentRoot;

    public void SetClip(AnimationClip clip) => runClip = clip;

    private void Start()
    {
        parentRoot = transform.parent != null ? transform.parent : transform;
        lastParentPos = parentRoot.position;
        if (runClip == null) return;
        Animator animator = GetComponent<Animator>();
        if (animator == null) animator = gameObject.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;

        graph = PlayableGraph.Create("KnightDrunkRun");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var output = AnimationPlayableOutput.Create(graph, "Run", animator);
        playable = AnimationClipPlayable.Create(graph, runClip);
        playable.SetApplyFootIK(false);
        playable.SetSpeed(0d);
        output.SetSourcePlayable(playable);
        graph.Play();
        graph.Evaluate(0f);
    }

    private void Update()
    {
        if (!graph.IsValid() || !playable.IsValid() || runClip == null) return;
        bool moving = (parentRoot.position - lastParentPos).sqrMagnitude > 0.00001f;
        lastParentPos = parentRoot.position;
        if (moving && runClip.length > 0.01f)
            clipTime = (clipTime + Time.deltaTime * 1.15f) % runClip.length;
        playable.SetTime(clipTime);
        graph.Evaluate(0f);
    }

    private void LateUpdate()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        float lowest = float.PositiveInfinity;
        foreach (var r in renderers)
            if (r.enabled) lowest = Mathf.Min(lowest, r.bounds.min.y);
        if (float.IsInfinity(lowest)) return;
        float feetY = parentRoot.position.y - 0.9f + groundOffset;
        transform.position += new Vector3(0f, feetY - lowest, 0f);
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}