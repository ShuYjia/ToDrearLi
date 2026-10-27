#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class CodexTempLegacyMaterialRepair
{
    private const string SessionKey = "Codex.LegacyMaterialRepair.20260723.2";
    private const string ScenePath = "Assets/A_MRNet/Sence/03SingleplayerScene.unity";
    private static readonly string[] Targets = { "changjing-2", "changjing-4", "ChangJing-8" };
    private static readonly Dictionary<string, Renderer[]> SourceRenderers =
        new Dictionary<string, Renderer[]>(StringComparer.Ordinal);

    static CodexTempLegacyMaterialRepair()
    {
        EditorApplication.delayCall += Repair;
    }

    private static void Repair()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Repair;
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath) return;

        var result = new List<string>();
        int totalRecovered = 0;
        foreach (string target in Targets)
        {
            int recovered = 0;
            int unresolved = 0;
            foreach (Renderer renderer in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
            {
                if (renderer.gameObject.scene != scene || !HasAncestor(renderer.transform, target)) continue;
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    if (materials[slot] != null) continue;
                    List<Material> candidates = FindCandidates(GetMesh(renderer), slot);
                    if (candidates.Count == 1)
                    {
                        materials[slot] = candidates[0];
                        changed = true;
                        recovered++;
                    }
                    else
                    {
                        unresolved++;
                        result.Add($"UNRESOLVED_BEFORE\t{target}\t{renderer.GetType().Name}\t{PathOf(renderer.transform)}\t{slot}");
                    }
                }

                if (changed)
                {
                    Undo.RecordObject(renderer, "Restore legacy model materials");
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                }
            }
            totalRecovered += recovered;
            result.Add($"REPAIR\t{target}\trecovered={recovered}\tunresolved={unresolved}");
        }

        SessionState.SetBool(SessionKey, true);
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        result.Add($"SAVE\trecovered={totalRecovered}\tsaved={saved}");
        string interim = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "CodexLegacyMaterialRepairInterim.txt");
        File.WriteAllLines(interim, result);
        EditorApplication.delayCall += ReloadAndValidate;
    }

    private static void ReloadAndValidate()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var result = new List<string>();
        foreach (string target in Targets)
        {
            int renderers = 0;
            int slots = 0;
            int nullMaterials = 0;
            int invalidShaders = 0;
            foreach (Renderer renderer in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
            {
                if (renderer.gameObject.scene != scene || !HasAncestor(renderer.transform, target)) continue;
                renderers++;
                foreach (Material material in renderer.sharedMaterials)
                {
                    slots++;
                    if (material == null)
                    {
                        nullMaterials++;
                        result.Add($"NULL_AFTER_RELOAD\t{target}\t{renderer.GetType().Name}\t{PathOf(renderer.transform)}");
                    }
                    else if (material.shader == null || !material.shader.isSupported || material.shader.name == "Hidden/InternalErrorShader")
                    {
                        invalidShaders++;
                        result.Add($"BAD_SHADER_AFTER_RELOAD\t{target}\t{PathOf(renderer.transform)}\t{material.name}");
                    }
                }
            }
            result.Add($"VALIDATE\t{target}\trenderers={renderers}\tslots={slots}\tnull={nullMaterials}\tbadShader={invalidShaders}");
        }

        string output = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "CodexLegacyMaterialRepairValidation.txt");
        File.WriteAllLines(output, result);
        Debug.Log($"[CodexLegacyMaterialRepair] Validation written to {output}");
    }

    private static List<Material> FindCandidates(Mesh mesh, int slot)
    {
        var candidates = new List<Material>();
        if (mesh == null) return candidates;
        string modelPath = AssetDatabase.GetAssetPath(mesh);
        if (string.IsNullOrEmpty(modelPath)) return candidates;

        Renderer[] renderers;
        if (!SourceRenderers.TryGetValue(modelPath, out renderers))
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            renderers = source != null ? source.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            SourceRenderers.Add(modelPath, renderers);
        }
        foreach (Renderer renderer in renderers)
        {
            if (GetMesh(renderer) != mesh) continue;
            Material[] materials = renderer.sharedMaterials;
            if (slot >= materials.Length || materials[slot] == null) continue;
            if (candidates.All(candidate => candidate != materials[slot])) candidates.Add(materials[slot]);
        }
        return candidates;
    }

    private static Mesh GetMesh(Renderer renderer)
    {
        SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
        if (skinned != null) return skinned.sharedMesh;
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        return filter != null ? filter.sharedMesh : null;
    }

    private static bool HasAncestor(Transform transform, string name)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current.name == name) return true;
        }
        return false;
    }

    private static string PathOf(Transform transform)
    {
        string path = transform.name;
        for (Transform parent = transform.parent; parent != null; parent = parent.parent)
        {
            path = parent.name + "/" + path;
        }
        return path;
    }
}
#endif
