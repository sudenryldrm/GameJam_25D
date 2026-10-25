using UnityEngine;

public class WitchBossAI : MonoBehaviour
{
    [Header("Takip")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 followOffset = new Vector3(-6f, 3.8f, 0f);
    [SerializeField] private float baseSpeed = 4.8f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float minimumDistance = 3f;
    [SerializeField] private float verticalFollowSpeed = 5f;
    [SerializeField] private float hoverFrequency = 1.5f;
    [SerializeField] private float hoverAmplitude = 0.4f;

    [Header("Zehirli Elma")]
    [SerializeField] private GameObject applePrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private Material poisonCloudMaterial;
    [SerializeField] private float minimumThrowInterval = 2.5f;
    [SerializeField] private float maximumThrowInterval = 4f;
    [SerializeField] private float startDelay = 3.5f;
    [SerializeField] private float minimumFlightTime = 0.5f;
    [SerializeField] private float maximumFlightTime = 0.85f;

    private PlayerController25D player;
    private Rigidbody targetBody;

    private float throwTimer;
    private float voiceTimer = 7;
    private bool stopped;

    private void Start()
    {
        if (target == null)
        {
            player = FindFirstObjectByType<PlayerController25D>();
            if (player != null) target = player.transform;
        }
        if (target != null)
        {
            player = target.GetComponent<PlayerController25D>();
            targetBody = target.GetComponent<Rigidbody>();
        }
        ResetChase();
    }

    public void ResetChase()
    {
        stopped = false;
        throwTimer = startDelay;
        if (target != null)
            transform.position = new Vector3(target.position.x + followOffset.x,
                target.position.y + followOffset.y, target.position.z);
    }

    public void StopChase() { stopped = true; }

    private void Update()
    {
        if (target == null || stopped || Time.timeScale == 0f) return;
        if (player != null && player.isInteracting) return;

        voiceTimer -= Time.deltaTime;
        if (voiceTimer <= 0) { if (Vector3.Distance(transform.position, target.position) < 18) StorySfx.Reaction("Witch"); voiceTimer = Random.Range(11f, 17f); }
        float gap = target.position.x - transform.position.x;
        float preferredGap = Mathf.Abs(followOffset.x);
        float speed = Mathf.Clamp(baseSpeed + Mathf.Max(0f, gap - preferredGap) * 1.5f,
            0f, maxSpeed);
        Vector3 position = transform.position;
        position.x = Mathf.Min(position.x + speed * Time.deltaTime,
            target.position.x - minimumDistance);
        float desiredY = target.position.y + followOffset.y +
            Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        position.y = Mathf.Lerp(position.y, desiredY, 1f - Mathf.Exp(-verticalFollowSpeed * Time.deltaTime));
        position.z = target.position.z;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 90f, 0f));

        throwTimer -= Time.deltaTime;
        if (throwTimer > 0f || applePrefab == null) return;
        ThrowPoisonApple();
        float min = Mathf.Max(0.25f, minimumThrowInterval);
        throwTimer = Random.Range(min, Mathf.Max(min, maximumThrowInterval));
    }

    private void ThrowPoisonApple()
    {
        StorySfx.Play("Throw", .25f, false, .92f);
        Vector3 origin = throwPoint != null ? throwPoint.position : transform.position + Vector3.right;
        origin.z = target.position.z;
        GameObject apple = Instantiate(applePrefab, origin, Quaternion.identity);
        PoisonApple projectile = apple.GetComponent<PoisonApple>();
        if (projectile == null) { Destroy(apple); return; }
        // Predict horizontal travel once at release: dangerous, but still dodgeable.
        float flightTime = Mathf.Clamp(Mathf.Abs(target.position.x - origin.x) / 18f,
            minimumFlightTime, maximumFlightTime);
        Vector3 velocity = targetBody != null ? targetBody.linearVelocity : Vector3.zero;
        Vector3 destination = target.position;
        destination.x += Mathf.Clamp(velocity.x * flightTime, -10f, 10f);
        destination.y += Mathf.Clamp(velocity.y * 0.16f, -1.5f, 2f);
        projectile.Launch(CalculateTimedVelocity(origin, destination, flightTime), poisonCloudMaterial);
    }

    public static Vector3 CalculateTimedVelocity(Vector3 origin, Vector3 destination, float seconds)
    {
        float time = Mathf.Max(0.1f, seconds);
        Vector3 velocity = (destination - origin) / time - Physics.gravity * (0.5f * time);
        velocity.z = 0f;
        return velocity;
    }
}
