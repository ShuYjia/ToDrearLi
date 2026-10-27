#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class CodexTempRemainingMaterialProbe
{
    static CodexTempRemainingMaterialProbe() { EditorApplication.delayCall += Run; }

    private static void Run()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Run;
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.path.EndsWith("03SingleplayerScene.unity", StringComparison.Ordinal)) return;

        var lines = new List<string>();
        foreach (Renderer renderer in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
        {
            string path = PathOf(renderer.transform);
            if (!path.EndsWith("/changjing-2/ChongDianZhuang/dianxian", StringComparison.Ordinal) &&
                !path.EndsWith("/ChangJing-8/AGV/XiangZi/XiangGai", StringComparison.Ordinal) &&
                !path.EndsWith("/ChangJing-8/AGV/AGV1", StringComparison.Ordinal)) continue;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            LineRenderer line = renderer as LineRenderer;
            lines.Add($"TARGET\t{path}");
            lines.Add($"STATE\ttype={renderer.GetType().Name}\tenabled={renderer.enabled}\tactive={renderer.gameObject.activeInHierarchy}\tmeshFilter={(filter != null)}\tmesh={(filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : "<null>")}\tbounds={renderer.bounds.size}");
            if (line != null) lines.Add($"LINE\tpositionCount={line.positionCount}\tstartWidth={line.startWidth}\tendWidth={line.endWidth}\tuseWorldSpace={line.useWorldSpace}");
            AppendMaterials(lines, "CURRENT", renderer.sharedMaterials);

            Renderer source = PrefabUtility.GetCorrespondingObjectFromSource(renderer);
            lines.Add($"SOURCE\t{(source != null ? AssetDatabase.GetAssetPath(source) + "|" + PathOf(source.transform) : "<null>")}");
            if (source != null) AppendMaterials(lines, "SOURCE_MAT", source.sharedMaterials);
            Renderer original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer);
            lines.Add($"ORIGINAL\t{(original != null ? AssetDatabase.GetAssetPath(original) + "|" + PathOf(original.transform) : "<null>")}");
            if (original != null) AppendMaterials(lines, "ORIGINAL_MAT", original.sharedMaterials);

            Transform parent = renderer.transform.parent;
            if (parent != null)
            {
                foreach (Renderer nearby in parent.GetComponentsInChildren<Renderer>(true))
                {
                    if (nearby == renderer) continue;
                    Material material = nearby.sharedMaterial;
                    if (material != null) lines.Add($"NEARBY\t{nearby.GetType().Name}\t{PathOf(nearby.transform)}\t{material.name}\t{AssetDatabase.GetAssetPath(material)}");
                }
            }
        }

        string output = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "CodexRemainingMaterialProbe.txt");
        File.WriteAllLines(output, lines);
        Debug.Log($"[CodexRemainingMaterialProbe] Wrote {output}");
    }

    private static void AppendMaterials(List<string> lines, string prefix, Material[] materials)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            lines.Add($"{prefix}\t{i}\t{(material != null ? material.name : "<null>")}\t{(material != null ? AssetDatabase.GetAssetPath(material) : string.Empty)}");
        }
    }

    private static string PathOf(Transform transform)
    {
        string path = transform.name;
        for (Transform parent = transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
        return path;
    }
}
#endif
