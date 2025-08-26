import { MetaMaskSDK } from "@metamask/sdk";

(function () {
	// ---------------------------------------------------------------------------
	// MetaMask Bridge for Unity WebGL
	// Exposes `window.MetaMaskBridge` to the template and Unity C# (via SendMessage).
	// Wraps MetaMask SDK lifecycle and common RPC/sign flows.
	// ---------------------------------------------------------------------------

	/** @type {import('@metamask/sdk').MetaMaskSDK | null} */
	let sdk = null;
	/** @type {import('@metamask/providers').MetaMaskInpageProvider | null} */
	let provider = null;
	/** @type {import('@metamask/providers').MetaMaskInpageProvider | null} */
	let subscribedProvider = null;
	/** @type {{ onAccountsChanged:(a:any[])=>void; onChainChanged:(c:any)=>void; onDisconnect:(e:any)=>void } | null} */
	let providerEventHandlers = null;
	/** @type {any} Unity instance (createUnityInstance return) */
	let unityInstance = null;
	/** @type {string} Target Unity GameObject that receives callbacks */
	let unityGameObjectName = "Web3Bridge";
	/** @type {string} Cached last connected address for convenience */
	let lastAddress = "";
	/** @type {boolean} Verbose debug logging */
	let debug = false;

	// ---------------------------------------------------------------------------
	// Public API (MetaMask SDK lifecycle and RPC)
	// ---------------------------------------------------------------------------

	/**
	 * Initialize MetaMask SDK. Call once before other APIs.
	 *
	 * Documented options: https://docs.metamask.io/sdk/connect/javascript/
	 *
	 * @param {Object} [options]
	 * @param {{ name?: string; url?: string; iconUrl?: string; }} [options.dappMetadata]
	 *        Dapp identity shown during connection (trust context)
	 * @param {string} options.infuraAPIKey
	 *        Required API key enabling read‑only RPC and load‑balancing
	 * @param {{ instance?: any; gameObjectName?: string }} [options.unity]
	 *        Unity wiring: explicit instance and/or callback GameObject name
	 * @param {boolean} [options.debug]
	 *        Enable verbose debug logging
	 * @param {boolean} [options.checkInstallationImmediately]
	 *        Immediately checks MetaMask installation on load and may prompt install (default false)
	 * @param {boolean} [options.checkInstallationOnAllCalls]
	 *        Checks installation before each RPC call (default false)
	 * @param {string}  [options.communicationServerUrl]
	 *        Custom communication server URL (mainly for debugging/testing)
	 * @param {boolean} [options.enableAnalytics]
	 *        Sends anonymous analytics to improve SDK (default true)
	 * @param {boolean} [options.extensionOnly]
	 *        Prefer and stick to extension when detected (default true)
	 * @param {boolean} [options.headless]
	 *        Enables headless mode to use custom UI/modals (default false)
	 * @param {(link:string)=>void} [options.openDeeplink]
	 *        Callback to open MetaMask mobile deeplink
	 * @param {Record<string,string>} [options.readonlyRPCMap]
	 *        Map of chainId (hex) → RPC URL for read‑only requests
	 * @param {boolean} [options.shouldShimWeb3]
	 *        Whether to shim window.web3 with the provider for legacy compatibility (default true)
	 * @returns {boolean} True if initialized (or already initialized)
	 * @throws {Error} When `infuraAPIKey` is missing
	 */
	function init(options = {}) {
		try {
			if (sdk) {
				log("init called but already initialized");
				return { success: true, result: true };
			}
			if (typeof options.debug !== "undefined") setDebug(Boolean(options.debug));
			// Unity wiring options
			if (options.unity?.gameObjectName)
				setUnityGameObjectName(String(options.unity.gameObjectName));
			if (options.unity?.instance) unityInstance = options.unity.instance;
			log("Unity wiring", { unityGameObjectName, hasInstance: Boolean(unityInstance) });
			
			
			const dappMetadata = options.dappMetadata;
			const infuraAPIKey = options.infuraAPIKey;
			if (!dappMetadata) throw new Error("MetaMaskBridge.init requires dappMetadata");
			if (!infuraAPIKey) throw new Error("MetaMaskBridge.init requires infuraAPIKey");

			// Construct MetaMask SDK instance (documented fields only and pass-through known options)
			const sdkOptions = { dappMetadata, infuraAPIKey };
			if (typeof options.checkInstallationImmediately !== 'undefined') sdkOptions.checkInstallationImmediately = Boolean(options.checkInstallationImmediately);
			if (typeof options.checkInstallationOnAllCalls !== 'undefined') sdkOptions.checkInstallationOnAllCalls = Boolean(options.checkInstallationOnAllCalls);
			if (typeof options.communicationServerUrl !== 'undefined') sdkOptions.communicationServerUrl = String(options.communicationServerUrl);
			if (typeof options.enableAnalytics !== 'undefined') sdkOptions.enableAnalytics = Boolean(options.enableAnalytics);
			if (typeof options.extensionOnly !== 'undefined') sdkOptions.extensionOnly = Boolean(options.extensionOnly);
			if (typeof options.headless !== 'undefined') sdkOptions.headless = Boolean(options.headless);
			if (typeof options.openDeeplink !== 'undefined') sdkOptions.openDeeplink = options.openDeeplink;
			if (typeof options.readonlyRPCMap !== 'undefined') sdkOptions.readonlyRPCMap = options.readonlyRPCMap;
			if (typeof options.shouldShimWeb3 !== 'undefined') sdkOptions.shouldShimWeb3 = Boolean(options.shouldShimWeb3);
			sdk = new MetaMaskSDK(sdkOptions);
			log("SDK initialized");

			// Subscribe to provider events (idempotent)
			if (!subscribeProviderEvents(getProvider())) {
				// ignore error
			}
		} catch (e) {
			error("init error:", e?.message || e);
			return { success: false, error: String(e?.message || e) };
		}
		return { success: true, result: true };
	}

	/**
	 * Connect to MetaMask.
	 * Uses SDK `connect()` (cross‑platform, deeplink capable); falls back to `eth_requestAccounts`.
	 *
	 * @returns {Promise<string[]>} Accounts array (primary at index 0)
	 * @throws {Error} When not initialized or user rejects the request
	 */
	async function connect() {
		try {
			if (!sdk) throw new Error("MetaMaskBridge not initialized");
			log("connect called");
			const accounts = (sdk.connect ? await sdk.connect() : await getProvider()?.request({
				method: "eth_requestAccounts",
			})) || [];
			const addr =
				Array.isArray(accounts) && accounts[0] ? String(accounts[0]) : "";
			if (!addr) throw new Error("No address");
			lastAddress = addr;
			log("connect success:", { addr, count: accounts.length });
			sendToUnity("OnConnected", addr);
			// Ensure provider is current and subscribe events
			provider = getProvider() || provider;
			if (!subscribeProviderEvents(provider)) {
				throw new Error("Failed to subscribe to provider events");
			}
			return { success: true, result: accounts };
		} catch (e) {
			error("connect error:", e?.message || e);
			emitConnectError(String(e?.message || e));
			return { success: false, error: String(e?.message || e) };
		}
	}

	/**
	 * Connect and sign a message in one step (SDK‑supported only).
	 *
	 * @param {string} message Message to sign upon connection
	 * @returns {Promise<{ accounts: string[], signature: string }>} Result including connected accounts and signature
	 * @throws {Error} When not initialized, unsupported, or user rejects
	 */
	async function connectAndSign(message) {
		try {
			if (!sdk) throw new Error("MetaMaskBridge not initialized");
			log("connectAndSign called");
			if (!sdk.connectAndSign) {
				throw new Error("connectAndSign is not supported by this SDK version");
			}
			const result = await sdk.connectAndSign({ msg: String(message ?? "") });
			// Ensure provider is set post-connect
			provider = getProvider() || provider;
			if (!subscribeProviderEvents(provider)) {
				throw new Error("Failed to subscribe to provider events");
			}
			if (result?.accounts?.length) {
				lastAddress = String(result.accounts[0]);
				sendToUnity("OnConnected", lastAddress);
			}
			log("connectAndSign success:", { addr: lastAddress, hasSignature: Boolean(result?.signature) });
			sendToUnity("OnSigned", String(result?.signature ?? ""));
			return { success: true, result };
		} catch (e) {
			error("connectAndSign error:", e?.message || e);
			emitSignError(String(e?.message || e));
			return { success: false, error: String(e?.message || e) };
		}
	}

	/**
	 * Connect and perform a specific JSON-RPC method in one step.
	 * @param {{ method: string; params?: any[] }} rpc
	 * @returns {Promise<any>}
	 */
	async function connectWith(rpc) {
		try {
			if (!sdk) throw new Error("MetaMaskBridge not initialized");
			log("connectWith called", rpc);
			if (!sdk.connectWith) {
				throw new Error("connectWith is not supported by this SDK version");
			}
			const result = await sdk.connectWith(rpc);
			log("connectWith success");
			sendToUnity("OnConnectedWith", JSON.stringify(result));
			return { success: true, result };
		} catch (e) {
			error("connectWith error:", e?.message || e);
			emitConnectWithError(String(e?.message || e));
			return { success: false, error: String(e?.message || e) };
		}
	}

	/**
	 * Clear SDK state. MetaMask extension has no hard disconnect for dapps.
	 *
	 * @returns {Promise<void>}
	 */
	async function disconnect() {
		try {
			log("disconnect called");
			lastAddress = "";
			if (sdk?.terminate) await sdk.terminate();
			sendToUnity("OnDisconnected", "");
			return { success: true, result: "" };
		} catch (e) {
			error("disconnect error:", e?.message || e);
			emitDisconnectError(String(e?.message || e));
			return { success: false, error: String(e?.message || e) };
		}
	}

	/**
	 * Check if the SDK is initialized.
	 * @returns {boolean}
	 */
	function isInitialized() {
		try { return Boolean(sdk && sdk.isInitialized && sdk.isInitialized()); } catch { return Boolean(sdk); }
	}

	/**
	 * Sign a message with the current account via `personal_sign`.
	 *
	 * @param {string} message The message to sign
	 * @returns {Promise<string>} Hex signature string
	 * @throws {Error} When not initialized, no account, or user rejects
	 */
	async function signMessage(message) {
		try {
			const p = getProvider();
			if (!p) throw new Error("Provider not available. Initialize and connect first.");
			log("signMessage called", { hasCachedAddress: Boolean(lastAddress) });
			const accounts = lastAddress
				? [lastAddress]
				: await p.request({ method: "eth_accounts" });
			const addr =
				Array.isArray(accounts) && accounts[0] ? String(accounts[0]) : "";
			if (!addr) throw new Error("No connected account");
			const sig = await p.request({
				method: "personal_sign",
				params: [String(message ?? ""), addr],
			});
			log("signMessage success", { addr, hasSignature: Boolean(sig) });
			sendToUnity("OnSigned", String(sig));
			return { success: true, result: String(sig) };
		} catch (e) {
			error("signMessage error:", e?.message || e);
			emitSignError(String(e?.message || e));
			return { success: false, error: String(e?.message || e) };
		}
	}

	/**
	 * Make an arbitrary JSON‑RPC request via the provider.
	 *
	 * @template T
	 * @param {{ method: string; params?: any[] }} req
	 * @returns {Promise<T>} JSON‑RPC result
	 * @throws {Error} When not initialized or RPC fails
	 */
	async function request(req) {
		try {
			const p = getProvider();
			if (!p) throw new Error("Provider not available. Initialize and connect first.");
			log("request:", req);
			const result = await p.request(req);
			sendToUnity("OnRequested", JSON.stringify(result));
			return { success: true, result };
		} catch (e) {
			error("request error:", req, e?.message || e);
			emitRequestError(String(e?.message || e));
			return { success: false, error: String(e?.message || e) };
		}
	}

	/**
	 * Wire the Unity instance for callbacks.
	 *
	 * @param {any | { instance?: any }} arg Unity instance (raw or wrapped)
	 * @returns {void}
	 */
	function setUnityInstance(arg) {
		log("setUnityInstance called", { type: typeof arg });
		// Accept raw Unity instance or object with { instance }
		if (arg && typeof arg === "object" && typeof arg.SendMessage === "function") {
			unityInstance = arg;
			log("Unity instance set (raw)");
			return;
		}
		if (arg && typeof arg === "object" && arg.instance) {
			unityInstance = arg.instance;
			log("Unity instance set (wrapped)");
			return;
		}
		log("Unity instance not provided or invalid; leaving unchanged");
	}

	/**
	 * Set the Unity GameObject name that receives callbacks.
	 * @param {string} name
	 */
	function setUnityGameObjectName(name) {
		const n = String(name || "").trim();
		if (!n) {
			warn("setUnityGameObjectName: empty name ignored");
			return;
		}
		unityGameObjectName = n;
		log("Unity gameObjectName set", { unityGameObjectName });
	}

	// ---------------------------------------------------------------------------
	// Unity related methods
	// ---------------------------------------------------------------------------

	/**
	 * Resolve Unity instance.
	 * If not set via `setUnityInstance`, fallback to `window.unityInstance`.
	 *
	 * @returns {any | null} The Unity instance (with SendMessage) or null if not available
	 */
	function getUnity() {
		if (unityInstance?.SendMessage) {
			return unityInstance;
		}
		const w = /** @type {any} */ (window);
		if (w?.unityInstance?.SendMessage) {
			return w.unityInstance;
		}
		return null;
	}

	/**
	 * Send a message to Unity if available.
	 *
	 * @param {string} method Name of the Unity method on GameObject
	 * @param {string} [payload] Optional string payload
	 * @returns {void}
	 */
	function sendToUnity(method, payload) {
		const u = getUnity();
		if (!u) {
			warn("sendToUnity: Unity instance not available; dropping", { method, payload });
			return;
		}
		try {
			u.SendMessage(unityGameObjectName, method, payload ?? "");
		} catch (e) {
			error("sendToUnity error:", e);
		}
	}

	/**
	 * Enable or disable verbose debug logging at runtime.
	 * @param {boolean} enabled
	 */
	function setDebug(enabled) {
		debug = Boolean(enabled);
		log("debug set:", debug);
	}

	// ---------------------------------------------------------------------------
	// Private API
	// ---------------------------------------------------------------------------
	
	// Expose error emitters for interop (.jslib) to forward errors uniformly
	function emitConnectError(message) { try { sendToUnity("OnConnectError", String(message ?? "")); } catch { } }
	function emitDisconnectError(message) { try { sendToUnity("OnDisconnectError", String(message ?? "")); } catch {} }
	function emitSignError(message) { try { sendToUnity("OnSignError", String(message ?? "")); } catch {} }
	function emitRequestError(message) { try { sendToUnity("OnRequestedError", String(message ?? "")); } catch {} }
	function emitConnectWithError(message) { try { sendToUnity("OnConnectedWithError", String(message ?? "")); } catch {} }

	// ---------------------------------------------------------------------------
	// Logging helpers (kept at the bottom for clarity)
	// ---------------------------------------------------------------------------
	function _logWithStackTrace(consoleMethod, showStackTrace, ...args) {
		if (showStackTrace) {
			// Methods that already have stack trace by default
			const methodsWithDefaultStackTrace = [console.error, console.warn];
			if (methodsWithDefaultStackTrace.includes(consoleMethod)) {
				consoleMethod(...args);
			} else {
				console.groupCollapsed(...args);
				console.trace();
				console.groupEnd();
			}
		} else {
			consoleMethod(...args);
		}
	}

	function log() {
		if (!debug) return;
		try { _logWithStackTrace(console.log, true, "[MetaMaskBridge]", ...arguments); } catch {}
	}

	function warn() {
		if (!debug) return;
		try { _logWithStackTrace(console.warn, true, "[MetaMaskBridge]", ...arguments); } catch {}
	}

	function error() {
		if (!debug) return;
		try { _logWithStackTrace(console.error, true, "[MetaMaskBridge]", ...arguments); } catch {}
	}

	/**
	 * Subscribe to provider events (accountsChanged, chainChanged, disconnect).
	 * Idempotent per provider instance; re-subscribes when provider changes.
	 * @param {import('@metamask/providers').MetaMaskInpageProvider} [passed]
	 * @returns {boolean} True if a new subscription was applied; false if skipped
	 */
	function subscribeProviderEvents(passed) {
		const p = passed || getProvider();
		if (!p) { log("subscribeProviderEvents: provider not available"); return false; }
		if (subscribedProvider === p) { log("subscribeProviderEvents: already subscribed"); return true; }
		// Unsubscribe previous
		try {
			if (subscribedProvider && providerEventHandlers) {
				subscribedProvider.removeListener?.("accountsChanged", providerEventHandlers.onAccountsChanged);
				subscribedProvider.removeListener?.("chainChanged", providerEventHandlers.onChainChanged);
				subscribedProvider.removeListener?.("disconnect", providerEventHandlers.onDisconnect);
			}
		} catch {}
		// New handlers
		const onAccountsChanged = (accounts) => {
			const addr = Array.isArray(accounts) && accounts[0] ? String(accounts[0]) : "";
			lastAddress = addr;
			log("accountsChanged:", { addr, accounts });
			if (addr) sendToUnity("OnConnected", addr); else sendToUnity("OnDisconnected", "");
		};
		const onChainChanged = (cid) => { log("chainChanged:", cid); sendToUnity("OnChainChanged", String(cid)); };
		const onDisconnect = (err) => { log("disconnect event:", err?.message || err); emitDisconnectError(String(err?.message || "Disconnected")); };
		try {
			if (!p) throw new Error("Provider not available");
			p.on?.("accountsChanged", onAccountsChanged);
			p.on?.("chainChanged", onChainChanged);
			p.on?.("disconnect", onDisconnect);
		} catch { return false; }
		subscribedProvider = p;
		providerEventHandlers = { onAccountsChanged, onChainChanged, onDisconnect };
		log("Provider events subscribed");
		return true;
	}

	/**
	 * Resolve and cache MetaMask inpage provider from SDK.
	 * Safe to call repeatedly; returns the cached instance if available.
	 * @returns {import('@metamask/providers').MetaMaskInpageProvider | null}
	 */
	function getProvider() {
		if (provider) return provider;
		try {
			const p = sdk?.getProvider?.();
			if (p) provider = p;
		} catch {}
		return provider;
	}

	// Public API exposed to the template and Unity
	window.MetaMaskBridge = {
		init,
		connect,
		connectAndSign,
		connectWith,
		disconnect,
		signMessage,
		request,
		isInitialized,
		setUnityInstance,
		setUnityGameObjectName,
		setDebug,
		emitConnectError,
		emitDisconnectError,
		emitSignError,
		emitRequestError,
		emitConnectWithError,
	};
})();
