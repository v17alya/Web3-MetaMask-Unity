# Minimal Sample — Web3-MetaMask Unity

This sample demonstrates a minimal end-to-end setup of the Web3-MetaMask bridge in a Unity WebGL project.

## What’s included
- A Unity package in this sample’s root containing:
  - A WebGL template (`Web3MetaMaskSample`) with `index.html` that initializes `MetaMaskBridge`, wires the Unity instance after load, and includes a prebuilt `web3-metamask-bridge.js` for immediate testing
  - A ready sample scene with a `Web3BridgeSample` MonoBehaviour (buttons to Initialize, Connect, Disconnect, Sign) and Unity-side logging of callbacks

## Setup
1) Import the sample (scene + template + scripts)
- Recommended: Package Manager → MetaMask Unity → Samples → Import "Minimal"
- Alternative: Double-click the Unity package in this sample’s root to import it into your project

2) Select the WebGL template in Project Settings
- Open: Edit → Project Settings → Player → Resolution and Presentation
- Set Template to: `PROJECT:Web3MetaMaskSample`

3) Initialize the bridge (choose one approach)
- JavaScript-driven (template `index.html`):
```html
<head>
  <!-- Bridge is already included by this sample template; if you move it, adjust the path -->
  <script>
    // Initialize before any MetaMaskBridge usage
    MetaMaskBridge.init({
      dappMetadata: { name: 'MetaMask Sample', url: window.location.href },
      infuraAPIKey: 'YOUR_INFURA_KEY',
      // Optional: pass callback GameObject name and/or Unity instance
      // unity: { gameObjectName: 'Web3Bridge', instance: window.unityInstance },
      debug: false
    });
  </script>
</head>
<body>
  <script>
    // After Unity loads, wire the instance (optional if passed above)
    createUnityInstance(canvas, config, progress).then(function(instance) {
      window.unityInstance = instance;
      MetaMaskBridge.setUnityInstance(instance);
    });
  </script>
</body>
```

- Unity-driven (C# via `.jslib` wrapper):
```csharp
using Gamenator.Web3.MetaMaskUnity.Runtime.WebGL;
using UnityEngine;

public class Example : MonoBehaviour
{
    void Start()
    {
        // Initialize from C# (call once before connect/sign/request)
        var optionsJson = JsonUtility.ToJson(new {
            dappMetadata = new { name = "MetaMask Sample", url = Application.absoluteURL },
            infuraAPIKey = "YOUR_INFURA_KEY",
            debug = true,
            unity = new { gameObjectName = gameObject.name }
        });
        bool ok = Web3MetaMaskJsBridge.Init(optionsJson);
        if (!ok) Debug.LogError("MetaMask init failed");

        // Optional: explicitly set callback target
        Web3MetaMaskJsBridge.SetUnityGameObjectName(gameObject.name);
    }

    public void OnConnectClicked() => Web3MetaMaskJsBridge.Connect();
    public void OnDisconnectClicked() => Web3MetaMaskJsBridge.Disconnect();
    public void OnSignClicked() => Web3MetaMaskJsBridge.SignMessage("Hello from Unity");
    public void OnRpcClicked() => Web3MetaMaskJsBridge.Request("eth_chainId", "[]");
}
```

Both approaches can be mixed. Ensure initialization happens once before other calls.

4) Build & Test
- File → Build Settings:
  - Add/select the imported sample scene
  - Switch Platform to WebGL
- Edit → Project Settings → Player → WebGL → Publishing Settings:
  - Compression Format: Disabled (no Gzip/Brotli)
- Build or Build And Run


## Callbacks
Implement these on the target GameObject (default `Web3BridgeSample`):
- `OnConnected(string address)` / `OnDisconnected(string empty)`
- `OnSigned(string signature)` / `OnSignError(string message)`
- `OnRequested(string json)` / `OnRequestedError(string message)`
- `OnConnectedWith(string json)` / `OnConnectedWithError(string message)`
- `OnConnectError(string message)` / `OnDisconnectError(string message)`
- `OnChainChanged(string chainId)`

## Notes
- The template’s `index.html` calls `MetaMaskBridge.init({ dappMetadata, infuraAPIKey, ... })` and, after Unity loads, passes the `unityInstance` via `MetaMaskBridge.setUnityInstance(unityInstance)`.
- This sample is intended to verify the integration using the prebuilt `web3-metamask-bridge.js` included with the template — no separate Vite build is required here.
- Ensure the bridge JS is loaded before any code that calls `MetaMaskBridge`.
