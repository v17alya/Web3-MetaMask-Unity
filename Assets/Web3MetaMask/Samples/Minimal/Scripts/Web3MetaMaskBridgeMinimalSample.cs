using UnityEngine;
using Gamenator.Web3.MetaMaskUnity.Runtime.WebGL;

namespace Gamenator.Web3.MetaMaskUnity.Samples
{
    /// <summary>
    /// Example MonoBehaviour wrapper for window.MetaMaskBridge via Web3MetaMaskJsBridge.
    /// Add this to a GameObject and wire up calls from your UI.
    /// </summary>
    public class Web3MetaMaskBridgeMinimalSample : MonoBehaviour
    {
        [System.Serializable]
        private class DappMetadata { public string name; public string url; }

        [System.Serializable]
        public class UnityOptions { public string gameObjectName; }

        [System.Serializable]
        private class InitOptions { public DappMetadata dappMetadata; public string infuraAPIKey; public bool debug; public UnityOptions unity; }

        [SerializeField] private string _dappName = "MetaMask Sample";
        [SerializeField] private string _dappUrl = "http://localhost";
        [SerializeField] private string _infuraApiKey = "";
        [SerializeField] private string _messageToSign = "Hello from Unity";
        [SerializeField] private string _requestMethod = "eth_chainId";
        [SerializeField][TextArea(2, 6)] private string _requestParamsJson = "[]";
        [SerializeField] private string _connectWithMethod = "eth_getBalance";
        [SerializeField][TextArea(2, 6)] private string _connectWithParamsJson = "[]";
        [SerializeField] private bool _showConnectionStateButtons = true;
        private string _lastLog = string.Empty;

        #region Public API

        /// <summary>Initialize MetaMaskBridge (must be called before connect/sign).</summary>
        public bool Initialize(string initOptionsJson)
        {
            if (string.IsNullOrWhiteSpace(initOptionsJson))
            {
                Debug.LogWarning("[Web3Bridge] initOptionsJson is empty");
                _lastLog = "Init failed";
                return false;
            }
            bool ok = Web3MetaMaskJsBridge.Init(initOptionsJson);
            if (!ok) Debug.LogError("[Web3Bridge] Init failed");
            _lastLog = ok ? "Init OK" : "Init failed";
            return ok;
        }

        /// <summary>Connect using MetaMask.</summary>
        public void Connect()
        {
            Web3MetaMaskJsBridge.Connect();
        }

        /// <summary>Disconnect wallet (clears local SDK state).</summary>
        public void Disconnect()
        {
            Web3MetaMaskJsBridge.Disconnect();
        }

        /// <summary>Request a message signature. Result will arrive in OnSigned(string signature).</summary>
        public void SignMessage(string message)
        {
            Web3MetaMaskJsBridge.SignMessage(message);
        }

        /// <summary>Make a JSON-RPC request. Result in OnRequested(string json) or OnRequestedError(string error).</summary>
        public void Request(string method, string paramsJson = null)
        {
            if (string.IsNullOrWhiteSpace(method)) { Debug.LogError("[Web3Bridge] Request: method is empty"); return; }
            Web3MetaMaskJsBridge.Request(method, paramsJson ?? string.Empty);
        }

        /// <summary>Connect and perform RPC. Result in OnConnectedWith(string json) or OnConnectedWithError(string error).</summary>
        public void ConnectWith(string method, string paramsJson = null)
        {
            if (string.IsNullOrWhiteSpace(method)) { Debug.LogError("[Web3Bridge] ConnectWith: method is empty"); return; }
            Web3MetaMaskJsBridge.ConnectWith(method, paramsJson ?? string.Empty);
        }

        /// <summary>Connect and sign a message. Result in OnSigned(string signature) or OnSignError(string error).</summary>
        public void ConnectAndSign(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) { Debug.LogError("[Web3Bridge] ConnectAndSign: message is empty"); return; }
            Web3MetaMaskJsBridge.ConnectAndSign(message);
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            const int pad = 8; int w = 300, h = 24;
            int total = (h + pad) * 16 + pad; // buttons + inputs
            int x = (Screen.width - w) / 2;
            int y = (Screen.height - total) / 2;

            _dappName = GUI.TextField(new Rect(x, y, w, h), _dappName); y += h + pad;
            _dappUrl = GUI.TextField(new Rect(x, y, w, h), _dappUrl); y += h + pad;
            _infuraApiKey = GUI.TextField(new Rect(x, y, w, h), _infuraApiKey); y += h + pad;
            if (GUI.Button(new Rect(x, y, w, h), "Initialize")) { Initialize(BuildInitOptionsJson()); }
            y += h + pad;
            if (GUI.Button(new Rect(x, y, w, h), "Connect")) { Connect(); }
            y += h + pad;
            if (GUI.Button(new Rect(x, y, w, h), "Disconnect")) { Disconnect(); }
            y += h + pad;
            _messageToSign = GUI.TextField(new Rect(x, y, w, h), _messageToSign); y += h + pad;
            if (GUI.Button(new Rect(x, y, w, h), "Sign Message")) { SignMessage(_messageToSign); }
            y += h + pad;
            if (GUI.Button(new Rect(x, y, w, h), "Connect + Sign")) { ConnectAndSign(_messageToSign); }
            y += h + pad;

