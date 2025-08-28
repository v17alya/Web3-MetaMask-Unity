using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gamenator.Web3.MetaMaskUnity.Editor
{
#if UNITY_EDITOR
    /// <summary>
    /// Exports the Minimal Sample into a .unitypackage for distribution via Samples~/Minimal.
    /// Includes the WebGL template and the Minimal sample folder.
    /// </summary>
    public static class SampleExporter
    {
        private const string WebGlTemplatePath = "Assets/WebGLTemplates/Web3MetaMaskSampleMinimal";
        private const string SampleMinimalPath = "Assets/Web3MetaMask/Samples/Minimal";
        private const string DefaultExportPath = "Assets/com.gamenator.web3-metamask-unity/Samples~/Minimal/Web3MetaMaskMinimalSample.unitypackage";

        [MenuItem("Tools/Web3/MetaMask/Export Minimal Sample UnityPackage", priority = 120)]
        public static void ExportMinimalSample()
        {
            string exportPath = EditorUtility.SaveFilePanelInProject(
                title: "Export Minimal Sample",
                defaultName: Path.GetFileName(DefaultExportPath),
                extension: "unitypackage",
                message: "Exports Web3 MetaMask Minimal sample");

            if (string.IsNullOrEmpty(exportPath))
            {
                Debug.Log("Minimal Sample export cancelled.");
                return;
            }

            DoExport(exportPath);
        }

        /// <summary>
        /// CLI entry point for batchmode export.
        /// </summary>
        public static void ExportMinimalSampleCli()
        {
            string parentDir = Path.GetDirectoryName(DefaultExportPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            DoExport(DefaultExportPath);
            EditorApplication.Exit(0);
        }

        private static void DoExport(string exportPath)
        {
            if (!AssetDatabase.IsValidFolder(WebGlTemplatePath))
            {
                Debug.LogError($"Missing folder: {WebGlTemplatePath}");
                return;
            }
            if (!AssetDatabase.IsValidFolder(SampleMinimalPath))
            {
                Debug.LogError($"Missing folder: {SampleMinimalPath}");
                return;
            }

            string[] assetPaths = new[]
            {
                WebGlTemplatePath,
                SampleMinimalPath
            };

            AssetDatabase.ExportPackage(assetPaths, exportPath, ExportPackageOptions.Recurse);
            AssetDatabase.Refresh();
            Debug.Log($"Exported Minimal Sample to: {exportPath}");
        }
    }
#endif
}


