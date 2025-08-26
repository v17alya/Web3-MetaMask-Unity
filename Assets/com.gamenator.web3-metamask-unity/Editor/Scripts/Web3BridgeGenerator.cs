using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gamenator.Web3.MetaMaskUnity.Editor
{
#if UNITY_EDITOR
    /// <summary>
    /// Generates a MonoBehaviour script (Web3Bridge.cs) that wraps calls to the WebGL JS bridge.
    /// </summary>
    public static class Web3BridgeGenerator
    {
        private const string DefaultFileName = "Web3Bridge.cs";
        private const string ResourceTemplatePath = "Templates/Web3BridgeTemplate";

        [MenuItem("Tools/Web3/MetaMask/Generate Web3Bridge MonoBehaviour", priority = 40)]
        public static void GenerateMonoBehaviour()
        {
            string defaultName = Path.GetFileNameWithoutExtension(DefaultFileName);
            string path = EditorUtility.SaveFilePanelInProject(
                title: "Create Web3Bridge MonoBehaviour",
                defaultName: defaultName,
                extension: "cs",
                message: "This script will call the WebGL bridge and receive callbacks");

            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Web3Bridge generation cancelled.");
                return;
            }

            string content = null;
            TextAsset textAsset = Resources.Load<TextAsset>(ResourceTemplatePath);
            if (textAsset != null)
            {
                content = textAsset.text;
            }
            else
            {
                MonoScript monoScript = Resources.Load<MonoScript>(ResourceTemplatePath);
                if (monoScript != null)
                {
                    content = monoScript.text;
                }
            }

            if (string.IsNullOrEmpty(content))
            {
                Debug.LogError($"Web3Bridge template not found in Resources at path: {ResourceTemplatePath}");
                return;
            }
            File.WriteAllText(path, content);
            AssetDatabase.Refresh();
            Debug.Log($"Generated Web3Bridge MonoBehaviour at: {path}");
        }
    }
#endif
}