            _requestMethod = GUI.TextField(new Rect(x, y, w, h), _requestMethod); y += h + pad;
            _requestParamsJson = GUI.TextArea(new Rect(x, y, w, h * 2), _requestParamsJson ?? string.Empty); y += h * 2 + pad;
            if (GUI.Button(new Rect(x, y, w, h), "Request (JSON-RPC)")) { Request(_requestMethod, string.IsNullOrWhiteSpace(_requestParamsJson) ? null : _requestParamsJson); }
            y += h + pad;

            _connectWithMethod = GUI.TextField(new Rect(x, y, w, h), _connectWithMethod); y += h + pad;
            _connectWithParamsJson = GUI.TextArea(new Rect(x, y, w, h * 2), _connectWithParamsJson ?? string.Empty); y += h * 2 + pad;
            if (GUI.Button(new Rect(x, y, w, h), "ConnectWith (RPC)")) { ConnectWith(_connectWithMethod, string.IsNullOrWhiteSpace(_connectWithParamsJson) ? null : _connectWithParamsJson); }
            y += h + pad;

            if (_showConnectionStateButtons)
            {
                if (GUI.Button(new Rect(x, y, w, h), "IsConnected?"))
                {
                    var ok = Web3MetaMaskJsBridge.IsConnected();
                    _lastLog = $"IsConnected: {ok}";
                    Debug.Log(_lastLog);
                }
                y += h + pad;

                if (GUI.Button(new Rect(x, y, w, h), "GetConnectionState (sync)"))
                {
                    var json = Web3MetaMaskJsBridge.GetConnectionState();
                    _lastLog = $"State: {json}";
                    Debug.Log(_lastLog);
                }
                y += h + pad;

                if (GUI.Button(new Rect(x, y, w, h), "GetConnectionDetails (async)"))
                {
                    Web3MetaMaskJsBridge.GetConnectionDetails();
                }
                y += h + pad;
            }

            GUI.Label(new Rect(x - 270, y, 800, 200), _lastLog);
        }

        #endregion

        #region Helpers

        private string BuildInitOptionsJson()
        {
            var options = new InitOptions
            {
                dappMetadata = new DappMetadata { name = _dappName ?? string.Empty, url = _dappUrl ?? string.Empty },
                infuraAPIKey = _infuraApiKey ?? string.Empty,
                debug = true,
                unity = new UnityOptions { gameObjectName = gameObject.name }
            };
            return JsonUtility.ToJson(options);
        }

        #endregion

        #region Callbacks 

        private void OnConnected(string address)
        {
            _lastLog = $"Connected: {address}";
            Debug.Log(_lastLog);
        }

        private void OnDisconnected(string _)
        {
            _lastLog = "Disconnected";
            Debug.Log(_lastLog);
        }

        private void OnSigned(string signature)
        {
            _lastLog = $"Signed: {signature}";
            Debug.Log(_lastLog);
        }

        private void OnConnectError(string error)
        {
            _lastLog = $"ConnectError: {error}";
            Debug.LogError(_lastLog);
        }

        private void OnDisconnectError(string error)
        {
            _lastLog = $"DisconnectError: {error}";
            Debug.LogError(_lastLog);
        }

        private void OnSignError(string error)
        {
            _lastLog = $"SignError: {error}";
            Debug.LogError(_lastLog);
        }

        private void OnRequested(string json)
        {
            _lastLog = $"Requested: {json}";
            Debug.Log(_lastLog);
        }

        private void OnRequestedError(string error)
        {
            _lastLog = $"RequestedError: {error}";
            Debug.LogError(_lastLog);
        }

        private void OnConnectedWith(string json)
        {
            _lastLog = $"ConnectedWith: {json}";
            Debug.Log(_lastLog);
        }

        private void OnConnectedWithError(string error)
        {
            _lastLog = $"ConnectedWithError: {error}";
            Debug.LogError(_lastLog);
        }

        // New async details callback
        private void OnConnectionDetails(string json)
        {
            _lastLog = $"ConnectionDetails: {json}";
            Debug.Log(_lastLog);
        }

        private void OnConnectionDetailsError(string error)
        {
            _lastLog = $"ConnectionDetailsError: {error}";
            Debug.LogError(_lastLog);
        }

        private void OnClipboardFromHtml(string text)
        {
            GUIUtility.systemCopyBuffer = text ?? string.Empty;
            _lastLog = "Clipboard text received from HTML";
            Debug.Log(_lastLog);
        }

        #endregion
    }
}