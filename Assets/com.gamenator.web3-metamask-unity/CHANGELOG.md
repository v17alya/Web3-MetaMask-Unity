# Changelog

All notable changes to this package will be documented in this file.

## [0.1.3] - 2025-08-28
### Added
- WebGL interop: added `W3MM_IsInitialized` in `.jslib` returning 1/0 from `window.MetaMaskBridge.isInitialized()`.
- C# runtime: added `Web3MetaMaskJsBridge.IsInitialized()` to query SDK initialization state.
- UI: added "IsInitialized?" button to `Web3BridgeTemplate.cs` and `Web3MetaMaskBridgeMinimalSample.cs` to log current initialization state.

## [0.1.2] - 2025-08-28
### Changed
- Sample `Web3MetaMaskBridgeMinimalSample.cs`: refactored inline GUI actions into dedicated private methods (`ShowIsConnected`, `ShowConnectionStateSync`, `RequestConnectionDetailsAsync`) and added public wrappers (`IsConnected`, `GetConnectionState`, `GetConnectionDetails`) for clarity and reuse.

### Fixed
- `Web3MetaMaskJsBridge.cs`: corrected connection-state retrieval and interop safety (marshalling and error handling) to prevent occasional errors in WebGL builds.

## [0.1.1] - 2025-08-27
### Added
- JS bridge events for JS-side interception and callbacks:
  - New events via init options `events: { ... }` and runtime `on/off`:
    - `connected`, `disconnected`, `chainChanged`, `signed`, `requested`, `connectedWith`,
    - Errors: `connectError`, `disconnectError`, `signError`, `requestError`, `connectedWithError`,
    - New: `connectionDetails`, `connectionDetailsError`.
- New JS APIs:
  - `isConnected()`, `getConnectionState()` (returns `{ connected, address }`),
  - `getConnectionDetails()` (returns `{ success, result?: { connected, address, accounts, chainId }, error?: string }`).
- New Unity callbacks:
  - `OnConnectionDetails(string json)`, `OnConnectionDetailsError(string message)`.

### Changed
- Unified success emitters (`emitConnected`, `emitDisconnected`, `emitSigned`, `emitRequested`, `emitConnectedWith`, `emitChainChanged`) and error emitters to centralize Unity/JS emissions.
- Added deduplication of consecutive `connected`/`disconnected` emits to avoid duplicate callbacks when multiple sources trigger the same state.
- `getConnectionState()` and `getConnectionDetails()` no longer include the provider object (non-serializable/circular); use `request(...)` and events instead.

### WebGL interop (.jslib / C#)
- `.jslib`:
  - `W3MM_SetUnityGameObjectName` now returns `1/0` (success/failure) and no longer emits errors.
  - `W3MM_GetConnectionState` returns `'null'` on error, no error emits.
  - `W3MM_GetConnectionDetails` invokes JS bridge emitters via `__W3MM_emit('emitConnectionDetails'| 'emitConnectionDetailsError', ...)`.
- C# wrapper (`Web3MetaMaskJsBridge`):
  - Added `IsConnected()`.
  - `GetConnectionState()` now returns a structured object `{ connected, address }` instead of raw JSON.
  - `GetConnectionDetails()` simplified (no parameters); results are emitted to Unity callbacks.

### Docs & Samples
- Updated READMEs (root, package, sample) to document new APIs, events, callbacks, and provider note.
- Sample template `index.html` demonstrates JS events, on/off, and connection state diagnostics.

## [0.1.0] - 2025-08-26
- Initial release
  - UPM package scaffold
  - Editor installer for embedded bridge payload
  - JS bridge (MetaMaskBridge) and .jslib interop
  - C# wrapper (Web3MetaMaskJsBridge)
  - Minimal sample (template + scene script)
