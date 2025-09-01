#!/usr/bin/env node

/**
 * Release automation for Web3-MetaMask-Unity.
 * Steps:
 * 1) Bump version in Assets/com.gamenator.web3-metamask-unity/package.json
 * 2) Build bridge (web3-metamask-bridge)
 * 3) Create payload (gzip+base64+zip)
 * 4) Copy dist/web3-metamask-bridge.js to Assets/WebGLTemplates/.../src/web3-metamask/web3-metamask-bridge.js
 * 5) Copy dist/bridge-payload.zip to Assets/com.gamenator.web3-metamask-unity/Editor/Embedded/bridge-payload.zip
 * 6) Invoke Unity batchmode to export Minimal Sample unitypackage
 */

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const projectRoot = path.resolve(__dirname, '..');
const unityPackageJsonPath = path.join(projectRoot, 'Assets', 'com.gamenator.web3-metamask-unity', 'package.json');
const bridgeRoot = path.join(projectRoot, 'web3-metamask-bridge');
const bridgeDist = path.join(bridgeRoot, 'dist');
const sampleBridgeJsTarget = path.join(projectRoot, 'Assets', 'WebGLTemplates', 'Web3MetaMaskSampleMinimal', 'src', 'web3-metamask', 'web3-metamask-bridge.js');
const payloadTarget = path.join(projectRoot, 'Assets', 'com.gamenator.web3-metamask-unity', 'Editor', 'Embedded', 'bridge-payload.zip');

function run(cmd, opts = {}) {
  console.log(`$ ${cmd}`);
  execSync(cmd, { stdio: 'inherit', ...opts });
}

function runNpm(cmd, nodePath, opts = {}) {
  // Use npm through the provided node path by setting PATH
  if (nodePath) {
    const nodeDir = path.dirname(nodePath);
    const npmPath = path.join(nodeDir, 'npm');
    
    // Set PATH to include the directory where node is located
    const env = { ...process.env };
    env.PATH = `${nodeDir}:${env.PATH || ''}`;
    
    console.log(`$ ${cmd} (using PATH: ${nodeDir})`);
    execSync(cmd, { stdio: 'inherit', env, ...opts });
  } else {
    console.log(`$ ${cmd}`);
    execSync(cmd, { stdio: 'inherit', ...opts });
  }
}

function runCapture(cmd, opts = {}) {
  return execSync(cmd, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], ...opts }).trim();
}

function gitCurrentBranch() {
  try { return runCapture('git rev-parse --abbrev-ref HEAD'); } catch { return null; }
}

function ensureNoUncommittedChanges() {
  const status = runCapture('git status --porcelain');
  if (status) {
    throw new Error('Working tree has uncommitted changes. Please commit or stash before running release.');
  }
}

function gitCheckout(branch) {
  run(`git checkout ${branch}`);
}

function gitAddAllAndCommit(message) {
  run('git add -A');
  run(`git commit -m "${message}"`);
}

function gitMergeSquashFrom(branch) {
  run(`git merge --squash ${branch}`);
}

function gitBranchExistsLocal(branch) {
  try {
    runCapture(`git show-ref --verify --quiet refs/heads/${branch}`);
    return true;
  } catch {
    return false;
  }
}

function gitRemoteBranchExists(branch) {
  try {
    const out = runCapture(`git ls-remote --heads origin ${branch}`);
    return !!out;
  } catch {
    return false;
  }
}

function normalizeBranch(input) {
  if (!input) return input;
  return input.startsWith('origin/') ? input.slice('origin/'.length) : input;
}

function gitCheckoutOrTrack(branch) {
  const b = normalizeBranch(branch);
  const currentBranch = gitCurrentBranch();
  
  // If we're already on the target branch, do nothing
  if (currentBranch === b) {
    console.log(`Already on branch: ${b}`);
    return;
  }
  
  if (gitBranchExistsLocal(b)) {
    gitCheckout(b);
    return;
  }
  if (gitRemoteBranchExists(b)) {
    // Create local branch tracking remote
    run(`git fetch origin ${b}:${b}`);
    gitCheckout(b);
    return;
  }
  throw new Error(`Branch not found locally or on origin: ${b}`);
}

function gitTagExists(tag) {
  try {
    runCapture(`git rev-parse --verify refs/tags/${tag}`);
    return true;
  } catch {
    return false;
  }
}

function gitDeleteLocalTag(tag) {
  run(`git tag -d ${tag}`);
}

function gitCreateAnnotatedTag(tag) {
  run(`git tag -a ${tag} -m "${tag}"`);
}

function gitPushMainAndTag(tag) {
  run('git push origin main');
  run(`git push origin ${tag}`);
}

function bumpVersion(newVersion) {
  const pkg = JSON.parse(fs.readFileSync(unityPackageJsonPath, 'utf8'));
  const oldVersion = pkg.version;
  if (!newVersion) {
    throw new Error('Provide version: --version x.y.z');
  }
  pkg.version = newVersion;
  fs.writeFileSync(unityPackageJsonPath, JSON.stringify(pkg, null, 2) + '\n');
  console.log(`Version bumped: ${oldVersion} -> ${newVersion}`);
}

function buildBridge(npmPathOverride, nodePath) {
  const npmPath = resolveNpmPath(npmPathOverride);
  console.log(`Using npm at: ${npmPath}`);
  
  if (nodePath) {
    // Use npm through the provided node path
    runNpm('npm ci', nodePath, { cwd: bridgeRoot });
    runNpm('npm run package:zip', nodePath, { cwd: bridgeRoot });
  } else {
    // Fallback to direct npm commands
    run(`${npmPath} ci`, { cwd: bridgeRoot });
    run(`${npmPath} run package:zip`, { cwd: bridgeRoot });
  }
}

