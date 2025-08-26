mergeInto(LibraryManager.library, {
	// Declare helper dependency for functions that emit Unity callbacks on error
	W3MM_Init__deps: ['$__W3MM_emit'],
	W3MM_Connect__deps: ['$__W3MM_emit'],
	W3MM_Disconnect__deps: ['$__W3MM_emit'],
	W3MM_SignMessage__deps: ['$__W3MM_emit'],
	W3MM_ConnectAndSign__deps: ['$__W3MM_emit'],
	W3MM_ConnectWith__deps: ['$__W3MM_emit'],
	W3MM_Request__deps: ['$__W3MM_emit'],
	W3MM_SetUnityInstance__deps: ['$__W3MM_emit'],
	W3MM_SetDebug__deps: ['$__W3MM_emit'],
	W3MM_SetUnityGameObjectName__deps: ['$__W3MM_emit'],

	// Initialize MetaMask bridge with options JSON
	W3MM_Init: function (optionsPtr) {
		try {
			var optionsJson = UTF8ToString(optionsPtr);
			var options = optionsJson && optionsJson.length ? JSON.parse(optionsJson) : null;
			if (!window || !window.MetaMaskBridge || !window.MetaMaskBridge.init) {
				throw new Error('MetaMaskBridge.init not found');
			}
			var r = window.MetaMaskBridge.init(options || {});
			if (r && typeof r === 'object' && ('success' in r)) {
				return r.success ? 1 : 0;
			}
			return r ? 1 : 0;
		} catch (e) { __W3MM_emit('emitConnectError', e); return 0; }
	},

	// Connect via MetaMask
	W3MM_Connect: function () {
		try {
			if (!window || !window.MetaMaskBridge || !window.MetaMaskBridge.connect) {
				throw new Error('MetaMaskBridge.connect not found');
			}
			var p1 = window.MetaMaskBridge.connect();
		} catch (e) { __W3MM_emit('emitConnectError', e); }
	},

	// Disconnect (clear SDK state)
	W3MM_Disconnect: function () {
		try {
			if (!window || !window.MetaMaskBridge || !window.MetaMaskBridge.disconnect) {
				throw new Error('MetaMaskBridge.disconnect not found');
			}
			var p2 = window.MetaMaskBridge.disconnect();
		} catch (e) { __W3MM_emit('emitDisconnectError', e); }
	},

	// Request a message signature
	W3MM_SignMessage: function (messagePtr) {
		try {
			var message = UTF8ToString(messagePtr);
			if (!window || !window.MetaMaskBridge || !window.MetaMaskBridge.signMessage) {
				throw new Error('MetaMaskBridge.signMessage not found');
			}
			var p = window.MetaMaskBridge.signMessage(message || '');
		} catch (e) { __W3MM_emit('emitSignError', e); }
	},

	// Connect and sign message
	W3MM_ConnectAndSign: function (messagePtr) {
		try {
			var message = UTF8ToString(messagePtr);
			if (!window || !window.MetaMaskBridge || !window.MetaMaskBridge.connectAndSign) {
				throw new Error('MetaMaskBridge.connectAndSign not found');
			}
			var p = window.MetaMaskBridge.connectAndSign(message || '');
		} catch (e) { __W3MM_emit('emitSignError', e); }
	},

	// Connect with RPC (sdk.connectWith)
	W3MM_ConnectWith: function (methodPtr, paramsJsonPtr) {
		try {
			var method = UTF8ToString(methodPtr);
			var paramsJson = UTF8ToString(paramsJsonPtr);
			var paramsObj = paramsJson && paramsJson.length ? JSON.parse(paramsJson) : undefined;
			if (!window || !window.MetaMaskBridge || !window.MetaMaskBridge.connectWith) {
				throw new Error('MetaMaskBridge.connectWith not found');
			}
			var p = window.MetaMaskBridge.connectWith({ method: method || '', params: paramsObj });
		} catch (e) { __W3MM_emit('emitConnectWithError', e); }
	},

	// Arbitrary JSON-RPC request
	W3MM_Request: function (methodPtr, paramsJsonPtr) {
		try {
			var method = UTF8ToString(methodPtr);
			var paramsJson = UTF8ToString(paramsJsonPtr);
			var paramsObj = paramsJson && paramsJson.length ? JSON.parse(paramsJson) : undefined;
			if (!window || !window.MetaMaskBridge || !window.MetaMaskBridge.request) {
				throw new Error('MetaMaskBridge.request not found');
			}
			var p = window.MetaMaskBridge.request({ method: method || '', params: paramsObj });
		} catch (e) { __W3MM_emit('emitRequestError', e); }
	},

	// Provide the active GameObject name for Unity callbacks
	W3MM_SetUnityInstance: function (goPtr) {
		try {
			var goName = UTF8ToString(goPtr);
			if (!window || !window.MetaMaskBridge || !window.MetaMaskBridge.setUnityGameObjectName) {
				throw new Error('MetaMaskBridge.setUnityGameObjectName not found');
			}
			window.MetaMaskBridge.setUnityGameObjectName(goName);
		} catch (e) { __W3MM_emit('emitRequestError', e); }
	},

	// Set debug flag on the JS bridge
	W3MM_SetDebug: function (enabled) {
		try {
			if (!window || !window.MetaMaskBridge || !window.MetaMaskBridge.setDebug) {
				throw new Error('MetaMaskBridge.setDebug not found');
			}
			window.MetaMaskBridge.setDebug(!!enabled);
		} catch (e) { __W3MM_emit('emitRequestError', e); }
	},

	// Set Unity GameObject name explicitly
	W3MM_SetUnityGameObjectName: function (goPtr) {
		try {
			var goName = UTF8ToString(goPtr);
			if (!window || !window.MetaMaskBridge || !window.MetaMaskBridge.setUnityGameObjectName) {
				throw new Error('MetaMaskBridge.setUnityGameObjectName not found');
			}
			window.MetaMaskBridge.setUnityGameObjectName(goName);
		} catch (e) { __W3MM_emit('emitRequestError', e); }
	}, 
	
	// Internal emit method (library-local helper)
	$__W3MM_emit: function(methodName, err) {
		try {
			var bridge = (typeof window !== 'undefined' && window.MetaMaskBridge) ? window.MetaMaskBridge : null;
			if (!bridge) return;
			var msg = (err && err.message) ? err.message : err;
			if (typeof bridge[methodName] === 'function') {
				bridge[methodName](msg);
			}
		} catch (e) {}
	}
});


