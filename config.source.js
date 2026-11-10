function createHotkeyConfig(t, pageId) {
  const keys = [...'ABCDEFGHIJKLMNOPQRSTUVWXYZ', ...'0123456789', ...Array.from({ length: 12 }, (_, i) => `F${i + 1}`)];
  function HotkeyForm() {
    const [mode, setMode] = React.useState('default');
    const [ctrl, setCtrl] = React.useState(true);
    const [alt, setAlt] = React.useState(true);
    const [shift, setShift] = React.useState(false);
    const [key, setKey] = React.useState('S');
    const [busy, setBusy] = React.useState(true);
    const [message, setMessage] = React.useState('');
    const [failed, setFailed] = React.useState(false);
    const applyStatus = React.useCallback(status => {
      const id = status.hotkeyId || 'left-ctrl+right-ctrl';
      if (id === 'left-ctrl+right-ctrl') setMode('default');
      else {
        const parts = id.split('+'), last = parts.at(-1).toUpperCase();
        setMode('custom'); setCtrl(parts.includes('ctrl')); setAlt(parts.includes('alt')); setShift(parts.includes('shift')); setKey(last);
      }
      setBusy(false);
    }, []);
    React.useEffect(() => {
      const lifetime = new AbortController();
      fetch('/api/dual-ctrl-snip', { credentials: 'same-origin', signal: lifetime.signal })
        .then(response => response.json()).then(status => { if (!lifetime.signal.aborted) applyStatus(status); })
        .catch(error => { if (!lifetime.signal.aborted) { setFailed(true); setMessage(error.message); setBusy(false); } });
      return () => lifetime.abort();
    }, [applyStatus]);
    async function save(id) {
      setBusy(true); setFailed(false); setMessage('');
      try {
        const response = await fetch('/api/dual-ctrl-snip', {
          method: 'POST', credentials: 'same-origin', headers: { 'content-type': 'application/json' },
          body: JSON.stringify({ action: 'set-hotkey', clientId: `${pageId}-config`, hotkey: id }),
        });
        const result = await response.json();
        if (!response.ok) throw new Error(result.error || `HTTP ${response.status}`);
        applyStatus(result); setMessage(t('shortcutSaved'));
      } catch (error) { setFailed(true); setMessage(error.message); setBusy(false); }
    }
    const custom = mode === 'custom';
    const selected = custom ? [...ctrl ? ['ctrl'] : [], ...alt ? ['alt'] : [], ...shift ? ['shift'] : [], key.toLowerCase()].join('+') : 'left-ctrl+right-ctrl';
    const missingModifier = custom && !ctrl && !alt && !shift;
    return h('section', { className: 'focuscapture-shortcut' },
      h('style', null, '.focuscapture-shortcut{display:grid;gap:12px;padding:16px;border:1px solid var(--dsw-alias-border-primary);border-radius:12px;max-width:620px}.focuscapture-shortcut h3{margin:0;font-size:15px}.focuscapture-shortcut p{margin:0;color:var(--dsw-alias-label-secondary);font-size:13px;line-height:1.6}.focuscapture-shortcut label{display:inline-flex;align-items:center;gap:6px;margin-right:16px}.focuscapture-shortcut select{min-height:32px;padding:4px 8px}.focuscapture-shortcut .shortcut-row{display:flex;flex-wrap:wrap;gap:10px;align-items:center}.focuscapture-shortcut button{min-height:32px;padding:5px 14px;border:1px solid var(--dsw-alias-border-primary);border-radius:7px;cursor:pointer}.focuscapture-shortcut button:disabled{opacity:.5;cursor:default}.focuscapture-shortcut .shortcut-error{color:var(--dsw-alias-error);white-space:pre-wrap}'),
      h('h3', null, t('shortcutTitle')),
      h('p', null, t('shortcutDescription')),
      h('label', null, t('shortcutMode'), h('select', { value: mode, disabled: busy, onChange: event => setMode(event.target.value) },
        h('option', { value: 'default' }, t('defaultShortcut')), h('option', { value: 'custom' }, t('customShortcut')))),
      custom ? h('fieldset', null,
        h('legend', null, t('shortcutModifiers')),
        h('label', null, h('input', { type: 'checkbox', checked: ctrl, disabled: busy, onChange: event => setCtrl(event.target.checked) }), 'Ctrl'),
        h('label', null, h('input', { type: 'checkbox', checked: alt, disabled: busy, onChange: event => setAlt(event.target.checked) }), 'Alt'),
        h('label', null, h('input', { type: 'checkbox', checked: shift, disabled: busy, onChange: event => setShift(event.target.checked) }), 'Shift')) : null,
      custom ? h('label', null, t('shortcutKey'), h('select', { value: key, disabled: busy, onChange: event => setKey(event.target.value) }, keys.map(item => h('option', { value: item, key: item }, item)))) : null,
      missingModifier ? h('p', { className: 'shortcut-error', role: 'alert' }, t('shortcutNeedsModifier')) : null,
      h('div', { className: 'shortcut-row' },
        h('button', { type: 'button', disabled: busy || missingModifier, onClick: () => save(selected) }, busy ? t('shortcutWorking') : t('saveShortcut')),
        h('button', { type: 'button', disabled: busy, onClick: () => save('left-ctrl+right-ctrl') }, t('resetShortcut'))),
      h('p', null, t('shortcutPersistence')),
      message ? h('p', { className: failed ? 'shortcut-error' : '', role: failed ? 'alert' : 'status' }, message) : null);
  }
  return function HotkeyConfig({ view }) { return view === 'summary' ? t('shortcutSummary') : h(HotkeyForm); };
}
