using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PrinceChaserAI : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform[] route;
    [System.Serializable] private struct JumpLink { public int targetIndex; }
    [SerializeField] private JumpLink[] jumpLinks;
    [SerializeField] private Material kissMaterial;
    [SerializeField] private float baseSpeed = 5.1f;
    [SerializeField] private float maximumSpeed = 9.5f;
    [SerializeField] private float startDelay = 3.5f;
    [SerializeField] private float minimumKissInterval = 2f;
    [SerializeField] private float maximumKissInterval = 3f;
    [SerializeField] private float jumpLift = 1.7f;

    private Rigidbody rb;
    private float[] cumulative;
    private float progress;
    private float delay;
    private float attackTimer;
    private float facing = 1f;
    private bool stopped;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        GetComponent<CapsuleCollider>().isTrigger = true;
        if (route == null || route.Length < 2)
        { Debug.LogError("Prince route needs at least two points.", this); enabled = false; return; }
        cumulative = new float[route.Length];
        for (int i = 1; i < route.Length; i++)
            cumulative[i] = cumulative[i - 1] + Vector3.Distance(route[i - 1].position, route[i].position);
    }

    public void ResetChase(int checkpointIndex)
    {
        if (cumulative == null) return;
        int index = Mathf.Clamp(checkpointIndex, 0, route.Length - 1);
        progress = cumulative[Mathf.Max(0, index - 1)];
        rb.position = PositionAt(progress) + Vector3.up * 0.9f;
        delay = startDelay;
        attackTimer = 1f;
        stopped = false;
    }

    public void StopChase() { stopped = true; }

    private bool Playing => !stopped && target != null &&
        TowerLevelController.Instance != null && TowerLevelController.Instance.IsPlaying;

    private void FixedUpdate()
    {
        if (!Playing || cumulative == null || delay > 0f) return;
        float playerProgress = ProjectToRoute(target.position - Vector3.up);
        float gap = Mathf.Abs(playerProgress - progress);
        float speed = Mathf.Clamp(baseSpeed + Mathf.Max(0f, gap - 5f) * 0.5f, baseSpeed, maximumSpeed);
        progress = Mathf.MoveTowards(progress, playerProgress, speed * Time.fixedDeltaTime);
        Vector3 next = PositionAt(progress) + Vector3.up * 0.9f;
        if (Mathf.Abs(next.x - rb.position.x) > 0.001f) facing = Mathf.Sign(next.x - rb.position.x);
        rb.MovePosition(next);
        rb.MoveRotation(Quaternion.Euler(0f, facing * 90f, 0f));
    }

    private void Update()
    {
        if (!Playing) return;
        if (delay > 0f) { delay -= Time.deltaTime; return; }
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;
        Vector3 toPlayer = target.position - transform.position;
        Vector3 origin = transform.position + Vector3.up * 0.35f +
            Vector3.right * (Mathf.Abs(toPlayer.x) > 0.1f ? Mathf.Sign(toPlayer.x) : facing) * 0.75f;
        KissProjectile.Spawn(origin, target.position, kissMaterial);
        attackTimer = Random.Range(minimumKissInterval, Mathf.Max(minimumKissInterval, maximumKissInterval));
    }

    private Vector3 PositionAt(float distance)
    {
        int segment = 1;
        while (segment < cumulative.Length - 1 && cumulative[segment] < distance) segment++;
        float length = cumulative[segment] - cumulative[segment - 1];
        float t = length > 0.001f ? Mathf.Clamp01((distance - cumulative[segment - 1]) / length) : 1f;
        Vector3 point = Vector3.Lerp(route[segment - 1].position, route[segment].position, t);
        if (jumpLinks != null)
            foreach (JumpLink link in jumpLinks)
                if (link.targetIndex == segment) { point.y += 4f * jumpLift * t * (1f - t); break; }
        return point;
    }

    private float ProjectToRoute(Vector3 feet)
    {
        float bestDistance = float.PositiveInfinity;
        float bestProgress = 0f;
        for (int i = 1; i < route.Length; i++)
        {
            Vector3 start = route[i - 1].position;
            Vector3 segment = route[i].position - start;
            float t = segment.sqrMagnitude > 0.001f ? Mathf.Clamp01(Vector3.Dot(feet - start, segment) / segment.sqrMagnitude) : 0f;
            float distance = (feet - (start + segment * t)).sqrMagnitude;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            bestProgress = Mathf.Lerp(cumulative[i - 1], cumulative[i], t);
        }
        return bestProgress;
    }

    private void OnTriggerEnter(Collider other) { TryCatch(other); }
    private void OnTriggerStay(Collider other) { TryCatch(other); }
    private void TryCatch(Collider other)
    {
        if (!Playing || delay > 0f) return;
        PlayerController25D player = other.GetComponentInParent<PlayerController25D>();
        if (player != null) TowerLevelController.Instance.HitPlayer(player, "Prens seni yakaladı!");
    }
}
