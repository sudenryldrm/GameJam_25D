using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class PoisonApple : MonoBehaviour
{
    [SerializeField] private float lifeTime = 8f;
    private Rigidbody rb;
    private Material cloudMaterial;
    private bool impacted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionZ;
        rb.useGravity = true;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector3 velocity, Material poisonMaterial = null)
    {
        cloudMaterial = poisonMaterial;
        rb.linearVelocity = velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (impacted) return;
        if (collision.collider.GetComponentInParent<PoisonApple>() != null) return;
        impacted = true;
        PlayerController25D player = collision.collider.GetComponentInParent<PlayerController25D>();
        if (player != null)
        {
            if (Scene2ChaseController.Instance != null)
                Scene2ChaseController.Instance.HitPlayer(player);
            else player.Respawn();
        }
        else
        {
            Vector3 point = transform.position;
            if (collision.contactCount > 0)
                point = collision.GetContact(0).point + collision.GetContact(0).normal * 0.15f;
            PoisonCloud.Spawn(point, cloudMaterial);
        }
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
