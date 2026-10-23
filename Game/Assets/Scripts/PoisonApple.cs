
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class PoisonApple : MonoBehaviour
{
    [Header("Yok Olma Süresi")]
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionZ; // 2.5D derinlikten sapmasın
        Destroy(gameObject, lifeTime); // Bir yere çarpmazsa 5 saniye sonra kendini siler
    }

    public void Launch(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Prenses ile çarpışırsa
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.LogWarning("Zehirli elma prensese çarptı! Bölüm başa sarıyor!");
            
            PlayerController25D player = collision.gameObject.GetComponent<PlayerController25D>();
            if (player != null)
            {
                // Prensesi 2. sahnenin başlangıç noktasına geri ışınla
                player.transform.position = new Vector3(0f, 3f, 0f);
            }
            Destroy(gameObject);
        }
        // Zemine veya platforma çarparsa parçalanıp yok olur
        else
        {
            Destroy(gameObject, 0.1f);
        }
    }
}