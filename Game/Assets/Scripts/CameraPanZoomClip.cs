using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public sealed class CameraPanZoomClip : PlayableAsset, ITimelineClipAsset
{
    [Header("Kamera hareketi")]
    public Vector3 StartPosition = new Vector3(-37.5f, 5.5f, -20f);
    public Vector3 EndPosition = new Vector3(-34f, 4.4f, -20f);
    [Min(0.1f)] public float StartSize = 7f;
    [Min(0.1f)] public float EndSize = 5.8f;
    public AnimationCurve MotionEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public ClipCaps clipCaps => ClipCaps.None;
    public override double duration => 15;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<CameraPanZoomBehaviour> playable = ScriptPlayable<CameraPanZoomBehaviour>.Create(graph);
        playable.GetBehaviour().Settings = this;
        return playable;
    }
}

public sealed class CameraPanZoomBehaviour : PlayableBehaviour
{
    public CameraPanZoomClip Settings;
}
