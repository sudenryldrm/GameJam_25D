using UnityEngine;

// Keep the two FBX mesh parts assigned in the editor and at runtime.
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PrincessAppearance : MonoBehaviour
{
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material outfitMaterial;

    private void OnEnable()
    {
        ApplyMaterials();
    }

    [ContextMenu("Apply Princess Materials")]
    public void ApplyMaterials()
    {
        foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Material selected = null;
            if (renderer.name == "body_body")
                selected = bodyMaterial;
            else if (renderer.name == "dress_hair_shoes_3_dress_hair_shoes_4")
                selected = outfitMaterial;

            if (selected != null && renderer.sharedMaterial != selected)
                renderer.sharedMaterial = selected;
        }
    }
}
