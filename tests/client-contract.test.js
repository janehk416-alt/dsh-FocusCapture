import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

test('client injects the conversation service used for screenshot attachment drafts', () => {
  const source = readFileSync(new URL('../client.source.js', import.meta.url), 'utf8');
  assert.match(source, /inject:\s*\['slots','locale','conversation'\]/);
});
