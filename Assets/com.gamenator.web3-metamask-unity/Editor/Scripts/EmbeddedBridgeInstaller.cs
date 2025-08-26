using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Gamenator.Web3.MetaMaskUnity.Editor
{
    /// <summary>
    /// Installs the embedded MetaMask JS bridge files into the consuming Unity project.
    /// - Reads a ZIP payload containing one or more *.gz.base64 entries
    /// - For each entry: decodes base64, gunzips, and writes to StreamingAssets/MetaMask
    /// </summary>
    public static class EmbeddedBridgeInstaller
    {
        private const string OutputFolderRelative = "Assets/StreamingAssets/MetaMask";
        private const string PayloadZipFile = "bridge-payload.zip";

        /// <summary>
        /// Installs the embedded bridge payload into <c>Assets/StreamingAssets/MetaMask</c>.
        /// </summary>
        /// <remarks>
        /// The payload is expected at <c>Assets/com.gamenator.web3-metamask-unity/Editor/Embedded/bridge-payload.zip</c>.
        /// Each <c>*.gz.base64</c> entry is decoded and gunzipped before being written to the destination.
        /// </remarks>
        [MenuItem("Tools/Web3/MetaMask/Install Embedded Bridge", priority = 20)]
        public static void InstallEmbeddedBridge()
        {
            try
            {
                string editorDir = Path.GetDirectoryName(GetThisFilePath()) ?? Application.dataPath;
                string embeddedDir = Path.Combine(Directory.GetParent(editorDir)!.FullName, "Embedded");
                string zipPath = Path.Combine(embeddedDir, PayloadZipFile);
                if (!File.Exists(zipPath))
                {
                    Debug.LogError($"Bridge payload not found: {zipPath}");
                    return;
                }

                string destDir = GetAbsolutePathFromAssetsRelative(OutputFolderRelative);
                Directory.CreateDirectory(destDir);

                using var zip = ZipFile.OpenRead(zipPath);
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue; // skip folders
                    var targetName = entry.Name;
                    bool isGzipBase64 = targetName.EndsWith(".gz.base64", StringComparison.OrdinalIgnoreCase);
                    string outName = isGzipBase64 ? targetName.Substring(0, targetName.Length - ".gz.base64".Length) : targetName;

                    using var entryStream = entry.Open();
                    using var ms = new MemoryStream();
                    entryStream.CopyTo(ms);
                    ms.Position = 0;

                    string outputPath = Path.Combine(destDir, outName);

                    if (isGzipBase64)
                    {
                        // decode base64 -> gunzip -> write
                        string base64 = Encoding.UTF8.GetString(ms.ToArray());
                        byte[] gzBytes = Convert.FromBase64String(base64.Replace("\n", string.Empty).Replace("\r", string.Empty));
                        using var gzipMs = new MemoryStream(gzBytes);
                        using var gzip = new GZipStream(gzipMs, CompressionMode.Decompress);
                        using var outMs = new MemoryStream();
                        gzip.CopyTo(outMs);
                        File.WriteAllBytes(outputPath, outMs.ToArray());
                    }
                    else
                    {
                        // raw file in zip
                        File.WriteAllBytes(outputPath, ms.ToArray());
                    }
                }

                AssetDatabase.Refresh();
                Debug.Log($"Installed MetaMask bridge files to: {destDir}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to install MetaMask embedded bridge: {ex}");
            }
        }

        /// <summary>
        /// Computes and logs SHA256 hashes of the decoded payload entries for verification.
        /// </summary>
        [MenuItem("Tools/Web3/MetaMask/Show Bridge SHA256", priority = 21)]
        public static void ShowBridgeSha256()
        {
            try
            {
                string editorDir = Path.GetDirectoryName(GetThisFilePath()) ?? Application.dataPath;
                string embeddedDir = Path.Combine(Directory.GetParent(editorDir)!.FullName, "Embedded");
                string zipPath = Path.Combine(embeddedDir, PayloadZipFile);
                if (!File.Exists(zipPath))
                {
                    Debug.LogError($"Bridge payload not found: {zipPath}");
                    return;
                }

                using var zip = ZipFile.OpenRead(zipPath);
                using var sha = SHA256.Create();
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    using var entryStream = entry.Open();
                    using var ms = new MemoryStream();
                    entryStream.CopyTo(ms);
                    ms.Position = 0;
                    byte[] data;
                    if (entry.Name.EndsWith(".gz.base64", StringComparison.OrdinalIgnoreCase))
                    {
                        string base64 = Encoding.UTF8.GetString(ms.ToArray());
                        byte[] gzBytes = Convert.FromBase64String(base64.Replace("\n", string.Empty).Replace("\r", string.Empty));
                        using var gzipMs = new MemoryStream(gzBytes);
                        using var gzip = new GZipStream(gzipMs, CompressionMode.Decompress);
                        using var outMs = new MemoryStream();
                        gzip.CopyTo(outMs);
                        data = outMs.ToArray();
                    }
                    else
                    {
                        data = ms.ToArray();
                    }

                    byte[] hash = sha.ComputeHash(data);
                    var sb = new StringBuilder(hash.Length * 2);
                    foreach (byte b in hash) sb.Append(b.ToString("x2"));
                    Debug.Log($"{entry.Name} → SHA256: {sb}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to compute MetaMask bridge SHA256: {ex}");
            }
        }

        /// <summary>
        /// Resolves the absolute path to this editor script file.
        /// </summary>
        /// <returns>Full path to <c>EmbeddedBridgeInstaller.cs</c>.</returns>
        private static string GetThisFilePath()
        {
            string[] guids = AssetDatabase.FindAssets("EmbeddedBridgeInstaller t:Script");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("EmbeddedBridgeInstaller.cs"))
                {
                    return Path.GetFullPath(path);
                }
            }
            return Path.Combine(Application.dataPath, "com.gamenator.web3-metamask-unity", "Editor", "Scripts", "EmbeddedBridgeInstaller.cs");
        }

        /// <summary>
        /// Converts an <c>Assets/</c>-relative path to an absolute project filesystem path.
        /// </summary>
        /// <param name="assetsRelativePath">Relative path beginning with <c>Assets/</c>.</param>
        /// <returns>Absolute filesystem path.</returns>
        private static string GetAbsolutePathFromAssetsRelative(string assetsRelativePath)
        {
            // Convert Assets/.. relative path to absolute project path
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, assetsRelativePath);
        }
    }
}


