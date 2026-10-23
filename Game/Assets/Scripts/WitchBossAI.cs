using UnityEngine;

public class WitchBossAI : MonoBehaviour
{
    [Header("Hedef Takip")]
    [SerializeField] private Transform target;                                  // Pamuk Prenses
    [SerializeField] private Vector3 followOffset = new Vector3(-6f, 3.8f, 0f); // Prensesin sol üstünde durur
    [SerializeField] private float smoothSpeed = 3f;

    [Header("GDD: Sinüs Havada Süzülme (Hover)")]
    [SerializeField] private float hoverFrequency = 1.5f;                       // Dalgalanma hızı
    [SerializeField] private float hoverAmplitude = 0.4f;                       // Havada yukarı-aşağı salınım

    [Header("GDD: Zehirli Elma Fırlatma (Otomatik)")]
    [SerializeField] private GameObject applePrefab;                            // Fırlatılacak elma prefab'ı
    [SerializeField] private Transform throwPoint;                              // Elmanın çıkış noktası
    [SerializeField] private float throwInterval = 3f;                          // Her 3 saniyede bir atar
    [SerializeField] private float arcHeight = 3.5f;                            // Parabolik yayın tepe yüksekliği

    private float throwTimer;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        throwTimer = throwInterval;
    }

    private void Update()
    {
        if (target == null) return;

        // 1. Prensesi sabit mesafeden takip et
        Vector3 targetPos = target.position + followOffset;

        // 2. GDD: Sinüs süzülme dalgası
        float hoverY = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        targetPos.y += hoverY;
        targetPos.z = 0f; // Z eksenini 2.5D derinlikte kilitle

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);

        // Yüzünü her zaman prensese (sağa) dönük tut
        transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        // 3. Otomatik Parabolik Elma Atış Zamanlayıcısı
        throwTimer -= Time.deltaTime;
        if (throwTimer <= 0f && applePrefab != null)
        {
            ThrowPoisonApple();
            throwTimer = throwInterval;
        }
    }

    private void ThrowPoisonApple()
    {
        Vector3 spawnPos = (throwPoint != null) ? throwPoint.position : transform.position;
        GameObject apple = Instantiate(applePrefab, spawnPos, Quaternion.identity);

        PoisonApple appleScript = apple.GetComponent<PoisonApple>();
        if (appleScript != null)
        {
            // Parabolik atış fiziği hesabı: Prensesin olduğu noktaya doğru kavisli fırlatma
            Vector3 targetPosition = target.position;
            Vector3 velocity = CalculateArcVelocity(spawnPos, targetPosition, arcHeight);
            appleScript.Launch(velocity);
        }
    }

    // Parabolik yay (AddForce / Açısal Atış) matematiksel hesabı
    private Vector3 CalculateArcVelocity(Vector3 origin, Vector3 targetPos, float arc)
    {
        float displacementY = targetPos.y - origin.y;
        Vector3 displacementXZ = new Vector3(targetPos.x - origin.x, 0f, 0f);

        float gravity = Mathf.Abs(Physics.gravity.y);
        float time = Mathf.Sqrt(-2 * arc / -gravity) + Mathf.Sqrt(2 * Mathf.Max(0.01f, displacementY - arc) / -gravity);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * -gravity * arc);
        Vector3 velocityXZ = displacementXZ / time;

        return velocityXZ + velocityY;
    }
}