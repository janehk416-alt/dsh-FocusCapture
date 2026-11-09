import test from 'node:test';
import assert from 'node:assert/strict';
import { apply } from '../index.js';

test('authenticated route registration, origin checks, Session binding and unload', {skip:process.platform !== 'win32'}, async () => {
  let route, cleanup, unregistered=false;
  apply({ effect(setup){cleanup=setup();},connection:{fetch:{register(value){route=value;return async()=>{unregistered=true;};}}} });
  try {
    assert.equal(route.path,'/api/dual-ctrl-snip');assert.deepEqual(route.methods,['GET','POST']);
    const request=(body,origin='http://127.0.0.1:8625') => route.fetch(new Request('http://127.0.0.1:8625/api/dual-ctrl-snip',{method:'POST',headers:{origin},body:JSON.stringify(body)}));
    assert.equal((await request({clientId:'x',action:'bind'},'https://other.example')).status,403);
    assert.equal((await request({clientId:'../bad',action:'bind'})).status,400);
    assert.equal((await request({clientId:'x',sessionId:'s',focused:true,action:'bind'})).status,200);
    await new Promise(resolve=>setTimeout(resolve,1100));
    const response=await route.fetch(new Request('http://127.0.0.1:8625/api/dual-ctrl-snip'));
    const status=await response.json(); assert.equal(status.ready,true);assert.equal(status.keyboardReady,true);assert.equal(status.nativeVersion,'1.3.0');assert.equal(status.armed,true);assert.equal(status.targetSession,'s');
    assert.equal((await request({clientId:'x',action:'unsupported'})).status,400);
    await request({clientId:'x',action:'detach'});
    const detached=await (await route.fetch(new Request('http://127.0.0.1:8625/api/dual-ctrl-snip'))).json();assert.equal(detached.armed,false);
  } finally { await cleanup(); }
  assert.equal(unregistered,true);
});
