using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Preview-only forest surrounds the first-person route; no colliders, AI or per-tree objects.
public sealed class OpeningForestScenery : MonoBehaviour
{
    private readonly List<Object> owned = new List<Object>();
    private sealed class Batch
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> indices = new List<int>();
        public readonly List<Vector2> weights = new List<Vector2>();
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, float wa = 0, float wb = 0, float wc = 0)
        { int i = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); indices.Add(i); indices.Add(i + 1); indices.Add(i + 2); weights.Add(new Vector2(wa, 0)); weights.Add(new Vector2(wb, 0)); weights.Add(new Vector2(wc, 0)); }
    }

    public static void Build(GameObject preview, bool haunted)
    {
        var owner = preview.AddComponent<OpeningForestScenery>();
        var rng = new System.Random(haunted ? 2107 : 1071);
        float Next(float min, float max) => Mathf.Lerp(min, max, (float)rng.NextDouble());
        var trunks = new Batch();
        var trees = new[] { new Batch(), new Batch(), new Batch() };
        float[] rows = { -55, -39, -25, -13, 8, 19, 33, 51 };
        foreach (float row in rows)
        {
            int layer = Mathf.Abs(row) > 35 ? 2 : Mathf.Abs(row) > 18 ? 1 : 0;
            float spacing = layer == 0 ? 5.2f : 6.6f;
            for (float x = -70; x < 105; x += spacing)
            {
                float z = row + Next(-2, 2), px = x + Next(-1.8f, 1.8f);
                Vector3 root = new Vector3(px, Height(px, z), z);
                float height = Next(6.5f, 10.5f) + layer * 2.3f;
                float radius = height * Next(.20f, .27f);
                float rotation = Next(0, Mathf.PI * 2);
                Trunk(trunks, root, height * .63f, radius * .13f, rotation);
                Cone(trees[layer], root + Vector3.up * height * .23f, height * .64f, radius, rotation);
                Cone(trees[layer], root + Vector3.up * height * .48f, height * .52f, radius * .76f, rotation + .3f);
            }
        }
        var ground = new Batch();
        const float step = 5;
        for (float x = -80; x < 115; x += step)
            for (float z = -75; z < 75; z += step)
            {
                Vector3 Point(float a, float b) => new Vector3(a, Height(a, b) - .08f, b);
                Vector3 a = Point(x, z), b = Point(x + step, z), c = Point(x, z + step), d = Point(x + step, z + step);
                ground.Triangle(a, c, b); ground.Triangle(b, c, d);
            }
        owner.Emit("Forest floor and rolling foothills", ground, haunted ? new Color(.15f, .22f, .14f) : new Color(.25f, .31f, .11f));
        owner.Emit("Forest trunks", trunks, new Color(.22f, .14f, .075f));
        owner.Emit("Near forest canopy", trees[0], haunted ? new Color(.10f, .19f, .14f) : new Color(.16f, .29f, .085f));
        owner.Emit("Middle forest canopy", trees[1], haunted ? new Color(.14f, .24f, .22f) : new Color(.22f, .34f, .14f));
        owner.Emit("Distant forest canopy", trees[2], haunted ? new Color(.22f, .30f, .31f) : new Color(.30f, .39f, .24f));
    }

    private static float Height(float x, float z)
    {
        float slope = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Mathf.Abs(z + 3) - 5) / 40));
        return -.8f + slope * (2 + Mathf.PerlinNoise(x * .028f + 12, z * .026f + 8) * 7);
    }

    private static void Cone(Batch batch, Vector3 root, float height, float radius, float rotation)
    {
        const int sides = 7;
        for (int i = 0; i < sides; i++)
        {
            float a = rotation + i * Mathf.PI * 2 / sides, b = rotation + (i + 1) * Mathf.PI * 2 / sides;
            Vector3 p = root + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            Vector3 q = root + new Vector3(Mathf.Cos(b) * radius, 0, Mathf.Sin(b) * radius);
            batch.Triangle(p, root + Vector3.up * height, q, 0, 1, 0);
        }
    }

    private static void Trunk(Batch batch, Vector3 root, float height, float radius, float rotation)
    {
        for (int i = 0; i < 5; i++)
        {
            float a = rotation + i * Mathf.PI * 2 / 5, b = rotation + (i + 1) * Mathf.PI * 2 / 5;
            Vector3 p = root + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            Vector3 q = root + new Vector3(Mathf.Cos(b) * radius, 0, Mathf.Sin(b) * radius);
            batch.Triangle(p, p + Vector3.up * height, q); batch.Triangle(q, p + Vector3.up * height, q + Vector3.up * height);
        }
    }

    private void Emit(string label, Batch batch, Color color)
    {
        var mesh = new Mesh { name = label };
        if (batch.vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(batch.vertices); mesh.SetUVs(1, batch.weights); mesh.SetTriangles(batch.indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        bool canopy = label.Contains("canopy");
        if (canopy) { Bounds bounds = mesh.bounds; bounds.Expand(new Vector3(.5f, 0, .5f)); mesh.bounds = bounds; }
        Shader shader = canopy && ForestWind.FoliageShader ? ForestWind.FoliageShader : Shader.Find("Universal Render Pipeline/Lit");
        var material = new Material(shader) { name = label + " material" };
        material.SetColor("_BaseColor", color);
        if (canopy) { material.SetFloat("_UseVertexWeights", 1); material.SetFloat("_WindStrength", .13f); }
        else material.SetFloat("_Smoothness", .08f);
        var piece = new GameObject(label); piece.transform.SetParent(transform, false);
        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = piece.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        owned.Add(mesh); owned.Add(material);
    }

    private void OnDestroy()
    {
        foreach (Object item in owned) if (item) { if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
    }
}
