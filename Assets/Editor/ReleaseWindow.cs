using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gamenator.Web3.MetaMaskUnity.Editor
{
#if UNITY_EDITOR
    /// <summary>
    /// Editor window to run the full release flow from inside Unity.
    /// </summary>
    public class ReleaseWindow : EditorWindow
    {
        private string _version = "0.0.0";
        private string _featureBranch = "feature/x.y.z";
        private string _unityPathOverride = string.Empty;
        private string _nodePathOverride = string.Empty;
        private string _npmPathOverride = string.Empty;
        private bool _skipUnityExport = false;
        private bool _skipBridgeBuild = false;
        private bool _closeUnityBeforeExport = true;

        [MenuItem("Tools/Web3/MetaMask/Release...", priority = 200)]
        public static void Open()
        {
            var wnd = GetWindow<ReleaseWindow>(utility: false, title: "Web3 MetaMask Release", focus: true);
            wnd.minSize = new Vector2(480, 280);
            wnd.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Release Parameters", EditorStyles.boldLabel);
            _version = EditorGUILayout.TextField("Version (tag)", _version);
            _featureBranch = EditorGUILayout.TextField("Feature Branch", _featureBranch);
            _unityPathOverride = EditorGUILayout.TextField("Unity Path (optional)", _unityPathOverride);
            _nodePathOverride = EditorGUILayout.TextField("Node Path (optional)", _nodePathOverride);
            _npmPathOverride = EditorGUILayout.TextField("NPM Path (optional)", _npmPathOverride);
            _skipUnityExport = EditorGUILayout.ToggleLeft("Skip Unity Export (only bump/build/copy)", _skipUnityExport);
            _skipBridgeBuild = EditorGUILayout.ToggleLeft("Skip Bridge Build (use existing dist)", _skipBridgeBuild);
            _closeUnityBeforeExport = EditorGUILayout.ToggleLeft("Close Unity before export (recommended)", _closeUnityBeforeExport);

            GUILayout.Space(8);
            if (GUILayout.Button("Run Release"))
            {
                RunRelease(_version, _featureBranch, _unityPathOverride, _nodePathOverride, _npmPathOverride, _skipUnityExport, _skipBridgeBuild, _closeUnityBeforeExport);
            }

            GUILayout.Space(6);
            EditorGUILayout.HelpBox("This will bump version in package.json, build the JS bridge, copy artifacts to the project, optionally export the Minimal Sample, then squash-merge the feature branch into main, create the tag and push.\n\nNote: If Unity export fails due to another Unity instance, check 'Close Unity before export' and run again.", MessageType.Info);
        }

        private static void RunRelease(string version, string branch, string unityPathOverride, string nodePathOverride, string npmPathOverride, bool skipUnity, bool skipBridge, bool closeUnityBeforeExport)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                UnityEngine.Debug.LogError("Version is required");
                return;
            }
            if (string.IsNullOrWhiteSpace(branch))
            {
                UnityEngine.Debug.LogError("Feature branch is required");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string scriptPath = Path.Combine(projectRoot, "scripts", "release.js");
            if (!File.Exists(scriptPath))
            {
                UnityEngine.Debug.LogError($"Release script not found: {scriptPath}");
                return;
            }

            // Resolve node path
            string nodePath = ResolveNodePath(nodePathOverride);
            if (string.IsNullOrWhiteSpace(nodePath) || !File.Exists(nodePath))
            {
                UnityEngine.Debug.LogError("Unable to find Node.js binary. Specify 'Node Path' explicitly in the window.");
                return;
            }
            
            UnityEngine.Debug.Log($"Using Node.js at: {nodePath}");

            // Resolve npm path
            string resolvedNpmPath = npmPathOverride;
            if (string.IsNullOrWhiteSpace(resolvedNpmPath))
            {
                resolvedNpmPath = ResolveNpmPath(string.Empty);
            }
            
            if (string.IsNullOrWhiteSpace(resolvedNpmPath) && !skipBridge)
            {
                UnityEngine.Debug.LogWarning("npm not found. Bridge build may fail. Consider specifying 'NPM Path' explicitly.");
            }
            else if (!string.IsNullOrWhiteSpace(resolvedNpmPath) && string.IsNullOrWhiteSpace(npmPathOverride))
            {
                UnityEngine.Debug.Log($"Auto-detected npm at: {resolvedNpmPath}");
            }

            // Build args
            string args = $"\"{scriptPath}\" --version {version} --branch {branch} --node \"{nodePath}\"";
            if (skipUnity)
            {
                args += " --skip-unity";
            }
            if (skipBridge)
            {
                args += " --skip-bridge";
            }
            if (!string.IsNullOrWhiteSpace(unityPathOverride))
            {
                args += $" --unity \"{unityPathOverride}\"";
            }
            // Use resolved npm path
            if (!string.IsNullOrWhiteSpace(resolvedNpmPath))
            {
                args += $" --npm \"{resolvedNpmPath}\"";
            }
            
            // Add close-unity flag if requested
            if (closeUnityBeforeExport)
            {
                args += " --close-unity";
            }

            // Prefer npm exec node to respect workspace Node
            var psi = new ProcessStartInfo
            {
                FileName = nodePath,
                Arguments = args,
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            try
            {
                var p = Process.Start(psi);
                if (p == null)
                {
                    UnityEngine.Debug.LogError("Failed to start release process.");
                    return;
                }

                p.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.Log($"[release.js] {e.Data}"); };
                p.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.LogError($"[release.js] {e.Data}"); };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();

                if (p.ExitCode == 0)
                {
                    UnityEngine.Debug.Log("Release flow completed successfully.");
                }
                else
                {
                    UnityEngine.Debug.LogError($"Release flow failed with exit code {p.ExitCode}.");
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Release flow failed: {ex}");
            }
        }

        private static string ResolveNodePath(string overridePath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
                {
                    return overridePath;
                }

                var envNode = Environment.GetEnvironmentVariable("NODE_PATH");
                if (!string.IsNullOrWhiteSpace(envNode) && File.Exists(envNode))
                {
                    return envNode;
                }

                // Common macOS locations
                string[] candidates = new[]
                {
                    "/opt/homebrew/bin/node",
                    "/usr/local/bin/node",
                    "/usr/bin/node"
                };
                foreach (var p in candidates)
                {
                    if (File.Exists(p)) return p;
                }
            }
            catch { }
            return string.Empty;
        }

        private static string ResolveNpmPath(string overridePath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
                {
                    return overridePath;
                }

                var envNpm = Environment.GetEnvironmentVariable("NPM_PATH");
                if (!string.IsNullOrWhiteSpace(envNpm) && File.Exists(envNpm))
                {
                    return envNpm;
                }

                // Common macOS locations
                string[] candidates = new[]
                {
                    "/opt/homebrew/bin/npm",
                    "/usr/local/bin/npm",
                    "/usr/bin/npm"
                };
                foreach (var p in candidates)
                {
                    if (File.Exists(p)) return p;
                }

                // Try to find npm relative to node
                try
                {
                    var nodePath = ResolveNodePath(string.Empty);
                    if (!string.IsNullOrWhiteSpace(nodePath))
                    {
                        var npmPath = Path.Combine(Path.GetDirectoryName(nodePath)!, "npm");
                        if (File.Exists(npmPath))
                        {
                            return npmPath;
                        }
                    }
                }
                catch { }
            }
            catch { }
            return string.Empty;
        }
    }
#endif
}


