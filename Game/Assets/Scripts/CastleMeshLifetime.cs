using System.Collections.Generic;
using UnityEngine;

// Only owns meshes generated at runtime; imported and cached mesh assets stay shared.
public sealed class CastleMeshLifetime : MonoBehaviour
{
    private readonly List<Mesh> meshes = new List<Mesh>();
    public void Own(Mesh mesh) { meshes.Add(mesh); }
    private void OnDestroy()
    {
        foreach (Mesh mesh in meshes) if (mesh) Destroy(mesh);
        meshes.Clear();
    }
}