function copyArtifacts() {
  const bridgeJs = path.join(bridgeDist, 'web3-metamask-bridge.js');
  const payloadZip = path.join(bridgeDist, 'bridge-payload.zip');
  if (!fs.existsSync(bridgeJs)) throw new Error(`Missing ${bridgeJs}`);
  if (!fs.existsSync(payloadZip)) throw new Error(`Missing ${payloadZip}`);

  fs.mkdirSync(path.dirname(sampleBridgeJsTarget), { recursive: true });
  fs.copyFileSync(bridgeJs, sampleBridgeJsTarget);
  console.log(`Copied bridge js -> ${sampleBridgeJsTarget}`);

  fs.mkdirSync(path.dirname(payloadTarget), { recursive: true });
  fs.copyFileSync(payloadZip, payloadTarget);
  console.log(`Copied payload zip -> ${payloadTarget}`);
}

function readProjectEditorVersion() {
  const verFile = path.join(projectRoot, 'ProjectSettings', 'ProjectVersion.txt');
  if (!fs.existsSync(verFile)) return null;
  const text = fs.readFileSync(verFile, 'utf8');
  const m = text.match(/m_EditorVersion:\s*([^\n\r]+)/);
  return m ? m[1].trim() : null;
}

function resolveUnityPath(cliUnityPath) {
  if (cliUnityPath && fs.existsSync(cliUnityPath)) return cliUnityPath;
  if (process.env.UNITY_PATH && fs.existsSync(process.env.UNITY_PATH)) return process.env.UNITY_PATH;
  // Try Hub default based on ProjectVersion.txt
  const ver = readProjectEditorVersion();
  if (ver) {
    const hubPath = `/Applications/Unity/Hub/Editor/${ver}/Unity.app/Contents/MacOS/Unity`;
    if (fs.existsSync(hubPath)) return hubPath;
  }
  // Fallback to 'Unity' on PATH
  return 'Unity';
}

function resolveNpmPath(overridePath) {
  // Use override if provided
  if (overridePath && fs.existsSync(overridePath)) {
    return overridePath;
  }

  // Check if npm is available in PATH
  try {
    const npmVersion = runCapture('npm --version');
    if (npmVersion) {
      return 'npm';
    }
  } catch {}

  // Common macOS locations for npm
  const nodePaths = [
    '/opt/homebrew/bin',
    '/usr/local/bin',
    '/usr/bin',
    process.env.PATH?.split(':').filter(Boolean) || []
  ].flat();

  for (const nodePath of nodePaths) {
    const npmPath = path.join(nodePath, 'npm');
    if (fs.existsSync(npmPath)) {
      return npmPath;
    }
  }

  // Try to find npm relative to node
  try {
    const nodePath = runCapture('which node');
    if (nodePath) {
      const npmPath = path.join(path.dirname(nodePath), 'npm');
      if (fs.existsSync(npmPath)) {
        return npmPath;
      }
    }
  } catch {}

  throw new Error('npm not found. Please ensure Node.js is installed and accessible.');
}

function exportUnityPackage(cliUnityPath) {
  const unity = resolveUnityPath(cliUnityPath);
  const projectPath = projectRoot;
  const method = 'Gamenator.Web3.MetaMaskUnity.Editor.SampleExporter.ExportMinimalSampleCli';
  const args = [
    `"${unity}"`,
    '-quit',
    '-batchmode',
    '-nographics',
    `-projectPath "${projectPath}"`,
    `-executeMethod ${method}`
  ].join(' ');
  run(args);
}

function parseArgs() {
  const out = {};
  for (let i = 2; i < process.argv.length; i++) {
    const a = process.argv[i];
    if (a === '--version' || a === '-v') {
      out.version = process.argv[++i];
    } else if (a === '--skip-unity') {
      out.skipUnity = true;
    } else if (a === '--skip-bridge') {
      out.skipBridge = true;
    } else if (a === '--unity') {
      out.unityPath = process.argv[++i];
    } else if (a === '--npm') {
      out.npmPath = process.argv[++i];
    } else if (a === '--node') {
      out.nodePath = process.argv[++i];
    } else if (a === '--branch' || a === '-b') {
      out.branch = process.argv[++i];
    } else if (a === '--no-git') {
      out.noGit = true;
    }
  }
  return out;
}

async function main() {
  const { version, skipUnity, skipBridge, unityPath, npmPath, nodePath, branch, noGit } = parseArgs();
  if (!version) throw new Error('Missing --version');

  if (!noGit) {
    ensureNoUncommittedChanges();
    if (!branch) throw new Error('Missing --branch <feature-branch>');
    gitCheckoutOrTrack(branch);
  }

  bumpVersion(version);
  if (!skipBridge) {
    buildBridge(npmPath, nodePath);
    copyArtifacts();
  } else {
    console.log('Skipping bridge build and artifact copy.');
  }
  if (!skipUnity) {
    exportUnityPackage(unityPath);
  }

  if (!noGit) {
    gitAddAllAndCommit(`chore: prepare bridge artifacts for release ${version}`);
    gitCheckout('main');
    run('git pull --ff-only');
    gitMergeSquashFrom(branch);
    gitAddAllAndCommit(`release: ${version}`);
    if (gitTagExists(version)) {
      gitDeleteLocalTag(version);
    }
    gitCreateAnnotatedTag(version);
    gitPushMainAndTag(version);
  }

  console.log('Release steps completed.');
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});


