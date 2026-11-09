import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';

test('composer has only an SVG capture button, without visible label, status or tooltip text',()=>{
  const client=readFileSync(new URL('../client.js',import.meta.url),'utf8');
  assert.ok(client.includes("h('svg'"));
  assert.ok(client.includes("'aria-label':t('button')"));
  assert.ok(!client.includes('snip-status'));
  assert.ok(!client.includes("role:'status'"));
  assert.ok(!client.includes("title:t('title')"));
  assert.ok(!client.includes(")),t('button'))"));
});
test('native editor exposes mosaic, topmost pin, save, copy, redo, but no sequence or description editor',()=>{
  const ui=readFileSync(new URL('../native/EditorUi.cs',import.meta.url),'utf8');
  assert.ok(ui.includes('"mosaic"'));assert.ok(ui.includes('"pin"'));assert.ok(ui.includes('"save"'));assert.ok(ui.includes('"copy"'));assert.ok(ui.includes('"redo"'));
  assert.ok(!ui.includes('new TextBox'));assert.ok(!ui.includes('sequence'));
  const pin=readFileSync(new URL('../native/PinnedShot.cs',import.meta.url),'utf8');
  assert.ok(pin.includes('TopMost=true'));assert.ok(pin.includes('new IntPtr(-1)'));assert.ok(pin.includes('0x0010'));
});
test('FocusCapture name is identical in both display locales',()=>{
  for(const locale of ['zh','en']){const data=JSON.parse(readFileSync(new URL(`../locale/${locale}.json`,import.meta.url),'utf8'));assert.equal(data.meta.title,'dsh-FocusCapture');}
  const manifest=JSON.parse(readFileSync(new URL('../package.json',import.meta.url),'utf8'));assert.equal(manifest.displayName,'dsh-FocusCapture');
});
test('attachment is a Return icon after Copy, with no footer button and no white input frame',()=>{
  const ui=readFileSync(new URL('../native/EditorUi.cs',import.meta.url),'utf8');assert.ok(ui.includes('"copy","attach"'));assert.ok(ui.includes('添加到对话'));assert.ok(!ui.includes('done=new ToolButton'));assert.ok(ui.includes('DrawTool.Select?44:124'));
  const region=readFileSync(new URL('../native/RegionEditor.cs',import.meta.url),'utf8');assert.ok(region.includes('new ScreenshotTextBox(background)'));assert.ok(!region.includes('textEditor.BackColor=Color.White'));
  const input=readFileSync(new URL('../native/ScreenshotTextBox.cs',import.meta.url),'utf8');assert.ok(input.includes('BorderStyle=BorderStyle.None'));assert.ok(input.includes('CreatePatternBrush'));assert.ok(input.includes('ImeMode=ImeMode.On'));
});
