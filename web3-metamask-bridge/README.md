# Web3MetaMask Bridge (Vite)

This project builds the JS bridge used by the Unity WebGL plugin.

## Contents
- [What do you want to do?](#what-do-you-want-to-do)
- [Prerequisites](#prerequisites)
- [Build the bridge (single-file IIFE)](#build-the-bridge-single-file-iife--alternative-1)
- [Building with chunks (multiple output files)](#building-with-chunks-multiple-output-files--alternative-2)
- [Package for Unity installer (ZIP)](#package-for-unity-installer-zip)
- [Use with the Unity Installer (Install Embedded Bridge)](#use-with-the-unity-installer-install-embedded-bridge)
- [Integrate without packaging (copy to WebGL template)](#integrate-without-packaging-copy-to-webgl-template)
- [Add to WebGL HTML/JS (script usage)](#add-to-webgl-htmljs-script-usage)
- [Options explained](#options-explained)
- [API surface (global)](#api-surface-global)
- [Unity callbacks (GameObject: default `Web3Bridge`)](#unity-callbacks-gameobject-default-web3bridge)
- [References](#references)

## What do you want to do?
- Use the Unity installer (packaged ZIP):
  1) Build the bridge (choose ONE: single-file IIFE or chunked) — see [“Build the bridge (single-file IIFE)](#build-the-bridge-single-file-iife--alternative-1) or [“Building with chunks (multiple output files)”](#building-with-chunks-multiple-output-files--alternative-2)
  2) Package the build into a ZIP — see [“Package for Unity installer (ZIP)”](#package-for-unity-installer-zip)
  3) Copy the ZIP and run the installer — see [“Use with the Unity Installer (Install Embedded Bridge)”](#use-with-the-unity-installer-install-embedded-bridge)
- Integrate without packaging (direct in WebGL template):
  1) Build the bridge — e.g., see [“Build the bridge (single-file IIFE)”](#build-the-bridge-single-file-iife--alternative-1)
  2) Copy the built JS to your WebGL template — see [“Integrate without packaging (copy to WebGL template)”](#integrate-without-packaging-copy-to-webgl-template)
  3) Include and initialize in HTML — see [“Add to WebGL HTML/JS (script usage)”](#add-to-webgl-htmljs-script-usage)

## Prerequisites
- Node.js and npm
- zip and gzip in your shell (macOS/Linux or Git Bash on Windows)

## Build the bridge (single-file IIFE) — alternative 1
```bash
# From repo root (or cd into this folder first)
cd "web3-metamask-bridge"

# Install deps (reproducible)
npm ci || npm install

# Build a single IIFE bundle at dist/web3-metamask-bridge.js
npm run build
# → dist/web3-metamask-bridge.js
```

## Building with chunks (multiple output files) — alternative 2
Need multiple output files instead of a single IIFE?

1) Edit vite.config.js to allow code-splitting and keep predictable names:
- The entry stays `dist/web3-metamask-bridge.js`
- All chunks and assets are emitted under `dist/assets/`
```js
// vite.config.js
import { defineConfig } from 'vite'

export default defineConfig({
  build: {
    sourcemap: false,       // keep bundles small
    minify: true,           // production minification
    rollupOptions: {
      input: './src/index.js',
      output: {
        entryFileNames: 'web3-metamask-bridge.js',
        chunkFileNames: 'assets/web3-metamask-bridge.[name].js',
        assetFileNames: 'assets/web3-metamask-bridge.[name][extname]'
      }
    }
  },
  define: { 'process.env': {} }
})
```
2) Build:
```bash
npm run build
```

## Package for Unity installer (ZIP)
The Unity installer expects a ZIP that contains one or more *.gz.base64 files (each is a gzip-compressed, Base64-encoded JS file). Use ONE of the following, depending on your build:

- Single-file build:
```bash
# 1) Encode the single JS file
gzip -c "dist/web3-metamask-bridge.js" | base64 > "dist/web3-metamask-bridge.js.gz.base64"

# 2) Create/refresh the ZIP payload with encoded files in dist/
zip -j "dist/bridge-payload.zip" "dist/web3-metamask-bridge.js.gz.base64"
```

- Chunked build (encode the entry and all .js files under dist/assets/):
```bash
# 1) Encode entry file
gzip -c "dist/web3-metamask-bridge.js" | base64 > "dist/web3-metamask-bridge.js.gz.base64"

# 2) Encode every chunk .js output as gzip+Base64 into dist/gz/
mkdir -p dist/gz
for f in dist/assets/*.js; do
  [ -f "$f" ] || continue
  base=$(basename "$f")
  gzip -c "$f" | base64 > "dist/gz/${base}.gz.base64"
done

# 3) Create the ZIP payload with all encoded files
zip -j dist/bridge-payload.zip dist/web3-metamask-bridge.js.gz.base64 dist/gz/*.gz.base64
```

### Optional: add an npm packaging script (single-file build)
Add this script to web3-metamask-bridge/package.json:
```json
{
  "scripts": {
    "package:zip": "npm run build && gzip -c dist/web3-metamask-bridge.js | base64 > dist/web3-metamask-bridge.js.gz.base64 && zip -j dist/bridge-payload.zip dist/web3-metamask-bridge.js.gz.base64"
  }
}
```
Then run:
```bash
cd "web3-metamask-bridge"
npm run package:zip
```

## Use with the Unity Installer (Install Embedded Bridge)
```bash
# Copy the ZIP into the plugin package
cp dist/bridge-payload.zip "../Assets/com.gamenator.web3-metamask-unity/Editor/Embedded/bridge-payload.zip"
```
In Unity:
- Tools → Web3 → MetaMask → Install Embedded Bridge
- The installer unpacks to Assets/StreamingAssets/MetaMask/ for WebGL builds

Editor scripts (references):
- `EmbeddedBridgeInstaller.cs` — installs the ZIP payload to StreamingAssets:
  - `Assets/com.gamenator.web3-metamask-unity/Editor/Scripts/EmbeddedBridgeInstaller.cs`
- `Web3BridgeGenerator.cs` — creates a sample MonoBehaviour that calls the JS bridge:
  - `Assets/com.gamenator.web3-metamask-unity/Editor/Scripts/Web3BridgeGenerator.cs`

## Integrate without packaging (copy to WebGL template)
```bash
cd "web3-metamask-bridge"
npm install
npm run build

# Place next to your WebGL template index.html (adjust path if needed)
cp dist/web3-metamask-bridge.js "../Assets/WebGLTemplates/<YourTemplate>/src/web3-metamask/web3-metamask-bridge.js"
```

## Add to WebGL HTML/JS (script usage)
Include the script in your template index.html (ensure it's loaded before any code that calls `window.MetaMaskBridge`):
```html
<head>
  <!-- Bridge: must be loaded before usage -->
  <script src="src/web3-metamask/web3-metamask-bridge.js"></script>
  <script>
    // Initialize bridge BEFORE calling connect/sign/request
    MetaMaskBridge.init({
      // Unity wiring (optional): callback GameObject and/or Unity instance if available
      unity: { gameObjectName: 'Web3Bridge', instance: window.unityInstance },
      debug: false,
      // Optional JS-side events (you can also use MetaMaskBridge.on/off)
      events: {
        connected: ({ address, accounts }) => console.log('connected', address, accounts),
        disconnected: () => console.log('disconnected'),
        chainChanged: (cid) => console.log('chainChanged', cid),
        signed: ({ signature, address }) => console.log('signed', signature, address),
        requested: (result) => console.log('requested', result),
        connectError: (m) => console.warn('connectError', m),
        requestError: (m) => console.warn('requestError', m),
        // Connection detail emits
        connectionDetails: (res) => console.log('connectionDetails', res),
        connectionDetailsError: (m) => console.warn('connectionDetailsError', m)
      },
      // REQUIRED: SDK options object containing dappMetadata and infuraAPIKey
      sdkOptions: {
        // dapp identity shown during connection (trust context)
        dappMetadata: { name: 'Your App', url: window.location.href },
        // REQUIRED: enables read-only RPC and load-balancing
        infuraAPIKey: 'YOUR_INFURA_KEY'
      }
    });
  </script>
</head>
```

Optionally wire the Unity instance after load:
```html
<script>
  createUnityInstance(canvas, config, progress).then(function(instance) {
    window.unityInstance = instance;
    MetaMaskBridge.setUnityInstance(instance);
  });
</script>
```

## Options explained
- **unity.gameObjectName**: Optional. Unity GameObject name targeted by `SendMessage` for bridge callbacks.
- **unity.instance**: Optional. Unity instance to enable callbacks immediately on init.
- **debug**: Optional. Enables verbose logging inside the bridge.
- **events**: Optional. JS-side event callbacks for various MetaMask events.
- **sdkOptions**: Required. Direct SDK options object passed through to MetaMaskSDK. This object MUST contain:
  - **dappMetadata**: Displays your dapp name/url/icon in the MetaMask connection prompt (trust context).
  - **infuraAPIKey**: Required. Enables read‑only RPC and load‑balancing for the SDK.

See the MetaMask SDK docs for details: https://docs.metamask.io/sdk/connect/javascript/

## Connection Checking
The bridge includes a `checkConnection()` method that uses the `eth_accounts` RPC method to check if MetaMask is connected without prompting the user. This is useful for:

- **Auto-detection**: Check if the user is already connected when the page loads
- **State management**: Update UI based on connection status without user interaction
- **Avoiding prompts**: Determine connection status without triggering MetaMask popups

Example usage:
```javascript
// Check connection status without prompting
const result = await MetaMaskBridge.checkConnection();
if (result.success && result.result.connected) {
  console.log('User is connected:', result.result.address);
  // Update UI to show connected state
} else {
  console.log('User is not connected');
  // Show connect button
}
```

The method automatically sets up `accountsChanged` event listeners to keep the connection state updated. It uses `window.ethereum` directly for better compatibility with bundled builds.

## Mobile Deep Links
The bridge includes a `generateMetaMaskDeepLink()` method that generates a MetaMask mobile deeplink URL for the current page. This is useful for:

- **Mobile users**: Provide a direct link to open the dapp in MetaMask mobile
- **Cross-platform**: Ensure mobile users can access the dapp even without the extension

Example usage:
```javascript
// Generate deeplink for current page
const deeplink = MetaMaskBridge.generateMetaMaskDeepLink();
console.log('MetaMask deeplink:', deeplink);
// Result: https://metamask.app.link/dapp/your-current-url
```

## API surface (global)
- `window.MetaMaskBridge.init({ unity?, debug?, events?, sdkOptions })` → `{ success: boolean, result?: any, error?: string }`
- `window.MetaMaskBridge.connect()`
- `window.MetaMaskBridge.connectAndSign(message)`
- `window.MetaMaskBridge.connectWith({ method, params? })`
- `window.MetaMaskBridge.disconnect()`
- `window.MetaMaskBridge.signMessage(message)`
- `window.MetaMaskBridge.request({ method, params? })`
- `window.MetaMaskBridge.isInitialized()`
- `window.MetaMaskBridge.isConnected()`
- `window.MetaMaskBridge.getConnectionState()` → `{ connected, address }`
- `window.MetaMaskBridge.getConnectionDetails()` → `{ success, result?: { connected, address, accounts, chainId }, error?: string }`
- `window.MetaMaskBridge.checkConnection()` → `{ success, result?: { connected, address, accounts }, error?: string }`
- `window.MetaMaskBridge.generateMetaMaskDeepLink()` → `string`
- `window.MetaMaskBridge.setUnityInstance({ instance? } | instance)`
- `window.MetaMaskBridge.setUnityGameObjectName(name)`
- `window.MetaMaskBridge.setDebug(enabled)`
- `window.MetaMaskBridge.on(event, handler)` / `off(event, handler?)`

Notes:
- Provider object is not returned (non-serializable); use `request({...})` for RPC and `on('chainChanged'|...)` for events.
- The `init` method now returns a result object with `success`, `result`, and optional `error` properties.
- Required options are now nested under `sdkOptions.dappMetadata` and `sdkOptions.infuraAPIKey`.

## Unity callbacks (GameObject: default `Web3Bridge`)
- `OnConnected(string address)` / `OnDisconnected(string empty)`
- `OnSigned(string signature)` / `OnSignError(string message)`
- `OnRequested(string json)` / `OnRequestedError(string message)`
- `OnConnectedWith(string json)` / `OnConnectedWithError(string message)`
- `OnConnectError(string message)` / `OnDisconnectError(string message)`
- `OnChainChanged(string chainId)`
- `OnConnectionDetails(string json)` / `OnConnectionDetailsError(string message)`

## C# interop (optional)
You can call the same functionality from Unity using the `.jslib` wrapper:
```csharp
using Gamenator.Web3.MetaMaskUnity.Runtime.WebGL;

// Initialize once
bool ok = Web3MetaMaskJsBridge.Init(JsonUtility.ToJson(new {
  dappMetadata = new { name = "Your App", url = Application.absoluteURL },
  infuraAPIKey = "YOUR_INFURA_KEY",
  debug = true,
  unity = new { gameObjectName = gameObject.name }
}));

// Calls
Web3MetaMaskJsBridge.Connect();
Web3MetaMaskJsBridge.SignMessage("hello");
Web3MetaMaskJsBridge.Request("eth_chainId", "[]");
Web3MetaMaskJsBridge.ConnectAndSign("welcome");
Web3MetaMaskJsBridge.ConnectWith("eth_getBalance", "[\"0x...\", \"latest\"]");
```

Lifecycle control note: You may perform init/connect/sign/request from JavaScript or from Unity (or mix both). Ensure `init(...)` is executed once before other calls.

## References
- MetaMask SDK docs: https://docs.metamask.io/sdk/connect/javascript/
- Minimal sample README: https://github.com/v17alya/Web3-MetaMask-Unity/tree/main/Assets/Web3MetaMask/Samples/Minimal/Documentation/README.md
- Unity plugin README: https://github.com/v17alya/Web3-MetaMask-Unity/tree/main/Assets/com.gamenator.web3-metamask-unity/README.md
 
