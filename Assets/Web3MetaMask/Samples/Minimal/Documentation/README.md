# Minimal Sample — Web3-MetaMask Unity

This sample demonstrates a minimal end-to-end setup of the Web3-MetaMask bridge in a Unity WebGL project.

## What’s included
- A Unity package in this sample’s root containing:
  - A WebGL template (`Web3MetaMaskSample`) with `index.html` that initializes `MetaMaskBridge`, wires the Unity instance after load, and includes a prebuilt `web3-metamask-bridge.js` for immediate testing
  - A ready sample scene with a `Web3BridgeSample` MonoBehaviour (buttons to Initialize, Connect, Disconnect, Sign) and Unity-side logging of callbacks

## Setup
1) Import the sample (scene + template + scripts)
- Recommended: Package Manager → MetaMask Unity → Samples → Import "Minimal"
- Alternative: Double-click the Unity package in this sample's root to import it into your project

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
      // Unity wiring (optional): callback GameObject name and/or Unity instance
      unity: { gameObjectName: 'Web3Bridge', instance: window.unityInstance },
      debug: false,
      // Optional JS-side events
      events: {
        connected: ({ address, accounts }) => console.log('connected', address, accounts),
        disconnected: () => console.log('disconnected'),
        chainChanged: (cid) => console.log('chainChanged', cid),
        signed: ({ signature, address }) => console.log('signed', signature, address),
        requested: (result) => console.log('requested', result),
        // Connection details
        connectionDetails: (res) => console.log('connectionDetails', res),
        connectionDetailsError: (m) => console.warn('connectionDetailsError', m)
      },
      // REQUIRED: SDK options object containing dappMetadata and infuraAPIKey
      sdkOptions: {
        dappMetadata: { name: 'MetaMask Sample', url: window.location.href },
        infuraAPIKey: 'YOUR_INFURA_KEY'
      }
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
            sdkOptions = new {
                dappMetadata = new { name = "MetaMask Sample", url = Application.absoluteURL },
                infuraAPIKey = "YOUR_INFURA_KEY"
            },
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
- `OnConnectionDetails(string json)` / `OnConnectionDetailsError(string message)`

## Diagnostics (optional)

### From JavaScript
```html
<script>
  // Quick checks
  const connected = MetaMaskBridge.isConnected();
  const state = MetaMaskBridge.getConnectionState(); // { connected, address }
  MetaMaskBridge.getConnectionDetails().then(res => console.log(res));

  // Check connection status without prompting
  MetaMaskBridge.checkConnection().then(result => {
    if (result.success && result.result.connected) {
      console.log('User is connected:', result.result.address);
    } else {
      console.log('User is not connected');
    }
  });

  // Generate mobile deeplink
  const deeplink = MetaMaskBridge.generateMetaMaskDeepLink();
  console.log('MetaMask deeplink:', deeplink);

  // Subscribe/unsubscribe at runtime
  const onConnected = ({ address }) => console.log('connected later', address);
  MetaMaskBridge.on('connected', onConnected);
  // MetaMaskBridge.off('connected', onConnected);
</script>
```

### From C# (WebGL)
```csharp
// Quick checks
bool isConnected = Web3MetaMaskJsBridge.IsConnected();
var state = Web3MetaMaskJsBridge.GetConnectionState(); // { connected, address }
Web3MetaMaskJsBridge.GetConnectionDetails(); // emits OnConnectionDetails / OnConnectionDetailsError
```

## Notes
- The template's `index.html` calls `MetaMaskBridge.init({ sdkOptions: { dappMetadata, infuraAPIKey }, ... })` and, after Unity loads, passes the `unityInstance` via `MetaMaskBridge.setUnityInstance(unityInstance)`.
- This sample is intended to verify the integration using the prebuilt `web3-metamask-bridge.js` included with the template — no separate Vite build is required here.
- Ensure the bridge JS is loaded before any code that calls `MetaMaskBridge`.
- The provider object is not returned (non‑serializable). Use `MetaMaskBridge.request({ method, params })` for RPC and JS events via `MetaMaskBridge.on(...)`.
- The `init` method now returns a result object with `success`, `result`, and optional `error` properties.
- Required options are now nested under `sdkOptions.dappMetadata` and `sdkOptions.infuraAPIKey`.
