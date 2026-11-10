import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { createInterface } from 'node:readline';
import { CaptureState } from './core.js';
import { keyboardPythonFromFile } from './runtime.js';
import { DEFAULT_HOTKEY, hotkeyDisplay, hotkeyWorkerSpec, normalizeHotkey } from './hotkey.js';
import { loadHotkey, saveHotkey } from './settings.js';

export const inject = ['connection'];
const PATH='/api/dual-ctrl-snip';
const STARTUP_TIMEOUT_MS=8000;
const MAX_RESTARTS=2;
const validId=value=>typeof value==='string'&&/^[a-zA-Z0-9_-]{1,160}$/.test(value);
const json=(data,status=200)=>new Response(JSON.stringify(data),{status,headers:{'content-type':'application/json; charset=utf-8','cache-control':'no-store'}});
const redact=value=>String(value??'').replace(/[A-Za-z]:[\\/][^\r\n]*/g,'<local path>').replace(/\\\\[^\r\n]+/g,'<network path>').slice(0,500);

export function apply(ctx){
  ctx.effect(()=>{
    const state=new CaptureState(),waiters=new Set(),readers=new Set(),timers=new Set();
    let child,keyboard,disposed=false,armState='',nativeReady=false,keyboardReady=false,nativePid=null,nativeVersion=null,globalTriggers=0;
    let nativeCrashes=0,keyboardCrashes=0,nativeError=null,keyboardError=null,keyboardSource=null,keyboardRestartTimer=null;
    let currentHotkey=DEFAULT_HOTKEY,configurationError=null;
    try{currentHotkey=loadHotkey();}catch(error){configurationError=redact(error?.message??error);state.lastError=configurationError;}
    const wake=()=>{for(const fn of [...waiters])fn();};
    const status=()=>({
      ...state.status(),hotkey:hotkeyDisplay(currentHotkey),hotkeyId:currentHotkey,
      hostLoaded:true,clientBound:Boolean(state.owner),keyboardReady,nativeReady,nativePid,nativeVersion,globalTriggers,
      keyboardBackend:'host-ctrl-worker',keyboardSource,
      diagnostics:{native:nativeError,keyboard:keyboardError,configuration:configurationError},
    });
    function send(command){if(child&&!child.killed&&child.stdin.writable)child.stdin.write(command+'\n',()=>{});}
    function refreshArm(){
      const snapshot=state.status(),next=snapshot.armed?`arm:${state.owner.locale}`:'disarm';
      if(next===armState)return;armState=next;
      if(snapshot.armed){send(state.owner.locale==='en'?'english':'chinese');send('arm');}else send('disarm');
    }
    function refreshReady(){state.ready=nativeReady&&keyboardReady;if(state.ready&&!configurationError)state.lastError=null;refreshArm();wake();}
    function fail(error,component){
      const message=redact(error?.message??error)||'Unknown startup error';
      state.capture=null;state.lastError=message;
      if(component==='native')nativeError=message;else if(component==='keyboard')keyboardError=message;
      wake();
    }
    function begin(clientId){const target=state.begin(clientId);send(target.locale==='en'?'english':'chinese');send('capture');wake();}
    function later(fn){const timer=setTimeout(()=>{timers.delete(timer);fn();},2000);timers.add(timer);}
    function startupTimeout(instance,kind){
      const timer=setTimeout(()=>{
        timers.delete(timer);
        if(disposed||instance.killed)return;
        instance.kill();
        const message=kind==='native'
          ?'Native helper startup timed out. Reload FocusCapture; if this persists, check Windows security software.'
          :'Ctrl worker startup timed out. Verify the DSH bundled Python runtime, then reload FocusCapture.';
        if(kind==='native')endNative(instance,new Error(message));else endKeyboard(instance,new Error(message));
      },STARTUP_TIMEOUT_MS);
      timers.add(timer);return timer;
    }
    function clearTimer(timer){if(timer){clearTimeout(timer);timers.delete(timer);}}
    function laterKeyboard(){
      if(keyboardRestartTimer)clearTimer(keyboardRestartTimer);
      const timer=setTimeout(()=>{timers.delete(timer);if(keyboardRestartTimer===timer)keyboardRestartTimer=null;startKeyboard();},2000);
      keyboardRestartTimer=timer;timers.add(timer);
    }

    function startNative(){
      if(disposed||process.platform!=='win32')return;
      let instance;
      try{instance=spawn(fileURLToPath(new URL('./native/DualCtrlSnip.exe',import.meta.url)),[String(process.pid)],{windowsHide:true,stdio:['pipe','pipe','pipe']});}
      catch(error){nativeReady=false;fail(error,'native');if(!disposed&&++nativeCrashes<=MAX_RESTARTS)later(startNative);return;}
      child=instance;let errorText='';
      const startup=startupTimeout(instance,'native');
      instance.stdin.on('error',error=>{if(!disposed)fail(error,'native');});
      instance.stderr.on('data',chunk=>{errorText=(errorText+chunk).slice(-1500);});
      const lines=createInterface({input:instance.stdout,crlfDelay:Infinity});readers.add(lines);
      lines.on('line',line=>{
        if(disposed||child!==instance)return;
        if(line.length>30*1024*1024){fail('Screenshot response exceeds memory limit','native');return;}
        try{
          const event=JSON.parse(line);
          if(event.kind==='ready'){
            clearTimer(startup);nativeReady=true;nativeError=null;nativePid=event.pid;nativeVersion=event.version??'1.0.0';armState='';refreshReady();
          }else if(event.kind==='image'){state.finish(event.data);wake();}
          else if(event.kind==='cancel'){state.capture=null;wake();}
          else if(event.kind==='error')fail(event.message,'native');
        }catch(error){fail(error,'native');}
      });
      instance.once('error',error=>endNative(instance,error,startup));
      instance.once('exit',code=>endNative(instance,new Error(`Native helper exited (${code}): ${errorText}`),startup));
    }
    function endNative(instance,error,startup){
      if(child!==instance)return;
      clearTimer(startup);child=null;nativeReady=false;nativePid=null;armState='';
      const actual=error?.code==='ENOENT'?new Error('Native helper could not start. Reinstall FocusCapture and check Windows security software.'):error;
      refreshReady();fail(actual,'native');
      if(!disposed&&++nativeCrashes<=MAX_RESTARTS)later(startNative);
    }

    function startKeyboard(){
      if(disposed||process.platform!=='win32'||keyboard)return;
      let instance;
      try{
        const runtime=keyboardPythonFromFile(new URL('./native/keyboard-runtime.json',import.meta.url));
        keyboardSource=runtime.source;
        // Launch directly from the Node Host, NOT from the C# editor: regression-tested input access.
        instance=spawn(runtime.executable,['-u',fileURLToPath(new URL('./native/keyboard-worker.py',import.meta.url)),String(process.pid),JSON.stringify(hotkeyWorkerSpec(currentHotkey))],{windowsHide:true,stdio:['pipe','pipe','pipe']});
      }catch(error){
        keyboardReady=false;fail(error,'keyboard');if(!disposed&&++keyboardCrashes<=MAX_RESTARTS)laterKeyboard();return;
      }
      keyboard=instance;let errorText='';
      const startup=startupTimeout(instance,'keyboard');
      instance.stdin.on('error',error=>{if(!disposed)fail(error,'keyboard');});
      instance.stderr.on('data',chunk=>{errorText=(errorText+chunk).slice(-1000);});
      const lines=createInterface({input:instance.stdout,crlfDelay:Infinity});readers.add(lines);
      lines.on('line',line=>{
        if(disposed||keyboard!==instance)return;
        if(line==='READY'){clearTimer(startup);keyboardReady=true;keyboardError=null;refreshReady();}
        else if(line==='TRIGGER'){
          globalTriggers++;state.expire();
          if(state.owner&&state.ready&&!state.capture){try{begin();}catch(error){fail(error);}}
        }
      });
      instance.once('error',error=>endKeyboard(instance,error,startup));
      instance.once('exit',code=>endKeyboard(instance,new Error(`Ctrl worker exited (${code}): ${errorText}`),startup));
    }
    function endKeyboard(instance,error,startup){
      if(keyboard!==instance)return;
      clearTimer(startup);keyboard=null;keyboardReady=false;
      const actual=error?.code==='ENOENT'?new Error('Ctrl worker could not start. Repair the DSH bundled Python runtime or correct the optional pythonExecutable override.'):error;
      refreshReady();fail(actual,'keyboard');
      if(!disposed&&++keyboardCrashes<=MAX_RESTARTS)laterKeyboard();
    }

    async function closeProcess(instance,isNative){
      if(!instance)return;
      const exited=new Promise(resolve=>instance.once('exit',resolve));
      if(isNative)send('stop');instance.stdin.end();
      let killTimer;
      await Promise.race([exited,new Promise(resolve=>{killTimer=setTimeout(()=>{instance.kill();resolve();},1500);})]);
      clearTimeout(killTimer);
    }
    async function changeHotkey(value){
      const next=normalizeHotkey(value);
      saveHotkey(next);
      clearTimer(keyboardRestartTimer);keyboardRestartTimer=null;
      const previous=keyboard;keyboard=null;keyboardReady=false;keyboardError=null;
      currentHotkey=next;configurationError=null;refreshReady();
      if(previous)await closeProcess(previous,false);
      keyboardCrashes=0;startKeyboard();wake();
      return next;
    }

    async function waitForImages(clientId,sessionId,signal){
      if(state.poll(clientId,sessionId).length||signal.aborted||disposed)return;
      await new Promise(resolve=>{
        let done=false;
        const finish=()=>{if(done)return;done=true;clearTimeout(timer);waiters.delete(finish);signal.removeEventListener('abort',finish);resolve();};
        const timer=setTimeout(finish,20000);waiters.add(finish);signal.addEventListener('abort',finish,{once:true});
        if(signal.aborted||disposed||state.poll(clientId,sessionId).length)finish();
      });
    }
    const unregister=ctx.connection.fetch.register({path:PATH,methods:['GET','POST'],requestBody:'buffered',async fetch(request){
      try{
        if(disposed)return json({error:'Plugin is stopping'},503);
        const origin=request.headers.get('origin');if(origin&&origin!==new URL(request.url).origin)return json({error:'Origin rejected'},403);
        if(request.method==='GET')return json(status());
        const text=await request.text();if(text.length>4096)return json({error:'Request is too large'},413);
        const body=JSON.parse(text);if(!validId(body.clientId))return json({error:'Invalid client identity'},400);
        if(body.action==='bind'){
          if(!validId(body.sessionId)||typeof body.focused!=='boolean')return json({error:'Invalid Session binding'},400);
          const active=state.bind(body);refreshArm();return json({active,...status()});
        }
        if(body.action==='detach'){state.detach(body.clientId);refreshArm();return json({ok:true});}
        if(body.action==='set-hotkey'){const hotkey=await changeHotkey(body.hotkey);return json({ok:true,hotkey,...status()});}
        if(body.action==='capture'){begin(body.clientId);return json({ok:true});}
        if(body.action==='ack')return json({ok:state.ack(body.clientId,body.id)});
        if(body.action==='poll'){
          if(!validId(body.sessionId))return json({error:'Invalid Session'},400);
          await waitForImages(body.clientId,body.sessionId,request.signal);
          const images=state.poll(body.clientId,body.sessionId).map(({bytes,...item})=>({...item,data:bytes.toString('base64')}));
          return json({images,...status()});
        }
        return json({error:'Unknown action'},400);
      }catch(error){return json({error:redact(error?.message??error)},409);}
    }});
    if(process.platform==='win32'){startNative();startKeyboard();}else state.lastError='This screenshot helper requires Windows 10 or 11';
    const timer=setInterval(refreshArm,5000);timer.unref?.();
    return async()=>{
      disposed=true;clearInterval(timer);for(const timer of timers)clearTimeout(timer);wake();state.images=[];state.owner=null;
      await Promise.all([closeProcess(child,true),closeProcess(keyboard,false)]);for(const lines of readers)lines.close();readers.clear();await unregister();
    };
  });
}
