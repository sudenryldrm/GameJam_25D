using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SphereCollider))]
public class InteractableObject : MonoBehaviour
{
    [Header("GDD: Nesne ve Hikaye Bilgileri")]
    [SerializeField] private string objectTitle = "Çirkin'in Maskesi";
    [TextArea(3, 6)]
    [SerializeField] private string descriptionText = "Karanlık bir balodan geriye kalan yırtık bir maske... Sevginin değil, derin bir esaretin sembolü.";

    [Header("Etkileşim Ayarları")]
    [SerializeField] private float detectionRadius = 3.5f; // Biraz daha genişlettik

    private SphereCollider triggerCol;
    private bool playerInRange = false;
    private PlayerController25D player;
    private bool isInspecting = false;

    private void Awake()
    {
        triggerCol = GetComponent<SphereCollider>();
        triggerCol.isTrigger = true;
        triggerCol.radius = detectionRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Tag'e bakmadan doğrudan karakteri kök objeden bulur
        PlayerController25D p = other.GetComponentInParent<PlayerController25D>();
        if (p != null)
        {
            playerInRange = true;
            player = p;
            // Warning olarak atıyoruz ki konsolda simgesi kapalı olsa bile kesin görünsün
            Debug.LogWarning($"[YAKLAŞILDI]: {objectTitle} yanındasın! [E] tuşuna basarak incele.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController25D p = other.GetComponentInParent<PlayerController25D>();
        if (p != null)
        {
            playerInRange = false;
            if (isInspecting && player != null)
            {
                player.isInteracting = false;
                isInspecting = false;
            }
        }
    }

    private void Update()
    {
        if (playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            isInspecting = !isInspecting;
            if (player != null) player.isInteracting = isInspecting;

            if (isInspecting)
            {
                Debug.LogWarning($"=== {objectTitle} ===\n{descriptionText}\n(Kapatmak için tekrar [E] tuşuna bas)");
            }
            else
            {
                Debug.LogWarning($"[İnceleme Bitti]: Yola devam edebilirsin.");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}