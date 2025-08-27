# MetaMask Unity (UPM Package)

Unity WebGL plugin that integrates MetaMask SDK and exposes a JavaScript bridge plus Editor tooling.

## Features
- Editor installer to unpack embedded bridge payload into `Assets/StreamingAssets/MetaMask`
- JavaScript bridge (`window.MetaMaskBridge`) with:
  - `init`, `connect`, `disconnect`, `signMessage`, `request`
  - `connectAndSign`, `connectWith`, `isInitialized`, `isConnected`
  - `getConnectionState`, `getConnectionDetails`
  - `setUnityInstance`, `setUnityGameObjectName`, `setDebug`
  - `on(event, handler)`, `off(event, handler?)`
  - etc.
- C# interop: `.jslib` + `Web3MetaMaskJsBridge` wrapper (methods mirror the JS API)
- Sample (`Minimal`) with WebGL template and a sample MonoBehaviour

## Installation (Unity Package Manager)
Choose one of the following:

1) Add from Git URL (recommended)
- Open Package Manager → + (Add) → Add package from git URL…
- Latest (tracks main):
  `https://github.com/v17alya/Web3-MetaMask-Unity.git?path=Assets/com.gamenator.web3-metamask-unity#main`
- Pin to a specific tag (recommended for reproducibility):
  `https://github.com/v17alya/Web3-MetaMask-Unity.git?path=Assets/com.gamenator.web3-metamask-unity#v0.2.0`

2) Add from disk
- Open Package Manager → + (Add) → Add package from disk…
- Select this file: `Assets/com.gamenator.web3-metamask-unity/package.json`

3) Via `Packages/manifest.json`
```json
{
  "dependencies": {
    "com.gamenator.web3-metamask-unity": "https://github.com/v17alya/Web3-MetaMask-Unity.git?path=Assets/com.gamenator/web3-metamask-unity#v0.2.0"
  }
}
```

## Getting Started
1) Install the embedded bridge
- Unity menu: Tools → Web3 → MetaMask → Install Embedded Bridge (writes to `Assets/StreamingAssets/MetaMask/`)

2) Add the bridge to your WebGL template and initialize
- Ensure your WebGL template loads the bridge before usage. For example in your `index.html`:
```html
<head>
  <!-- Include the installed bridge (adjust path if copied elsewhere) -->
  <script src="StreamingAssets/MetaMask/web3-metamask-bridge.js"></script>
  <script>
    // Initialize bridge BEFORE calling connect/sign/request
    MetaMaskBridge.init({
      dappMetadata: { name: 'Your App', url: window.location.href },
      infuraAPIKey: 'YOUR_INFURA_KEY',
      // Optionally pass callback GameObject and/or Unity instance (if available here)
      // unity: { gameObjectName: 'Web3Bridge', instance: window.unityInstance },
      debug: false
      // Optional JS-side events (you can also use MetaMaskBridge.on/off later)
        disconnected: () => console.log('disconnected'),
        chainChanged: (cid) => console.log('chainChanged', cid),
        signed: ({ signature, address }) => console.log('signed', signature, address),
        requestError: (m) => console.warn('requestError', m),
        // Connection details emits
        connectionDetails: (res) => console.log('connectionDetails', res),
        connectionDetailsError: (m) => console.warn('connectionDetailsError', m)
      }
    });
  </script>
</head>
```
- After Unity creates the instance, wire it once (e.g., near the code that calls `createUnityInstance`):
```html
<script>
  createUnityInstance(canvas, config, progress).then(function(instance) {
    window.unityInstance = instance;
    MetaMaskBridge.setUnityInstance(instance);
  });
</script>
```

