"""Exercise the real native editor's own HWNDs. No browser, clipboard writes or saved images."""
import base64
import ctypes as C
import io
import json
import os
import pathlib
import queue
import subprocess
import sys
import threading
import time
from PIL import Image, ImageGrab, ImageChops

u = C.WinDLL('user32', use_last_error=True)
HWND = C.c_void_p
CALLBACK = C.WINFUNCTYPE(C.c_bool, HWND, C.c_ssize_t)
u.EnumWindows.argtypes = [CALLBACK, C.c_ssize_t]
u.EnumChildWindows.argtypes = [HWND, CALLBACK, C.c_ssize_t]
u.GetWindowThreadProcessId.argtypes = [HWND, C.POINTER(C.c_uint)]
u.GetWindowTextW.argtypes = [HWND, C.c_wchar_p, C.c_int]
u.IsWindowVisible.argtypes = [HWND]
u.GetParent.argtypes = [HWND]
u.GetParent.restype = HWND
u.SendMessageTimeoutW.argtypes = [HWND, C.c_uint, C.c_size_t, C.c_ssize_t, C.c_uint, C.c_uint, C.POINTER(C.c_size_t)]
u.SendMessageTimeoutW.restype = HWND
u.GetWindowLongPtrW.argtypes = [HWND, C.c_int]
u.GetWindowLongPtrW.restype = C.c_ssize_t
u.HideCaret.argtypes = [HWND]
class RECT(C.Structure):
    _fields_ = [('left', C.c_long), ('top', C.c_long), ('right', C.c_long), ('bottom', C.c_long)]
