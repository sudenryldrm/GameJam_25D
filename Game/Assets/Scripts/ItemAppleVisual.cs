using UnityEngine;

// Only the art changes; lore anchors, triggers and controllers stay in place.
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class ItemAppleVisual : MonoBehaviour
{
    [SerializeField] private Transform model;
    [SerializeField] private Renderer[] placeholders;
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material stemMaterial;
    [SerializeField] private float height = 0.8f;
    [SerializeField] private float bottomOffset = -0.4f;
    private bool needsFit;
    private float retryAt;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.035f;
    [SerializeField, Min(0f)] private float bobFrequency = 2.2f;
    [SerializeField, Min(0f)] private float tiltDegrees = 4f;
    private Vector3 fittedLocalPosition;
    private Quaternion fittedLocalRotation;
    private bool fitted;

    private void OnEnable() { RequestFit(); }
    private void OnValidate() { RequestFit(); }

    [ContextMenu("Fit Apple Item")]
    public void RequestFit() { needsFit = true; retryAt = 0f; }

    private void Update()
    {
        if (needsFit && Time.realtimeSinceStartup >= retryAt)
        {
            needsFit = !FitModel();
            retryAt = Time.realtimeSinceStartup + 1f;
        }
        if (fitted && model != null)
        {
            float t = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            model.localPosition = fittedLocalPosition + Vector3.up * (Mathf.Sin(t * bobFrequency) * bobAmplitude);
            model.localRotation = fittedLocalRotation *
                Quaternion.Euler(0f, 0f, Mathf.Sin(t * bobFrequency * 0.72f) * tiltDegrees);
        }
    }

    private bool FitModel()
    {
        if (model == null || bodyMaterial == null || stemMaterial == null) return false;
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;
        model.localScale = Vector3.one;
        MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>(true);
        if (!TryBounds(renderers, out Bounds bounds) || bounds.size.y < 0.0001f) return false;
        model.localScale *= Mathf.Max(0.05f, height) / bounds.size.y;
        if (!TryBounds(renderers, out bounds)) return false;
        Vector3 floor = transform.position + Vector3.up * bottomOffset;
        model.position += new Vector3(floor.x - bounds.center.x, floor.y - bounds.min.y,
            floor.z - bounds.center.z);
        foreach (MeshRenderer renderer in renderers)
        {
            if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) continue;
            // These are the two mesh names in the supplied elma.fbx.
            Material material = renderer.name.StartsWith("Cylinder") ? stemMaterial : bodyMaterial;
            Material[] slots = new Material[Mathf.Max(1, filter.sharedMesh.subMeshCount)];
            for (int i = 0; i < slots.Length; i++) slots[i] = material;
            renderer.sharedMaterials = slots;
            renderer.enabled = true;
        }
        if (placeholders != null)
            foreach (Renderer placeholder in placeholders)
                if (placeholder != null) placeholder.enabled = false;
        fittedLocalPosition = model.localPosition;
        fittedLocalRotation = model.localRotation;
        fitted = true;
        return true;
    }

    private static bool TryBounds(MeshRenderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (MeshRenderer renderer in renderers)
        {
            if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return found;
    }
}
