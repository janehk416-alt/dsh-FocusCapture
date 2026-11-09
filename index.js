import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { readFileSync } from 'node:fs';
import { createInterface } from 'node:readline';
import { CaptureState } from './core.js';

export const inject = ['connection'];
const PATH='/api/dual-ctrl-snip';
const validId=value=>typeof value==='string'&&/^[a-zA-Z0-9_-]{1,160}$/.test(value);
const json=(data,status=200)=>new Response(JSON.stringify(data),{status,headers:{'content-type':'application/json; charset=utf-8','cache-control':'no-store'}});
export function apply(ctx){
  ctx.effect(()=>{
    const state=new CaptureState(),waiters=new Set(),readers=new Set(),restartTimers=new Set();
    let child,keyboard,disposed=false,armState='',nativeReady=false,keyboardReady=false,nativePid=null,nativeVersion=null,globalTriggers=0;
    let nativeCrashes=0,keyboardCrashes=0;
    const wake=()=>{for(const fn of [...waiters])fn();};
    const status=()=>({...state.status(),keyboardReady,nativeReady,nativePid,nativeVersion,globalTriggers,keyboardBackend:'host-ctrl-worker'});
    function send(command){if(child&&!child.killed&&child.stdin.writable)child.stdin.write(command+'\n',()=>{});}
    function refreshArm(){
      const snapshot=state.status(),next=snapshot.armed?`arm:${state.owner.locale}`:'disarm';
      if(next===armState)return;armState=next;
      if(snapshot.armed){send(state.owner.locale==='en'?'english':'chinese');send('arm');}else send('disarm');
    }
    function refreshReady(){state.ready=nativeReady&&keyboardReady;refreshArm();wake();}
    function fail(error){state.capture=null;state.lastError=String(error?.message??error).slice(0,500);wake();}
    function begin(clientId){const target=state.begin(clientId);send(target.locale==='en'?'english':'chinese');send('capture');wake();}
    function later(fn){const timer=setTimeout(()=>{restartTimers.delete(timer);fn();},2000);restartTimers.add(timer);}
    function startNative(){
      if(disposed||process.platform!=='win32')return;
      const instance=spawn(fileURLToPath(new URL('./native/DualCtrlSnip.exe',import.meta.url)),[String(process.pid)],{windowsHide:true,stdio:['pipe','pipe','pipe']});
      child=instance;let errorText='';
      instance.stdin.on('error',error=>{if(!disposed)fail(error);});
      instance.stderr.on('data',chunk=>{errorText=(errorText+chunk).slice(-1500);});
      const lines=createInterface({input:instance.stdout,crlfDelay:Infinity});readers.add(lines);
      lines.on('line',line=>{
        if(disposed||child!==instance)return;
        if(line.length>30*1024*1024){fail('Screenshot response exceeds memory limit');return;}
        try{
          const event=JSON.parse(line);
          if(event.kind==='ready'){nativeReady=true;nativePid=event.pid;nativeVersion=event.version??'1.0.0';armState='';refreshReady();}
          else if(event.kind==='image'){state.finish(event.data);wake();}
          else if(event.kind==='cancel'){state.capture=null;wake();}
          else if(event.kind==='error')fail(event.message);
        }catch(error){fail(error);}
      });
      const ended=error=>{if(child!==instance)return;child=null;nativeReady=false;nativePid=null;armState='';refreshReady();fail(error);lines.close();readers.delete(lines);if(!disposed&&++nativeCrashes<=2)later(startNative);};
      instance.once('error',ended);instance.once('exit',code=>ended(new Error(`Native helper exited (${code}): ${errorText}`)));
    }
    function startKeyboard(){
      if(disposed||process.platform!=='win32')return;
      try{
        const config=JSON.parse(readFileSync(new URL('./native/keyboard-runtime.json',import.meta.url),'utf8'));
        // Launch directly from the Node Host, NOT from the C# editor: regression-tested input access.
        const instance=spawn(config.pythonExecutable,['-u',fileURLToPath(new URL('./native/keyboard-worker.py',import.meta.url)),String(process.pid)],{windowsHide:true,stdio:['pipe','pipe','pipe']});
        keyboard=instance;let errorText='';
        instance.stdin.on('error',error=>{if(!disposed)fail(error);});
        instance.stderr.on('data',chunk=>{errorText=(errorText+chunk).slice(-1000);});
        const lines=createInterface({input:instance.stdout,crlfDelay:Infinity});readers.add(lines);
        lines.on('line',line=>{
          if(disposed||keyboard!==instance)return;
          if(line==='READY'){keyboardReady=true;refreshReady();}
          else if(line==='TRIGGER'){
            globalTriggers++;state.expire();
            if(state.owner&&state.ready&&!state.capture){try{begin();}catch(error){fail(error);}}
          }
        });
        const ended=error=>{if(keyboard!==instance)return;keyboard=null;keyboardReady=false;refreshReady();fail(error);lines.close();readers.delete(lines);if(!disposed&&++keyboardCrashes<=2)later(startKeyboard);};
        instance.once('error',ended);instance.once('exit',code=>ended(new Error(`Ctrl worker exited (${code}): ${errorText}`)));
      }catch(error){fail(error);}
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
        if(body.action==='capture'){begin(body.clientId);return json({ok:true});}
        if(body.action==='ack')return json({ok:state.ack(body.clientId,body.id)});
        if(body.action==='poll'){
          if(!validId(body.sessionId))return json({error:'Invalid Session'},400);
          await waitForImages(body.clientId,body.sessionId,request.signal);
          const images=state.poll(body.clientId,body.sessionId).map(({bytes,...item})=>({...item,data:bytes.toString('base64')}));
          return json({images,...status()});
        }
        return json({error:'Unknown action'},400);
      }catch(error){return json({error:String(error?.message??error)},409);}
    }});
    if(process.platform==='win32'){startNative();startKeyboard();}else state.lastError='This screenshot helper requires Windows 10 or 11';
    const timer=setInterval(refreshArm,5000);timer.unref?.();
    return async()=>{
      disposed=true;clearInterval(timer);for(const timer of restartTimers)clearTimeout(timer);wake();state.images=[];state.owner=null;
      async function close(instance,isNative){
        if(!instance)return;const exited=new Promise(resolve=>instance.once('exit',resolve));if(isNative)send('stop');instance.stdin.end();let killTimer;
        await Promise.race([exited,new Promise(resolve=>{killTimer=setTimeout(()=>{instance.kill();resolve();},1500);})]);clearTimeout(killTimer);
      }
      await Promise.all([close(child,true),close(keyboard,false)]);for(const lines of readers)lines.close();readers.clear();await unregister();
    };
  });
}
