export const DEFAULT_HOTKEY = 'left-ctrl+right-ctrl';
const MODIFIERS = ['ctrl', 'alt', 'shift'];
const MODIFIER_LABELS = { ctrl: 'Ctrl', alt: 'Alt', shift: 'Shift' };

export function normalizeHotkey(value) {
  if (typeof value !== 'string') throw new Error('Shortcut must be a string.');
  const compact = value.toLowerCase().replace(/\s+/g, '');
  if (compact === DEFAULT_HOTKEY) return DEFAULT_HOTKEY;
  const parts = compact.split('+').filter(Boolean);
  if (parts.length < 2) throw new Error('Choose at least one modifier and one letter, digit, or F1–F12 key.');
  const key = parts.at(-1), modifiers = parts.slice(0, -1);
  if (modifiers.some(part => !MODIFIERS.includes(part)) || new Set(modifiers).size !== modifiers.length) {
    throw new Error('Shortcut modifiers may only contain Ctrl, Alt, and Shift once each.');
  }
  if (!/^[a-z0-9]$/.test(key) && !/^f(?:[1-9]|1[0-2])$/.test(key)) {
    throw new Error('The final shortcut key must be A–Z, 0–9, or F1–F12.');
  }
  return [...MODIFIERS.filter(item => modifiers.includes(item)), key].join('+');
}

export function hotkeyDisplay(value) {
  const id = normalizeHotkey(value);
  if (id === DEFAULT_HOTKEY) return 'LeftCtrl+RightCtrl';
  const parts = id.split('+');
  return [...parts.slice(0, -1).map(part => MODIFIER_LABELS[part]), parts.at(-1).toUpperCase()].join('+');
}

function virtualKey(key) {
  if (/^[a-z0-9]$/.test(key)) return key.toUpperCase().charCodeAt(0);
  return 0x70 + Number(key.slice(1)) - 1;
}

export function hotkeyWorkerSpec(value) {
  const id = normalizeHotkey(value);
  if (id === DEFAULT_HOTKEY) return { id, keys: [
    { vk: [0xA2], probe: { vk: 0xA2, scan: 0x1D, flags: 0 } },
    { vk: [0xA3], probe: { vk: 0xA3, scan: 0x1D, flags: 1 } },
  ] };
  const parts = id.split('+');
  const modifiers = {
    ctrl: { vk: [0xA2, 0xA3], probe: { vk: 0xA2, scan: 0x1D, flags: 0 } },
    alt: { vk: [0xA4, 0xA5], probe: { vk: 0xA4, scan: 0x38, flags: 0 } },
    shift: { vk: [0xA0, 0xA1], probe: { vk: 0xA0, scan: 0x2A, flags: 0 } },
  };
  const keys = parts.slice(0, -1).map(part => modifiers[part]);
  const key = parts.at(-1), vk = virtualKey(key);
  keys.push({ vk: [vk], probe: { vk, scan: 0, flags: 0 } });
  return { id, keys };
}
