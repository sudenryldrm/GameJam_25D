using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class KissProjectile : MonoBehaviour
{
    private const float Speed = 8f;
    private const float Radius = 0.22f;
    private static Mesh heartMesh;
    private Rigidbody rb;
    private Vector3 origin;
    private Vector3 direction;
    private Vector3 perpendicular;
    private float elapsed;
    private bool spent;
    private Transform target;
    private Transform visual;
    private float initialDistance;
    private float closestDistance;
    private float visualScale = 1f;

    public static void Spawn(Vector3 start, Vector3 destination, Material material, Transform target = null)
    {
        StorySfx.Play("Kiss", .52f, false);
        GameObject projectile = new GameObject("Kiss_Heart_Placeholder");
        projectile.transform.position = start;
        SphereCollider collider = projectile.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = Radius;
        Rigidbody body = projectile.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        GameObject heartVisual = new GameObject("Heart_Visual");
        heartVisual.transform.SetParent(projectile.transform, false);
        MeshFilter filter = heartVisual.AddComponent<MeshFilter>();
        if (heartMesh == null) heartMesh = BuildHeart();
        filter.sharedMesh = heartMesh;
        if (material != null) heartVisual.AddComponent<MeshRenderer>().sharedMaterial = material;
        KissProjectile kiss = projectile.AddComponent<KissProjectile>();
        kiss.rb = body;
        kiss.target = target;
        kiss.visual = heartVisual.transform;
        kiss.initialDistance = Mathf.Max(1f, Vector3.Distance(start, destination));
        kiss.closestDistance = kiss.initialDistance;
        kiss.origin = start;
        Vector3 aim = destination - start;
        aim.z = 0f;
        kiss.direction = aim.sqrMagnitude > 0.001f ? aim.normalized : Vector3.right;
        kiss.perpendicular = new Vector3(-kiss.direction.y, kiss.direction.x, 0f);
        Destroy(projectile, 5f);
    }

    private static Mesh BuildHeart()
    {
        const int count = 40;
        Vector3[] vertices = new Vector3[count + 1];
        Vector3[] normals = new Vector3[count + 1];
        int[] triangles = new int[count * 3];
        for (int i = 0; i <= count; i++) normals[i] = Vector3.back;
        for (int i = 0; i < count; i++)
        {
            float t = 2f * Mathf.PI * i / count;
            vertices[i + 1] = new Vector3(16f * Mathf.Pow(Mathf.Sin(t), 3f),
                13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t), 0f) * 0.014f;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % count + 1;
        }
        Mesh mesh = new Mesh { name = "PlaceholderHeart" };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private void FixedUpdate()
    {
        if (spent || rb == null) return;
        elapsed += Time.fixedDeltaTime;
        Vector3 next = origin + direction * (Speed * elapsed) + perpendicular * (Mathf.Sin(elapsed * 6f) * 0.18f);
        if (target != null)
        {
            closestDistance = Mathf.Min(closestDistance, Vector3.Distance(next, target.position));
            float proximity = 1f - Mathf.Clamp01((closestDistance - 0.8f) / Mathf.Max(0.1f, initialDistance - 0.8f));
            float desiredScale = Mathf.Lerp(1f, 1.9f, Mathf.SmoothStep(0f, 1f, proximity));
            visualScale = Mathf.MoveTowards(visualScale, desiredScale, 3f * Time.fixedDeltaTime);
            visual.localScale = Vector3.one * visualScale;
        }
        // Only the visual grows; the dodge hitbox stays at its original radius.
        Vector3 delta = next - rb.position;
        if (delta.sqrMagnitude > 0.00001f && Physics.SphereCast(rb.position, Radius, delta.normalized,
            out RaycastHit hit, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            Hit(hit.collider);
            return;
        }
        rb.MovePosition(next);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController25D>() != null) Hit(other);
    }

    private void Hit(Collider other)
    {
        if (spent) return;
        spent = true;
        PlayerController25D player = other.GetComponentInParent<PlayerController25D>();
        if (player != null && TowerLevelController.Instance != null)
            TowerLevelController.Instance.HitPlayer(player, "Prensin öpücüğü sana çarptı!");
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
