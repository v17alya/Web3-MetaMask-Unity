using System;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Gamenator.Web3.MetaMaskUnity.Runtime.WebGL
{
    /// <summary>
    /// Thin C# wrapper over the WebGL .jslib interop for calling window.MetaMaskBridge API.
    /// Non-MonoBehaviour by design.
    /// </summary>
    public static class Web3MetaMaskJsBridge
    {
        [Serializable]
        public struct ConnectionState
        {
            public bool connected;
            public string address;

            public override string ToString()
            {
                return $"ConnectionState: connected={connected}, address={address}";
            }
        }

        // --- Native bindings (available in WebGL builds) ---
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int W3MM_Init(string optionsJson);
        [DllImport("__Internal")] private static extern void W3MM_Connect();
        [DllImport("__Internal")] private static extern void W3MM_Disconnect();
        [DllImport("__Internal")] private static extern void W3MM_SignMessage(string message);
        [DllImport("__Internal")] private static extern void W3MM_SetUnityInstance(string gameObjectName);
        [DllImport("__Internal")] private static extern void W3MM_ConnectAndSign(string message);
        [DllImport("__Internal")] private static extern void W3MM_ConnectWith(string method, string paramsJson);
        [DllImport("__Internal")] private static extern void W3MM_Request(string method, string paramsJson);
        [DllImport("__Internal")] private static extern void W3MM_SetDebug(int enabled);
        [DllImport("__Internal")] private static extern int W3MM_SetUnityGameObjectName(string gameObjectName);
        [DllImport("__Internal")] private static extern int W3MM_IsConnected();
        [DllImport("__Internal")] private static extern IntPtr W3MM_GetConnectionState();
        [DllImport("__Internal")] private static extern void W3MM_GetConnectionDetails();
#endif

        /// <summary>
        /// Initialize the JS bridge by forwarding options JSON to window.MetaMaskBridge.init.
        /// Returns true if initialized, false otherwise.
        /// </summary>
        /// <param name="optionsJson">JSON serialized options object for MetaMask initialization.</param>
        public static bool Init(string optionsJson)
        {
            if (string.IsNullOrWhiteSpace(optionsJson))
            {
                throw new ArgumentException("optionsJson cannot be null or empty", nameof(optionsJson));
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            return W3MM_Init(optionsJson) == 1;
#else
            return true; // No-op in Editor/Non-WebGL
#endif
        }

        /// <summary>
        /// Connect wallet via MetaMask SDK.
        /// </summary>
        public static void Connect()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            W3MM_Connect();
#else
            // No-op in Editor/Non-WebGL
#endif
        }

        /// <summary>
        /// Disconnect (clears local SDK state).
        /// </summary>
        public static void Disconnect()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            W3MM_Disconnect();
#else
            // No-op in Editor/Non-WebGL
#endif
        }

        /// <summary>
        /// Request a message signature; result is forwarded via JS to Unity callback methods.
        /// </summary>
        /// <param name="message">Message to be signed by the connected wallet.</param>
        public static void SignMessage(string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            W3MM_SignMessage(message ?? string.Empty);
#else
            // No-op in Editor/Non-WebGL
#endif
        }

        /// <summary>
        /// Connect and sign a message in one call (if supported by the SDK).
        /// Result is delivered via Unity callbacks.
        /// </summary>
        /// <param name="message">Message to sign after connection.</param>
        public static void ConnectAndSign(string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            W3MM_ConnectAndSign(message ?? string.Empty);
#else
            // No-op in Editor/Non-WebGL
#endif
        }

        /// <summary>
        /// Connect and perform an RPC method in one call.
        /// </summary>
        /// <param name="method">RPC method name.</param>
        /// <param name="paramsJson">JSON array of params.</param>
        public static void ConnectWith(string method, string paramsJson = null)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                throw new ArgumentException("method cannot be null or empty", nameof(method));
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            W3MM_ConnectWith(method, paramsJson ?? string.Empty);
#else
            // No-op in Editor/Non-WebGL
#endif
        }

        /// <summary>
        /// Make an arbitrary JSON-RPC request. Result is handled via JS and forwarded by callbacks if applicable.
        /// </summary>
        /// <param name="method">JSON-RPC method name (e.g., 'eth_chainId').</param>
        /// <param name="paramsJson">JSON stringified params array or object.</param>
        public static void Request(string method, string paramsJson = null)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                throw new ArgumentException("method cannot be null or empty", nameof(method));
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            W3MM_Request(method, paramsJson ?? string.Empty);
#else
            // No-op in Editor/Non-WebGL
#endif
        }

        /// <summary>
        /// Enable or disable verbose JS logging.
        /// </summary>
        public static void SetDebug(bool enabled)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            W3MM_SetDebug(enabled ? 1 : 0);
#else
            // No-op in Editor/Non-WebGL
#endif
        }

        /// <summary>
        /// Set the Unity GameObject name that should receive JS callbacks.
        /// </summary>
        /// <param name="gameObjectName">Target GameObject name.</param>
        public static void SetUnityGameObjectName(string gameObjectName)
        {
            if (string.IsNullOrWhiteSpace(gameObjectName))
            {
                throw new ArgumentException("gameObjectName cannot be null or empty", nameof(gameObjectName));
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            var ok = W3MM_SetUnityGameObjectName(gameObjectName);
            if (ok != 1) { /* optional: handle failure silently */ }
#else
            // No-op in Editor/Non-WebGL
#endif
        }

        /// <summary>
        /// Returns whether a wallet address is currently cached as connected.
        /// </summary>
        public static bool IsConnected()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { return W3MM_IsConnected() == 1; } catch { return false; }
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns cached connection state with minimal fields.
        /// </summary>
        public static ConnectionState GetConnectionState()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                var ptr = W3MM_GetConnectionState();
                var json = Marshal.PtrToStringAnsi(ptr);
                if (string.IsNullOrEmpty(json)) return null;
                return JsonUtility.FromJson<ConnectionState>(json);
            }
            catch { return null; }
#else
            return default;
#endif
        }

        /// <summary>
        /// Asynchronously queries provider for details and sends result to Unity callback OnConnectionDetails(string json).
        /// </summary>
        public static void GetConnectionDetails()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { W3MM_GetConnectionDetails(); } catch { }
#else
            // No-op in Editor/Non-WebGL
#endif
        }

        /// <summary>
        /// Provide the active Unity GameObject name so the JS bridge can SendMessage back.
        /// </summary>
        /// <param name="gameObjectName">The GameObject name that should receive JS callbacks.</param>
        public static void SetUnityInstance(string gameObjectName)
        {
            if (string.IsNullOrWhiteSpace(gameObjectName))
            {
                throw new ArgumentException("gameObjectName cannot be null or empty", nameof(gameObjectName));
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            W3MM_SetUnityInstance(gameObjectName);
#else
            // No-op in Editor/Non-WebGL
#endif
        }
    }
}


