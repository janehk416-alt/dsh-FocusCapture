import test from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { apply } from '../index.js';
import { bundledRuntimeOrNull, isolatedDshHome } from './runtime-testkit.js';
import { shortcutSettingsPath } from '../settings.js';

const pause = ms => new Promise(resolve => setTimeout(resolve, ms));

test('plugin-page shortcut update persists and drives the real custom worker', { skip: process.platform !== 'win32' || !bundledRuntimeOrNull() }, async t => {
  const { runtime } = isolatedDshHome(t);
  let route, cleanup;
  apply({ effect(setup) { cleanup = setup(); }, connection: { fetch: { register(value) { route = value; return async () => {}; } } } });
  const status = async () => await (await route.fetch(new Request('http://127.0.0.1:8625/api/dual-ctrl-snip'))).json();
  const update = hotkey => route.fetch(new Request('http://127.0.0.1:8625/api/dual-ctrl-snip', {
    method: 'POST', headers: { origin: 'http://127.0.0.1:8625' }, body: JSON.stringify({ clientId: 'config-test', action: 'set-hotkey', hotkey }),
  }));
  const waitReady = async () => {
    let current;
    for (let i = 0; i < 60; i++) { current = await status(); if (current.ready) return current; await pause(100); }
    assert.fail(JSON.stringify(current));
  };
  try {
    let current = await waitReady();
    assert.equal(current.hotkeyId, 'left-ctrl+right-ctrl');
    assert.equal(current.hotkey, 'LeftCtrl+RightCtrl');
    assert.equal((await update('ctrl+enter')).status, 409);
    const response = await update('ctrl+alt+s');
    assert.equal(response.status, 200, await response.clone().text());
    current = await response.json(); assert.equal(current.hotkeyId, 'ctrl+alt+s');
    current = await waitReady(); assert.equal(current.hotkey, 'Ctrl+Alt+S');
    assert.equal(JSON.parse(readFileSync(shortcutSettingsPath(), 'utf8')).hotkey, 'ctrl+alt+s');

    const code = 'import ctypes,time;u=ctypes.WinDLL("user32");u.keybd_event(162,29,0,0);u.keybd_event(164,56,0,0);u.keybd_event(83,0,0,0);time.sleep(.25);u.keybd_event(83,0,2,0);u.keybd_event(164,56,2,0);u.keybd_event(162,29,2,0)';
    const driver = spawn(runtime.executable, ['-c', code], { windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
    let error = ''; driver.stderr.on('data', chunk => { error += chunk; });
    const exit = await new Promise((resolve, reject) => { driver.once('error', reject); driver.once('exit', resolve); });
    assert.equal(exit, 0, error);
    for (let i = 0; i < 25; i++) { current = await status(); if (current.globalTriggers > 0) break; await pause(60); }
    assert.equal(current.globalTriggers, 1, JSON.stringify(current));
    assert.equal(current.capturing, false);

    assert.equal((await update('left-ctrl+right-ctrl')).status, 200);
    current = await waitReady(); assert.equal(current.hotkeyId, 'left-ctrl+right-ctrl');
    assert.equal(JSON.parse(readFileSync(shortcutSettingsPath(), 'utf8')).hotkey, 'left-ctrl+right-ctrl');
  } finally { await cleanup(); }
});
