using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PoisonCloud : MonoBehaviour
{
    private const float LifeTime = 2.5f;
    private float elapsed;

    public static void Spawn(Vector3 position, Material material)
    {
        GameObject cloud = new GameObject("PoisonCloud_Placeholder");
        cloud.transform.position = position;
        SphereCollider area = cloud.AddComponent<SphereCollider>();
        area.isTrigger = true;
        area.radius = 1f;
        cloud.AddComponent<PoisonCloud>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "CloudVisual_ReplaceWithVFX";
        visual.transform.SetParent(cloud.transform, false);
        visual.transform.localScale = new Vector3(2f, 0.6f, 1.6f);
        Collider visualCollider = visual.GetComponent<Collider>();
        visualCollider.enabled = false;
        Destroy(visualCollider);
        if (material != null) visual.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= LifeTime) Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other) { TryDamage(other); }
    private void OnTriggerStay(Collider other) { TryDamage(other); }

    private void TryDamage(Collider other)
    {
        PlayerController25D player = other.GetComponentInParent<PlayerController25D>();
        if (player == null) return;
        if (Scene2ChaseController.Instance != null)
            Scene2ChaseController.Instance.HitPlayer(player);
        else player.Respawn();
    }
}
