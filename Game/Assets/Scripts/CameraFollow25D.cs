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

    private Transform FollowTransform => gameplayCamera != null ? gameplayCamera.transform : transform;
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
        if (!configured || target == null || Time.timeScale <= 0f) return;
        UpdateViewSize();
        if (targetBody != null && Mathf.Abs(targetBody.linearVelocity.x) > 0.2f)
            facing = Mathf.MoveTowards(facing, Mathf.Sign(targetBody.linearVelocity.x), Time.deltaTime * 4f);
        float dy = target.position.y + offset.y - FollowTransform.position.y;
        float y = FollowTransform.position.y + Mathf.Sign(dy) * Mathf.Max(0f, Mathf.Abs(dy) - verticalDeadZone);
        Vector3 destination = new Vector3(CameraX(), CameraY(y), offset.z);
        FollowTransform.position = Vector3.SmoothDamp(FollowTransform.position, destination, ref followVelocity,
            1f / Mathf.Max(0.1f, smoothSpeed), Mathf.Infinity, Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (player != null) player.Respawned -= SnapToTarget;
    }
}
