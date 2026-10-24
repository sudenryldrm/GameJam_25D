using UnityEngine;

// Coloured low-poly axe projectile with trigger and proximity hit detection.
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class DwarfAxeProjectile : MonoBehaviour
{
    private const float Lifetime = 7f;
    private Rigidbody body;
    private PlayerController25D target;
    private bool impacted;
    private float age;

    public static void Spawn(Vector3 origin, Vector3 destination, float flightTime, PlayerController25D target)
    {
        GameObject axe = new GameObject("Dwarf_Thrown_Axe");
        axe.transform.position = origin;
        axe.transform.localScale = Vector3.one * 0.72f;

        SphereCollider hitbox = axe.AddComponent<SphereCollider>();
        hitbox.radius = 0.48f;
        hitbox.isTrigger = true;

        Rigidbody rb = axe.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.mass = 0.65f;
        rb.linearDamping = 0.02f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        BuildVisual(axe.transform);
        DwarfAxeProjectile projectile = axe.AddComponent<DwarfAxeProjectile>();
        projectile.body = rb;
        projectile.target = target;

        float time = Mathf.Max(0.2f, flightTime);
        Vector3 velocity = (destination - origin) / time - Physics.gravity * (0.5f * time);
        velocity.z = 0f;
        rb.linearVelocity = velocity;
        rb.angularVelocity = new Vector3(0f, 0f, -7f);
        Destroy(axe, Lifetime);
    }

    private static void BuildVisual(Transform root)
    {
        Material handle = MakeMaterial(new Color(0.34f, 0.16f, 0.07f));
        Material grip = MakeMaterial(new Color(0.12f, 0.075f, 0.055f));
        Material steel = MakeMaterial(new Color(0.68f, 0.73f, 0.78f), 0.45f);
        Material edge = MakeMaterial(new Color(0.95f, 0.79f, 0.36f), 0.25f);
        AddCube(root, "Wooden_Handle", new Vector3(-0.06f, 0f, 0f), new Vector3(0.12f, 0.82f, 0.12f), handle);
        AddCube(root, "Leather_Grip", new Vector3(-0.06f, -0.27f, -0.005f), new Vector3(0.14f, 0.23f, 0.145f), grip);
        AddCube(root, "Steel_Axe_Head", new Vector3(0.13f, 0.27f, 0f), new Vector3(0.45f, 0.25f, 0.16f), steel);
        AddCube(root, "Golden_Cutting_Edge", new Vector3(0.34f, 0.27f, 0f), new Vector3(0.11f, 0.29f, 0.17f), edge);
        AddCube(root, "Steel_Pommel", new Vector3(-0.06f, -0.44f, 0f), new Vector3(0.19f, 0.09f, 0.16f), steel);
    }

    private static void AddCube(Transform parent, string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = objectName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
    }

    private static Material MakeMaterial(Color color, float metallic = 0f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        return material;
    }

    private void FixedUpdate()
    {
        if (impacted || target == null) return;
        age += Time.fixedDeltaTime;
        // A 2.5D game can have thin or offset colliders; proximity detection makes
        // the attack reliable even if the rendered axe passes between physics steps.
        Vector3 closest = target.GetComponent<Collider>() != null
            ? target.GetComponent<Collider>().ClosestPoint(transform.position)
            : target.transform.position;
        if (Vector3.Distance(closest, transform.position) <= 0.62f)
            HitTarget();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (impacted) return;
        PlayerController25D player = other.GetComponentInParent<PlayerController25D>();
        if (player != null && player == target) HitTarget();
    }

    private void HitTarget()
    {
        if (impacted) return;
        impacted = true;
        if (target != null)
        {
            if (Scene2ChaseController.Instance != null)
                Scene2ChaseController.Instance.HitPlayer(target, "Cüce baltası çarptı!");
            else
                target.Respawn();
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
            foreach (Material material in renderer.sharedMaterials)
                if (material != null) Destroy(material);
    }
}
