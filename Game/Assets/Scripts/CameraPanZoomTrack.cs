using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;

[TrackColor(0.25f, 0.45f, 0.35f)]
[TrackClipType(typeof(CameraPanZoomClip))]
[TrackBindingType(typeof(CinemachineCamera))]
public sealed class CameraPanZoomTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CameraPanZoomMixer>.Create(graph, inputCount);
    }

    public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
    {
        CinemachineCamera camera = director.GetGenericBinding(this) as CinemachineCamera;
        if (camera != null)
        {
            driver.AddFromName(camera.transform, "m_LocalPosition.x");
            driver.AddFromName(camera.transform, "m_LocalPosition.y");
            driver.AddFromName(camera.transform, "m_LocalPosition.z");
            driver.AddFromName(camera, "Lens.OrthographicSize");
        }
        base.GatherProperties(director, driver);
    }
}

public sealed class CameraPanZoomMixer : PlayableBehaviour
{
    private CinemachineCamera camera;
    private Vector3 originalPosition;
    private float originalSize;
    private bool captured;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        CinemachineCamera target = playerData as CinemachineCamera;
        if (target == null) return;
        for (int i = 0; i < playable.GetInputCount(); i++)
        {
            if (playable.GetInputWeight(i) <= 0f) continue;
            ScriptPlayable<CameraPanZoomBehaviour> input = (ScriptPlayable<CameraPanZoomBehaviour>)playable.GetInput(i);
            CameraPanZoomClip settings = input.GetBehaviour().Settings;
            if (settings == null) continue;
            if (captured && camera != target) Restore();
            if (!captured)
            {
                camera = target;
                originalPosition = target.transform.localPosition;
                originalSize = target.Lens.OrthographicSize;
                captured = true;
            }
            double duration = input.GetDuration();
            float time = duration > 0 ? Mathf.Clamp01((float)(input.GetTime() / duration)) : 0f;
            float eased = settings.MotionEase != null ? settings.MotionEase.Evaluate(time) : time;
            target.transform.localPosition = Vector3.LerpUnclamped(settings.StartPosition, settings.EndPosition, eased);
            LensSettings lens = target.Lens;
            lens.OrthographicSize = Mathf.Max(0.1f, Mathf.LerpUnclamped(settings.StartSize, settings.EndSize, eased));
            target.Lens = lens;
            return;
        }
        Restore();
    }

    private void Restore()
    {
        if (!captured) return;
        if (camera != null)
        {
            camera.transform.localPosition = originalPosition;
            LensSettings lens = camera.Lens;
            lens.OrthographicSize = originalSize;
            camera.Lens = lens;
        }
        captured = false;
    }

    public override void OnGraphStop(Playable playable) { Restore(); }
    public override void OnPlayableDestroy(Playable playable) { Restore(); }
}
