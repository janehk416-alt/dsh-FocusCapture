import test from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { apply } from '../index.js';
import { keyboardPythonFromFile } from '../runtime.js';
import { bundledRuntimeOrNull, isolatedDshHome } from './runtime-testkit.js';
const pause=ms=>new Promise(resolve=>setTimeout(resolve,ms));

test('real OS dual-Ctrl events reach the Node Host, without opening a capture when unbound',{skip:process.platform!=='win32'||!bundledRuntimeOrNull()},async t=>{
  isolatedDshHome(t);
  let route,cleanup;
  apply({effect(setup){cleanup=setup();},connection:{fetch:{register(value){route=value;return async()=>{};}}}});
  const status=async()=>await(await route.fetch(new Request('http://127.0.0.1:8625/api/dual-ctrl-snip'))).json();
  try{
    let current;
    for(let i=0;i<50;i++){current=await status();if(current.ready)break;await pause(100);}
    assert.equal(current.ready,true,JSON.stringify(current));assert.equal(current.targetSession,null);
    const runtime=keyboardPythonFromFile(new URL('../native/keyboard-runtime.json',import.meta.url));
    const code='import ctypes,time;u=ctypes.WinDLL("user32");u.keybd_event(162,29,0,0);u.keybd_event(163,29,1,0);time.sleep(.2);u.keybd_event(162,29,2,0);u.keybd_event(163,29,3,0)';
    const process=spawn(runtime.executable,['-c',code],{windowsHide:true,stdio:['ignore','pipe','pipe']});
    let error='';process.stderr.on('data',x=>{error+=x;});
    const exit=await new Promise((resolve,reject)=>{process.once('error',reject);process.once('exit',resolve);});assert.equal(exit,0,error);
    for(let i=0;i<20;i++){current=await status();if(current.globalTriggers>0)break;await pause(50);}
    assert.equal(current.globalTriggers,1,JSON.stringify(current));assert.equal(current.capturing,false);assert.equal(current.pending,0);
  }finally{await cleanup();}
});
