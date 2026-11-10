import { readFileSync, statSync } from 'node:fs';
import { homedir } from 'node:os';
import { basename, isAbsolute, join, resolve } from 'node:path';

const RUNTIME_SEGMENTS = ['dsh-runtimes', 'dsh-primary-runtime'];

function expandHome(value, home) {
  if (value === '~') return home;
  if (value.startsWith('~/') || value.startsWith('~\\')) return join(home, value.slice(2));
  return value;
}

// Match DSH's official home resolution: non-blank DSH_HOME, then ~/.dsh.
export function resolveDshHome(env = process.env, home = homedir()) {
  const configured = env.DSH_HOME;
  const selected = typeof configured === 'string' && configured.trim().length > 0 ? configured : join(home, '.dsh');
  return resolve(expandHome(selected, home));
}

function isFile(path) {
  try { return statSync(path).isFile(); } catch { return false; }
}

function readJson(path, message) {
  try {
    const value = JSON.parse(readFileSync(path, 'utf8'));
    if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error('not an object');
    return value;
  } catch { throw new Error(message); }
}

export function resolveKeyboardPython({ env = process.env, home = homedir(), config = {}, platform = process.platform, arch = process.arch } = {}) {
  const rawOverride = config?.pythonExecutable;
  if (rawOverride !== void 0 && rawOverride !== null && typeof rawOverride !== 'string') {
    throw new Error('Invalid pythonExecutable override: use an absolute python.exe path or leave it empty.');
  }
  const override = typeof rawOverride === 'string' ? rawOverride.trim() : '';
  if (override) {
    const expanded = expandHome(override, home);
    if (!isAbsolute(expanded) || basename(expanded).toLowerCase() !== 'python.exe') {
      throw new Error('Invalid pythonExecutable override: use an absolute path to python.exe, or leave it empty for the DSH runtime.');
    }
    const executable = resolve(expanded);
    if (!isFile(executable)) throw new Error('The configured pythonExecutable override does not exist. Correct or clear it in native/keyboard-runtime.json.');
    return { executable, source: 'explicit-override', dshHome: null, runtimeRoot: null };
  }

  if (platform !== 'win32') throw new Error('FocusCapture keyboard capture requires Windows 10 or 11.');
  const dshHome = resolveDshHome(env, home);
  const runtimeRoot = join(dshHome, ...RUNTIME_SEGMENTS);
  const manifest = readJson(
    join(runtimeRoot, 'runtime.json'),
    'DSH primary runtime metadata is unavailable or invalid. Reopen DSH and repair its bundled runtime before enabling FocusCapture.',
  );
  if (manifest.platform !== platform || manifest.arch !== arch || typeof manifest.python !== 'string') {
    throw new Error(`DSH primary runtime is incompatible with this ${platform}/${arch} host. Repair the DSH runtime before enabling FocusCapture.`);
  }
  const executable = join(runtimeRoot, 'dependencies', 'python', 'python.exe');
  if (!isFile(executable)) {
    throw new Error('DSH bundled Python was not found under $DSH_HOME/dsh-runtimes/dsh-primary-runtime. Repair the DSH runtime; no system Python or PATH fallback is used.');
  }
  return { executable, source: 'dsh-primary-runtime', dshHome, runtimeRoot };
}

export function keyboardPythonFromFile(configUrl, options = {}) {
  const config = readJson(
    configUrl,
    'native/keyboard-runtime.json is invalid. Restore the default {"pythonExecutable":""} or provide a valid absolute override.',
  );
  return resolveKeyboardPython({ ...options, config });
}
