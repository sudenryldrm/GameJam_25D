using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public class WitchFlightVisual : MonoBehaviour
{
    [SerializeField] private AnimationClip flightClip;
    private PlayableGraph graph;
    private void OnEnable()
    {
        PlayFlight();
    }
    private void PlayFlight()
    {
        if (graph.IsValid()) graph.Destroy();
        if (flightClip == null) return;
        var animator = GetComponent<Animator>();
        if (animator.avatar == null || !animator.avatar.isValid || flightClip.humanMotion != animator.avatar.isHuman)
        { Debug.LogError("Witch animation clip and imported avatar have incompatible rigs.", this); return; }
        animator.applyRootMotion = false;
        graph = PlayableGraph.Create("WitchFlight");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var output = AnimationPlayableOutput.Create(graph, "Flight", animator);
        var clip = AnimationClipPlayable.Create(graph, flightClip);
        output.SetSourcePlayable(clip);
        graph.Play();
    }
    private void OnDisable() { if (graph.IsValid()) graph.Destroy(); }
    public void SetClip(AnimationClip clip)
    {
        flightClip = clip;
        if (Application.isPlaying && isActiveAndEnabled) PlayFlight();
    }
}
