using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class WitchFlightVisual : MonoBehaviour
{
    [SerializeField] private AnimationClip flightClip;
    private PlayableGraph graph;
    private void OnEnable()
    {
        if (flightClip == null) return;
        var animator = GetComponent<Animator>();
        if (animator == null) animator = gameObject.AddComponent<Animator>();
        graph = PlayableGraph.Create("WitchFlight");
        var output = AnimationPlayableOutput.Create(graph, "Flight", animator);
        var clip = AnimationClipPlayable.Create(graph, flightClip);
        output.SetSourcePlayable(clip);
        graph.Play();
    }
    private void OnDisable() { if (graph.IsValid()) graph.Destroy(); }
    public void SetClip(AnimationClip clip) { flightClip = clip; }
}
