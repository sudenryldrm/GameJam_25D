using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DwarfSourceImporter
{
    [Serializable] private sealed class SourceManifest { public SourceFile[] files; }
    [Serializable] private sealed class SourceFile { public string name; public string sha256; public string[] parts; }
    private const string AssetsFolder = "Assets/Resources/Dwarfs/";
    private static bool repairing;

    static DwarfSourceImporter()
    {
        EditorApplication.delayCall += RestoreSources;
        EditorApplication.delayCall += RepairChaseScripts;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    { EditorApplication.delayCall += RepairChaseScripts; }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode) RepairChaseScripts();
    }

    [MenuItem("Tools/Dwarfs/Repair Missing Chase Scripts")]
    private static void RepairChaseScripts()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.isUpdating)
        { EditorApplication.delayCall += RepairChaseScripts; return; }
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)) continue;
            GameObject[] roots = scene.GetRootGameObjects();
            PlayerController25D player = null;
            Collider ground = null;
            GameObject groupRoot = null;
            foreach (GameObject root in roots)
            {
                if (root.name == "SevenDwarfs") groupRoot = root;
                if (root.name == "Ground") ground = root.GetComponent<Collider>();
                if (player == null) player = root.GetComponentInChildren<PlayerController25D>(true);
            }
            if (groupRoot == null || player == null || ground == null) continue;
            // This repair is restricted to the authored seven-dwarf hierarchy.
            // Do not strip missing scripts from unrelated scene objects.
            var followers = new System.Collections.Generic.List<GameObject>();
            for (int i = 1; i <= 7; i++)
            {
                Transform child = groupRoot.transform.Find("Dwarf_" + i.ToString("00"));
                if (child != null && child.GetComponent<CapsuleCollider>() != null &&
                    child.GetComponent<Rigidbody>() != null) followers.Add(child.gameObject);
            }
            if (followers.Count != 7) continue;
            bool changed = false;
            DwarfChaseGroup group = groupRoot.GetComponent<DwarfChaseGroup>();
            if (group == null)
            { RemoveMissingScripts(groupRoot); group = Undo.AddComponent<DwarfChaseGroup>(groupRoot); changed = true; }
            changed |= SetReference(group, "target", player.transform);
            changed |= SetReference(group, "walkableGround", ground);
            for (int i = 0; i < followers.Count; i++)
            {
                GameObject dwarf = followers[i];
                DwarfPursuer follower = dwarf.GetComponent<DwarfPursuer>();
                if (follower == null)
                { RemoveMissingScripts(dwarf); follower = Undo.AddComponent<DwarfPursuer>(dwarf); changed = true; }
                changed |= SetReference(follower, "chase", group);
                var serialized = new SerializedObject(follower);
                SerializedProperty order = serialized.FindProperty("order");
                if (order.intValue != i)
                { order.intValue = i; serialized.ApplyModifiedProperties(); changed = true; }
            }
            if (!changed) continue;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Dwarfs: missing chase scripts repaired; player, ground and all seven followers reconnected. Save the scene (Ctrl+S).", groupRoot);
        }
    }

    private static bool RemoveMissingScripts(GameObject target)
    {
        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target) == 0) return false;
        Undo.RegisterCompleteObjectUndo(target, "Repair dwarf chase scripts");
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
        return true;
    }

    private static bool SetReference(UnityEngine.Object component, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(field);
        if (property.objectReferenceValue == value) return false;
        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
        return true;
    }

    [MenuItem("Tools/Dwarfs/Restore Uploaded Model Sources")]
    [MenuItem("Tools/Dwarfs/Repair Materials and Run Animation")]
    private static void RestoreSources()
    {
        if (repairing || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RestoreSources;
            return;
        }
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../ModelSources/Dwarfs"));
        string manifestPath = Path.Combine(folder, "manifest.json");
        if (!File.Exists(manifestPath)) return;
        repairing = true;
        try
        {
            SourceManifest manifest = JsonUtility.FromJson<SourceManifest>(File.ReadAllText(manifestPath));
            string destination = Path.Combine(Application.dataPath, "Resources/Dwarfs");
            Directory.CreateDirectory(destination);
            foreach (SourceFile file in manifest.files)
            {
                string target = Path.Combine(destination, file.name);
                if (File.Exists(target) && Hash(File.ReadAllBytes(target)) == file.sha256) continue;
                StringBuilder encoded = new StringBuilder();
                foreach (string part in file.parts) encoded.Append(File.ReadAllText(Path.Combine(folder, part)));
                byte[] bytes = Convert.FromBase64String(encoded.ToString());
                if (Hash(bytes) != file.sha256) throw new InvalidDataException("Source checksum mismatch: " + file.name);
                File.WriteAllBytes(target, bytes);
            }
            // Import even when bytes already exist: their .meta GUIDs may have been
            // recreated before source restoration on the first checkout.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTexture("GnomeDiffuse.png", false);
            ConfigureTexture("GnomeNormal.png", true);
            ConfigureAnimation();
            RepairMaterial();
            AssetDatabase.SaveAssets();
            foreach (DwarfModelVisual visual in Resources.FindObjectsOfTypeAll<DwarfModelVisual>())
                if (visual.gameObject.scene.IsValid()) visual.RebuildVisual();
            SceneView.RepaintAll();
            Debug.Log("Dwarfs: textures reconnected and looping run animation repaired.");
        }
        catch (Exception error) { Debug.LogError("Could not repair dwarf assets: " + error); }
        finally { repairing = false; }
    }

    private static void ConfigureTexture(string name, bool normal)
    {
        string path = AssetsFolder + name;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidDataException("Missing texture importer: " + path);
        TextureImporterType type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (importer.textureType == type && importer.sRGBTexture == !normal) return;
        importer.textureType = type;
        importer.sRGBTexture = !normal;
        importer.SaveAndReimport();
    }

    private static void ConfigureAnimation()
    {
        string path = AssetsFolder + "GnomeRun.fbx";
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) throw new InvalidDataException("Missing dwarf model importer.");
        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.optimizeGameObjects = false;
        // Read the actual take/frame range, rather than assuming a 22-frame take.
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips.Length == 0) throw new InvalidDataException("No animation takes in GnomeRun.fbx.");
        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.name = "GnomeRun";
            clip.loopTime = true;
            clip.loopPose = true;
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        AnimationClip run = null;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is AnimationClip candidate && !candidate.name.StartsWith("__preview__") && candidate.length > 0f)
            { run = candidate; break; }
        if (run == null || AnimationUtility.GetCurveBindings(run).Length == 0)
            throw new InvalidDataException("No usable bone animation curves in the dwarf run clip.");
        string clipPath = AssetsFolder + "GnomeRunAnimation.anim";
        AnimationClip standalone = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (standalone == null)
        {
            standalone = UnityEngine.Object.Instantiate(run);
            AssetDatabase.CreateAsset(standalone, clipPath);
        }
        else EditorUtility.CopySerialized(run, standalone);
        standalone.hideFlags = HideFlags.None;
        standalone.name = "GnomeRunAnimation";
        standalone.legacy = false;
        standalone.wrapMode = WrapMode.Loop;
        EditorUtility.SetDirty(standalone);
    }

    private static void RepairMaterial()
    {
        string path = AssetsFolder + "Gnome.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) throw new InvalidDataException("No supported dwarf shader.");
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.shader = shader;
        Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetsFolder + "GnomeDiffuse.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetsFolder + "GnomeNormal.png");
        if (diffuse == null || normal == null) throw new InvalidDataException("Dwarf textures did not import.");
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", diffuse);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", diffuse);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", normal);
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
    }

    private static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }
}