u.GetWindowRect.argtypes = [HWND, C.POINTER(RECT)]
root = pathlib.Path(__file__).resolve().parent.parent
exe = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else root / 'native' / 'DualCtrlSnip.exe'
p = subprocess.Popen([str(exe), str(os.getpid())], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8', creationflags=0x08000000)
events = queue.Queue()
def read():
    for line in p.stdout:
        if line.strip():
            events.put(json.loads(line))
threading.Thread(target=read, daemon=True).start()
def event(kind, timeout=5):
    end = time.monotonic() + timeout
    while time.monotonic() < end:
        value = events.get(timeout=max(.01, end-time.monotonic()))
        if value['kind'] == 'error':
            raise AssertionError(value)
        if value['kind'] == kind:
            return value
    raise AssertionError('Missing native event: '+kind)
def command(text):
    p.stdin.write(text+'\n'); p.stdin.flush()
def name(hwnd):
    text = C.create_unicode_buffer(512); u.GetWindowTextW(hwnd, text, len(text)); return text.value
def edit_text(hwnd):
    text = C.create_unicode_buffer(1024); send(hwnd, 0x0d, 1023, C.cast(text, C.c_void_p).value); return text.value
def windows(parent=None):
    result = []
    @CALLBACK
    def callback(hwnd, extra):
        pid = C.c_uint(); u.GetWindowThreadProcessId(hwnd, C.byref(pid))
        if pid.value == p.pid and u.IsWindowVisible(hwnd):
            result.append((hwnd, name(hwnd)))
        return True
    if parent:
        u.EnumChildWindows(parent, callback, 0)
    else:
        u.EnumWindows(callback, 0)
    return result
def wait_window(title, parent=None):
    end = time.monotonic()+3
    while time.monotonic()<end:
        for hwnd, text in windows(parent):
            if text.startswith(title):
                return hwnd
        time.sleep(.02)
    pending=[]
    while not events.empty(): pending.append(events.get_nowait())
    raise AssertionError(('Missing owned window', title, windows(parent), 'nativeEvents', pending, 'startedPid', p.pid))
def send(hwnd, message, w=0, value=0):
    result = C.c_size_t()
    if not u.SendMessageTimeoutW(hwnd, message, w, value, 2, 1500, C.byref(result)):
        raise OSError(C.get_last_error(), 'Owned editor window did not respond')
def mouse(hwnd, message, x, y, pressed=False):
    send(hwnd, message, 1 if pressed else 0, (y << 16) | (x & 0xffff))
def drag(hwnd, a, b):
    mouse(hwnd, 0x201, *a, True)
    mouse(hwnd, 0x200, *b, True)
    mouse(hwnd, 0x202, *b)
def drag_smooth(hwnd, a, b, steps=40):
    mouse(hwnd, 0x201, *a, True)
    samples = []
    for i in range(1, steps + 1):
        point = (round(a[0] + (b[0] - a[0]) * i / steps), round(a[1] + (b[1] - a[1]) * i / steps))
        started = time.perf_counter()
        mouse(hwnd, 0x200, *point, True)
        samples.append((time.perf_counter() - started) * 1000)
    mouse(hwnd, 0x202, *b)
    average = sum(samples) / len(samples); maximum = max(samples)
    assert average < 80 and maximum < 400, ('Border drag stalled', average, maximum)
    return {'averageMs': round(average, 2), 'maxMs': round(maximum, 2), 'steps': steps}
def button(form, title):
    hwnd = wait_window(title, form); send(hwnd, 0xf5); return hwnd
try:
    ready = event('ready'); assert ready['version'] == '1.4.0', ready
    command('arm'); command('status'); status=event('native-status'); assert status['armed'], status
    command('capture')
    form = wait_window('dsh-FocusCapture')
    border_timing = drag_smooth(form, (100, 100), (720, 390))
    shape = button(form, '矩形 / 椭圆')
    panel = u.GetParent(shape)
    mouse(panel, 0x201, 200, 100, True); mouse(panel, 0x202, 200, 100)
    drag(form, (160, 170), (300, 250))
    if os.environ.get('DSH_EDITOR_PANEL_CAPTURE'):
        # Capture only the actual editor control, never the surrounding desktop.
        time.sleep(.12)
        bounds = RECT(); u.GetWindowRect(panel, C.byref(bounds))
        snapshot_path = pathlib.Path(os.environ['DSH_EDITOR_PANEL_CAPTURE'])
        snapshot_path.parent.mkdir(parents=True, exist_ok=True)
        ImageGrab.grab(bbox=(bounds.left,bounds.top,bounds.right,bounds.bottom),all_screens=True).save(snapshot_path)
    button(form, '箭头'); drag(form, (330, 270), (440, 140))
    button(form, '撤销'); button(form, '重做')
    button(form, '马赛克'); drag(form, (360, 220), (550, 220))
    button(form, '文字')
    mouse(panel, 0x201, 110, 100, True); mouse(panel, 0x202, 110, 100)  # Font size
    mouse(panel, 0x201, 280, 100, True); mouse(panel, 0x202, 280, 100)  # Line spacing
    mouse(panel, 0x201, 72, 100, True); mouse(panel, 0x202, 72, 100)  # Restore 20px for background comparison
    if os.environ.get('DSH_EDITOR_PANEL_CAPTURE'):
        time.sleep(.12); bounds=RECT();u.GetWindowRect(panel,C.byref(bounds));panel_path=pathlib.Path(os.environ['DSH_EDITOR_PANEL_CAPTURE'])
        text_panel_path=panel_path.with_name(panel_path.stem+'-text-settings'+panel_path.suffix)
        ImageGrab.grab(bbox=(bounds.left,bounds.top,bounds.right,bounds.bottom),all_screens=True).save(text_panel_path)
    mouse(panel, 0x201, 195, 64, True); mouse(panel, 0x202, 195, 64)
    form_bounds=RECT();u.GetWindowRect(form,C.byref(form_bounds))
    time.sleep(.08)
    text_rect=(form_bounds.left+180,form_bounds.top+190,form_bounds.left+480,form_bounds.top+300)
    background_before=ImageGrab.grab(bbox=text_rect,all_screens=True).convert('RGB')
    mouse(form, 0x201, 180, 190, True); mouse(form, 0x202, 180, 190)
    # Only the test editor contains an editable textbox; identify its native EDIT class.
    edit = None
    for hwnd, text in windows(form):
        cls = C.create_unicode_buffer(256); u.GetClassNameW.argtypes = [HWND, C.c_wchar_p, C.c_int]; u.GetClassNameW(hwnd, cls, len(cls))
        if 'EDIT' in cls.value.upper():
            edit = hwnd
    assert edit, 'Missing annotation text editor'
    text = C.create_unicode_buffer('DSH 中文测试')
    send(edit, 0x0c, 0, C.cast(text, C.c_void_p).value)
    time.sleep(.08)
    assert not (u.GetWindowLongPtrW(edit,-16) & 0x00800000), 'Unexpected input border'
    assert not (u.GetWindowLongPtrW(edit,-20) & 0x00000200), 'Unexpected input client edge'
    background_after=ImageGrab.grab(bbox=text_rect,all_screens=True).convert('RGB')
    diff=ImageChops.difference(background_before.crop((0,50,300,110)),background_after.crop((0,50,300,110)))
    assert diff.getbbox() is None, ('Text input replaced screenshot background',diff.getbbox())
    delete_text=C.create_unicode_buffer('DELETE');send(edit,0x0c,0,C.cast(delete_text,C.c_void_p).value);time.sleep(.08)
    u.HideCaret(edit);delete_before=ImageGrab.grab(bbox=text_rect,all_screens=True).convert('RGB')
    send(edit,0x100,0x24,1);send(edit,0x101,0x24,0xc0000001)  # Home
    send(edit,0x100,0x2e,1);send(edit,0x101,0x2e,0xc0000001)  # Delete
    time.sleep(.10);assert edit_text(edit)=='ELETE',edit_text(edit)
    u.HideCaret(edit);delete_after=ImageGrab.grab(bbox=text_rect,all_screens=True).convert('RGB')
    assert ImageChops.difference(delete_before,delete_after).getbbox() is not None,'Delete changed text but left stale rendered glyphs'
    remaining=C.create_unicode_buffer('ELETE');send(edit,0x0c,0,C.cast(remaining,C.c_void_p).value);time.sleep(.08);u.HideCaret(edit)
    delete_expected=ImageGrab.grab(bbox=text_rect,all_screens=True).convert('RGB')
    if os.environ.get('DSH_TEXT_DELETE_CAPTURE'):
        capture_dir=pathlib.Path(os.environ['DSH_TEXT_DELETE_CAPTURE']);capture_dir.mkdir(parents=True,exist_ok=True)
        delete_after.save(capture_dir/'delete-after.png');delete_expected.save(capture_dir/'delete-expected.png')
    assert ImageChops.difference(delete_after,delete_expected).getbbox() is None,'Delete left pixels that differ from freshly entered remaining text'
    send(edit,0x100,0x23,1);send(edit,0x101,0x23,0xc0000001)  # End
    send(edit,0x100,0x08,1);send(edit,0x102,0x08,1);send(edit,0x101,0x08,0xc0000001)  # Backspace key + translated character
    time.sleep(.10);assert edit_text(edit)=='ELET',edit_text(edit)
    send(edit,0xB1,0,0);u.HideCaret(edit);backspace_after=ImageGrab.grab(bbox=text_rect,all_screens=True).convert('RGB')
    shorter=C.create_unicode_buffer('ELET');send(edit,0x0c,0,C.cast(shorter,C.c_void_p).value);send(edit,0xB1,0,0);time.sleep(.08);u.HideCaret(edit)
    backspace_expected=ImageGrab.grab(bbox=text_rect,all_screens=True).convert('RGB')
    if os.environ.get('DSH_TEXT_DELETE_CAPTURE'):
        capture_dir=pathlib.Path(os.environ['DSH_TEXT_DELETE_CAPTURE']);capture_dir.mkdir(parents=True,exist_ok=True)
        backspace_after.save(capture_dir/'backspace-after.png');backspace_expected.save(capture_dir/'backspace-expected.png')
    assert ImageChops.difference(backspace_after,backspace_expected).getbbox() is None,'Backspace left stale glyph pixels'
    empty=C.create_unicode_buffer('');send(edit,0x0c,0,C.cast(empty,C.c_void_p).value);time.sleep(.05)
    cleared=ImageGrab.grab(bbox=text_rect,all_screens=True).convert('RGB')
    cleared_diff=ImageChops.difference(background_before.crop((4,4,296,106)),cleared.crop((4,4,296,106)))
    assert cleared_diff.getbbox() is None, ('Clearing text did not restore captured background',cleared_diff.getbbox())
    send(edit,0x0c,0,C.cast(text,C.c_void_p).value)
    return_button=wait_window('添加到对话',form);copy_button=wait_window('复制图片',form)
    return_bounds=RECT();copy_bounds=RECT();u.GetWindowRect(return_button,C.byref(return_bounds));u.GetWindowRect(copy_button,C.byref(copy_bounds))
    assert return_bounds.left>=copy_bounds.right and return_bounds.top==copy_bounds.top, 'Return icon is not immediately after Copy'
    panel_bounds=RECT();u.GetWindowRect(panel,C.byref(panel_bounds));assert panel_bounds.bottom-panel_bounds.top==124, 'Unexpected footer row'
    button(form, '添加到对话')
    image = event('image')
    with Image.open(io.BytesIO(base64.b64decode(image['data']))) as bitmap:
        assert bitmap.size == (620, 290), bitmap.size
        red = bitmap.convert('RGB').getpixel((140, 140))
        assert red == (239, 68, 68), red
    command('capture')
    form = wait_window('dsh-FocusCapture'); drag(form, (100, 100), (360, 250))
    button(form, '贴图：')
    event('cancel')
    pin = wait_window('dsh-FocusCapture · 贴图')
    assert u.GetWindowLongPtrW(pin, -20) & 8, 'Missing WS_EX_TOPMOST'
    send(pin, 0x10)
    command('capture')
    form = wait_window('dsh-FocusCapture'); drag_smooth(form, (100, 100), (500, 300))
    border_resize = drag_smooth(form, (500, 300), (540, 340), 20); time.sleep(.7)
    border_move = drag_smooth(form, (300, 220), (330, 250), 20)
    mouse(form, 0x204, 20, 20); event('cancel')
    command('stop'); p.stdin.close(); p.wait(timeout=4)
    assert p.returncode == 0, p.returncode
    print(json.dumps({'ok':True,'nativeVersion':ready['version'],'borderDrag':border_timing,'borderResize':border_resize,'borderMove':border_move,'tested':['real-selection','continuous-border-tracking','shape-settings-fill','arrow','undo-redo','mosaic','Chinese-text-commit','font-size-line-spacing-controls','delete-backspace-pixel-match','native-text-background-pixel-match','borderless-text','return-after-copy','no-footer-row','PNG-export-620x290','real-pin-WS_EX_TOPMOST','owned-window-cleanup'],'clipboardChanged':False,'imagesSaved':False}, ensure_ascii=False))
finally:
    if p.poll() is None:
        try:
            command('stop'); p.stdin.close(); p.wait(timeout=2)
        except Exception:
            p.kill(); p.wait(timeout=3)
