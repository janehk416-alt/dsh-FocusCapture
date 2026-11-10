import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { DEFAULT_HOTKEY } from '../hotkey.js';
import { loadHotkey, saveHotkey, shortcutSettingsPath } from '../settings.js';

function fixture(t) {
  const root = mkdtempSync(join(tmpdir(), 'FocusCapture 设置-'));
  t.after(() => rmSync(root, { recursive: true, force: true }));
  return { env: { DSH_HOME: join(root, '自定义 DSH Home') }, home: root };
}

test('missing settings use dual Ctrl without creating files', t => {
  const options = fixture(t);
  assert.equal(loadHotkey(options), DEFAULT_HOTKEY);
});

test('custom shortcut persists under the current DSH_HOME and survives reload', t => {
  const options = fixture(t);
  assert.equal(saveHotkey('alt+shift+p', options), 'alt+shift+p');
  assert.equal(loadHotkey(options), 'alt+shift+p');
  const saved = JSON.parse(readFileSync(shortcutSettingsPath(options), 'utf8'));
  assert.deepEqual(saved, { version: 1, hotkey: 'alt+shift+p' });
  assert.equal(saveHotkey(DEFAULT_HOTKEY, options), DEFAULT_HOTKEY);
  assert.equal(loadHotkey(options), DEFAULT_HOTKEY);
});

test('invalid settings produce a recovery instruction and do not leak paths', t => {
  const options = fixture(t);
  saveHotkey(DEFAULT_HOTKEY, options);
  writeFileSync(shortcutSettingsPath(options), '{bad json', 'utf8');
  assert.throws(() => loadHotkey(options), error => {
    assert.match(error.message, /plugin configuration page/);
    assert.ok(!error.message.includes(options.env.DSH_HOME));
    return true;
  });
});

test('invalid new shortcuts are refused before writing', t => {
  const options = fixture(t);
  saveHotkey(DEFAULT_HOTKEY, options);
  assert.throws(() => saveHotkey('ctrl+enter', options));
  assert.equal(loadHotkey(options), DEFAULT_HOTKEY);
});
