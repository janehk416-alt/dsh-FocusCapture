import test from 'node:test';
import assert from 'node:assert/strict';
import { attachScreenshot } from '../delivery.js';
const image={data:'aGVsbG8=',name:'screenshot-test.png'};
test('real browser File is admitted to the exact Session and does not submit or modify text',() => {
  const files=[];let attachments=[];
  const conversation={createDrafts(sessionId,items,upload){assert.equal(sessionId,'session-1');assert.equal(upload,false);files.push(...items);return [{id:'image-draft'}];},releaseDraftAttachments(){assert.fail('must not release admitted draft');}};
  const input={addAttachments(ids){attachments=ids;return true;},submit(){assert.fail('must never send automatically');},setDraft(){assert.fail('must preserve draft text');}};
  attachScreenshot(conversation,input,'session-1',image);
  assert.deepEqual(attachments,['image-draft']);assert.equal(files[0].type,'image/png');assert.equal(files[0].name,image.name);assert.equal(files[0].size,5);
});
test('busy composer releases allocated drafts, leaves screenshot pending for retry',() => {let released;const c={createDrafts(){return[{id:'draft'}];},releaseDraftAttachments(items){released=items;}};assert.throws(()=>attachScreenshot(c,{addAttachments(){return false;}},'s',image),/busy/);assert.equal(released[0].id,'draft');});
test('throwing admission releases allocated drafts',() => {let released=false;const c={createDrafts(){return[{id:'draft'}];},releaseDraftAttachments(){released=true;}};assert.throws(()=>attachScreenshot(c,{addAttachments(){throw new Error('denied');}},'s',image),/denied/);assert.equal(released,true);});
test('missing attachment service fails loudly rather than inserting a path',() => {assert.throws(()=>attachScreenshot(null,{},'s',image),/unavailable/);});
