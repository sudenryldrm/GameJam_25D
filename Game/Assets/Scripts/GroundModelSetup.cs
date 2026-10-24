using UnityEngine;

// Align the imported art to the existing walkable collider in edit mode and play mode.
[ExecuteAlways]
public class GroundModelSetup : MonoBehaviour
{
    [SerializeField] private BoxCollider groundCollider;
    [SerializeField] private Transform[] tiles;
    [SerializeField] private Material stoneMaterial;
    [SerializeField] private float overlap = 0.04f;
    private bool needsFit;

    private void OnEnable() { needsFit = true; }
    private void OnValidate() { needsFit = true; }
    private void Update()
    {
        if (needsFit) needsFit = !FitTiles();
    }

    [ContextMenu("Fit Ground Model")]
    public void FitGroundModel() { needsFit = !FitTiles(); }

    private bool FitTiles()
    {
        if (groundCollider == null || tiles == null || tiles.Length == 0) return false;
        Bounds target = groundCollider.bounds;
        float tileWidth = (target.size.x + overlap * (tiles.Length - 1)) / tiles.Length;
        for (int i = 0; i < tiles.Length; i++)
        {
            Transform tile = tiles[i];
            if (tile == null) return false;
            tile.localScale = Vector3.one;
            tile.position = Vector3.zero;
            MeshRenderer[] renderers = tile.GetComponentsInChildren<MeshRenderer>(true);
            if (!TryBounds(renderers, out Bounds bounds)) return false;
            if (bounds.size.x < 0.001f || bounds.size.z < 0.001f) return false;
            tile.localScale = new Vector3(tileWidth / bounds.size.x, 1f, target.size.z / bounds.size.z);
            if (!TryBounds(renderers, out bounds)) return false;
            float centerX = target.min.x + tileWidth * 0.5f + i * (tileWidth - overlap);
            tile.position += new Vector3(centerX - bounds.center.x,
                target.max.y - bounds.max.y, target.center.z - bounds.center.z);
            if (stoneMaterial != null)
                foreach (MeshRenderer renderer in renderers)
                    if (renderer.sharedMaterial != stoneMaterial) renderer.sharedMaterial = stoneMaterial;
        }
        Renderer oldVisual = groundCollider.GetComponent<Renderer>();
        if (oldVisual != null) oldVisual.enabled = false;
        return true;
    }

    private static bool TryBounds(MeshRenderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null || !renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return found;
    }
}
