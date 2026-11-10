import { mkdtempSync, mkdirSync, rmSync, symlinkSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { keyboardPythonFromFile } from '../runtime.js';

const configUrl = new URL('../native/keyboard-runtime.json', import.meta.url);

export function bundledRuntimeOrNull() {
  try {
    const runtime = keyboardPythonFromFile(configUrl);
    return runtime.source === 'dsh-primary-runtime' ? runtime : null;
  } catch { return null; }
}

export function isolatedDshHome(t) {
  const runtime = bundledRuntimeOrNull();
  if (!runtime) throw new Error('DSH primary runtime is required for this integration test');
  const root = mkdtempSync(join(tmpdir(), 'FocusCapture Host 验收-'));
  const home = join(root, '自定义 DSH Home');
  mkdirSync(join(home, 'dsh-runtimes'), { recursive: true });
  symlinkSync(runtime.runtimeRoot, join(home, 'dsh-runtimes', 'dsh-primary-runtime'), 'junction');
  const previous = process.env.DSH_HOME;
  process.env.DSH_HOME = home;
  t.after(() => {
    if (previous === void 0) delete process.env.DSH_HOME; else process.env.DSH_HOME = previous;
    rmSync(root, { recursive: true, force: true });
  });
  return { home, runtime };
}
