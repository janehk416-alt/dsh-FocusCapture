import { mkdirSync, readFileSync, renameSync, rmSync, writeFileSync } from 'node:fs';
import { randomUUID } from 'node:crypto';
import { dirname, join } from 'node:path';
import { DEFAULT_HOTKEY, normalizeHotkey } from './hotkey.js';
import { resolveDshHome } from './runtime.js';

export function shortcutSettingsPath(options = {}) {
  return join(resolveDshHome(options.env, options.home), 'focuscapture', 'settings.json');
}

export function loadHotkey(options = {}) {
  const path = shortcutSettingsPath(options);
  let parsed;
  try { parsed = JSON.parse(readFileSync(path, 'utf8')); }
  catch (error) {
    if (error?.code === 'ENOENT') return DEFAULT_HOTKEY;
    throw new Error('Shortcut settings could not be read. Restore the default shortcut from the FocusCapture plugin configuration page.');
  }
  try { return normalizeHotkey(parsed?.hotkey ?? DEFAULT_HOTKEY); }
  catch { throw new Error('Shortcut settings are invalid. Save a new shortcut from the FocusCapture plugin configuration page.'); }
}

export function saveHotkey(value, options = {}) {
  const hotkey = normalizeHotkey(value);
  const path = shortcutSettingsPath(options);
  try {
    mkdirSync(dirname(path), { recursive: true });
    const temporary = `${path}.${process.pid}.${randomUUID()}.tmp`;
    writeFileSync(temporary, JSON.stringify({ version: 1, hotkey }) + '\n', { encoding: 'utf8', flag: 'wx' });
    try { renameSync(temporary, path); } catch (error) { try { rmSync(temporary, { force: true }); } catch {} throw error; }
    return hotkey;
  } catch { throw new Error('Shortcut settings could not be saved under $DSH_HOME/focuscapture. Check directory permissions and retry.'); }
}
