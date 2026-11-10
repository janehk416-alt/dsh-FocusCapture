"""Local selected-shortcut worker: no hooks, files, network, or unselected keys."""
import ctypes
import json
import sys
import threading
import time

DEFAULT_KEYS = [
    {'vk': [0xA2], 'probe': {'vk': 0xA2, 'scan': 0x1D, 'flags': 0}},
    {'vk': [0xA3], 'probe': {'vk': 0xA3, 'scan': 0x1D, 'flags': 1}},
]
config = json.loads(sys.argv[2]) if len(sys.argv) > 2 and sys.argv[2] != '--probe' else {'keys': DEFAULT_KEYS}
keys = config['keys']
probe = '--probe' in sys.argv[2:]

user32 = ctypes.WinDLL('user32', use_last_error=True)
kernel32 = ctypes.WinDLL('kernel32', use_last_error=True)
user32.GetAsyncKeyState.argtypes = [ctypes.c_int]
user32.GetAsyncKeyState.restype = ctypes.c_short
user32.keybd_event.argtypes = [ctypes.c_ubyte, ctypes.c_ubyte, ctypes.c_uint, ctypes.c_size_t]
kernel32.OpenProcess.argtypes = [ctypes.c_uint, ctypes.c_bool, ctypes.c_uint]
kernel32.OpenProcess.restype = ctypes.c_void_p
kernel32.WaitForSingleObject.argtypes = [ctypes.c_void_p, ctypes.c_uint]
kernel32.WaitForSingleObject.restype = ctypes.c_uint
kernel32.CloseHandle.argtypes = [ctypes.c_void_p]

parent = kernel32.OpenProcess(0x00100000, False, int(sys.argv[1]))
if not parent:
    raise OSError(ctypes.get_last_error(), 'Cannot retain screenshot parent')
stop = threading.Event()
threading.Thread(target=lambda: (sys.stdin.buffer.read(), stop.set()), daemon=True).start()


def emit_probe_key(key, release):
    spec = key['probe']
    user32.keybd_event(spec['vk'], spec['scan'], spec['flags'] | (2 if release else 0), 0)


def selected_key_down(key):
    return any(bool(user32.GetAsyncKeyState(vk) & 0x8000) for vk in key['vk'])


print('READY', flush=True)
start = time.monotonic()
latched = False
pressed = released = False
samples = paired = 0
reported = False
try:
    while not stop.wait(0.015):
        if kernel32.WaitForSingleObject(parent, 0) != 258:
            break
        elapsed = time.monotonic() - start
        if probe and not pressed and elapsed > 0.25:
            for key in keys:
                emit_probe_key(key, False)
            pressed = True
        if probe and pressed and not released and elapsed > 0.55:
            for key in reversed(keys):
                emit_probe_key(key, True)
            released = True
        samples += 1
        if probe and elapsed > 1 and not reported:
            print('PROBE samples=' + str(samples) + ' paired=' + str(paired), flush=True)
            reported = True
        states = [selected_key_down(key) for key in keys]
        if all(states):
            paired += 1
        if not any(states):
            latched = False
        if all(states) and not latched:
            latched = True
            print('TRIGGER', flush=True)
finally:
    if probe:
        for key in reversed(keys):
            emit_probe_key(key, True)
    kernel32.CloseHandle(parent)
