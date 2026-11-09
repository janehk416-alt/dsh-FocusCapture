import ctypes
import json
import pathlib
import subprocess
import time

exe = pathlib.Path(__file__).resolve().parent.parent / 'native' / 'DualCtrlSnip.exe'
user32 = ctypes.WinDLL('user32', use_last_error=True)
user32.keybd_event.argtypes = [ctypes.c_ubyte, ctypes.c_ubyte, ctypes.c_uint, ctypes.c_size_t]
process = subprocess.Popen([str(exe), '--probe-hotkey'], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8-sig', creationflags=0x08000000)
try:
    ready = json.loads(process.stdout.readline())
    assert ready['kind'] == 'ready', ready
    # Drive OS events from a separate process, as a physical keyboard would.
    time.sleep(0.10)
    user32.keybd_event(0xA2, 0x1D, 0, 0)
    user32.keybd_event(0xA3, 0x1D, 1, 0)
    time.sleep(0.18)
    user32.keybd_event(0xA2, 0x1D, 2, 0)
    user32.keybd_event(0xA3, 0x1D, 3, 0)
    output, error = process.communicate(timeout=6)
    print(json.dumps(ready), flush=True)
    print(output, flush=True)
    events = [json.loads(line) for line in output.splitlines() if line.strip()]
    result = next(event for event in events if event['kind'] == 'hotkey-probe')
    assert result['ok'] and result['triggers'] == 1, (result, error)
finally:
    user32.keybd_event(0xA2, 0x1D, 2, 0)
    user32.keybd_event(0xA3, 0x1D, 3, 0)
    if process.poll() is None:
        process.kill()
        process.wait(timeout=3)
