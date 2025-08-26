import { defineConfig } from 'vite'

// Build a single IIFE bundle for browser injection into Unity WebGL template
export default defineConfig({
  build: {
    target: 'es2019',
    lib: {
      entry: './src/index.js',
      name: 'Web3MetaMaskUnityBundle',
      formats: ['iife'],
      fileName: () => 'web3-metamask-bridge.js'
    },
    sourcemap: false,
    minify: true,
    rollupOptions: {
      output: {
        inlineDynamicImports: true,
        banner: '// Web3 MetaMask Bridge for Unity WebGL — built via Vite\n'
      }
    }
  },
  define: {
    'process.env': {},
    __BUILD_TARGET__: JSON.stringify('unity-webgl')
  }
})


