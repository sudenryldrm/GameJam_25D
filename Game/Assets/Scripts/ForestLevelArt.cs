using UnityEngine;
using UnityEngine.Rendering;

// Fits the supplied FBX art to existing gameplay surfaces without changing physics.
[ExecuteAlways]
public sealed class ForestLevelArt : MonoBehaviour
{
    public enum ArtKind { Platform, Pine, Grass, Rock, Log, Flower }

    [System.Serializable]
    public sealed class Placement
    {
        public Transform model;
        public ArtKind kind;
        public string[] meshNames;
        public Vector3 surfaceCenter;
        public Vector3 dimensions = Vector3.one;
        public float yaw;
        public float width;
        public Material tint;
        public bool silhouette;
    }

    [SerializeField] private Placement[] placements;
    [SerializeField] private Material earth;
    [SerializeField] private Material turf;
    [SerializeField] private Material pine;
    [SerializeField] private Material grass;
    [SerializeField] private Material rock;
    [SerializeField] private Material wood;
    [SerializeField] private Material cutWood;
    [SerializeField] private Material flowerPetal;
    [SerializeField] private Material flowerCenter;

    private bool needsFit;
    private bool reportedFailure;
    private float retryAt;

    private void OnEnable() { needsFit = true; retryAt = 0f; }
    private void OnValidate() { needsFit = true; }

    [ContextMenu("Fit Forest Art")]
    public void RequestFit() { needsFit = true; retryAt = 0f; reportedFailure = false; }

    private void Update()
    {
        if (!needsFit || Time.realtimeSinceStartup < retryAt) return;
        needsFit = !ApplyLayout();
        retryAt = Time.realtimeSinceStartup + 1f;
        if (needsFit && !reportedFailure)
        {
            Debug.LogWarning("Forest art is waiting for an imported model/mesh. Check the FBX import if it persists.", this);
            reportedFailure = true;
        }
    }

    private bool ApplyLayout()
    {
        if (placements == null) return true;
        bool complete = true;
        foreach (Placement item in placements)
        {
            if (item == null || item.model == null) { complete = false; continue; }
            Transform model = item.model;
            model.localScale = Vector3.one;
            model.SetPositionAndRotation(Vector3.zero,
                item.kind == ArtKind.Grass ? Quaternion.identity : Quaternion.Euler(0f, item.yaw, 0f));
            MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer renderer in renderers)
            {
                bool selected = IsSelected(renderer.name, item.meshNames);
                renderer.enabled = selected;
                if (!selected) continue;
                AssignMaterials(renderer, item.kind, item.tint, item.silhouette);
                // Repeated low-poly decorations receive light but do not add shadow draws.
                renderer.shadowCastingMode = item.kind == ArtKind.Platform ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }
            // Grass.fbx blades use their longest axis as height. Some Unity FBX
            // imports put the axis conversion on the root, which layout resets.
            // Inspect the unscaled art first, stand it up, then apply ground yaw.
            if (item.kind == ArtKind.Grass && TryBounds(renderers, out Bounds grassBounds))
            {
                Vector3 size = grassBounds.size;
                Quaternion upright = Quaternion.identity;
                if (size.z > size.y && size.z >= size.x)
                    upright = Quaternion.FromToRotation(Vector3.forward, Vector3.up);
                else if (size.x > size.y && size.x > size.z)
                    upright = Quaternion.FromToRotation(Vector3.right, Vector3.up);
                model.rotation = Quaternion.Euler(0f, item.yaw, 0f) * upright;
            }
            if (!TryBounds(renderers, out Bounds bounds) || bounds.size.x < 0.0001f || bounds.size.y < 0.0001f || bounds.size.z < 0.0001f)
            { complete = false; continue; }
            if (item.kind == ArtKind.Platform)
                model.localScale = new Vector3(item.dimensions.x / bounds.size.x,
                    item.dimensions.y / bounds.size.y, item.dimensions.z / bounds.size.z);
            else
                model.localScale = Vector3.one * (item.dimensions.y * (item.kind == ArtKind.Log ? 1.18f : 1f) / bounds.size.y);
            if (!TryBounds(renderers, out bounds)) { complete = false; continue; }
            if (item.kind == ArtKind.Grass && item.width > 0f && bounds.size.x > 0.0001f)
            {
                // The grass row is authored at zero yaw; its local X remains
                // horizontal after standing the exported blade axis upright.
                Vector3 scale = model.localScale;
                scale.x *= item.width / bounds.size.x;
                model.localScale = scale;
                if (!TryBounds(renderers, out bounds)) { complete = false; continue; }
            }
            float anchorY = item.kind == ArtKind.Platform ? bounds.max.y : bounds.min.y;
            model.position += new Vector3(item.surfaceCenter.x - bounds.center.x,
                item.surfaceCenter.y - anchorY, item.surfaceCenter.z - bounds.center.z);
        }
        return complete;
    }

    private static bool IsSelected(string meshName, string[] names)
    {
        if (names == null || names.Length == 0) return true;
        foreach (string candidate in names) if (meshName == candidate) return true;
        return false;
    }

    private static bool TryBounds(MeshRenderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (MeshRenderer renderer in renderers)
        {
            if (!renderer.enabled || !renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return found;
    }

    private void AssignMaterials(MeshRenderer renderer, ArtKind kind, Material tint, bool silhouette)
    {
        if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) return;
        int slots = Mathf.Max(1, filter.sharedMesh.subMeshCount);
        Material[] materials = new Material[slots];
        for (int slot = 0; slot < slots; slot++)
        {
            switch (kind)
            {
                case ArtKind.Platform: materials[slot] = renderer.name.StartsWith("grassGround") ? turf : earth; break;
                case ArtKind.Pine: materials[slot] = silhouette && tint != null ? tint :
                    renderer.name.StartsWith("TreeLeaves") ? (tint != null ? tint : pine) : wood; break;
                case ArtKind.Grass: materials[slot] = tint != null ? tint : grass; break;
                case ArtKind.Rock: materials[slot] = rock; break;
                case ArtKind.Flower:
                    materials[slot] = renderer.name.StartsWith("FlowerLeaves") ? grass :
                        renderer.name.StartsWith("FlowerCenters") ? flowerCenter :
                        (tint != null ? tint : flowerPetal);
                    break;
                default: materials[slot] = slot == 0 ? wood : cutWood; break;
            }
        }
        renderer.sharedMaterials = materials;
    }
}

