using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow25D : MonoBehaviour
{
    [SerializeField] private Transform target;
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
                case "Scene2_WitchChase":
                    viewSize = 6.5f; minimumX = -5f; maximumX = 59f;
                    lookAhead = 1.8f; verticalDeadZone = 0.8f;
                    lockHorizontalPosition = false;
                    break;
                case "Scene3_TowerEscape":
                    viewSize = 7.6f; minimumHalfWidth = 12.8f;
                    lockHorizontalPosition = true; horizontalPosition = 0f;
                    lookAhead = 0f; verticalDeadZone = 1.1f;
                    break;
                default:
                    lockHorizontalPosition = false;
                    break;
            }
        }
        view.orthographic = true;
        view.nearClipPlane = 0.1f;
        view.farClipPlane = 100f;
        transform.rotation = Quaternion.Euler(8f, 0f, 0f);
        configured = true;
        UpdateViewSize();
        SnapToTarget();
    }

    private void UpdateViewSize()
    {
        // Keep both tower edges visible on narrower windows as well.
        view.orthographicSize = Mathf.Max(viewSize, minimumHalfWidth / Mathf.Max(0.1f, view.aspect));
    }

    private float CameraX()
    {
        if (lockHorizontalPosition) return horizontalPosition;
        float halfWidth = view.orthographicSize * view.aspect;
        if (maximumX - minimumX <= halfWidth * 2f) return (minimumX + maximumX) * 0.5f;
        return Mathf.Clamp(target.position.x + offset.x + facing * lookAhead,
            minimumX + halfWidth, maximumX - halfWidth);
    }

    private void SnapToTarget()
    {
        if (!configured || target == null) return;
        followVelocity = Vector3.zero;
        facing = 1f;
        transform.position = new Vector3(CameraX(), Mathf.Max(minimumY, target.position.y + offset.y), offset.z);
    }

    private void LateUpdate()
    {
        if (!configured || target == null || Time.timeScale <= 0f) return;
        UpdateViewSize();
        if (targetBody != null && Mathf.Abs(targetBody.linearVelocity.x) > 0.2f)
            facing = Mathf.MoveTowards(facing, Mathf.Sign(targetBody.linearVelocity.x), Time.deltaTime * 4f);
        float dy = target.position.y + offset.y - transform.position.y;
        float y = transform.position.y + Mathf.Sign(dy) * Mathf.Max(0f, Mathf.Abs(dy) - verticalDeadZone);
        Vector3 destination = new Vector3(CameraX(), Mathf.Max(minimumY, y), offset.z);
        transform.position = Vector3.SmoothDamp(transform.position, destination, ref followVelocity,
            1f / Mathf.Max(0.1f, smoothSpeed), Mathf.Infinity, Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (player != null) player.Respawned -= SnapToTarget;
    }
}
