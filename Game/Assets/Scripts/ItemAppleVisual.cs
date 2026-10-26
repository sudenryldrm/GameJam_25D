using UnityEngine;
using UnityEngine.Rendering;

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
    [Header("Hikaye elmasini belirginlestir")]
    [SerializeField, Min(0f)] private float hoverLift = .85f;
    [SerializeField, Min(0f)] private float turnSpeed = 22f;
    private MeshRenderer[] modelRenderers;
    private Transform player;
    private GameObject halo;
    private Material haloMaterial;
    private Texture2D glowTexture;
    private Mesh haloMesh;

    public Vector3 InteractionPoint => modelRenderers != null && TryBounds(modelRenderers, out Bounds bounds)
        ? bounds.center : transform.position + Vector3.up * hoverLift;

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
        if (fitted && model != null && Application.isPlaying)
        {
            float t = Time.time;
            // These are story collectibles, so their hover must remain noticeable at gameplay zoom.
            model.position = transform.TransformPoint(fittedLocalPosition) +
                Vector3.up * (Mathf.Sin(t * bobFrequency * .65f) * Mathf.Max(.14f, bobAmplitude));
            model.localRotation = fittedLocalRotation *
                Quaternion.Euler(0f, t * turnSpeed, Mathf.Sin(t * bobFrequency * 0.72f) * tiltDegrees);
            if (!halo) CreateHalo();
            Camera camera = Camera.main;
            if (halo && camera)
            {
                halo.transform.position = InteractionPoint + camera.transform.forward * .08f;
                halo.transform.rotation = camera.transform.rotation;
                float size = Mathf.Max(1.25f, height * 2.1f) * (1 + Mathf.Sin(t * 2) * .06f);
                halo.transform.localScale = Vector3.one * size;
                haloMaterial.SetColor("_BaseColor", new Color(1f, .68f, .2f, .35f + Mathf.Sin(t * 2) * .06f));
            }
        }
    }

    private bool FitModel()
    {
        if (model == null || bodyMaterial == null || stemMaterial == null) return false;
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;
        model.localScale = Vector3.one;
        MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>(true);
        modelRenderers = renderers;
        if (!TryBounds(renderers, out Bounds bounds) || bounds.size.y < 0.0001f) return false;
        model.localScale *= Mathf.Max(0.05f, height) / bounds.size.y;
        if (!TryBounds(renderers, out bounds)) return false;
        Vector3 floor = transform.position + Vector3.up * (bottomOffset + hoverLift);
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

    private void CreateHalo()
    {
        glowTexture = new Texture2D(48, 48, TextureFormat.RGBA32, false)
        { name = "Story apple soft glow", hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
        var colors = new Color[48 * 48];
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
        {
            float radius = Vector2.Distance(new Vector2(x, y), Vector2.one * 23.5f) / 24;
            colors[y * 48 + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - radius), 1.8f));
        }
        glowTexture.SetPixels(colors); glowTexture.Apply(false, true);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        haloMaterial = new Material(shader) { name = "Story apple gold aura", hideFlags = HideFlags.HideAndDontSave };
        haloMaterial.SetTexture("_BaseMap", glowTexture);
        haloMaterial.SetFloat("_Surface", 1); haloMaterial.SetFloat("_ZWrite", 0);
        haloMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        haloMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        haloMaterial.SetFloat("_Cull", (float)CullMode.Off);
        haloMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); haloMaterial.renderQueue = 3000;
        haloMesh = new Mesh { name = "Story apple halo quad", hideFlags = HideFlags.HideAndDontSave };
        haloMesh.vertices = new[] {new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};
        haloMesh.uv = new[] {Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
        haloMesh.triangles = new[] {0,2,1,0,3,2}; haloMesh.RecalculateBounds();
        halo = new GameObject("Story apple glow") { hideFlags = HideFlags.DontSave };
        halo.transform.SetParent(transform, false); halo.AddComponent<MeshFilter>().sharedMesh = haloMesh;
        var renderer = halo.AddComponent<MeshRenderer>(); renderer.sharedMaterial = haloMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !fitted || !glowTexture || LevelIntroCutscene.IsPlaying ||
            StorybookCoverScreen.IsShowing || Time.timeScale <= 0) return;
        if (!player) { var found = FindFirstObjectByType<PlayerController25D>(); if (found) player = found.transform; }
        if (!player || Mathf.Abs(player.position.x - transform.position.x) > 17f) return;
        Camera camera = Camera.main; if (!camera) return;
        Vector3 projected = camera.WorldToViewportPoint(InteractionPoint + Vector3.up * .85f);
        if (projected.z <= 0 || projected.x < -.05f || projected.x > 1.05f) return;
        Matrix4x4 previousMatrix = GUI.matrix; Color previousColor = GUI.color; int previousDepth = GUI.depth;
        GUI.matrix = Matrix4x4.identity; GUI.depth = -120;
        float size = Mathf.Clamp(Screen.height * .035f, 24, 40);
        float x = Mathf.Clamp(projected.x * Screen.width, size, Screen.width - size);
        float rawY = (1 - projected.y) * Screen.height;
        float y = Mathf.Clamp(rawY, Screen.height * .18f, Screen.height * .8f);
        float pulse = .75f + Mathf.Sin(Time.time * 2.5f) * .15f;
        GUI.color = new Color(1f, .72f, .26f, pulse);
        GUI.DrawTexture(new Rect(x-size*1.1f,y-size*1.1f,size*2.2f,size*2.2f), glowTexture);
        // Open-book silhouette tells players this apple contains a journal, even below its platform.
        GUI.color = new Color(.25f,.13f,.045f,.96f);
        GUI.DrawTexture(new Rect(x-size*.56f,y-size*.34f,size*1.12f,size*.75f), Texture2D.whiteTexture);
        GUI.color = new Color(1f,.87f,.56f,.98f);
        GUI.DrawTexture(new Rect(x-size*.49f,y-size*.28f,size*.43f,size*.59f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x+size*.06f,y-size*.28f,size*.43f,size*.59f), Texture2D.whiteTexture);
        GUI.color = new Color(.49f,.29f,.10f,.8f);
        for(int i=0;i<3;i++)
        {
            GUI.DrawTexture(new Rect(x-size*.4f,y-size*.12f+i*size*.14f,size*.25f,1),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x+size*.15f,y-size*.12f+i*size*.14f,size*.25f,1),Texture2D.whiteTexture);
        }
        if (rawY < Screen.height*.18f)
        {
            GUI.color = new Color(1f,.82f,.4f,pulse);
            GUIUtility.RotateAroundPivot(45,new Vector2(x,y-size*.65f));
            GUI.DrawTexture(new Rect(x-size*.07f,y-size*.72f,size*.14f,size*.14f),Texture2D.whiteTexture);
        }
        GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth;
    }

    private void OnDisable()
    {
        Release(halo); Release(haloMesh); Release(haloMaterial); Release(glowTexture);
        halo = null; haloMesh = null; haloMaterial = null; glowTexture = null;
    }

    private static void Release(Object item)
    { if (!item) return; if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }

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
