using System.Collections.Generic;
using UnityEngine;

public class DwarfChaseGroup : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float firstDistance = 2.2f;
    [SerializeField] private float spacing = 0.95f;
    private struct TrailPoint
    {
        public Vector3 feet;
        public float distance;
        public TrailPoint(Vector3 point, float traveled) { feet = point; distance = traveled; }
    }
    private readonly List<TrailPoint> trail = new List<TrailPoint>();
    private PlayerController25D player;
    private CapsuleCollider playerCollider;
    private Rigidbody playerBody;
    private DwarfPursuer[] dwarfs;
    public bool Ready { get; private set; }
    public bool IsFollowing => Ready && player != null && !player.isInteracting && Time.timeScale > 0f;
    public float TotalDistance => trail.Count > 0 ? trail[trail.Count - 1].distance : 0f;

    private void Start()
    {
        player = target != null ? target.GetComponent<PlayerController25D>() : FindFirstObjectByType<PlayerController25D>();
        if (player == null) { Debug.LogError("Dwarf chase target is missing.", this); return; }
        target = player.transform;
        playerCollider = player.GetComponent<CapsuleCollider>();
        playerBody = player.GetComponent<Rigidbody>();
        dwarfs = GetComponentsInChildren<DwarfPursuer>(true);
        player.Respawned += ResetTrail;
        ResetTrail();
    }

    private Vector3 PlayerFeet()
    {
        Vector3 point = playerBody != null ? playerBody.position : target.position;
        if (playerCollider != null) point.y += playerCollider.center.y - playerCollider.height * 0.5f;
        else point.y -= 1f;
        return point;
    }

    private void FixedUpdate()
    {
        if (!IsFollowing) return;
        Vector3 feet = PlayerFeet();
        if (trail.Count == 0 || Vector3.Distance(feet, trail[trail.Count - 1].feet) >= 0.04f)
            AddPoint(feet);
    }

    private void AddPoint(Vector3 point)
    {
        float distance = trail.Count == 0 ? 0f : TotalDistance + Vector3.Distance(point, trail[trail.Count - 1].feet);
        trail.Add(new TrailPoint(point, distance));
        // Keep enough history for all seven followers; discard only points already behind them.
        if (trail.Count > 2048)
        {
            float retainFrom = TotalDistance - (firstDistance + spacing * 7 + 60f);
            int remove = 0;
            while (remove + 1 < trail.Count && trail[remove + 1].distance < retainFrom) remove++;
            if (remove > 0) trail.RemoveRange(0, remove);
        }
    }

    private void ResetTrail()
    {
        if (player == null) return;
        Ready = false;
        trail.Clear();
        Vector3 feet = PlayerFeet();
        if (Physics.Raycast(feet + Vector3.up * 1.1f, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            feet.y = hit.point.y;
        Vector3 start = feet - Vector3.right * (firstDistance + spacing * 6 + 0.5f);
        AddPoint(start);
        AddPoint(feet);
        Ready = true;
        if (dwarfs != null)
            foreach (DwarfPursuer dwarf in dwarfs) dwarf.ResetToTrail();
    }

    public float DesiredDistance(int order) => Mathf.Max(trail.Count > 0 ? trail[0].distance : 0f,
        TotalDistance - firstDistance - spacing * order);

    public Vector3 Evaluate(float distance)
    {
        if (trail.Count == 0) return target != null ? target.position : transform.position;
        if (trail.Count == 1 || distance <= trail[0].distance) return trail[0].feet;
        int low = 1, high = trail.Count - 1;
        while (low < high)
        {
            int middle = (low + high) / 2;
            if (trail[middle].distance < distance) low = middle + 1;
            else high = middle;
        }
        TrailPoint a = trail[low - 1], b = trail[low];
        float length = b.distance - a.distance;
        return Vector3.Lerp(a.feet, b.feet, length > 0.0001f ? Mathf.Clamp01((distance - a.distance) / length) : 1f);
    }

    private void OnDestroy()
    {
        if (player != null) player.Respawned -= ResetTrail;
    }
}
