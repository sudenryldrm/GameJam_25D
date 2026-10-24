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
    [SerializeField] private float arcHeight = 2f;

    private float throwTimer;
    private bool stopped;

    private void Start()
    {
        if (target == null)
        {
            PlayerController25D player = FindFirstObjectByType<PlayerController25D>();
            if (player != null) target = player.transform;
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
        PlayerController25D player = target.GetComponent<PlayerController25D>();
        if (player != null && player.isInteracting) return;

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
        Vector3 origin = throwPoint != null ? throwPoint.position : transform.position + Vector3.right;
        origin.z = target.position.z;
        GameObject apple = Instantiate(applePrefab, origin, Quaternion.identity);
        PoisonApple projectile = apple.GetComponent<PoisonApple>();
        if (projectile == null) { Destroy(apple); return; }
        Vector3 destination = target.position + Vector3.down * 0.25f;
        projectile.Launch(CalculateArcVelocity(origin, destination, arcHeight), poisonCloudMaterial);
    }

    public static Vector3 CalculateArcVelocity(Vector3 origin, Vector3 destination, float arc)
    {
        float gravity = Mathf.Max(0.01f, Mathf.Abs(Physics.gravity.y));
        float apex = Mathf.Max(origin.y, destination.y) + Mathf.Max(0.1f, arc);
        float velocityY = Mathf.Sqrt(2f * gravity * (apex - origin.y));
        float flightTime = velocityY / gravity + Mathf.Sqrt(2f * (apex - destination.y) / gravity);
        return new Vector3((destination.x - origin.x) / flightTime, velocityY, 0f);
    }
}
