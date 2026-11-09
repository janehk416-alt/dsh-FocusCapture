window.__ModuleLoader__.load({
  id: '@local/dsh-focuscapture',
  factory(require) {
    const React = require('react');
    const h = React.createElement;
    /* INSERT_DELIVERY */
    return {
      inject: ['slots','locale'],
      apply(ctx) {
        const pageId = crypto.randomUUID();
        const acknowledged = new Set();
        const conversation = ctx.get('conversation');
        const dicts = {
          zh: { button:'区域截图', title:'全局左 Ctrl + 右 Ctrl；框选后可添加七色箭头、矩形、圆形、文字和涂鸦。回车确认，Esc 取消；仅加入附件。', added:'截图已加入附件', starting:'正在启动截图组件', ready:'双 Ctrl 全局截图已就绪', waiting:'等待框选确认', failed:'截图失败', retained:'截图已保留，请回到截图前的会话', inactive:'点击此处激活截图目标会话' },
          en: { button:'Region capture', title:'Global left Ctrl + right Ctrl. Select a region and annotate with shapes, text or freehand in seven colors. Enter attaches; Esc cancels. Never sends.', added:'Screenshot attached', starting:'Starting screenshot helper', ready:'Global Dual Ctrl capture is ready', waiting:'Waiting for region selection', failed:'Screenshot failed', retained:'Screenshot retained; return to the original chat', inactive:'Click to activate this chat as the capture target' },
        };
        const offLocales = Object.entries(dicts).map(([locale,dict]) => ctx.locale.register('dual-ctrl-snip',locale,dict));
        const t = ctx.locale.bind('dual-ctrl-snip');
        function CaptureControl(props) {
          const { sessionId, inputActions } = props;
          const [notice,setNotice] = React.useState('');
          const [ready,setReady] = React.useState(false);
          const [busy,setBusy] = React.useState(false);
          const mounted = React.useRef(false);
          const clientId = pageId + '-' + sessionId;
          const phase = props.useInput(s => s.phase);
          const blocked = !['plain','claimed'].includes(phase);
          const blockedRef = React.useRef(blocked); blockedRef.current = blocked;
          const request = React.useCallback(async (action,extra = {},signal) => {
            const response = await fetch('/api/dual-ctrl-snip',{ method:'POST', credentials:'same-origin', signal, headers:{'content-type':'application/json'}, body:JSON.stringify({ action, clientId, sessionId, ...extra }) });
            const data = await response.json();
            if (!response.ok) throw new Error(data.error || `HTTP ${response.status}`);
            return data;
          },[clientId,sessionId]);
          React.useEffect(() => {
            mounted.current = true;
            const lifetime = new AbortController();
            const locale = () => ctx.locale.getLocale().active?.startsWith('en') ? 'en' : 'zh';
            const bind = async (focused = document.hasFocus()) => {
              try {
                const status = await request('bind',{focused,locale:locale()},lifetime.signal);
                if (!lifetime.signal.aborted) { setReady(status.ready); setNotice(status.lastError || (!status.ready ? t('starting') : status.active ? t('ready') : t('inactive'))); }
              } catch(error) { if (!lifetime.signal.aborted) setNotice(error.message); }
            };
            const activate = () => { if (document.visibilityState === 'visible') void bind(true); };
            const visibility = () => { if (document.visibilityState === 'visible') void bind(document.hasFocus()); };
            const detach = () => { void fetch('/api/dual-ctrl-snip',{ method:'POST',credentials:'same-origin',keepalive:true,headers:{'content-type':'application/json'},body:JSON.stringify({action:'detach',clientId}) }).catch(() => {}); };
            window.addEventListener('focus',activate);
            window.addEventListener('pointerdown',activate);
            window.addEventListener('pagehide',detach);
            document.addEventListener('visibilitychange',visibility);
            const heartbeat = setInterval(() => void bind(document.hasFocus()),15000);
            const pause = ms => new Promise(resolve => { const finish = () => { clearTimeout(timer); lifetime.signal.removeEventListener('abort',finish); resolve(); }; const timer=setTimeout(finish,ms); lifetime.signal.addEventListener('abort',finish,{once:true}); if(lifetime.signal.aborted) finish(); });
            async function receive() {
              await bind();
              while(!lifetime.signal.aborted) {
                try {
                  const result = await request('poll',{},lifetime.signal);
                  if(lifetime.signal.aborted) break;
                  setReady(result.ready); setBusy(result.capturing);
                  if(result.lastError) setNotice(result.lastError);
                  for(const image of result.images) {
                    if(lifetime.signal.aborted) break;
                    if(!acknowledged.has(image.id)) {
                      if(blockedRef.current) { setNotice(t('retained')); continue; }
                      attachScreenshot(conversation,inputActions,sessionId,image);
                      acknowledged.add(image.id);
                      if(acknowledged.size > 64) acknowledged.delete(acknowledged.values().next().value);
                      setNotice(`${t('added')} · ${image.width} × ${image.height}`);
                    }
                    await request('ack',{id:image.id},lifetime.signal);
                  }
                  if(result.images.length) await pause(1200);
                } catch(error) {
                  if(lifetime.signal.aborted) break;
                  setNotice(error.message || t('failed')); await pause(3000);
                }
              }
            }
            void receive();
            return () => {
              mounted.current=false; lifetime.abort(); clearInterval(heartbeat);
              window.removeEventListener('focus',activate); window.removeEventListener('pointerdown',activate); window.removeEventListener('pagehide',detach); document.removeEventListener('visibilitychange',visibility); detach();
            };
          },[request,inputActions,sessionId]);
          async function click() {
            try {
              await request('bind',{focused:true,locale:ctx.locale.getLocale().active?.startsWith('en')?'en':'zh'});
              await request('capture'); if(mounted.current) { setBusy(true);setNotice(t('waiting')); }
            } catch(error) { if(mounted.current) { setBusy(false);setNotice(error.message); } }
          }
          return h('span',{className:'dual-ctrl-snip'},
            h('style',null,'.dual-ctrl-snip{display:inline-flex;align-items:center;color:var(--dsw-alias-label-secondary)}.dual-ctrl-snip button{display:inline-flex;align-items:center;justify-content:center;width:30px;height:30px;padding:6px;border:0;border-radius:8px;background:transparent;color:inherit;cursor:pointer}.dual-ctrl-snip button:hover{background:var(--dsw-alias-interactive-bg-hover)}.dual-ctrl-snip button:focus-visible{outline:2px solid var(--dsw-alias-brand-primary);outline-offset:2px}.dual-ctrl-snip button:disabled{opacity:.5;cursor:default}'),
            h('button',{type:'button','aria-label':t('button')+(notice?'：'+notice:''),'aria-busy':busy,disabled:!ready||busy||blocked,onMouseDown:e=>e.preventDefault(),onClick:click},
              h('svg',{width:17,height:17,viewBox:'0 0 24 24',fill:'none',stroke:'currentColor',strokeWidth:1.8,'aria-hidden':true},h('path',{d:'M8 3H3v5m13-5h5v5M3 16v5h5m13-5v5h-5M8 8h8v8H8z'}))));
        }
        ctx.slots.inject('conversation.input.left',() => ctx.slots.register({name:'conversation.input.left',id:'local-dual-ctrl-snip',order:15,label:() => t('button')},CaptureControl));
        ctx.effect(() => () => { for(const off of offLocales) off(); acknowledged.clear(); });
      },
    };
  },
});
