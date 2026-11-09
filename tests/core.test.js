import test from 'node:test';
import assert from 'node:assert/strict';
import { CaptureState, LEASE_MS, IMAGE_TTL_MS, pngInfo } from '../core.js';
const PNG = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jOaQAAAAASUVORK5CYII=';
const binding = (clientId='page-a',sessionId='session-a',focused=true) => ({clientId,sessionId,focused,locale:'zh'});
function fixture() { let now=1000000; const state=new CaptureState(() => now); state.ready=true; state.bind(binding()); return {state, advance:ms => {now+=ms;}}; }
test('left/right shortcut has the exact intended identity',() => assert.equal(fixture().state.status().hotkey,'LeftCtrl+RightCtrl'));
test('only a focused second page can replace the target',() => { const {state}=fixture();state.bind(binding('page-b','session-b',false)); assert.equal(state.owner.sessionId,'session-a');state.bind(binding('page-b','session-b'));assert.equal(state.owner.sessionId,'session-b'); });
test('capture target is frozen before selection, never rerouted after switching',() => {const {state}=fixture();state.begin();state.bind(binding('page-b','session-b'));const image=state.finish(PNG);assert.equal(image.sessionId,'session-a');assert.equal(state.poll('page-b','session-b').length,0);assert.equal(state.poll('page-a','session-a').length,1);});
test('stale page detach cannot detach a newly active page',() => {const {state}=fixture();state.bind(binding('page-b','session-b'));state.detach('page-a');assert.equal(state.owner.sessionId,'session-b');});
test('no Session, unready helper, wrong page and concurrent captures refuse',() => {const s=new CaptureState();assert.throws(()=>s.begin(),/not ready/);s.ready=true;assert.throws(()=>s.begin(),/activate/);s.bind(binding());assert.throws(()=>s.begin('other'),/activate/);s.begin();assert.throws(()=>s.begin(),/progress/);});
test('a lease expires and disarms capture',() => {const {state,advance}=fixture();advance(LEASE_MS+1);assert.equal(state.status().armed,false);assert.throws(()=>state.begin(),/activate/);});
test('ack is bound to its page, idempotent and records only metadata',() => {const {state}=fixture();state.begin();const image=state.finish(PNG);assert.equal(state.ack('page-b',image.id),false);assert.equal(state.ack('page-a',image.id),true);assert.equal(state.ack('page-a',image.id),false);assert.equal(state.images.length,0);assert.equal('data' in state.status().lastDelivery,false);});
test('pending screenshot memory expires',() => {const {state,advance}=fixture();state.begin();state.finish(PNG);advance(IMAGE_TTL_MS+1);assert.equal(state.poll('page-a','session-a').length,0);});
test('unsolicited and invalid native payloads refuse',() => {const {state}=fixture();assert.throws(()=>state.finish(PNG),/without/);state.begin();assert.throws(()=>state.finish('aGVsbG8='),/Invalid/);assert.throws(()=>pngInfo(PNG.replace('iVBOR','AAAAA')),/Invalid/);});
test('pending queue is bounded to three images',() => {const {state}=fixture();for(let i=0;i<3;i++){state.begin();state.finish(PNG);}assert.throws(()=>state.begin(),/pending/);});
