using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Camera))]
public class CameraFollow25D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private CinemachineCamera gameplayCamera;
    [SerializeField] private float smoothSpeed = 6f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, -20f);
    [SerializeField] private bool lockHorizontalPosition;
    [SerializeField] private float horizontalPosition;
    [SerializeField] private bool useSceneProfile = true;
    [SerializeField] private float viewSize = 7.2f;
    [SerializeField] private float verticalDeadZone = 0.9f;
    [SerializeField] private float lookAhead = 1.6f;

    private Camera view;
    private PlayerController25D player;
    private Rigidbody targetBody;
    private Vector3 followVelocity;
    private float facing = 1f;
    private float minimumX = -50f;
    private float maximumX = 50f;
    private float minimumY = 3.8f;
    private float minimumHalfWidth;
    private bool configured;
    private bool conversationActive;
    private Vector3 conversationFrom, conversationTo;
    private Quaternion conversationRotation;
    private float conversationViewSize;
    private float conversationCloseSize;
    private Vector3 cinematicHome;
    private Quaternion cinematicHomeRotation;
    private float cinematicHomeSize;
    private float cinematicNear, cinematicFar, cinematicFov;
    private CinemachineBrain cinematicBrain;
    private bool brainWasEnabled;
    public Vector3 CinematicHome => cinematicHome;
    public Quaternion CinematicHomeRotation => cinematicHomeRotation;
    public float CinematicHomeSize => cinematicHomeSize;

    private Transform FollowTransform => conversationActive ? transform : gameplayCamera != null ? gameplayCamera.transform : transform;
    private float CurrentViewSize => gameplayCamera != null ? gameplayCamera.Lens.OrthographicSize : view.orthographicSize;

    private void Start()
    {
        view = GetComponent<Camera>();
        player = target != null ? target.GetComponent<PlayerController25D>() : FindFirstObjectByType<PlayerController25D>();
        if (player != null)
        {
            target = player.transform;
            targetBody = player.GetComponent<Rigidbody>();
            player.Respawned += SnapToTarget;
        }
        if (useSceneProfile)
        {
            offset = new Vector3(0f, 2.2f, -20f);
            switch (gameObject.scene.name)
            {
                case "SampleScene":
                    offset.y = 1.1f; minimumY = 2.6f;
                    viewSize = 6.6f;
                    lockHorizontalPosition = false;
                    break;
                case "Scene2_WitchChase":
                    offset.y = 1.1f; minimumY = 2.6f;
                    viewSize = 6.5f; minimumX = -5f; maximumX = 59f;
                    lookAhead = 1.8f; verticalDeadZone = 0.8f;
                    lockHorizontalPosition = false;
                    break;
                case "Scene3_TowerEscape":
                    viewSize = 6.3f; minimumHalfWidth = 11.2f;
                    lockHorizontalPosition = true; horizontalPosition = 0f;
                    lookAhead = 0f; verticalDeadZone = 1.1f;
                    break;
                default:
                    lockHorizontalPosition = false;
                    break;
            }
        }
        if (gameplayCamera != null)
        {
            LensSettings lens = gameplayCamera.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.NearClipPlane = 0.1f;
            lens.FarClipPlane = 100f;
            gameplayCamera.Lens = lens;
        }
        else
        {
            view.orthographic = true;
            view.nearClipPlane = 0.1f;
            view.farClipPlane = 100f;
        }
        // Frame the facade straight on so the floor never exposes the empty world below.
        bool forestProfile = useSceneProfile && (gameObject.scene.name == "SampleScene" ||
            gameObject.scene.name == "Scene2_WitchChase");
        float pitch = forestProfile || IsTowerProfile ? 0f : 8f;
        FollowTransform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        configured = true;
        UpdateViewSize();
        SnapToTarget();
    }

    private void UpdateViewSize()
    {
        // Keep both tower edges visible on narrower windows as well.
        float size = Mathf.Max(viewSize, minimumHalfWidth / Mathf.Max(0.1f, view.aspect));
        if (gameplayCamera != null)
        {
            LensSettings lens = gameplayCamera.Lens;
            lens.OrthographicSize = size;
            gameplayCamera.Lens = lens;
        }
        else view.orthographicSize = size;
    }

    private float CameraX()
    {
        if (lockHorizontalPosition) return horizontalPosition;
        float halfWidth = CurrentViewSize * view.aspect;
        if (maximumX - minimumX <= halfWidth * 2f) return (minimumX + maximumX) * 0.5f;
        return Mathf.Clamp(target.position.x + offset.x + facing * lookAhead,
            minimumX + halfWidth, maximumX - halfWidth);
    }

    private bool IsTowerProfile => useSceneProfile && gameObject.scene.name == "Scene3_TowerEscape";

    private float CameraY(float desired)
    {
        if (!IsTowerProfile) return Mathf.Max(minimumY, desired);
        float bottom = CurrentViewSize - .35f;
        float top = 42f - CurrentViewSize;
        return top > bottom ? Mathf.Clamp(desired, bottom, top) : 20.825f;
    }

    private void SnapToTarget()
    {
        if (!configured || target == null) return;
        followVelocity = Vector3.zero;
        facing = 1f;
        FollowTransform.position = new Vector3(CameraX(), CameraY(target.position.y + offset.y), offset.z);
    }

    private void LateUpdate()
    {
        if (conversationActive || !configured || target == null || Time.timeScale <= 0f) return;
        UpdateViewSize();
        if (targetBody != null && Mathf.Abs(targetBody.linearVelocity.x) > 0.2f)
            facing = Mathf.MoveTowards(facing, Mathf.Sign(targetBody.linearVelocity.x), Time.deltaTime * 4f);
        float dy = target.position.y + offset.y - FollowTransform.position.y;
        float y = FollowTransform.position.y + Mathf.Sign(dy) * Mathf.Max(0f, Mathf.Abs(dy) - verticalDeadZone);
        Vector3 destination = new Vector3(CameraX(), CameraY(y), offset.z);
        FollowTransform.position = Vector3.SmoothDamp(FollowTransform.position, destination, ref followVelocity,
            1f / Mathf.Max(0.1f, smoothSpeed), Mathf.Infinity, Time.deltaTime);
    }

    public void BeginConversationShot(Vector3 center)
    {
        BeginCameraSequence();
        conversationFrom = transform.position;
        conversationRotation = transform.rotation;
        conversationViewSize = view.orthographicSize;
        conversationTo = new Vector3(center.x, center.y - .35f, offset.z);
        conversationCloseSize = Mathf.Max(2.8f, 3.2f / Mathf.Max(.1f, view.aspect));
    }

    public void BeginCameraSequence()
    {
        if (conversationActive) return;
        SnapToTarget();
        cinematicHome = FollowTransform.position;
        cinematicHomeRotation = FollowTransform.rotation;
        cinematicHomeSize = CurrentViewSize;
        cinematicNear = view.nearClipPlane; cinematicFar = view.farClipPlane; cinematicFov = view.fieldOfView;
        cinematicBrain = GetComponent<CinemachineBrain>();
        if (cinematicBrain) { brainWasEnabled = cinematicBrain.enabled; cinematicBrain.enabled = false; }
        conversationActive = true;
        SetCinematicPose(cinematicHome, cinematicHomeRotation, cinematicHomeSize);
    }

    public void SetCinematicPose(Vector3 position, Quaternion rotation, float size, bool perspective = false, float fieldOfView = 45)
    {
        if (!conversationActive) BeginCameraSequence();
        transform.SetPositionAndRotation(position, rotation);
        view.orthographic = !perspective;
        view.orthographicSize = size;
        view.fieldOfView = fieldOfView;
        view.nearClipPlane = .1f; view.farClipPlane = 250;
    }

    public void BlendConversationShot(float amount)
    {
        if (!conversationActive) return;
        float size = Mathf.Lerp(conversationViewSize, conversationCloseSize, amount);
        SetCinematicPose(Vector3.Lerp(conversationFrom, conversationTo, amount),
            Quaternion.Slerp(conversationRotation, Quaternion.identity, amount), size);
    }

    public void EndConversationShot()
    {
        if (!conversationActive) return;
        SetCinematicPose(cinematicHome, cinematicHomeRotation, cinematicHomeSize);
        conversationActive = false;
        view.nearClipPlane = cinematicNear; view.farClipPlane = cinematicFar; view.fieldOfView = cinematicFov;
        if (cinematicBrain) cinematicBrain.enabled = brainWasEnabled;
        followVelocity = Vector3.zero;
    }

    private void OnDestroy()
    {
        if (player != null) player.Respawned -= SnapToTarget;
    }
}
