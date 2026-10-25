using System.Collections.Generic;
using UnityEngine;

// Low, straight throws: jump over the visible axe, without player prediction.
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class DwarfAxeProjectile : MonoBehaviour
{
    private const float Lifetime = 5f;
    private const float Speed = 7.5f;
    private const float HitRadius = 0.22f;
    private static readonly List<DwarfAxeProjectile> active = new List<DwarfAxeProjectile>();
    private readonly Collider[] contacts = new Collider[32];
    private Rigidbody body;
    private Transform spin;
    private PlayerController25D target;
    private float direction;
    private bool impacted;

    public static void SpawnAlongGround(Vector3 origin, float facing, PlayerController25D target)
    {
        GameObject axe = new GameObject("Dwarf_Thrown_Axe");
        axe.transform.position = origin;
        SphereCollider hitbox = axe.AddComponent<SphereCollider>();
        hitbox.radius = HitRadius;
        hitbox.isTrigger = true;
        Rigidbody rb = axe.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        GameObject visual = new GameObject("Spinning_Axe_Visual");
        visual.transform.SetParent(axe.transform, false);
        visual.transform.localScale = Vector3.one * 0.68f;
        BuildVisual(visual.transform);
        DwarfAxeProjectile projectile = axe.AddComponent<DwarfAxeProjectile>();
        projectile.body = rb;
        projectile.spin = visual.transform;
        projectile.direction = facing < 0f ? -1f : 1f;
        projectile.target = target;
        active.Add(projectile);
        Destroy(axe, Lifetime);
    }

    public static void ClearFor(PlayerController25D player)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            DwarfAxeProjectile axe = active[i];
            if (axe == null) { active.RemoveAt(i); continue; }
            if (axe.target != player) continue;
            active.RemoveAt(i);
            axe.impacted = true;
            axe.gameObject.SetActive(false);
            Destroy(axe.gameObject);
        }
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
        if (target.isInteracting || LevelIntroCutscene.IsPlaying) return;
        Vector3 next = body.position + Vector3.right * (direction * Speed * Time.fixedDeltaTime);
        // Sweep only the small physical radius over this physics step. This avoids
        // tunnelling without the old 0.62-unit invisible proximity damage area.
        int count = Physics.OverlapCapsuleNonAlloc(body.position, next, HitRadius,
            contacts, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
            if (contacts[i].GetComponentInParent<PlayerController25D>() == target)
            { HitTarget(); return; }
        body.MovePosition(next);
        if (spin != null) spin.Rotate(0f, 0f, -direction * 540f * Time.fixedDeltaTime, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (impacted || target == null || target.isInteracting ||
            LevelIntroCutscene.IsPlaying || Time.timeScale <= 0f) return;
        if (other.GetComponentInParent<PlayerController25D>() == target) HitTarget();
    }

    private void HitTarget()
    {
        if (impacted || target == null || target.isInteracting) return;
        impacted = true;
        gameObject.SetActive(false);
        if (Scene2ChaseController.Instance != null)
            Scene2ChaseController.Instance.HitPlayer(target, "Cüce baltası çarptı!");
        else target.Respawn();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        active.Remove(this);
        // The five visual parts share materials; destroy each allocation once.
        var materials = new HashSet<Material>();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            foreach (Material material in renderer.sharedMaterials)
                if (material != null) materials.Add(material);
        foreach (Material material in materials) Destroy(material);
    }
}
