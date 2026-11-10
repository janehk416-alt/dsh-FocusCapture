import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdirSync, writeFileSync } from 'node:fs';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { resolveDshHome, resolveKeyboardPython } from '../runtime.js';

function fixture(t, { platform = 'win32', arch = process.arch } = {}) {
  const root = mkdtempSync(join(tmpdir(), 'FocusCapture 运行时-'));
  t.after(() => rmSync(root, { recursive: true, force: true }));
  const home = join(root, '自定义 DSH Home');
  const runtime = join(home, 'dsh-runtimes', 'dsh-primary-runtime');
  const python = join(runtime, 'dependencies', 'python', 'python.exe');
  mkdirSync(join(python, '..'), { recursive: true });
  writeFileSync(join(runtime, 'runtime.json'), JSON.stringify({ platform, arch, python: '3.12.0' }));
  writeFileSync(python, 'test fixture');
  return { root, home, runtime, python };
}

test('discovers DSH Python under a custom DSH_HOME with spaces and Chinese characters', t => {
  const { root, home, python } = fixture(t);
  const result = resolveKeyboardPython({ env: { DSH_HOME: home, PATH: 'C:/must-not-be-used' }, home: root });
  assert.equal(result.executable, python);
  assert.equal(result.source, 'dsh-primary-runtime');
  assert.equal(result.dshHome, home);
});

test('blank DSH_HOME uses the current account home, never a developer username', t => {
  const { home, python } = fixture(t);
  const accountHome = join(home, '..', '普通 用户');
  const defaultHome = join(accountHome, '.dsh');
  const runtime = join(defaultHome, 'dsh-runtimes', 'dsh-primary-runtime');
  mkdirSync(join(runtime, 'dependencies', 'python'), { recursive: true });
  writeFileSync(join(runtime, 'runtime.json'), JSON.stringify({ platform: 'win32', arch: process.arch, python: '3.12.0' }));
  const expected = join(runtime, 'dependencies', 'python', 'python.exe');
  writeFileSync(expected, 'fixture');
  const result = resolveKeyboardPython({ env: { DSH_HOME: '   ' }, home: accountHome });
  assert.equal(result.executable, expected);
  assert.notEqual(result.executable, python);
  assert.equal(result.dshHome, defaultHome);
  assert.ok(result.executable.includes('普通 用户'));
});

test('DSH_HOME tilde expansion matches official home-path behavior', t => {
  const { home } = fixture(t);
  assert.equal(resolveDshHome({ DSH_HOME: '~/资料/DSH Home' }, home), join(home, '资料', 'DSH Home'));
  assert.equal(resolveDshHome({}, home), join(home, '.dsh'));
});

test('explicit Python override works without a runtime installation', t => {
  const { root } = fixture(t);
  const executable = join(root, '自定义 Python', 'python.exe');
  mkdirSync(join(executable, '..'), { recursive: true });
  writeFileSync(executable, 'fixture');
  const result = resolveKeyboardPython({ env: {}, home: root, config: { pythonExecutable: executable } });
  assert.equal(result.executable, executable);
  assert.equal(result.source, 'explicit-override');
});

test('relative or non-Python overrides are rejected', () => {
  assert.throws(() => resolveKeyboardPython({ config: { pythonExecutable: 'python.exe' } }), /absolute/);
  assert.throws(() => resolveKeyboardPython({ config: { pythonExecutable: 'C:/tools/not-python.exe' } }), /python\.exe/);
  assert.throws(() => resolveKeyboardPython({ config: { pythonExecutable: 42 } }), /Invalid/);
});

test('missing override reports an actionable error without leaking its local path', t => {
  const { root } = fixture(t);
  const missing = join(root, 'Administrator 私密目录', 'python.exe');
  assert.throws(() => resolveKeyboardPython({ config: { pythonExecutable: missing } }), error => {
    assert.match(error.message, /Correct or clear/);
    assert.ok(!error.message.includes('Administrator'));
    assert.ok(!error.message.includes(missing));
    return true;
  });
});

test('missing runtime metadata fails clearly instead of falling back to PATH', t => {
  const { root, home } = fixture(t);
  rmSync(join(home, 'dsh-runtimes'), { recursive: true, force: true });
  assert.throws(() => resolveKeyboardPython({ env: { DSH_HOME: home, PATH: root }, home: root }), /repair its bundled runtime/);
});

test('incompatible runtime architecture is rejected', t => {
  const { home } = fixture(t, { arch: 'arm64' });
  assert.throws(() => resolveKeyboardPython({ env: { DSH_HOME: home }, arch: 'x64' }), /incompatible/);
});

test('malformed runtime metadata and malformed override config are actionable', t => {
  const { home, runtime } = fixture(t);
  writeFileSync(join(runtime, 'runtime.json'), '{bad json');
  assert.throws(() => resolveKeyboardPython({ env: { DSH_HOME: home } }), /unavailable or invalid/);
});