3) Generate the example C# bridge and adapt
- Unity menu: Tools → Web3 → MetaMask → Generate Web3Bridge MonoBehaviour
- The generated script demonstrates calls to the `.jslib` wrapper. Key snippets:
```csharp
// Initialize (JSON options)
bool ok = Web3MetaMaskJsBridge.Init(JsonUtility.ToJson(new {
  dappMetadata = new { name = "Your App", url = Application.absoluteURL },
  infuraAPIKey = "YOUR_INFURA_KEY",
  debug = true,
  unity = new { gameObjectName = gameObject.name }
}));

// Connect / Disconnect
Web3MetaMaskJsBridge.Connect();
Web3MetaMaskJsBridge.Disconnect();

// Sign message
Web3MetaMaskJsBridge.SignMessage("Hello from Unity");

// JSON-RPC request
Web3MetaMaskJsBridge.Request("eth_chainId", "[]");

// Connect and sign / connect and request
Web3MetaMaskJsBridge.ConnectAndSign("Welcome!");
Web3MetaMaskJsBridge.ConnectWith("eth_getBalance", "[\"0x...\", \"latest\"]");

// Optional: set target GameObject name for callbacks
Web3MetaMaskJsBridge.SetUnityGameObjectName(gameObject.name);

// Optional (diagnostics):
bool isConnected = Web3MetaMaskJsBridge.IsConnected(); // bool
var state = Web3MetaMaskJsBridge.GetConnectionState();  // { connected, address }
Web3MetaMaskJsBridge.GetConnectionDetails();            // emits OnConnectionDetails/OnConnectionDetailsError
```
- Unity callbacks you can implement on the same GameObject:
```csharp
void OnConnected(string address) { /* handle connected */ }
void OnDisconnected(string _) { /* handle disconnected */ }
void OnSigned(string signature) { /* handle signature */ }
void OnRequested(string json) { /* handle RPC result */ }
void OnConnectedWith(string json) { /* handle connect-with result */ }
void OnConnectError(string error) { /* handle errors */ }
void OnDisconnectError(string error) { }
void OnSignError(string error) { }
void OnRequestedError(string error) { }
void OnConnectedWithError(string error) { }
void OnChainChanged(string chainId) { /* react to network changes */ }
void OnConnectionDetails(string json) { /* read { success, result?: { connected, address, accounts, chainId }, error? } */ }
void OnConnectionDetailsError(string error) { }
```

4) Build & run (WebGL)
- File → Build Settings → WebGL → Build or Build And Run

## Samples
- You can import available samples via Package Manager → MetaMask Unity → Samples → Import.
- Each sample includes its own README with setup notes and usage instructions.

## JavaScript Bridge API (global `MetaMaskBridge`)
- `init(options)` — must be called first; required options:
  - `dappMetadata` (required): shows your dapp name/url/icon during connection (trust context)
  - `infuraAPIKey` (required): enables read‑only RPC and load‑balancing
  - `unity.gameObjectName` (optional): GameObject name for callbacks
  - `unity.instance` (optional): pass Unity instance if available
  - `debug` (optional): enable/disable bridge logs
- `connect()` / `disconnect()`
- `connectAndSign(message)` — connect and sign in one step if supported
- `connectWith({ method, params? })` — connect and make a JSON‑RPC in one step
- `signMessage(message)` — signs a message via `personal_sign`
- `request({ method, params? })` — arbitrary JSON-RPC
- `isInitialized()` — returns whether the SDK is initialized
- `isConnected()` — whether an address is cached as connected
- `getConnectionState()` — returns `{ connected, address }`
- `getConnectionDetails()` — Promise resolving `{ success, result?: { connected, address, accounts, chainId }, error?: string }`
- `setUnityInstance({ instance? })`
- `setUnityGameObjectName(name)` — set callback target GameObject name
- `setDebug(enabled)` — enable verbose logging in the bridge
- `on(event, handler)` / `off(event, handler?)`

Lifecycle control (choose what suits your project):
- JavaScript‑driven: call `MetaMaskBridge.init(...)` in your template JS and use `MetaMaskBridge.connect()/signMessage()/request()` directly from the page.
- Unity‑driven: call `Web3MetaMaskJsBridge.Init(...)`/`Connect()`/`Request(...)` from C#; the `.jslib` forwards to the same JS API under the hood.
Both approaches can be mixed. Ensure initialization happens once before other calls.

See the MetaMask SDK docs for details: https://docs.metamask.io/sdk/connect/javascript/

Unity callbacks (GameObject default is `Web3Bridge`):
- `OnConnected(address)` / `OnDisconnected("")`
- `OnSigned(signature)` / `OnSignError(error)`
- `OnRequested(json)` / `OnRequestedError(error)`
- `OnConnectedWith(json)` / `OnConnectedWithError(error)`
- `OnConnectError(error)` / `OnDisconnectError(error)`
- `OnChainChanged(chainId)`
- `OnConnectionDetails(json)` / `OnConnectionDetailsError(error)`

## Editor Utilities
- Tools → Web3 → MetaMask → Install Embedded Bridge
- Tools → Web3 → MetaMask → Generate Web3Bridge MonoBehaviour

## Requirements
- Unity 2021.3+ (WebGL)
- Modern browser with MetaMask extension (desktop) or MetaMask mobile (deeplink)

## References
- MetaMask SDK: https://docs.metamask.io/sdk/connect/javascript/
