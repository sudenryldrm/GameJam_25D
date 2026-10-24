using System.Collections.Generic;
using UnityEngine;

public class DwarfChaseGroup : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Collider walkableGround;
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
    [Header("Cüce Baltaları")]
    [SerializeField] private float firstAxeDelay = 4.5f;
    [SerializeField] private float minimumAxeInterval = 4f;
    [SerializeField] private float maximumAxeInterval = 6f;
    private float axeThrowTimer;
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
        axeThrowTimer = firstAxeDelay;
    }

    // Record the horizontal route on the floor, never the player's jump arc.
    private Vector3 PlayerFeet()
    {
        Vector3 point = playerBody != null ? playerBody.position : target.position;
        if (playerCollider != null) point.y += playerCollider.center.y - playerCollider.height * 0.5f;
        else point.y -= 1f;
        return GroundPoint(point);
    }

    public Vector3 GroundPoint(Vector3 point)
    {
        if (walkableGround == null) return point;
        Bounds bounds = walkableGround.bounds;
        Ray ray = new Ray(new Vector3(point.x, bounds.max.y + 1f, point.z), Vector3.down);
        if (walkableGround.Raycast(ray, out RaycastHit hit, bounds.size.y + 2f))
            point.y = hit.point.y;
        else point.y = bounds.max.y;
        return point;
    }

    private void Update()
    {
        if (!IsFollowing || dwarfs == null || dwarfs.Length == 0) return;
        axeThrowTimer -= Time.deltaTime;
        if (axeThrowTimer > 0f) return;

        DwarfPursuer thrower = dwarfs[Random.Range(0, dwarfs.Length)];
        if (thrower != null && target != null)
        {
            Vector3 origin = thrower.transform.position + Vector3.up * 0.8f +
                Vector3.right * (target.position.x >= thrower.transform.position.x ? 0.25f : -0.25f);
            origin.z = target.position.z;
            float flightTime = Mathf.Clamp(Mathf.Abs(target.position.x - origin.x) / 13f, 0.45f, 1.05f);
            Vector3 destination = target.position;
            if (playerBody != null)
            {
                destination.x += Mathf.Clamp(playerBody.linearVelocity.x * flightTime, -4f, 4f);
                destination.y += Mathf.Clamp(playerBody.linearVelocity.y * flightTime * 0.35f, -1.5f, 1.5f);
            }
            DwarfAxeProjectile.Spawn(origin, destination, flightTime);
        }
        axeThrowTimer = Random.Range(Mathf.Max(1f, minimumAxeInterval),
            Mathf.Max(minimumAxeInterval, maximumAxeInterval));
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
        axeThrowTimer = firstAxeDelay;
        trail.Clear();
        Vector3 feet = PlayerFeet();
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
