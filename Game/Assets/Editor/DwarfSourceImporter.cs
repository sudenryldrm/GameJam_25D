using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

// Reassemble lossless source parts before Unity imports the supplied character.
// This avoids the connected upload service's per-request size limit.
[InitializeOnLoad]
public static class DwarfSourceImporter
{
    [Serializable] private sealed class SourceManifest { public SourceFile[] files; }
    [Serializable] private sealed class SourceFile { public string name; public string sha256; public string[] parts; }

    static DwarfSourceImporter() { EditorApplication.delayCall += RestoreSources; }

    [MenuItem("Tools/Dwarfs/Restore Uploaded Model Sources")]
    private static void RestoreSources()
    {
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../ModelSources/Dwarfs"));
        string manifestPath = Path.Combine(folder, "manifest.json");
        if (!File.Exists(manifestPath)) return;
        try
        {
            SourceManifest manifest = JsonUtility.FromJson<SourceManifest>(File.ReadAllText(manifestPath));
            string destination = Path.Combine(Application.dataPath, "Resources/Dwarfs");
            Directory.CreateDirectory(destination);
            bool changed = false;
            foreach (SourceFile file in manifest.files)
            {
                string target = Path.Combine(destination, file.name);
                if (File.Exists(target) && Hash(File.ReadAllBytes(target)) == file.sha256) continue;
                StringBuilder encoded = new StringBuilder();
                foreach (string part in file.parts) encoded.Append(File.ReadAllText(Path.Combine(folder, part)));
                byte[] bytes = Convert.FromBase64String(encoded.ToString());
                if (Hash(bytes) != file.sha256) throw new InvalidDataException("Source checksum mismatch: " + file.name);
                File.WriteAllBytes(target, bytes);
                changed = true;
            }
            if (changed) AssetDatabase.Refresh();
        }
        catch (Exception error) { Debug.LogError("Could not restore dwarf source files: " + error.Message); }
    }

    private static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }
}
