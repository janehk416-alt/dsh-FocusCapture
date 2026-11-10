import test from 'node:test';
import assert from 'node:assert/strict';
import { DEFAULT_HOTKEY, hotkeyDisplay, hotkeyWorkerSpec, normalizeHotkey } from '../hotkey.js';

test('dual Ctrl remains the exact default', () => {
  assert.equal(normalizeHotkey(DEFAULT_HOTKEY), DEFAULT_HOTKEY);
  assert.equal(hotkeyDisplay(DEFAULT_HOTKEY), 'LeftCtrl+RightCtrl');
  assert.deepEqual(hotkeyWorkerSpec(DEFAULT_HOTKEY).keys.map(key => key.vk), [[162], [163]]);
});

test('custom shortcuts normalize modifier order and key case', () => {
  assert.equal(normalizeHotkey(' Shift + Ctrl + f5 '), 'ctrl+shift+f5');
  assert.equal(hotkeyDisplay('ctrl+shift+f5'), 'Ctrl+Shift+F5');
  const spec = hotkeyWorkerSpec('alt+s');
  assert.deepEqual(spec.keys.map(key => key.vk), [[164, 165], [83]]);
});

test('Ctrl, Alt and Shift accept either left or right physical modifier', () => {
  const spec = hotkeyWorkerSpec('ctrl+alt+shift+0');
  assert.deepEqual(spec.keys[0].vk, [162, 163]);
  assert.deepEqual(spec.keys[1].vk, [164, 165]);
  assert.deepEqual(spec.keys[2].vk, [160, 161]);
  assert.deepEqual(spec.keys[3].vk, [48]);
});

test('invalid or unsafe shortcut shapes are rejected', () => {
  for (const value of ['', 'ctrl', 's', 'ctrl+ctrl+s', 'win+s', 'ctrl+enter', 'ctrl+f13', null, 42]) {
    assert.throws(() => normalizeHotkey(value), String(value));
  }
});
